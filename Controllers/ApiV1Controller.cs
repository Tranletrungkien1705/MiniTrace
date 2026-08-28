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
