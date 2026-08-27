using Microsoft.EntityFrameworkCore;
using MiniTrace.Models;

namespace MiniTrace.Data;

public static class Seeder
{
    public static async Task SeedAsync(AppDbContext db)
    {
        await db.Database.EnsureCreatedAsync();
        await MigratePostgresAsync(db);

        if (!await db.Orgs.AnyAsync(o => o.Id == TenantContext.DefaultOrgId))
        {
            db.Orgs.Add(new Org { Id = TenantContext.DefaultOrgId, Name = "Demo Trace", ApiKey = TenantContext.DefaultApiKey });
            await db.SaveChangesAsync();
        }
        if (!await db.Products.AnyAsync())
        {
            var p = new Product { Code = "8930001001", Name = "Gạo ST25 túi 5kg", Origin = "Sóc Trăng", Manufacturer = "HTX Lúa gạo ST" };
            db.Products.Add(p); await db.SaveChangesAsync();

            // 1 đơn vị truy xuất mẫu đã đi qua vài bước
            var unit = new TraceUnit { ProductId = p.Id, LotNo = "L2026-001", Code = "89DEMO0001AB",
                Events = [
                    new TraceEvent { Type = EventType.Produced, Location = "Sóc Trăng", Actor = "HTX Lúa gạo ST", OccurredAt = DateTime.Now.AddDays(-20), Note = "Thu hoạch + xay xát" },
                    new TraceEvent { Type = EventType.QualityChecked, Location = "PTN Cần Thơ", Actor = "TT Kiểm định NN", OccurredAt = DateTime.Now.AddDays(-18), Note = "Đạt VietGAP" },
                    new TraceEvent { Type = EventType.Packed, Location = "Nhà máy Sóc Trăng", Actor = "HTX Lúa gạo ST", OccurredAt = DateTime.Now.AddDays(-15) },
                    new TraceEvent { Type = EventType.Shipped, Location = "Sóc Trăng → TP.HCM", Actor = "GHTK", OccurredAt = DateTime.Now.AddDays(-10) },
                    new TraceEvent { Type = EventType.Received, Location = "Siêu thị Q.1 TP.HCM", Actor = "Co.opmart", OccurredAt = DateTime.Now.AddDays(-8) },
                ] };
            db.Units.Add(unit);
            await db.SaveChangesAsync();
        }
    }

    private static async Task MigratePostgresAsync(AppDbContext db)
    {
        if (!db.Database.IsNpgsql()) return;
        var def = TenantContext.DefaultOrgId;
        var tables = new[] { "Products", "Units", "Events" };
        var sql = new List<string>
        {
            "CREATE TABLE IF NOT EXISTS minitrace.\"Orgs\" (\"Id\" uuid PRIMARY KEY, \"Name\" text NOT NULL DEFAULT '', \"ApiKey\" text NOT NULL DEFAULT '', \"CreatedAt\" timestamp NOT NULL DEFAULT now())",
            "CREATE UNIQUE INDEX IF NOT EXISTS \"IX_Orgs_ApiKey\" ON minitrace.\"Orgs\" (\"ApiKey\")",
        };
        foreach (var t in tables) sql.Add($"ALTER TABLE minitrace.\"{t}\" ADD COLUMN IF NOT EXISTS \"OrgId\" uuid NOT NULL DEFAULT '{def}'");
        foreach (var s in sql) try { await db.Database.ExecuteSqlRawAsync(s); } catch { }
    }
}
