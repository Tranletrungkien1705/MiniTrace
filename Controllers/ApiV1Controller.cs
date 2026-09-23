using Microsoft.AspNetCore.Mvc;
using MiniTrace.Data;
using MiniTrace.Models;
using MiniTrace.Services;

namespace MiniTrace.Controllers;

/// <summary>
/// API JSON cho SPA React. DTO phẳng. Dashboard cache Redis 30s theo tenant (X-Cache).
/// Sự kiện truy xuất FORWARD-ONLY (8 giai đoạn GS1). Tra cứu công khai xuyên tenant theo mã đơn vị.
/// </summary>
[ApiController]
[Route("api/v1")]
[Produces("application/json")]
public class ApiV1Controller(ITraceService svc, ICache cache, ITenantContext tenant) : ControllerBase
{
    [HttpGet("dashboard")]
    public async Task<IActionResult> Dashboard()
    {
        var key = $"trace:dash:{tenant.OrgId}";
        var hit = await cache.GetAsync<DashDto>(key);
        if (hit != null) { Response.Headers["X-Cache"] = "HIT"; return Ok(hit); }
        var d = await svc.DashboardAsync();
        var dto = new DashDto(d.Products, d.Units, d.Events, d.Completed,
            d.ByStage.Select(s => new ByStageDto((int)s.Stage, Ui.Stage(s.Stage).text, s.Count)).ToList());
        await cache.SetAsync(key, dto, TimeSpan.FromSeconds(30));
        Response.Headers["X-Cache"] = "MISS";
        return Ok(dto);
    }

    [HttpGet("products")]
    public async Task<IActionResult> Products()
        => Ok((await svc.ProductsAsync()).Select(p => new { p.Id, p.Code, p.Name, p.Origin, p.Manufacturer }));

    /// <summary>Đồng bộ danh mục sản phẩm từ MiniPIM (nguồn master data) — upsert theo mã.</summary>
    [HttpPost("products/import-pim")]
    public async Task<IActionResult> ImportFromPim()
    {
        try
        {
            var (added, updated, total) = await svc.ImportFromPimAsync();
            return Ok(new { ok = true, added, updated, total, msg = $"Đồng bộ từ PIM: +{added} mới, {updated} cập nhật ({total} SP)." });
        }
        catch (Exception ex) { return BadRequest(new { ok = false, error = "Không kết nối được MiniPIM: " + ex.Message }); }
    }

    [HttpPost("products")]
    public async Task<IActionResult> CreateProduct([FromBody] ProductReq r)
    {
        if (string.IsNullOrWhiteSpace(r.Name)) return BadRequest(new { error = "Cần tên sản phẩm." });
        var id = await svc.CreateProductAsync(new Product { Name = r.Name.Trim(), Code = r.Code ?? "", Origin = r.Origin, Manufacturer = r.Manufacturer });
        return Ok(new { id });
    }

    [HttpGet("units")]
    public async Task<IActionResult> Units([FromQuery] string? q)
        => Ok((await svc.UnitsAsync(q)).Select(u => new
        {
            u.Id, u.Code, product = u.Product?.Name, u.LotNo, u.CreatedAt,
            events = u.Events.Count, lastStage = u.LastStage == null ? null : Ui.Stage(u.LastStage.Value).text
        }));

    [HttpGet("units/{id:int}")]
    public async Task<IActionResult> Unit(int id)
    {
        var u = await svc.GetUnitAsync(id);
        return u == null ? NotFound(new { error = "Không tìm thấy." }) : Ok(ToUnitDto(u));
    }

    [HttpPost("units")]
    public async Task<IActionResult> CreateUnit([FromBody] UnitReq r)
    {
        if (r.ProductId <= 0) return BadRequest(new { error = "Cần chọn sản phẩm." });
        var id = await svc.CreateUnitAsync(r.ProductId, r.LotNo ?? "");
        return Ok(new { id });
    }

    [HttpPost("units/{id:int}/events")]
    public async Task<IActionResult> AddEvent(int id, [FromBody] EventReq r)
    {
        var (ok, msg) = await svc.AddEventAsync(id, (EventType)r.Type, r.Location ?? "", r.Actor ?? "", r.Note);
        return ok ? Ok(new { ok, msg }) : BadRequest(new { ok, error = msg });
    }

    // ===== Chống hàng giả: xác thực khi NTD quét mã =====
    [HttpPost("verify")]
    public async Task<IActionResult> Verify([FromBody] VerifyReq r)
    {
        if (string.IsNullOrWhiteSpace(r.Code)) return BadRequest(new { error = "Cần mã truy xuất." });
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
        var res = await svc.VerifyAsync(r.Code, ip, r.Location, r.Latitude, r.Longitude, r.Phone);
        if (res == null) return NotFound(new { found = false, error = "Mã truy xuất không tồn tại — sản phẩm có thể không rõ nguồn gốc." });
        return Ok(new
        {
            res.Code, res.Product, res.Origin, res.Manufacturer, res.LotNo,
            res.VerifyCount, status = (int)res.Status, res.StatusText, res.Message, res.ScannedAt
        });
    }

    [HttpGet("verifications")]
    public async Task<IActionResult> Verifications([FromQuery] string? q)
        => Ok((await svc.VerificationsAsync(q)).Select(v => new
        {
            v.Id, v.Code, product = v.Unit?.Product?.Name, v.VerifyCount,
            status = (int)v.Status, statusText = Ui.Verify(v.Status).text, css = Ui.Verify(v.Status).css,
            v.IpAddress, v.Location, v.Phone, v.ScannedAt
        }));

    // ===== Danh mục sự kiện truy xuất trọng yếu (GS1 CTE — Mst_CTE của InBrandCloud eTEM) =====
    [HttpGet("ctes")]
    public async Task<IActionResult> Ctes([FromQuery] string? q)
        => Ok((await svc.CtesAsync(q)).Select(c => new { c.Id, c.Code, c.Description, c.NetworkType, c.ApiLink, c.Active, c.CreatedAt }));

    [HttpPost("ctes")]
    public async Task<IActionResult> SaveCte([FromBody] CteReq r)
    {
        var (ok, msg) = await svc.SaveCteAsync(r.Id, r.Code ?? "", r.Description ?? "", r.NetworkType, r.ApiLink, r.Active);
        return ok ? Ok(new { ok, msg }) : BadRequest(new { ok, error = msg });
    }

    [HttpDelete("ctes/{id:int}")]
    public async Task<IActionResult> DeleteCte(int id)
    {
        var (ok, msg) = await svc.DeleteCteAsync(id);
        return ok ? Ok(new { ok, msg }) : BadRequest(new { ok, error = msg });
    }

    // ===== Danh mục thành phần dữ liệu trọng yếu (GS1 KDE — Mst_KDE của InBrandCloud eTEM) =====
    [HttpGet("kdes")]
    public async Task<IActionResult> Kdes([FromQuery] string? q)
        => Ok((await svc.KdesAsync(q)).Select(k => new { k.Id, k.Code, k.Description, k.DataType, k.RefNoList, k.NetworkType, k.FlagList, k.FlagQuery, k.Active, k.CreatedAt }));

    [HttpPost("kdes")]
    public async Task<IActionResult> SaveKde([FromBody] KdeReq r)
    {
        var (ok, msg) = await svc.SaveKdeAsync(r.Id, r.Code ?? "", r.Description ?? "", r.DataType, r.RefNoList, r.NetworkType, r.FlagList, r.FlagQuery, r.Active);
        return ok ? Ok(new { ok, msg }) : BadRequest(new { ok, error = msg });
    }

    [HttpDelete("kdes/{id:int}")]
    public async Task<IActionResult> DeleteKde(int id)
    {
        var (ok, msg) = await svc.DeleteKdeAsync(id);
        return ok ? Ok(new { ok, msg }) : BadRequest(new { ok, error = msg });
    }

    // ===== Danh mục kiểu dữ liệu (GS1 Data Type — Mst_DataType của InBrandCloud eTEM) =====
    [HttpGet("data-types")]
    public async Task<IActionResult> DataTypes([FromQuery] string? q)
        => Ok((await svc.DataTypesAsync(q)).Select(d => new { d.Id, d.Code, d.Description, d.NetworkType, d.Active, d.CreatedAt }));

    [HttpPost("data-types")]
    public async Task<IActionResult> SaveDataType([FromBody] DataTypeReq r)
    {
        var (ok, msg) = await svc.SaveDataTypeAsync(r.Id, r.Code ?? "", r.Description ?? "", r.NetworkType, r.Active);
        return ok ? Ok(new { ok, msg }) : BadRequest(new { ok, error = msg });
    }

    [HttpDelete("data-types/{id:int}")]
    public async Task<IActionResult> DeleteDataType(int id)
    {
        var (ok, msg) = await svc.DeleteDataTypeAsync(id);
        return ok ? Ok(new { ok, msg }) : BadRequest(new { ok, error = msg });
    }

    // ===== Ánh xạ sự kiện ↔ thành phần dữ liệu (GS1 CTE_KDE) =====
    [HttpGet("cte-kdes")]
    public async Task<IActionResult> CteKdes([FromQuery] string? cteCode)
        => Ok((await svc.CteKdesAsync(cteCode)).Select(m => new { m.Id, m.CteCode, m.KdeCode, m.NetworkType, m.FlagKey, m.FlagOsOrgView }));

    [HttpPost("cte-kdes")]
    public async Task<IActionResult> SaveCteKdes([FromBody] CteKdeReq r)
    {
        var items = (r.Items ?? []).Select(i => new CteKdeInput(i.KdeCode ?? "", i.FlagKey, i.FlagOsOrgView)).ToList();
        var (ok, msg) = await svc.SaveCteKdesAsync(r.CteCode ?? "", items);
        return ok ? Ok(new { ok, msg }) : BadRequest(new { ok, error = msg });
    }

    // ===== Danh mục địa điểm toàn cầu (GS1 GLN — Mst_GLN của InBrandCloud eTEM) =====
    [HttpGet("glns")]
    public async Task<IActionResult> Glns([FromQuery] string? q)
        => Ok((await svc.GlnsAsync(q)).Select(g => new { g.Id, g.Code, g.Name, g.GpsLat, g.GpsLong, g.Remark, g.Active, g.CreatedAt }));

    [HttpPost("glns")]
    public async Task<IActionResult> SaveGln([FromBody] GlnReq r)
    {
        var (ok, msg) = await svc.SaveGlnAsync(r.Id, r.Code ?? "", r.Name ?? "", r.GpsLat, r.GpsLong, r.Remark, r.Active);
        return ok ? Ok(new { ok, msg }) : BadRequest(new { ok, error = msg });
    }

    [HttpDelete("glns/{id:int}")]
    public async Task<IActionResult> DeleteGln(int id)
    {
        var (ok, msg) = await svc.DeleteGlnAsync(id);
        return ok ? Ok(new { ok, msg }) : BadRequest(new { ok, error = msg });
    }

    // ===== Danh mục nông trại / vùng trồng (GS1 Farm — Mst_Farm của InBrandCloud eTEM) =====
    [HttpGet("farms")]
    public async Task<IActionResult> Farms([FromQuery] string? q)
        => Ok((await svc.FarmsAsync(q)).Select(f => new { f.Id, f.Code, f.Name, f.NetworkType, f.Active, f.CreatedAt }));

    [HttpPost("farms")]
    public async Task<IActionResult> SaveFarm([FromBody] FarmReq r)
    {
        var (ok, msg) = await svc.SaveFarmAsync(r.Id, r.Code ?? "", r.Name ?? "", r.NetworkType, r.Active);
        return ok ? Ok(new { ok, msg }) : BadRequest(new { ok, error = msg });
    }

    [HttpDelete("farms/{id:int}")]
    public async Task<IActionResult> DeleteFarm(int id)
    {
        var (ok, msg) = await svc.DeleteFarmAsync(id);
        return ok ? Ok(new { ok, msg }) : BadRequest(new { ok, error = msg });
    }

    // ===== Ánh xạ tổ chức ↔ địa điểm (Mst_OrgIDMapGLN của InBrandCloud eTEM) =====
    [HttpGet("org-glns")]
    public async Task<IActionResult> OrgGlns([FromQuery] string? q)
        => Ok((await svc.OrgGlnsAsync(q)).Select(m => new { m.Id, m.OrgCode, m.GlnCode, m.GlnName, m.GpsLat, m.GpsLong, m.Remark, m.CreatedAt }));

    [HttpPost("org-glns")]
    public async Task<IActionResult> SaveOrgGln([FromBody] OrgGlnReq r)
    {
        var (ok, msg) = await svc.SaveOrgGlnAsync(r.Id, r.OrgCode ?? "", r.GlnCode ?? "", r.Remark);
        return ok ? Ok(new { ok, msg }) : BadRequest(new { ok, error = msg });
    }

    [HttpDelete("org-glns/{id:int}")]
    public async Task<IActionResult> DeleteOrgGln(int id)
    {
        var (ok, msg) = await svc.DeleteOrgGlnAsync(id);
        return ok ? Ok(new { ok, msg }) : BadRequest(new { ok, error = msg });
    }

    // ===== Mẫu loại tổ chức (GS1 Network Type Template — Mst_TemplateNWType của InBrandCloud eTEM) =====
    [HttpGet("templates")]
    public async Task<IActionResult> Templates([FromQuery] string? q)
        => Ok((await svc.TemplatesAsync(q)).Select(t => new
        {
            t.Id, t.TplNWType, t.Description, status = (int)t.Status, statusText = Ui.TplStatus(t.Status).text, css = Ui.TplStatus(t.Status).css,
            t.Remark, t.CreatedAt, ctes = t.Ctes.Count, kdes = t.Kdes.Count, maps = t.CteKdes.Count
        }));

    [HttpGet("templates/{id:int}")]
    public async Task<IActionResult> Template(int id)
    {
        var t = await svc.GetTemplateAsync(id);
        if (t == null) return NotFound(new { error = "Không tìm thấy mẫu loại tổ chức." });
        return Ok(new
        {
            t.Id, t.TplNWType, t.Description, status = (int)t.Status, statusText = Ui.TplStatus(t.Status).text, t.Remark, t.CreatedAt,
            ctes = t.Ctes.OrderBy(c => c.CteCode).Select(c => new { c.CteCode, c.CteDesc, c.ApiLink, c.Active }),
            kdes = t.Kdes.OrderBy(k => k.KdeCode).Select(k => new { k.KdeCode, k.KdeDesc, k.DataType, k.RefNoList, k.FlagList, k.FlagQuery, k.Active }),
            cteKdes = t.CteKdes.OrderBy(m => m.CteCode).ThenBy(m => m.KdeCode).Select(m => new { m.CteCode, m.KdeCode, m.ApiLink, m.FlagKey, m.FlagOsOrgView })
        });
    }

    [HttpPost("templates")]
    public async Task<IActionResult> SaveTemplate([FromBody] TemplateReq r)
    {
        var ctes = (r.Ctes ?? []).Select(c => new TplNwtCteInput(c.CteCode ?? "", c.CteDesc, c.ApiLink, c.Active)).ToList();
        var kdes = (r.Kdes ?? []).Select(k => new TplNwtKdeInput(k.KdeCode ?? "", k.KdeDesc, k.DataType, k.RefNoList, k.FlagList, k.FlagQuery, k.Active)).ToList();
        var maps = (r.CteKdes ?? []).Select(m => new TplNwtCteKdeInput(m.CteCode ?? "", m.KdeCode ?? "", m.ApiLink, m.FlagKey, m.FlagOsOrgView)).ToList();
        var (ok, msg) = await svc.SaveTemplateAsync(r.Id, r.TplNWType ?? "", r.Description ?? "", r.Remark, ctes, kdes, maps);
        return ok ? Ok(new { ok, msg }) : BadRequest(new { ok, error = msg });
    }

    [HttpPost("templates/{id:int}/approve")]
    public async Task<IActionResult> ApproveTemplate(int id)
    {
        var (ok, msg) = await svc.ApproveTemplateAsync(id);
        return ok ? Ok(new { ok, msg }) : BadRequest(new { ok, error = msg });
    }

    [HttpDelete("templates/{id:int}")]
    public async Task<IActionResult> DeleteTemplate(int id)
    {
        var (ok, msg) = await svc.DeleteTemplateAsync(id);
        return ok ? Ok(new { ok, msg }) : BadRequest(new { ok, error = msg });
    }

    // ===== Mẫu hiển thị sự kiện truy xuất (GS1 Template View Event — Mst_TplViewEvent của InBrandCloud eTEM) =====
    [HttpGet("tpl-view-events")]
    public async Task<IActionResult> TplViewEvents([FromQuery] string? q)
        => Ok((await svc.TplViewEventsAsync(q)).Select(v => new { v.Id, v.Code, v.Description, v.Detail, v.CteCode, v.Remark, v.Active, v.FlagBG, v.CreatedAt }));

    [HttpPost("tpl-view-events")]
    public async Task<IActionResult> SaveTplViewEvent([FromBody] TplViewEventReq r)
    {
        var (ok, msg) = await svc.SaveTplViewEventAsync(r.Id, r.Code ?? "", r.Description ?? "", r.Detail ?? "", r.CteCode, r.Remark, r.Active, r.FlagBG);
        return ok ? Ok(new { ok, msg }) : BadRequest(new { ok, error = msg });
    }

    [HttpDelete("tpl-view-events/{id:int}")]
    public async Task<IActionResult> DeleteTplViewEvent(int id)
    {
        var (ok, msg) = await svc.DeleteTplViewEventAsync(id);
        return ok ? Ok(new { ok, msg }) : BadRequest(new { ok, error = msg });
    }

    // ===== Sự kiện truy xuất theo CTE + KDE (Event_Event + Event_EventSpec của InBrandCloud eTEM) =====
    [HttpGet("records")]
    public async Task<IActionResult> Records([FromQuery] string? q)
        => Ok((await svc.RecordsAsync(q)).Select(r => new
        {
            r.Id, r.EventNo, r.CteCode, r.TplVECode, r.GlnOrgCode, r.GlnOrgName, r.GpsLat, r.GpsLong,
            r.Remark, r.Active, r.CreatedAt, r.UpdatedAt, specs = r.Specs.Count
        }));

    [HttpGet("records/{id:int}")]
    public async Task<IActionResult> Record(int id)
    {
        var r = await svc.GetRecordAsync(id);
        if (r == null) return NotFound(new { error = "Không tìm thấy bản ghi sự kiện." });
        return Ok(new
        {
            r.Id, r.EventNo, r.CteCode, r.TplVECode, r.TplVEDetail, r.GlnOrgCode, r.GlnOrgName, r.GpsLat, r.GpsLong,
            r.Remark, r.Active, r.CreatedAt, r.UpdatedAt,
            specs = r.Specs.OrderBy(s => s.KdeCode).Select(s => new { s.KdeCode, s.KdeValue, s.FlagKey, s.FlagList, s.FlagOsOrgView })
        });
    }

    [HttpPost("records")]
    public async Task<IActionResult> SaveRecord([FromBody] RecordReq r)
    {
        var specs = (r.Specs ?? []).Select(s => new RecordSpecInput(s.KdeCode ?? "", s.KdeValue)).ToList();
        var (ok, msg) = await svc.SaveRecordAsync(r.Id, r.CteCode ?? "", r.GlnOrgCode, r.Remark, specs);
        return ok ? Ok(new { ok, msg }) : BadRequest(new { ok, error = msg });
    }

    [HttpDelete("records/{id:int}")]
    public async Task<IActionResult> DeleteRecord(int id)
    {
        var (ok, msg) = await svc.DeleteRecordAsync(id);
        return ok ? Ok(new { ok, msg }) : BadRequest(new { ok, error = msg });
    }

    // ===== Sinh tem / kho số tem (Inv_GenTimes + Inv_InventoryGenID của InBrandCloud eTEM) =====
    [HttpGet("stamp-batches")]
    public async Task<IActionResult> StampBatches([FromQuery] string? q)
        => Ok((await svc.StampBatchesAsync(q)).Select(b => new
        {
            b.Id, b.GenTimesNo, b.ProductCode, b.ProductName,
            qrType = (int)b.QrType, qrTypeText = Ui.Qr(b.QrType).text, css = Ui.Qr(b.QrType).css,
            b.ConfigDomain, b.Qty, b.FlagPIN, b.FlagMap, b.ProductionLotNo, b.ProductionDate,
            b.ShiftInCode, b.UserKCS, b.Remark, b.CreatedAt, stamps = b.Stamps.Count
        }));

    [HttpGet("stamp-batches/{id:int}")]
    public async Task<IActionResult> StampBatch(int id)
    {
        var b = await svc.GetStampBatchAsync(id);
        if (b == null) return NotFound(new { error = "Không tìm thấy lần sinh tem." });
        return Ok(new
        {
            b.Id, b.GenTimesNo, b.ProductCode, b.ProductName,
            qrType = (int)b.QrType, qrTypeText = Ui.Qr(b.QrType).text,
            b.ConfigDomain, b.Qty, b.FlagPIN, b.FlagMap, b.ProductionLotNo, b.ProductionDate,
            b.ShiftInCode, b.UserKCS, b.Remark, b.CreatedAt,
            stamps = b.Stamps.OrderBy(s => s.IDNo).Select(s => new { s.IDNo, s.QR_ID, s.PIN, s.SecretNo, s.HashInformation, s.FlagMap, s.FlagUsed })
        });
    }

    [HttpPost("stamp-batches")]
    public async Task<IActionResult> GenerateStamps([FromBody] StampBatchReq r)
    {
        var (ok, msg) = await svc.GenerateStampsAsync(r.GenTimesNo ?? "", r.ProductCode ?? "", r.ProductName,
            (QrType)r.QrType, r.Qty, r.FlagPIN, r.ProductionLotNo, r.ProductionDate, r.ShiftInCode, r.UserKCS, r.Remark);
        return ok ? Ok(new { ok, msg }) : BadRequest(new { ok, error = msg });
    }

    [HttpDelete("stamp-batches/{id:int}")]
    public async Task<IActionResult> DeleteStampBatch(int id)
    {
        var (ok, msg) = await svc.DeleteStampBatchAsync(id);
        return ok ? Ok(new { ok, msg }) : BadRequest(new { ok, error = msg });
    }

    [HttpGet("stamps")]
    public async Task<IActionResult> Stamps([FromQuery] int? batchId, [FromQuery] string? q)
        => Ok((await svc.StampsAsync(batchId, q)).Select(s => new { s.Id, s.BatchId, s.IDNo, s.QR_ID, s.PIN, s.SecretNo, s.HashInformation, s.FlagMap, s.FlagUsed, s.CreatedAt }));

    // ===== Đóng hộp / gán tem vào hộp (Inv_InventoryGenBox + Map_IDInBox của InBrandCloud eTEM) =====
    [HttpGet("boxes")]
    public async Task<IActionResult> Boxes([FromQuery] string? q)
        => Ok((await svc.BoxesAsync(q)).Select(b => new
        {
            b.Id, b.BoxNo, b.QR_BoxNo, b.GenTimesNo, b.ProductCode, b.ProductName, b.Remark,
            b.FlagMap, b.FlagUsed, b.CreatedAt, items = b.Items.Count
        }));

    [HttpGet("boxes/{id:int}")]
    public async Task<IActionResult> Box(int id)
    {
        var b = await svc.GetBoxAsync(id);
        if (b == null) return NotFound(new { error = "Không tìm thấy hộp." });
        return Ok(new
        {
            b.Id, b.BoxNo, b.QR_BoxNo, b.GenTimesNo, b.ProductCode, b.ProductName, b.Remark,
            b.FlagMap, b.FlagUsed, b.CreatedAt,
            items = b.Items.OrderBy(i => i.IDNo).Select(i => new { i.Id, i.IDNo, i.ProductCode, i.InvCode, i.FlagActive, i.CreatedAt })
        });
    }

    [HttpPost("boxes")]
    public async Task<IActionResult> CreateBox([FromBody] BoxReq r)
    {
        var (ok, msg) = await svc.CreateBoxAsync(r.BoxNo ?? "", r.ProductCode, r.ProductName, r.Remark);
        return ok ? Ok(new { ok, msg }) : BadRequest(new { ok, error = msg });
    }

    [HttpPost("boxes/{id:int}/stamps")]
    public async Task<IActionResult> AddStampsToBox(int id, [FromBody] BoxStampsReq r)
    {
        var (ok, msg) = await svc.AddStampsToBoxAsync(id, r.IdNos ?? [], r.InvCode);
        return ok ? Ok(new { ok, msg }) : BadRequest(new { ok, error = msg });
    }

    [HttpDelete("boxes/{id:int}")]
    public async Task<IActionResult> DeleteBox(int id)
    {
        var (ok, msg) = await svc.DeleteBoxAsync(id);
        return ok ? Ok(new { ok, msg }) : BadRequest(new { ok, error = msg });
    }

    // ===== Đóng thùng / gán hộp vào thùng (Inv_InventoryGenCarton + Map_BoxInCarton của InBrandCloud eTEM) =====
    [HttpGet("cartons")]
    public async Task<IActionResult> Cartons([FromQuery] string? q)
        => Ok((await svc.CartonsAsync(q)).Select(c => new
        {
            c.Id, c.CanNo, c.QR_CanNo, c.GenTimesNo, c.ProductCode, c.ProductName, c.Remark,
            c.FlagMap, c.FlagUsed, c.CreatedAt, items = c.Items.Count
        }));

    [HttpGet("cartons/{id:int}")]
    public async Task<IActionResult> Carton(int id)
    {
        var c = await svc.GetCartonAsync(id);
        if (c == null) return NotFound(new { error = "Không tìm thấy thùng." });
        return Ok(new
        {
            c.Id, c.CanNo, c.QR_CanNo, c.GenTimesNo, c.ProductCode, c.ProductName, c.Remark,
            c.FlagMap, c.FlagUsed, c.CreatedAt,
            items = c.Items.OrderBy(i => i.BoxNo).Select(i => new { i.Id, i.BoxNo, i.ProductCode, i.InvCode, i.FlagActive, i.CreatedAt })
        });
    }

    [HttpPost("cartons")]
    public async Task<IActionResult> CreateCarton([FromBody] CartonReq r)
    {
        var (ok, msg) = await svc.CreateCartonAsync(r.CanNo ?? "", r.ProductCode, r.ProductName, r.Remark);
        return ok ? Ok(new { ok, msg }) : BadRequest(new { ok, error = msg });
    }

    [HttpPost("cartons/{id:int}/boxes")]
    public async Task<IActionResult> AddBoxesToCarton(int id, [FromBody] CartonBoxesReq r)
    {
        var (ok, msg) = await svc.AddBoxesToCartonAsync(id, r.BoxNos ?? [], r.InvCode);
        return ok ? Ok(new { ok, msg }) : BadRequest(new { ok, error = msg });
    }

    [HttpDelete("cartons/{id:int}")]
    public async Task<IActionResult> DeleteCarton(int id)
    {
        var (ok, msg) = await svc.DeleteCartonAsync(id);
        return ok ? Ok(new { ok, msg }) : BadRequest(new { ok, error = msg });
    }

    // ===== Hàng đợi đồng bộ dữ liệu truy xuất (MstSv_QueSync của InBrandCloud eTEM) =====
    [HttpGet("que-syncs")]
    public async Task<IActionResult> QueSyncs([FromQuery] string? q)
        => Ok((await svc.QueSyncsAsync(q)).Select(x => new
        {
            x.Id, x.NetworkId, x.QueSyncNo, x.TableCode, x.FlagSync, x.FlagSyncBL,
            status = (int)x.Status, statusText = Ui.QueSync(x.Status).text, css = Ui.QueSync(x.Status).css,
            x.RetryCount, x.ErrorDetail, x.SyncedAt, x.Remark, x.CreatedAt
        }));

    [HttpPost("que-syncs")]
    public async Task<IActionResult> SaveQueSync([FromBody] QueSyncReq r)
    {
        var (ok, msg) = await svc.SaveQueSyncAsync(r.Id, r.NetworkId ?? "", r.QueSyncNo ?? "", r.TableCode ?? "", r.FlagSyncBL, r.Remark);
        return ok ? Ok(new { ok, msg }) : BadRequest(new { ok, error = msg });
    }

    [HttpPost("que-syncs/{id:int}/mark")]
    public async Task<IActionResult> MarkQueSync(int id, [FromBody] QueSyncMarkReq r)
    {
        var (ok, msg) = await svc.MarkQueSyncAsync(id, (QueSyncStatus)r.Status, r.ErrorDetail);
        return ok ? Ok(new { ok, msg }) : BadRequest(new { ok, error = msg });
    }

    [HttpDelete("que-syncs/{id:int}")]
    public async Task<IActionResult> DeleteQueSync(int id)
    {
        var (ok, msg) = await svc.DeleteQueSyncAsync(id);
        return ok ? Ok(new { ok, msg }) : BadRequest(new { ok, error = msg });
    }

    // ===== Danh mục dữ liệu gốc (GS1 Master Data — Mst_MasterData của InBrandCloud eTEM) =====
    [HttpGet("master-datas")]
    public async Task<IActionResult> MasterDatas([FromQuery] string? q)
        => Ok((await svc.MasterDatasAsync(q)).Select(m => new { m.Id, m.Code, m.NetworkId, m.TableName, m.Active, m.Remark, m.CreatedAt }));

    [HttpPost("master-datas")]
    public async Task<IActionResult> SaveMasterData([FromBody] MasterDataReq r)
    {
        var (ok, msg) = await svc.SaveMasterDataAsync(r.Id, r.Code ?? "", r.NetworkId, r.TableName ?? "", r.Active, r.Remark);
        return ok ? Ok(new { ok, msg }) : BadRequest(new { ok, error = msg });
    }

    [HttpDelete("master-datas/{id:int}")]
    public async Task<IActionResult> DeleteMasterData(int id)
    {
        var (ok, msg) = await svc.DeleteMasterDataAsync(id);
        return ok ? Ok(new { ok, msg }) : BadRequest(new { ok, error = msg });
    }

    // ===== Tổ chức tham gia mạng lưới truy xuất (GS1 Network Organization — Mst_NNT của InBrandCloud eTEM) =====
    [HttpGet("network-orgs")]
    public async Task<IActionResult> NetworkOrgs([FromQuery] string? q)
        => Ok((await svc.NetworkOrgsAsync(q)).Select(o => new
        {
            o.Id, o.Mst, o.FullName, o.NetworkType, o.OrgCode, o.Address, o.Mobile, o.ContactName, o.ContactEmail, o.Gln,
            o.EltsMstId, status = (int)o.Status, statusText = Ui.NetworkOrg(o.Status).text, css = Ui.NetworkOrg(o.Status).css,
            o.Active, o.Remark, o.CreatedAt
        }));

    [HttpPost("network-orgs")]
    public async Task<IActionResult> SaveNetworkOrg([FromBody] NetworkOrgReq r)
    {
        var (ok, msg) = await svc.SaveNetworkOrgAsync(r.Id, r.Mst ?? "", r.FullName ?? "", r.NetworkType, r.OrgCode,
            r.Address, r.Mobile, r.ContactName, r.ContactEmail, r.Gln, r.Active, r.Remark);
        return ok ? Ok(new { ok, msg }) : BadRequest(new { ok, error = msg });
    }

    [HttpPost("network-orgs/{id:int}/register")]
    public async Task<IActionResult> RegisterNetworkOrg(int id)
    {
        var (ok, msg) = await svc.RegisterNetworkOrgAsync(id);
        return ok ? Ok(new { ok, msg }) : BadRequest(new { ok, error = msg });
    }

    [HttpDelete("network-orgs/{id:int}")]
    public async Task<IActionResult> DeleteNetworkOrg(int id)
    {
        var (ok, msg) = await svc.DeleteNetworkOrgAsync(id);
        return ok ? Ok(new { ok, msg }) : BadRequest(new { ok, error = msg });
    }

    // ===== Kho số bí mật (Inv_InventorySecret của InBrandCloud eTEM) =====
    [HttpGet("secrets")]
    public async Task<IActionResult> Secrets([FromQuery] string? q, [FromQuery] bool? used)
        => Ok((await svc.SecretsAsync(q, used)).Select(s => new
        {
            s.Id, s.SerialNo, s.SecretNo, s.QR_SerialNo, s.NetworkId, s.Mst, s.OrgCode, s.GenTimesNo,
            s.FlagMap, s.FlagUsed, s.Remark, s.CreatedAt
        }));

    [HttpPost("secrets")]
    public async Task<IActionResult> SaveSecret([FromBody] SecretReq r)
    {
        var (ok, msg) = await svc.SaveSecretAsync(r.Id, r.SerialNo ?? "", r.SecretNo ?? "", r.QR_SerialNo, r.NetworkId, r.Mst, r.OrgCode, r.GenTimesNo, r.FlagMap, r.Remark);
        return ok ? Ok(new { ok, msg }) : BadRequest(new { ok, error = msg });
    }

    [HttpPost("secrets/{id:int}/use")]
    public async Task<IActionResult> MarkSecretUsed(int id)
    {
        var (ok, msg) = await svc.MarkSecretUsedAsync(id);
        return ok ? Ok(new { ok, msg }) : BadRequest(new { ok, error = msg });
    }

    [HttpDelete("secrets/{id:int}")]
    public async Task<IActionResult> DeleteSecret(int id)
    {
        var (ok, msg) = await svc.DeleteSecretAsync(id);
        return ok ? Ok(new { ok, msg }) : BadRequest(new { ok, error = msg });
    }

    // Tra cứu công khai xuyên tenant theo mã đơn vị.
    [HttpGet("trace/{code}")]
    public async Task<IActionResult> Trace(string code)
    {
        var u = await svc.PublicLookupAsync(code);
        if (u == null) return NotFound(new { error = "Không tìm thấy mã truy xuất." });
        return Ok(new
        {
            u.Code, product = u.Product?.Name, gtin = u.Product?.Code, origin = u.Product?.Origin, manufacturer = u.Product?.Manufacturer, u.LotNo,
            journey = u.Events.OrderBy(e => e.OccurredAt).Select(e => new { stage = Ui.Stage(e.Type).text, e.Location, e.Actor, e.OccurredAt, e.Note })
        });
    }

    private static object ToUnitDto(TraceUnit u) => new
    {
        u.Id, u.Code, product = u.Product?.Name, u.LotNo, u.CreatedAt,
        lastStage = u.LastStage == null ? (int?)null : (int)u.LastStage.Value,
        events = u.Events.OrderBy(e => e.OccurredAt).Select(e => new { stage = (int)e.Type, stageText = Ui.Stage(e.Type).text, css = Ui.Stage(e.Type).css, e.Location, e.Actor, e.OccurredAt, e.Note })
    };
}

public record DashDto(int Products, int Units, int Events, int Completed, List<ByStageDto> ByStage);
public record ByStageDto(int Stage, string StageText, int Count);

public class ProductReq { public string Name { get; set; } = ""; public string? Code { get; set; } public string? Origin { get; set; } public string? Manufacturer { get; set; } }
public class UnitReq { public int ProductId { get; set; } public string? LotNo { get; set; } }
public class EventReq { public int Type { get; set; } public string? Location { get; set; } public string? Actor { get; set; } public string? Note { get; set; } }
public class VerifyReq { public string Code { get; set; } = ""; public string? Location { get; set; } public double? Latitude { get; set; } public double? Longitude { get; set; } public string? Phone { get; set; } }
public class CteReq { public int Id { get; set; } public string? Code { get; set; } public string? Description { get; set; } public string? NetworkType { get; set; } public string? ApiLink { get; set; } public bool Active { get; set; } = true; }
public class KdeReq { public int Id { get; set; } public string? Code { get; set; } public string? Description { get; set; } public string? DataType { get; set; } public string? RefNoList { get; set; } public string? NetworkType { get; set; } public bool FlagList { get; set; } public bool FlagQuery { get; set; } public bool Active { get; set; } = true; }
public class DataTypeReq { public int Id { get; set; } public string? Code { get; set; } public string? Description { get; set; } public string? NetworkType { get; set; } public bool Active { get; set; } = true; }
public class CteKdeReq { public string? CteCode { get; set; } public List<CteKdeItemReq>? Items { get; set; } }
public class CteKdeItemReq { public string? KdeCode { get; set; } public bool FlagKey { get; set; } public bool FlagOsOrgView { get; set; } }
public class GlnReq { public int Id { get; set; } public string? Code { get; set; } public string? Name { get; set; } public string? GpsLat { get; set; } public string? GpsLong { get; set; } public string? Remark { get; set; } public bool Active { get; set; } = true; }
public class FarmReq { public int Id { get; set; } public string? Code { get; set; } public string? Name { get; set; } public string? NetworkType { get; set; } public bool Active { get; set; } = true; }
public class OrgGlnReq { public int Id { get; set; } public string? OrgCode { get; set; } public string? GlnCode { get; set; } public string? Remark { get; set; } }
public class TemplateReq { public int Id { get; set; } public string? TplNWType { get; set; } public string? Description { get; set; } public string? Remark { get; set; } public List<TplCteItemReq>? Ctes { get; set; } public List<TplKdeItemReq>? Kdes { get; set; } public List<TplCteKdeItemReq>? CteKdes { get; set; } }
public class TplCteItemReq { public string? CteCode { get; set; } public string? CteDesc { get; set; } public string? ApiLink { get; set; } public bool Active { get; set; } = true; }
public class TplKdeItemReq { public string? KdeCode { get; set; } public string? KdeDesc { get; set; } public string? DataType { get; set; } public string? RefNoList { get; set; } public bool FlagList { get; set; } public bool FlagQuery { get; set; } public bool Active { get; set; } = true; }
public class TplCteKdeItemReq { public string? CteCode { get; set; } public string? KdeCode { get; set; } public string? ApiLink { get; set; } public bool FlagKey { get; set; } public bool FlagOsOrgView { get; set; } }
public class TplViewEventReq { public int Id { get; set; } public string? Code { get; set; } public string? Description { get; set; } public string? Detail { get; set; } public string? CteCode { get; set; } public string? Remark { get; set; } public bool Active { get; set; } = true; public bool FlagBG { get; set; } }
public class RecordReq { public int Id { get; set; } public string? CteCode { get; set; } public string? GlnOrgCode { get; set; } public string? Remark { get; set; } public List<RecordSpecReq>? Specs { get; set; } }
public class RecordSpecReq { public string? KdeCode { get; set; } public string? KdeValue { get; set; } }
public class StampBatchReq { public string? GenTimesNo { get; set; } public string? ProductCode { get; set; } public string? ProductName { get; set; } public int QrType { get; set; } public int Qty { get; set; } public bool FlagPIN { get; set; } public string? ProductionLotNo { get; set; } public string? ProductionDate { get; set; } public string? ShiftInCode { get; set; } public string? UserKCS { get; set; } public string? Remark { get; set; } }
public class BoxReq { public string? BoxNo { get; set; } public string? ProductCode { get; set; } public string? ProductName { get; set; } public string? Remark { get; set; } }
public class BoxStampsReq { public List<string>? IdNos { get; set; } public string? InvCode { get; set; } }
public class CartonReq { public string? CanNo { get; set; } public string? ProductCode { get; set; } public string? ProductName { get; set; } public string? Remark { get; set; } }
public class CartonBoxesReq { public List<string>? BoxNos { get; set; } public string? InvCode { get; set; } }
public class QueSyncReq { public int Id { get; set; } public string? NetworkId { get; set; } public string? QueSyncNo { get; set; } public string? TableCode { get; set; } public bool FlagSyncBL { get; set; } public string? Remark { get; set; } }
public class QueSyncMarkReq { public int Status { get; set; } public string? ErrorDetail { get; set; } }
public class MasterDataReq { public int Id { get; set; } public string? Code { get; set; } public string? NetworkId { get; set; } public string? TableName { get; set; } public bool Active { get; set; } = true; public string? Remark { get; set; } }
public class NetworkOrgReq { public int Id { get; set; } public string? Mst { get; set; } public string? FullName { get; set; } public string? NetworkType { get; set; } public string? OrgCode { get; set; } public string? Address { get; set; } public string? Mobile { get; set; } public string? ContactName { get; set; } public string? ContactEmail { get; set; } public string? Gln { get; set; } public bool Active { get; set; } = true; public string? Remark { get; set; } }
public class SecretReq { public int Id { get; set; } public string? SerialNo { get; set; } public string? SecretNo { get; set; } public string? QR_SerialNo { get; set; } public string? NetworkId { get; set; } public string? Mst { get; set; } public string? OrgCode { get; set; } public string? GenTimesNo { get; set; } public bool FlagMap { get; set; } public string? Remark { get; set; } }

