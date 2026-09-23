using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using MiniTrace.Data;
using MiniTrace.Models;

namespace MiniTrace.Services;

public record TraceDash(int Products, int Units, int Events, int Completed, List<(EventType Stage, int Count)> ByStage);

public interface ITraceService
{
    Task<List<Product>> ProductsAsync();
    Task<int> CreateProductAsync(Product p);
    Task<List<TraceUnit>> UnitsAsync(string? q);
    Task<TraceUnit?> GetUnitAsync(int id);
    Task<int> CreateUnitAsync(int productId, string lotNo);
    Task<(bool ok, string msg)> AddEventAsync(int unitId, EventType type, string location, string actor, string? note);
    Task<TraceUnit?> PublicLookupAsync(string code);   // xuyên tenant
    Task<TraceDash> DashboardAsync();
    Task<(int added, int updated, int total)> ImportFromPimAsync();   // đồng bộ danh mục từ MiniPIM
    Task<VerifyResult?> VerifyAsync(string code, string? ip, string? location, double? lat, double? lng, string? phone);   // xác thực chống hàng giả
    Task<List<Verification>> VerificationsAsync(string? q);
    // Danh mục sự kiện truy xuất trọng yếu (GS1 CTE — Mst_CTE của InBrandCloud eTEM)
    Task<List<Cte>> CtesAsync(string? q);
    Task<(bool ok, string msg)> SaveCteAsync(int id, string code, string description, string? networkType, string? apiLink, bool active);
    Task<(bool ok, string msg)> DeleteCteAsync(int id);
    // Danh mục thành phần dữ liệu trọng yếu (GS1 KDE — Mst_KDE của InBrandCloud eTEM)
    Task<List<Kde>> KdesAsync(string? q);
    Task<(bool ok, string msg)> SaveKdeAsync(int id, string code, string description, string? dataType, string? refNoList, string? networkType, bool flagList, bool flagQuery, bool active);
    Task<(bool ok, string msg)> DeleteKdeAsync(int id);
    // Ánh xạ sự kiện ↔ thành phần dữ liệu (GS1 CTE_KDE)
    Task<List<CteKde>> CteKdesAsync(string? cteCode);
    Task<(bool ok, string msg)> SaveCteKdesAsync(string cteCode, List<CteKdeInput> items);
    // Danh mục địa điểm toàn cầu (GS1 GLN — Mst_GLN của InBrandCloud eTEM)
    Task<List<Gln>> GlnsAsync(string? q);
    Task<(bool ok, string msg)> SaveGlnAsync(int id, string code, string name, string? gpsLat, string? gpsLong, string? remark, bool active);
    Task<(bool ok, string msg)> DeleteGlnAsync(int id);
    // Danh mục nông trại / vùng trồng (GS1 Farm — Mst_Farm của InBrandCloud eTEM)
    Task<List<Farm>> FarmsAsync(string? q);
    Task<(bool ok, string msg)> SaveFarmAsync(int id, string code, string name, string? networkType, bool active);
    Task<(bool ok, string msg)> DeleteFarmAsync(int id);
    // Mẫu loại tổ chức (GS1 Network Type Template — Mst_TemplateNWType của InBrandCloud eTEM)
    Task<List<TemplateNWType>> TemplatesAsync(string? q);
    Task<TemplateNWType?> GetTemplateAsync(int id);
    Task<(bool ok, string msg)> SaveTemplateAsync(int id, string tplNWType, string description, string? remark, List<TplNwtCteInput> ctes, List<TplNwtKdeInput> kdes, List<TplNwtCteKdeInput> cteKdes);
    Task<(bool ok, string msg)> DeleteTemplateAsync(int id);
    Task<(bool ok, string msg)> ApproveTemplateAsync(int id);
    // Mẫu hiển thị sự kiện truy xuất (GS1 Template View Event — Mst_TplViewEvent của InBrandCloud eTEM)
    Task<List<TplViewEvent>> TplViewEventsAsync(string? q);
    Task<(bool ok, string msg)> SaveTplViewEventAsync(int id, string code, string description, string detail, string? cteCode, string? remark, bool active, bool flagBG);
    Task<(bool ok, string msg)> DeleteTplViewEventAsync(int id);
}

/// <summary>Kết quả 1 lần quét xác thực (trả về cho NTD).</summary>
public record VerifyResult(string Code, string Product, string? Origin, string? Manufacturer, string LotNo,
    int VerifyCount, VerifyStatus Status, string StatusText, string Message, DateTime ScannedAt);

/// <summary>1 dòng ánh xạ CTE↔KDE khi lưu (tương đương 1 dòng bảng CTE_KDE).</summary>
public record CteKdeInput(string KdeCode, bool FlagKey, bool FlagOsOrgView);

/// <summary>1 sự kiện (CTE) trong mẫu loại tổ chức (TplNWT_Mst_CTE).</summary>
public record TplNwtCteInput(string CteCode, string? CteDesc, string? ApiLink, bool Active);

/// <summary>1 thành phần dữ liệu (KDE) trong mẫu loại tổ chức (TplNWT_Mst_KDE).</summary>
public record TplNwtKdeInput(string KdeCode, string? KdeDesc, string? DataType, string? RefNoList, bool FlagList, bool FlagQuery, bool Active);

/// <summary>1 ánh xạ CTE↔KDE trong mẫu loại tổ chức (TplNWT_CTE_KDE).</summary>
public record TplNwtCteKdeInput(string CteCode, string KdeCode, string? ApiLink, bool FlagKey, bool FlagOsOrgView);

public class TraceService(AppDbContext db, IHttpClientFactory httpFactory) : ITraceService
{
    public Task<List<Product>> ProductsAsync() => db.Products.OrderBy(p => p.Code).ToListAsync();

    // Đồng bộ danh mục chuẩn từ MiniPIM (nguồn master data) — upsert theo Code.
    public async Task<(int added, int updated, int total)> ImportFromPimAsync()
    {
        var pimUrl = (Environment.GetEnvironmentVariable("PIM_URL") ?? "https://minipim.onrender.com").TrimEnd('/');
        var http = httpFactory.CreateClient(); http.Timeout = TimeSpan.FromSeconds(20);
        var items = await http.GetFromJsonAsync<List<PimProduct>>($"{pimUrl}/api/products") ?? [];
        int added = 0, updated = 0;
        foreach (var it in items)
        {
            if (string.IsNullOrWhiteSpace(it.code)) continue;
            var p = await db.Products.FirstOrDefaultAsync(x => x.Code == it.code);
            if (p == null) { p = new Product { Code = it.code.Trim() }; db.Products.Add(p); added++; }
            else updated++;
            p.Name = it.name ?? p.Name;
        }
        await db.SaveChangesAsync();
        return (added, updated, added + updated);
    }
    private sealed record PimProduct(string code, string? name, string? group, string? uom, string? barcode, decimal costPrice, decimal salePrice);
    public async Task<int> CreateProductAsync(Product p)
    {
        if (string.IsNullOrWhiteSpace(p.Code)) p.Code = $"893{await db.Products.CountAsync() + 1:D7}";
        db.Products.Add(p); await db.SaveChangesAsync(); return p.Id;
    }

    public async Task<List<TraceUnit>> UnitsAsync(string? q)
    {
        var query = db.Units.Include(u => u.Product).Include(u => u.Events).AsQueryable();
        if (!string.IsNullOrWhiteSpace(q)) query = query.Where(u => u.Code.Contains(q) || u.LotNo.Contains(q) || u.Product.Name.Contains(q));
        var list = await query.ToListAsync();
        return list.OrderByDescending(u => u.CreatedAt).Take(500).ToList();
    }

    public Task<TraceUnit?> GetUnitAsync(int id) =>
        db.Units.Include(u => u.Product).Include(u => u.Events).FirstOrDefaultAsync(u => u.Id == id);

    public async Task<int> CreateUnitAsync(int productId, string lotNo)
    {
        var prod = await db.Products.FirstOrDefaultAsync(p => p.Id == productId) ?? throw new InvalidOperationException("SP không tồn tại.");
        var unit = new TraceUnit { ProductId = productId, LotNo = lotNo, Code = NewCode() };
        // Sự kiện đầu chuỗi: Sản xuất
        unit.Events.Add(new TraceEvent { Type = EventType.Produced, Location = prod.Origin ?? "Nhà máy", Actor = prod.Manufacturer ?? "Nhà sản xuất", Note = "Khởi tạo lô sản xuất" });
        db.Units.Add(unit);
        await db.SaveChangesAsync();
        return unit.Id;
    }

    public async Task<(bool ok, string msg)> AddEventAsync(int unitId, EventType type, string location, string actor, string? note)
    {
        if (!Enum.IsDefined(typeof(EventType), type)) return (false, "Loại sự kiện không hợp lệ.");
        var unit = await db.Units.Include(u => u.Events).FirstOrDefaultAsync(u => u.Id == unitId);
        if (unit == null) return (false, "Không tìm thấy đơn vị truy xuất.");
        var last = unit.LastStage;
        // Sự kiện phải TIẾN theo chuỗi (không lùi) — đảm bảo tính toàn vẹn truy xuất.
        if (last != null && (int)type <= (int)last) return (false, $"Sự kiện phải sau '{Ui.Stage(last.Value).text}'.");
        db.Events.Add(new TraceEvent { UnitId = unitId, Type = type, Location = location, Actor = actor, Note = note });
        await db.SaveChangesAsync();
        return (true, $"Đã ghi sự kiện: {Ui.Stage(type).text}.");
    }

    public Task<TraceUnit?> PublicLookupAsync(string code) =>
        db.Units.IgnoreQueryFilters().Include(u => u.Product).Include(u => u.Events)
          .FirstOrDefaultAsync(u => u.Code == code.Trim());

    public async Task<TraceDash> DashboardAsync()
    {
        var units = await db.Units.Include(u => u.Events).ToListAsync();
        var byStage = new List<(EventType, int)>();
        foreach (EventType s in Enum.GetValues(typeof(EventType)))
            byStage.Add((s, units.Count(u => u.LastStage == s)));
        return new TraceDash(
            await db.Products.CountAsync(), units.Count, await db.Events.CountAsync(),
            units.Count(u => u.LastStage == EventType.Sold), byStage);
    }

    // ===== Xác thực chống hàng giả (tương đương Inv_InventoryVerifiedID của InBrandCloud) =====
    // Quy tắc (doc 09 §9): VerifyCount=1 → chính hãng; >1 → cảnh báo; quét nhiều nơi / sau khi đã bán → nghi hàng giả.
    public async Task<VerifyResult?> VerifyAsync(string code, string? ip, string? location, double? lat, double? lng, string? phone)
    {
        if (string.IsNullOrWhiteSpace(code)) return null;
        var c = code.Trim();
        var unit = await db.Units.IgnoreQueryFilters().Include(u => u.Product).Include(u => u.Events)
            .FirstOrDefaultAsync(u => u.Code == c);
        if (unit == null) return null;

        var prior = await db.Verifications.IgnoreQueryFilters().Where(v => v.Code == c).ToListAsync();
        var count = prior.Count + 1;

        // Phát hiện hàng giả: quét ở vị trí KHÁC với các lần trước (nhiều nơi) hoặc sau khi đã bán cho NTD.
        var sold = unit.LastStage == EventType.Sold;
        var otherPlace = !string.IsNullOrWhiteSpace(location)
            && prior.Any(v => !string.IsNullOrWhiteSpace(v.Location) && !string.Equals(v.Location, location, StringComparison.OrdinalIgnoreCase));
        var status = count == 1 && !sold ? VerifyStatus.Genuine
            : (sold || otherPlace) ? VerifyStatus.Suspect
            : VerifyStatus.Warning;

        var v = new Verification
        {
            OrgId = unit.OrgId, UnitId = unit.Id, Code = c, VerifyCount = count, Status = status,
            IpAddress = ip, Location = location, Latitude = lat, Longitude = lng, Phone = phone
        };
        db.Verifications.Add(v);
        await db.SaveChangesAsync();

        var (text, msg) = status switch
        {
            VerifyStatus.Genuine => ("Chính hãng", "Sản phẩm chính hãng — xác thực thành công."),
            VerifyStatus.Warning => ("Cảnh báo", $"Mã này đã được quét {count} lần — hãy kiểm tra kỹ trước khi mua."),
            _ => ("Nghi hàng giả", sold
                    ? "Mã đã bán cho người tiêu dùng nhưng vẫn bị quét lại — nghi hàng giả."
                    : "Mã bị quét ở nhiều địa điểm khác nhau — nghi hàng giả.")
        };
        return new VerifyResult(unit.Code, unit.Product.Name, unit.Product.Origin, unit.Product.Manufacturer,
            unit.LotNo, count, status, text, msg, v.ScannedAt);
    }

    public async Task<List<Verification>> VerificationsAsync(string? q)
    {
        var query = db.Verifications.Include(v => v.Unit).ThenInclude(u => u.Product).AsQueryable();
        if (!string.IsNullOrWhiteSpace(q)) query = query.Where(v => v.Code.Contains(q));
        var list = await query.ToListAsync();
        return list.OrderByDescending(v => v.ScannedAt).Take(500).ToList();
    }

    // ===== Danh mục sự kiện truy xuất trọng yếu (GS1 CTE — Mst_CTE của InBrandCloud eTEM) =====
    // Định nghĩa "từ điển" các loại sự kiện chuỗi cung ứng dùng để ghi hành trình truy xuất.
    public async Task<List<Cte>> CtesAsync(string? q)
    {
        var query = db.Ctes.AsQueryable();
        if (!string.IsNullOrWhiteSpace(q)) query = query.Where(c => c.Code.Contains(q) || c.Description.Contains(q));
        var list = await query.ToListAsync();
        return list.OrderBy(c => c.Code).ToList();
    }

    public async Task<(bool ok, string msg)> SaveCteAsync(int id, string code, string description, string? networkType, string? apiLink, bool active)
    {
        code = (code ?? "").Trim();
        description = (description ?? "").Trim();
        if (code.Length == 0) return (false, "Cần mã sự kiện (CTECode).");
        if (description.Length == 0) return (false, "Cần diễn giải sự kiện (CTEDesc).");
        // Mã sự kiện phải duy nhất trong tenant.
        if (await db.Ctes.AnyAsync(c => c.Code == code && c.Id != id)) return (false, $"Mã '{code}' đã tồn tại.");

        Cte cte;
        if (id > 0)
        {
            cte = await db.Ctes.FirstOrDefaultAsync(c => c.Id == id) ?? null!;
            if (cte == null) return (false, "Không tìm thấy sự kiện.");
        }
        else { cte = new Cte(); db.Ctes.Add(cte); }

        cte.Code = code; cte.Description = description;
        cte.NetworkType = string.IsNullOrWhiteSpace(networkType) ? null : networkType.Trim();
        cte.ApiLink = string.IsNullOrWhiteSpace(apiLink) ? null : apiLink.Trim();
        cte.Active = active;
        await db.SaveChangesAsync();
        return (true, id > 0 ? "Đã cập nhật sự kiện." : "Đã thêm sự kiện.");
    }

    public async Task<(bool ok, string msg)> DeleteCteAsync(int id)
    {
        var cte = await db.Ctes.FirstOrDefaultAsync(c => c.Id == id);
        if (cte == null) return (false, "Không tìm thấy sự kiện.");
        db.Ctes.Remove(cte);
        await db.SaveChangesAsync();
        return (true, "Đã xóa sự kiện.");
    }

    // ===== Danh mục thành phần dữ liệu trọng yếu (GS1 KDE — Mst_KDE của InBrandCloud eTEM) =====
    // "Từ điển" các trường dữ liệu phải thu thập tại mỗi sự kiện truy xuất.
    public async Task<List<Kde>> KdesAsync(string? q)
    {
        var query = db.Kdes.AsQueryable();
        if (!string.IsNullOrWhiteSpace(q)) query = query.Where(k => k.Code.Contains(q) || k.Description.Contains(q));
        var list = await query.ToListAsync();
        return list.OrderBy(k => k.Code).ToList();
    }

    public async Task<(bool ok, string msg)> SaveKdeAsync(int id, string code, string description, string? dataType, string? refNoList, string? networkType, bool flagList, bool flagQuery, bool active)
    {
        code = (code ?? "").Trim();
        description = (description ?? "").Trim();
        if (code.Length == 0) return (false, "Cần mã thành phần (KDECode).");
        if (description.Length == 0) return (false, "Cần mô tả thành phần (KDEDesc).");
        // Mã thành phần phải duy nhất trong tenant.
        if (await db.Kdes.AnyAsync(k => k.Code == code && k.Id != id)) return (false, $"Mã '{code}' đã tồn tại.");

        Kde kde;
        if (id > 0)
        {
            kde = await db.Kdes.FirstOrDefaultAsync(k => k.Id == id) ?? null!;
            if (kde == null) return (false, "Không tìm thấy thành phần.");
        }
        else { kde = new Kde(); db.Kdes.Add(kde); }

        kde.Code = code; kde.Description = description;
        kde.DataType = string.IsNullOrWhiteSpace(dataType) ? null : dataType.Trim();
        kde.RefNoList = string.IsNullOrWhiteSpace(refNoList) ? null : refNoList.Trim();
        kde.NetworkType = string.IsNullOrWhiteSpace(networkType) ? null : networkType.Trim();
        kde.FlagList = flagList; kde.FlagQuery = flagQuery; kde.Active = active;
        await db.SaveChangesAsync();
        return (true, id > 0 ? "Đã cập nhật thành phần." : "Đã thêm thành phần.");
    }

    public async Task<(bool ok, string msg)> DeleteKdeAsync(int id)
    {
        var kde = await db.Kdes.FirstOrDefaultAsync(k => k.Id == id);
        if (kde == null) return (false, "Không tìm thấy thành phần.");
        // Không cho xóa nếu đang được ánh xạ vào sự kiện (giữ toàn vẹn CTE_KDE).
        if (await db.CteKdes.AnyAsync(m => m.KdeCode == kde.Code))
            return (false, $"Thành phần '{kde.Code}' đang được dùng trong sự kiện — gỡ ánh xạ trước.");
        db.Kdes.Remove(kde);
        await db.SaveChangesAsync();
        return (true, "Đã xóa thành phần.");
    }

    // ===== Ánh xạ sự kiện ↔ thành phần dữ liệu (GS1 CTE_KDE) =====
    public async Task<List<CteKde>> CteKdesAsync(string? cteCode)
    {
        var query = db.CteKdes.AsQueryable();
        if (!string.IsNullOrWhiteSpace(cteCode)) query = query.Where(m => m.CteCode == cteCode.Trim());
        var list = await query.ToListAsync();
        return list.OrderBy(m => m.CteCode).ThenBy(m => m.KdeCode).ToList();
    }

    // Lưu toàn bộ ánh xạ của 1 sự kiện (thay thế). Áp 2 quy tắc nghiệp vụ của InBrandCloud:
    //  (1) Mỗi sự kiện chỉ được có TỐI ĐA 1 KDE loại danh sách (FlagList).
    //  (2) Mỗi sự kiện phải có ÍT NHẤT 1 KDE là Key (FlagKey).
    public async Task<(bool ok, string msg)> SaveCteKdesAsync(string cteCode, List<CteKdeInput> items)
    {
        cteCode = (cteCode ?? "").Trim();
        if (cteCode.Length == 0) return (false, "Cần mã sự kiện (CTECode).");
        var cte = await db.Ctes.FirstOrDefaultAsync(c => c.Code == cteCode);
        if (cte == null) return (false, $"Sự kiện '{cteCode}' không tồn tại.");

        items ??= [];
        // Chuẩn hóa + bỏ trùng mã thành phần.
        var clean = items.Where(i => !string.IsNullOrWhiteSpace(i.KdeCode))
                         .GroupBy(i => i.KdeCode.Trim()).Select(g => g.First()).ToList();
        if (clean.Count == 0) return (false, "Cần chọn ít nhất 1 thành phần dữ liệu.");

        // Kiểm tra các mã thành phần tồn tại.
        var codes = clean.Select(i => i.KdeCode.Trim()).ToList();
        var kdes = await db.Kdes.Where(k => codes.Contains(k.Code)).ToListAsync();
        var missing = codes.Except(kdes.Select(k => k.Code)).ToList();
        if (missing.Count > 0) return (false, $"Thành phần không tồn tại: {string.Join(", ", missing)}.");

        // Quy tắc (1): tối đa 1 KDE loại danh sách.
        var listKdes = kdes.Where(k => k.FlagList).Select(k => k.Code).ToList();
        if (listKdes.Count > 1) return (false, $"Mỗi sự kiện chỉ được có tối đa 1 thành phần loại danh sách (đang có: {string.Join(", ", listKdes)}).");
        // Quy tắc (2): phải có ít nhất 1 KDE là Key.
        if (!clean.Any(i => i.FlagKey)) return (false, "Mỗi sự kiện phải có ít nhất 1 thành phần là Key (FlagKey).");

        // Thay thế toàn bộ ánh xạ cũ của sự kiện.
        var old = await db.CteKdes.Where(m => m.CteCode == cteCode).ToListAsync();
        db.CteKdes.RemoveRange(old);
        foreach (var i in clean)
            db.CteKdes.Add(new CteKde { CteCode = cteCode, KdeCode = i.KdeCode.Trim(), NetworkType = cte.NetworkType, FlagKey = i.FlagKey, FlagOsOrgView = i.FlagOsOrgView });
        await db.SaveChangesAsync();
        return (true, $"Đã lưu {clean.Count} thành phần cho sự kiện '{cteCode}'.");
    }

    // ===== Danh mục địa điểm toàn cầu (GS1 GLN — Mst_GLN của InBrandCloud eTEM) =====
    // "Từ điển" các địa điểm chuỗi cung ứng (nhà máy/kho/đại lý/cửa hàng) kèm toạ độ GPS.
    public async Task<List<Gln>> GlnsAsync(string? q)
    {
        var query = db.Glns.AsQueryable();
        if (!string.IsNullOrWhiteSpace(q)) query = query.Where(g => g.Code.Contains(q) || g.Name.Contains(q));
        var list = await query.ToListAsync();
        return list.OrderBy(g => g.Code).ToList();
    }

    public async Task<(bool ok, string msg)> SaveGlnAsync(int id, string code, string name, string? gpsLat, string? gpsLong, string? remark, bool active)
    {
        code = (code ?? "").Trim();
        name = (name ?? "").Trim();
        if (code.Length == 0) return (false, "Cần mã địa điểm (GLNCode).");
        if (name.Length == 0) return (false, "Cần tên địa điểm (GLNName).");
        // Mã địa điểm phải duy nhất trong tenant.
        if (await db.Glns.AnyAsync(g => g.Code == code && g.Id != id)) return (false, $"Mã '{code}' đã tồn tại.");

        Gln gln;
        if (id > 0)
        {
            gln = await db.Glns.FirstOrDefaultAsync(g => g.Id == id) ?? null!;
            if (gln == null) return (false, "Không tìm thấy địa điểm.");
        }
        else { gln = new Gln(); db.Glns.Add(gln); }

        gln.Code = code; gln.Name = name;
        gln.GpsLat = string.IsNullOrWhiteSpace(gpsLat) ? null : gpsLat.Trim();
        gln.GpsLong = string.IsNullOrWhiteSpace(gpsLong) ? null : gpsLong.Trim();
        gln.Remark = string.IsNullOrWhiteSpace(remark) ? null : remark.Trim();
        gln.Active = active;
        await db.SaveChangesAsync();
        return (true, id > 0 ? "Đã cập nhật địa điểm." : "Đã thêm địa điểm.");
    }

    public async Task<(bool ok, string msg)> DeleteGlnAsync(int id)
    {
        var gln = await db.Glns.FirstOrDefaultAsync(g => g.Id == id);
        if (gln == null) return (false, "Không tìm thấy địa điểm.");
        db.Glns.Remove(gln);
        await db.SaveChangesAsync();
        return (true, "Đã xóa địa điểm.");
    }

    // ===== Danh mục nông trại / vùng trồng (GS1 Farm — Mst_Farm của InBrandCloud eTEM) =====
    // "Từ điển" các nông trại/vùng trồng trong chuỗi truy xuất nguồn gốc (nơi nuôi trồng/thu hoạch).
    public async Task<List<Farm>> FarmsAsync(string? q)
    {
        var query = db.Farms.AsQueryable();
        if (!string.IsNullOrWhiteSpace(q)) query = query.Where(f => f.Code.Contains(q) || f.Name.Contains(q));
        var list = await query.ToListAsync();
        return list.OrderBy(f => f.Code).ToList();
    }

    public async Task<(bool ok, string msg)> SaveFarmAsync(int id, string code, string name, string? networkType, bool active)
    {
        code = (code ?? "").Trim();
        name = (name ?? "").Trim();
        if (code.Length == 0) return (false, "Cần mã nông trại (FarmCode).");
        if (name.Length == 0) return (false, "Cần tên nông trại (FarmName).");
        // Mã nông trại phải duy nhất trong tenant.
        if (await db.Farms.AnyAsync(f => f.Code == code && f.Id != id)) return (false, $"Mã '{code}' đã tồn tại.");

        Farm farm;
        if (id > 0)
        {
            farm = await db.Farms.FirstOrDefaultAsync(f => f.Id == id) ?? null!;
            if (farm == null) return (false, "Không tìm thấy nông trại.");
        }
        else { farm = new Farm(); db.Farms.Add(farm); }

        farm.Code = code; farm.Name = name;
        farm.NetworkType = string.IsNullOrWhiteSpace(networkType) ? null : networkType.Trim();
        farm.Active = active;
        await db.SaveChangesAsync();
        return (true, id > 0 ? "Đã cập nhật nông trại." : "Đã thêm nông trại.");
    }

    public async Task<(bool ok, string msg)> DeleteFarmAsync(int id)
    {
        var farm = await db.Farms.FirstOrDefaultAsync(f => f.Id == id);
        if (farm == null) return (false, "Không tìm thấy nông trại.");
        db.Farms.Remove(farm);
        await db.SaveChangesAsync();
        return (true, "Đã xóa nông trại.");
    }

    // ===== Mẫu loại tổ chức (GS1 Network Type Template — Mst_TemplateNWType của InBrandCloud eTEM) =====
    // "Bộ khung" sự kiện (CTE) + thành phần dữ liệu (KDE) + ánh xạ CTE_KDE cho một loại tổ chức.
    public async Task<List<TemplateNWType>> TemplatesAsync(string? q)
    {
        var query = db.Templates.Include(t => t.Ctes).Include(t => t.Kdes).Include(t => t.CteKdes).AsQueryable();
        if (!string.IsNullOrWhiteSpace(q)) query = query.Where(t => t.TplNWType.Contains(q) || t.Description.Contains(q));
        var list = await query.ToListAsync();
        return list.OrderBy(t => t.TplNWType).ToList();
    }

    public Task<TemplateNWType?> GetTemplateAsync(int id) =>
        db.Templates.Include(t => t.Ctes).Include(t => t.Kdes).Include(t => t.CteKdes)
          .FirstOrDefaultAsync(t => t.Id == id);

    // Lưu mẫu loại tổ chức (thay thế toàn bộ CTE/KDE/CTE_KDE con). Áp quy tắc nghiệp vụ InBrandCloud:
    //  (1) Cần mã loại tổ chức (TplNWType) + tên (TplNWTDesc).
    //  (2) Phải có ÍT NHẤT 1 sự kiện (CTE) và ÍT NHẤT 1 thành phần dữ liệu (KDE).
    //  (3) Mẫu đã APPROVE thì không cho sửa (phải hủy/duyệt lại) — giữ toàn vẹn mẫu đã phát hành.
    //  (4) Mọi CTE/KDE tham chiếu phải tồn tại trong danh mục; ánh xạ CTE_KDE phải nằm trong tập đã chọn.
    public async Task<(bool ok, string msg)> SaveTemplateAsync(int id, string tplNWType, string description, string? remark,
        List<TplNwtCteInput> ctes, List<TplNwtKdeInput> kdes, List<TplNwtCteKdeInput> cteKdes)
    {
        tplNWType = (tplNWType ?? "").Trim();
        description = (description ?? "").Trim();
        if (tplNWType.Length == 0) return (false, "Cần mã loại tổ chức (TplNWType).");
        if (description.Length == 0) return (false, "Cần tên loại tổ chức (TplNWTDesc).");
        if (await db.Templates.AnyAsync(t => t.TplNWType == tplNWType && t.Id != id)) return (false, $"Mã '{tplNWType}' đã tồn tại.");

        ctes ??= []; kdes ??= []; cteKdes ??= [];
        var cleanCtes = ctes.Where(c => !string.IsNullOrWhiteSpace(c.CteCode)).GroupBy(c => c.CteCode.Trim()).Select(g => g.First()).ToList();
        var cleanKdes = kdes.Where(k => !string.IsNullOrWhiteSpace(k.KdeCode)).GroupBy(k => k.KdeCode.Trim()).Select(g => g.First()).ToList();
        if (cleanCtes.Count == 0) return (false, "Mẫu phải có ít nhất 1 sự kiện (CTE).");
        if (cleanKdes.Count == 0) return (false, "Mẫu phải có ít nhất 1 thành phần dữ liệu (KDE).");

        // Kiểm tra các mã CTE/KDE tham chiếu tồn tại trong danh mục.
        var cteCodes = cleanCtes.Select(c => c.CteCode.Trim()).ToList();
        var kdeCodes = cleanKdes.Select(k => k.KdeCode.Trim()).ToList();
        var existCtes = await db.Ctes.Where(c => cteCodes.Contains(c.Code)).Select(c => c.Code).ToListAsync();
        var existKdes = await db.Kdes.Where(k => kdeCodes.Contains(k.Code)).Select(k => k.Code).ToListAsync();
        var missCte = cteCodes.Except(existCtes).ToList();
        var missKde = kdeCodes.Except(existKdes).ToList();
        if (missCte.Count > 0) return (false, $"Sự kiện không tồn tại: {string.Join(", ", missCte)}.");
        if (missKde.Count > 0) return (false, $"Thành phần không tồn tại: {string.Join(", ", missKde)}.");

        // Ánh xạ CTE_KDE phải nằm trong tập CTE/KDE đã chọn.
        var cleanMaps = cteKdes.Where(m => !string.IsNullOrWhiteSpace(m.CteCode) && !string.IsNullOrWhiteSpace(m.KdeCode))
            .Select(m => new TplNwtCteKdeInput(m.CteCode.Trim(), m.KdeCode.Trim(), m.ApiLink, m.FlagKey, m.FlagOsOrgView))
            .GroupBy(m => (m.CteCode, m.KdeCode)).Select(g => g.First()).ToList();
        var badMap = cleanMaps.FirstOrDefault(m => !cteCodes.Contains(m.CteCode) || !kdeCodes.Contains(m.KdeCode));
        if (badMap != null) return (false, $"Ánh xạ {badMap.CteCode}↔{badMap.KdeCode} không thuộc tập sự kiện/thành phần đã chọn.");

        TemplateNWType tpl;
        if (id > 0)
        {
            tpl = await db.Templates.Include(t => t.Ctes).Include(t => t.Kdes).Include(t => t.CteKdes).FirstOrDefaultAsync(t => t.Id == id) ?? null!;
            if (tpl == null) return (false, "Không tìm thấy mẫu loại tổ chức.");
            // Quy tắc (3): mẫu đã duyệt không cho sửa.
            if (tpl.Status == TplNwtStatus.Approve) return (false, "Mẫu đã duyệt — không thể sửa. Hãy hủy duyệt trước.");
            db.TplNwtCtes.RemoveRange(tpl.Ctes);
            db.TplNwtKdes.RemoveRange(tpl.Kdes);
            db.TplNwtCteKdes.RemoveRange(tpl.CteKdes);
        }
        else { tpl = new TemplateNWType(); db.Templates.Add(tpl); }

        tpl.TplNWType = tplNWType; tpl.Description = description;
        tpl.Remark = string.IsNullOrWhiteSpace(remark) ? null : remark.Trim();
        tpl.Status = TplNwtStatus.Pending;   // mọi thay đổi đưa mẫu về trạng thái chờ duyệt
        await db.SaveChangesAsync();

        foreach (var c in cleanCtes)
            db.TplNwtCtes.Add(new TplNwtCte { TemplateId = tpl.Id, CteCode = c.CteCode.Trim(), CteDesc = c.CteDesc, ApiLink = c.ApiLink, Active = c.Active });
        foreach (var k in cleanKdes)
            db.TplNwtKdes.Add(new TplNwtKde { TemplateId = tpl.Id, KdeCode = k.KdeCode.Trim(), KdeDesc = k.KdeDesc, DataType = k.DataType, RefNoList = k.RefNoList, FlagList = k.FlagList, FlagQuery = k.FlagQuery, Active = k.Active });
        foreach (var m in cleanMaps)
            db.TplNwtCteKdes.Add(new TplNwtCteKde { TemplateId = tpl.Id, CteCode = m.CteCode, KdeCode = m.KdeCode, ApiLink = m.ApiLink, FlagKey = m.FlagKey, FlagOsOrgView = m.FlagOsOrgView });
        await db.SaveChangesAsync();
        return (true, id > 0 ? "Đã cập nhật mẫu loại tổ chức." : "Đã thêm mẫu loại tổ chức.");
    }

    public async Task<(bool ok, string msg)> DeleteTemplateAsync(int id)
    {
        var tpl = await db.Templates.Include(t => t.Ctes).Include(t => t.Kdes).Include(t => t.CteKdes).FirstOrDefaultAsync(t => t.Id == id);
        if (tpl == null) return (false, "Không tìm thấy mẫu loại tổ chức.");
        db.TplNwtCtes.RemoveRange(tpl.Ctes);
        db.TplNwtKdes.RemoveRange(tpl.Kdes);
        db.TplNwtCteKdes.RemoveRange(tpl.CteKdes);
        db.Templates.Remove(tpl);
        await db.SaveChangesAsync();
        return (true, "Đã xóa mẫu loại tổ chức.");
    }

    // Duyệt mẫu (PENDING → APPROVE). Mẫu đã duyệt là chuẩn để ghi sự kiện cho loại tổ chức đó.
    public async Task<(bool ok, string msg)> ApproveTemplateAsync(int id)
    {
        var tpl = await db.Templates.FirstOrDefaultAsync(t => t.Id == id);
        if (tpl == null) return (false, "Không tìm thấy mẫu loại tổ chức.");
        if (tpl.Status == TplNwtStatus.Approve) return (false, "Mẫu đã được duyệt.");
        tpl.Status = TplNwtStatus.Approve;
        await db.SaveChangesAsync();
        return (true, $"Đã duyệt mẫu '{tpl.TplNWType}'.");
    }

    // ===== Mẫu hiển thị sự kiện truy xuất (GS1 Template View Event — Mst_TplViewEvent của InBrandCloud eTEM) =====
    // "Khuôn hiển thị" cho một sự kiện (CTE): mô tả + chi tiết bố cục dùng để render hành trình truy xuất.
    public async Task<List<TplViewEvent>> TplViewEventsAsync(string? q)
    {
        var query = db.TplViewEvents.AsQueryable();
        if (!string.IsNullOrWhiteSpace(q)) query = query.Where(v => v.Code.Contains(q) || v.Description.Contains(q) || (v.CteCode != null && v.CteCode.Contains(q)));
        var list = await query.ToListAsync();
        return list.OrderBy(v => v.Code).ToList();
    }

    // Lưu mẫu hiển thị. Áp quy tắc nghiệp vụ InBrandCloud (Mst_TplViewEvent_Create/Update):
    //  (1) Cần mã (TplVECode) + mô tả (TplVEDesc) + chi tiết (TplVEDetail).
    //  (2) Mã mẫu hiển thị duy nhất trong tenant.
    //  (3) Nếu gắn sự kiện (CTECode) thì sự kiện phải tồn tại trong danh mục CTE.
    //  (4) Mỗi sự kiện chỉ được có TỐI ĐA 1 mẫu hiển thị đang hoạt động (FlagActive) — giữ tính duy nhất khi render.
    public async Task<(bool ok, string msg)> SaveTplViewEventAsync(int id, string code, string description, string detail, string? cteCode, string? remark, bool active, bool flagBG)
    {
        code = (code ?? "").Trim();
        description = (description ?? "").Trim();
        detail = (detail ?? "").Trim();
        cteCode = string.IsNullOrWhiteSpace(cteCode) ? null : cteCode.Trim();
        if (code.Length == 0) return (false, "Cần mã mẫu hiển thị (TplVECode).");
        if (description.Length == 0) return (false, "Cần mô tả mẫu hiển thị (TplVEDesc).");
        if (detail.Length == 0) return (false, "Cần chi tiết bố cục hiển thị (TplVEDetail).");
        // Mã mẫu hiển thị phải duy nhất trong tenant.
        if (await db.TplViewEvents.AnyAsync(v => v.Code == code && v.Id != id)) return (false, $"Mã '{code}' đã tồn tại.");
        // Sự kiện tham chiếu (nếu có) phải tồn tại trong danh mục CTE.
        if (cteCode != null && !await db.Ctes.AnyAsync(c => c.Code == cteCode)) return (false, $"Sự kiện '{cteCode}' không tồn tại.");
        // Quy tắc (4): mỗi sự kiện tối đa 1 mẫu hiển thị đang hoạt động.
        if (active && cteCode != null && await db.TplViewEvents.AnyAsync(v => v.CteCode == cteCode && v.Active && v.Id != id))
            return (false, $"Sự kiện '{cteCode}' đã có mẫu hiển thị đang hoạt động — chỉ được 1 mẫu.");

        TplViewEvent ve;
        if (id > 0)
        {
            ve = await db.TplViewEvents.FirstOrDefaultAsync(v => v.Id == id) ?? null!;
            if (ve == null) return (false, "Không tìm thấy mẫu hiển thị.");
        }
        else { ve = new TplViewEvent(); db.TplViewEvents.Add(ve); }

        ve.Code = code; ve.Description = description; ve.Detail = detail;
        ve.CteCode = cteCode;
        ve.Remark = string.IsNullOrWhiteSpace(remark) ? null : remark.Trim();
        ve.Active = active; ve.FlagBG = flagBG;
        await db.SaveChangesAsync();
        return (true, id > 0 ? "Đã cập nhật mẫu hiển thị." : "Đã thêm mẫu hiển thị.");
    }

    public async Task<(bool ok, string msg)> DeleteTplViewEventAsync(int id)
    {
        var ve = await db.TplViewEvents.FirstOrDefaultAsync(v => v.Id == id);
        if (ve == null) return (false, "Không tìm thấy mẫu hiển thị.");
        db.TplViewEvents.Remove(ve);
        await db.SaveChangesAsync();
        return (true, "Đã xóa mẫu hiển thị.");
    }

    private static string NewCode() => "89" + Guid.NewGuid().ToString("N")[..10].ToUpperInvariant();
}
