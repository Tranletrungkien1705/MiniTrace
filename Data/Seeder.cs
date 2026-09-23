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

            // Lịch sử quét xác thực mẫu (chống hàng giả): 1 chính hãng + 1 nghi giả (quét nhiều nơi).
            db.Verifications.AddRange(
                new Verification { UnitId = unit.Id, Code = unit.Code, VerifyCount = 1, Status = VerifyStatus.Genuine,
                    IpAddress = "203.113.10.5", Location = "TP.HCM", Latitude = 10.7769, Longitude = 106.7009, Phone = "0901234567",
                    ScannedAt = DateTime.Now.AddDays(-6), Note = "Quét lần đầu tại điểm bán" },
                new Verification { UnitId = unit.Id, Code = unit.Code, VerifyCount = 2, Status = VerifyStatus.Suspect,
                    IpAddress = "42.118.7.9", Location = "Hà Nội", Latitude = 21.0278, Longitude = 105.8342,
                    ScannedAt = DateTime.Now.AddDays(-2), Note = "Quét lại ở địa điểm khác — nghi hàng giả" });
            await db.SaveChangesAsync();
        }

        // Danh mục sự kiện truy xuất trọng yếu (GS1 CTE) — "từ điển" 8 giai đoạn chuỗi cung ứng.
        if (!await db.Ctes.AnyAsync())
        {
            db.Ctes.AddRange(
                new Cte { Code = "PRODUCTION_IN", Description = "Nhập kho thành phẩm (sản xuất)", NetworkType = "Manufacturer", Active = true },
                new Cte { Code = "QUALITY_CHECK", Description = "Kiểm định chất lượng", NetworkType = "Manufacturer", Active = true },
                new Cte { Code = "PACKING", Description = "Đóng gói", NetworkType = "Manufacturer", Active = true },
                new Cte { Code = "WAREHOUSE_IN", Description = "Nhập kho", NetworkType = "Warehouse", Active = true },
                new Cte { Code = "SALE_TO_DISTRIBUTOR", Description = "Xuất kho → Đại lý cấp 1", NetworkType = "Distributor", Active = true },
                new Cte { Code = "DISTRIBUTOR_IN", Description = "Đại lý nhận hàng", NetworkType = "Distributor", Active = true },
                new Cte { Code = "RETAIL_SALE", Description = "Bày bán tại điểm bán lẻ", NetworkType = "Dealer", Active = true },
                new Cte { Code = "CONSUMER_SCAN", Description = "Bán cho người tiêu dùng / quét xác thực", NetworkType = "Consumer", Active = true });
            await db.SaveChangesAsync();
        }

        // Danh mục thành phần dữ liệu trọng yếu (GS1 KDE) — "từ điển" các trường dữ liệu phải thu thập.
        if (!await db.Kdes.AnyAsync())
        {
            db.Kdes.AddRange(
                new Kde { Code = "LOT_NO", Description = "Số lô sản xuất", DataType = "Text", NetworkType = "Manufacturer", Active = true },
                new Kde { Code = "PROD_DATE", Description = "Ngày sản xuất", DataType = "Date", NetworkType = "Manufacturer", Active = true },
                new Kde { Code = "EXP_DATE", Description = "Hạn sử dụng", DataType = "Date", NetworkType = "Manufacturer", Active = true },
                new Kde { Code = "SERIAL_NO", Description = "Số serial / mã định danh đơn vị", DataType = "Text", NetworkType = "Manufacturer", Active = true },
                new Kde { Code = "QUANTITY", Description = "Số lượng", DataType = "Number", NetworkType = "Warehouse", Active = true },
                new Kde { Code = "FROM_LOCATION", Description = "Vị trí xuất phát", DataType = "Text", NetworkType = "Warehouse", Active = true },
                new Kde { Code = "TO_LOCATION", Description = "Vị trí đích", DataType = "Text", NetworkType = "Warehouse", Active = true },
                new Kde { Code = "CARRIER", Description = "Đơn vị vận chuyển", DataType = "Text", NetworkType = "Distributor", Active = true },
                new Kde { Code = "STORAGE_TEMP", Description = "Nhiệt độ bảo quản", DataType = "Number", NetworkType = "Warehouse", Active = true },
                new Kde { Code = "QUALITY_RESULT", Description = "Kết quả kiểm định", DataType = "List", RefNoList = "Đạt;Không đạt;Chờ", NetworkType = "Manufacturer", FlagList = true, Active = true });
            await db.SaveChangesAsync();
        }

        // Ánh xạ sự kiện ↔ thành phần dữ liệu (GS1 CTE_KDE) — mỗi sự kiện cần thu thập KDE nào.
        if (!await db.CteKdes.AnyAsync())
        {
            db.CteKdes.AddRange(
                new CteKde { CteCode = "PRODUCTION_IN", KdeCode = "LOT_NO", NetworkType = "Manufacturer", FlagKey = true },
                new CteKde { CteCode = "PRODUCTION_IN", KdeCode = "PROD_DATE", NetworkType = "Manufacturer", FlagKey = true },
                new CteKde { CteCode = "PRODUCTION_IN", KdeCode = "SERIAL_NO", NetworkType = "Manufacturer", FlagKey = true },
                new CteKde { CteCode = "QUALITY_CHECK", KdeCode = "QUALITY_RESULT", NetworkType = "Manufacturer", FlagKey = true },
                new CteKde { CteCode = "PACKING", KdeCode = "LOT_NO", NetworkType = "Manufacturer", FlagKey = true },
                new CteKde { CteCode = "PACKING", KdeCode = "EXP_DATE", NetworkType = "Manufacturer", FlagKey = true },
                new CteKde { CteCode = "WAREHOUSE_IN", KdeCode = "QUANTITY", NetworkType = "Warehouse", FlagKey = true },
                new CteKde { CteCode = "WAREHOUSE_IN", KdeCode = "STORAGE_TEMP", NetworkType = "Warehouse", FlagKey = false },
                new CteKde { CteCode = "SALE_TO_DISTRIBUTOR", KdeCode = "FROM_LOCATION", NetworkType = "Distributor", FlagKey = true },
                new CteKde { CteCode = "SALE_TO_DISTRIBUTOR", KdeCode = "TO_LOCATION", NetworkType = "Distributor", FlagKey = true },
                new CteKde { CteCode = "SALE_TO_DISTRIBUTOR", KdeCode = "CARRIER", NetworkType = "Distributor", FlagKey = false },
                new CteKde { CteCode = "DISTRIBUTOR_IN", KdeCode = "TO_LOCATION", NetworkType = "Distributor", FlagKey = true },
                new CteKde { CteCode = "RETAIL_SALE", KdeCode = "LOT_NO", NetworkType = "Dealer", FlagKey = true },
                new CteKde { CteCode = "CONSUMER_SCAN", KdeCode = "SERIAL_NO", NetworkType = "Consumer", FlagKey = true });
            await db.SaveChangesAsync();
        }

        // Danh mục địa điểm toàn cầu (GS1 GLN) — "từ điển" các địa điểm chuỗi cung ứng kèm toạ độ GPS.
        if (!await db.Glns.AnyAsync())
        {
            db.Glns.AddRange(
                new Gln { Code = "8930001000001", Name = "Nhà máy HTX Lúa gạo ST", GpsLat = "9.6025", GpsLong = "105.9739", Remark = "Sóc Trăng", Active = true },
                new Gln { Code = "8930001000002", Name = "Kho thành phẩm Sóc Trăng", GpsLat = "9.6031", GpsLong = "105.9801", Remark = "Kho FG", Active = true },
                new Gln { Code = "8930001000003", Name = "Đại lý phân phối TP.HCM", GpsLat = "10.7769", GpsLong = "106.7009", Remark = "Đại lý cấp 1", Active = true },
                new Gln { Code = "8930001000004", Name = "Siêu thị Co.opmart Q.1", GpsLat = "10.7756", GpsLong = "106.7019", Remark = "Điểm bán lẻ", Active = true });
            await db.SaveChangesAsync();
        }
    }

    private static async Task MigratePostgresAsync(AppDbContext db)
    {
        if (!db.Database.IsNpgsql()) return;
        var def = TenantContext.DefaultOrgId;
        var tables = new[] { "Products", "Units", "Events", "Verifications", "Ctes", "Kdes", "CteKdes", "Glns" };
        var sql = new List<string>
        {
            "CREATE TABLE IF NOT EXISTS minitrace.\"Orgs\" (\"Id\" uuid PRIMARY KEY, \"Name\" text NOT NULL DEFAULT '', \"ApiKey\" text NOT NULL DEFAULT '', \"CreatedAt\" timestamp NOT NULL DEFAULT now())",
            "CREATE UNIQUE INDEX IF NOT EXISTS \"IX_Orgs_ApiKey\" ON minitrace.\"Orgs\" (\"ApiKey\")",
        };
        foreach (var t in tables) sql.Add($"ALTER TABLE minitrace.\"{t}\" ADD COLUMN IF NOT EXISTS \"OrgId\" uuid NOT NULL DEFAULT '{def}'");
        foreach (var s in sql) try { await db.Database.ExecuteSqlRawAsync(s); } catch { }
    }
}
