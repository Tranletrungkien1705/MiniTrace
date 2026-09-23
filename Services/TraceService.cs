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
}

/// <summary>Kết quả 1 lần quét xác thực (trả về cho NTD).</summary>
public record VerifyResult(string Code, string Product, string? Origin, string? Manufacturer, string LotNo,
    int VerifyCount, VerifyStatus Status, string StatusText, string Message, DateTime ScannedAt);

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

    private static string NewCode() => "89" + Guid.NewGuid().ToString("N")[..10].ToUpperInvariant();
}
