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
}

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

    private static string NewCode() => "89" + Guid.NewGuid().ToString("N")[..10].ToUpperInvariant();
}
