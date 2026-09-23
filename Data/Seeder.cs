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

        // Danh mục kiểu dữ liệu (GS1 Data Type — Mst_DataType của InBrandCloud eTEM) — "từ điển" kiểu dữ liệu cho KDE.
        if (!await db.DataTypes.AnyAsync())
        {
            db.DataTypes.AddRange(
                new DataType { Code = "Text", Description = "Chuỗi ký tự", NetworkType = "Manufacturer", Active = true },
                new DataType { Code = "Number", Description = "Số", NetworkType = "Manufacturer", Active = true },
                new DataType { Code = "Date", Description = "Ngày tháng", NetworkType = "Manufacturer", Active = true },
                new DataType { Code = "List", Description = "Danh sách giá trị (chọn 1)", NetworkType = "Manufacturer", Active = true });
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

        // Danh mục nông trại / vùng trồng (GS1 Farm — Mst_Farm của InBrandCloud eTEM) — "từ điển" nơi nuôi trồng/thu hoạch.
        if (!await db.Farms.AnyAsync())
        {
            db.Farms.AddRange(
                new Farm { Code = "FARM-ST01", Name = "Vùng trồng lúa ST25 Sóc Trăng", NetworkType = "Manufacturer", Active = true },
                new Farm { Code = "FARM-CT01", Name = "Vùng trồng Cần Thơ", NetworkType = "Manufacturer", Active = true },
                new Farm { Code = "FARM-DL01", Name = "Trang trại đối tác Đồng Tháp", NetworkType = "Distributor", Active = true });
            await db.SaveChangesAsync();
        }

        // Danh mục vùng thị trường (GS1 Market Area — Mst_MarketArea của InBrandCloud eTEM) — "từ điển" vùng phân phối.
        if (!await db.MarketAreas.AnyAsync())
        {
            db.MarketAreas.AddRange(
                new MarketArea { Code = "MA-MIENB", Name = "Miền Bắc", AreaType = "Region", Description = "Vùng thị trường miền Bắc", Active = true },
                new MarketArea { Code = "MA-MIENT", Name = "Miền Trung", AreaType = "Region", Description = "Vùng thị trường miền Trung", Active = true },
                new MarketArea { Code = "MA-MIENN", Name = "Miền Nam", AreaType = "Region", Description = "Vùng thị trường miền Nam", Active = true },
                new MarketArea { Code = "MA-TPHCM", Name = "TP. Hồ Chí Minh", AreaType = "City", Description = "Vùng thị trường trọng điểm TP.HCM", Active = true },
                new MarketArea { Code = "MA-MIENDBSCL", Name = "Đồng bằng sông Cửu Long", AreaType = "Region", Description = "Vùng nguyên liệu + tiêu thụ ĐBSCL", Active = false });
            await db.SaveChangesAsync();
        }

        // Ánh xạ tổ chức ↔ địa điểm (Mst_OrgIDMapGLN của InBrandCloud eTEM) — tổ chức hoạt động tại địa điểm nào.
        if (!await db.OrgGlns.AnyAsync())
        {
            db.OrgGlns.AddRange(
                new OrgGln { OrgCode = "MST-NXSX-ST", GlnCode = "8930001000001", Remark = "Nhà máy sản xuất chính" },
                new OrgGln { OrgCode = "MST-NXSX-ST", GlnCode = "8930001000002", Remark = "Kho thành phẩm" },
                new OrgGln { OrgCode = "MST-DL-HCM", GlnCode = "8930001000003", Remark = "Đại lý phân phối cấp 1" },
                new OrgGln { OrgCode = "MST-BANLE-COOP", GlnCode = "8930001000004", Remark = "Điểm bán lẻ" });
            await db.SaveChangesAsync();
        }

        // Mẫu loại tổ chức (GS1 Network Type Template) — "bộ khung" CTE + KDE + ánh xạ cho từng loại tổ chức.
        if (!await db.Templates.AnyAsync())
        {
            var tplMfg = new TemplateNWType { TplNWType = "MANUFACTURER", Description = "Nhà sản xuất", Status = TplNwtStatus.Approve, Remark = "Mẫu chuẩn cho nhà sản xuất" };
            db.Templates.Add(tplMfg); await db.SaveChangesAsync();
            db.TplNwtCtes.AddRange(
                new TplNwtCte { TemplateId = tplMfg.Id, CteCode = "PRODUCTION_IN", CteDesc = "Nhập kho thành phẩm (sản xuất)", Active = true },
                new TplNwtCte { TemplateId = tplMfg.Id, CteCode = "QUALITY_CHECK", CteDesc = "Kiểm định chất lượng", Active = true },
                new TplNwtCte { TemplateId = tplMfg.Id, CteCode = "PACKING", CteDesc = "Đóng gói", Active = true });
            db.TplNwtKdes.AddRange(
                new TplNwtKde { TemplateId = tplMfg.Id, KdeCode = "LOT_NO", KdeDesc = "Số lô sản xuất", DataType = "Text", Active = true },
                new TplNwtKde { TemplateId = tplMfg.Id, KdeCode = "PROD_DATE", KdeDesc = "Ngày sản xuất", DataType = "Date", Active = true },
                new TplNwtKde { TemplateId = tplMfg.Id, KdeCode = "SERIAL_NO", KdeDesc = "Số serial / mã định danh đơn vị", DataType = "Text", Active = true },
                new TplNwtKde { TemplateId = tplMfg.Id, KdeCode = "QUALITY_RESULT", KdeDesc = "Kết quả kiểm định", DataType = "List", RefNoList = "Đạt;Không đạt;Chờ", FlagList = true, Active = true });
            db.TplNwtCteKdes.AddRange(
                new TplNwtCteKde { TemplateId = tplMfg.Id, CteCode = "PRODUCTION_IN", KdeCode = "LOT_NO", FlagKey = true },
                new TplNwtCteKde { TemplateId = tplMfg.Id, CteCode = "PRODUCTION_IN", KdeCode = "PROD_DATE", FlagKey = true },
                new TplNwtCteKde { TemplateId = tplMfg.Id, CteCode = "PRODUCTION_IN", KdeCode = "SERIAL_NO", FlagKey = true },
                new TplNwtCteKde { TemplateId = tplMfg.Id, CteCode = "QUALITY_CHECK", KdeCode = "QUALITY_RESULT", FlagKey = true },
                new TplNwtCteKde { TemplateId = tplMfg.Id, CteCode = "PACKING", KdeCode = "LOT_NO", FlagKey = true });
            await db.SaveChangesAsync();

            var tplDist = new TemplateNWType { TplNWType = "DISTRIBUTOR", Description = "Đại lý phân phối", Status = TplNwtStatus.Pending, Remark = "Mẫu cho đại lý cấp 1" };
            db.Templates.Add(tplDist); await db.SaveChangesAsync();
            db.TplNwtCtes.AddRange(
                new TplNwtCte { TemplateId = tplDist.Id, CteCode = "SALE_TO_DISTRIBUTOR", CteDesc = "Xuất kho → Đại lý cấp 1", Active = true },
                new TplNwtCte { TemplateId = tplDist.Id, CteCode = "DISTRIBUTOR_IN", CteDesc = "Đại lý nhận hàng", Active = true });
            db.TplNwtKdes.AddRange(
                new TplNwtKde { TemplateId = tplDist.Id, KdeCode = "FROM_LOCATION", KdeDesc = "Vị trí xuất phát", DataType = "Text", Active = true },
                new TplNwtKde { TemplateId = tplDist.Id, KdeCode = "TO_LOCATION", KdeDesc = "Vị trí đích", DataType = "Text", Active = true },
                new TplNwtKde { TemplateId = tplDist.Id, KdeCode = "CARRIER", KdeDesc = "Đơn vị vận chuyển", DataType = "Text", Active = true });
            db.TplNwtCteKdes.AddRange(
                new TplNwtCteKde { TemplateId = tplDist.Id, CteCode = "SALE_TO_DISTRIBUTOR", KdeCode = "FROM_LOCATION", FlagKey = true },
                new TplNwtCteKde { TemplateId = tplDist.Id, CteCode = "SALE_TO_DISTRIBUTOR", KdeCode = "TO_LOCATION", FlagKey = true },
                new TplNwtCteKde { TemplateId = tplDist.Id, CteCode = "SALE_TO_DISTRIBUTOR", KdeCode = "CARRIER", FlagKey = false },
                new TplNwtCteKde { TemplateId = tplDist.Id, CteCode = "DISTRIBUTOR_IN", KdeCode = "TO_LOCATION", FlagKey = true });
            await db.SaveChangesAsync();
        }

        // Mẫu hiển thị sự kiện truy xuất (GS1 Template View Event) — "khuôn hiển thị" cho từng sự kiện.
        if (!await db.TplViewEvents.AnyAsync())
        {
            db.TplViewEvents.AddRange(
                new TplViewEvent { Code = "VE_PRODUCTION", Description = "Hiển thị sự kiện sản xuất", CteCode = "PRODUCTION_IN",
                    Detail = "{Tên SP} · Lô {LOT_NO} · NSX {PROD_DATE} · {Location}", Remark = "Mẫu chuẩn cho nhà sản xuất", Active = true },
                new TplViewEvent { Code = "VE_QUALITY", Description = "Hiển thị kết quả kiểm định", CteCode = "QUALITY_CHECK",
                    Detail = "Kiểm định: {QUALITY_RESULT} · {Actor} · {Location}", Active = true },
                new TplViewEvent { Code = "VE_SHIPMENT", Description = "Hiển thị chặng vận chuyển", CteCode = "SALE_TO_DISTRIBUTOR",
                    Detail = "{FROM_LOCATION} → {TO_LOCATION} · ĐVVC {CARRIER}", Active = true },
                new TplViewEvent { Code = "VE_RETAIL", Description = "Hiển thị điểm bán lẻ", CteCode = "RETAIL_SALE",
                    Detail = "Bày bán tại {Location} · {Actor}", Active = true },
                new TplViewEvent { Code = "VE_DEFAULT", Description = "Mẫu hiển thị mặc định (nền)",
                    Detail = "{stage} · {Location} · {Actor} · {OccurredAt}", Remark = "Dùng khi sự kiện chưa có mẫu riêng", Active = false, FlagBG = true });
            await db.SaveChangesAsync();
        }

        // Sự kiện truy xuất theo CTE + KDE (Event_Event + Event_EventSpec) — bản ghi hành trình mẫu.
        if (!await db.Records.AnyAsync())
        {
            var rec1 = new TraceRecord { EventNo = "EV2601010001A", CteCode = "PRODUCTION_IN", TplVECode = "VE_PRODUCTION",
                TplVEDetail = "{Tên SP} · Lô {LOT_NO} · NSX {PROD_DATE} · {Location}", GlnOrgCode = "8930001000001",
                GlnOrgName = "Nhà máy HTX Lúa gạo ST", GpsLat = "9.6025", GpsLong = "105.9739", Remark = "Lô ST25 vụ đông xuân" };
            db.Records.Add(rec1); await db.SaveChangesAsync();
            db.RecordSpecs.AddRange(
                new TraceRecordSpec { RecordId = rec1.Id, CteCode = "PRODUCTION_IN", KdeCode = "LOT_NO", KdeValue = "L2026-001", FlagKey = true },
                new TraceRecordSpec { RecordId = rec1.Id, CteCode = "PRODUCTION_IN", KdeCode = "PROD_DATE", KdeValue = "2026-01-05", FlagKey = true },
                new TraceRecordSpec { RecordId = rec1.Id, CteCode = "PRODUCTION_IN", KdeCode = "SERIAL_NO", KdeValue = "89DEMO0001AB", FlagKey = true });
            await db.SaveChangesAsync();

            var rec2 = new TraceRecord { EventNo = "EV2601010002B", CteCode = "QUALITY_CHECK", TplVECode = "VE_QUALITY",
                TplVEDetail = "Kiểm định: {QUALITY_RESULT} · {Actor} · {Location}", GlnOrgCode = "8930001000002",
                GlnOrgName = "Kho thành phẩm Sóc Trăng", GpsLat = "9.6031", GpsLong = "105.9801", Remark = "Đạt VietGAP" };
            db.Records.Add(rec2); await db.SaveChangesAsync();
            db.RecordSpecs.AddRange(
                new TraceRecordSpec { RecordId = rec2.Id, CteCode = "QUALITY_CHECK", KdeCode = "QUALITY_RESULT", KdeValue = "Đạt", FlagKey = true, FlagList = true });
            await db.SaveChangesAsync();
        }

        // Sinh tem / kho số tem (Inv_GenTimes + Inv_InventoryGenID của InBrandCloud eTEM) — lần sinh tem mẫu.
        if (!await db.StampBatches.AnyAsync())
        {
            var batch = new StampBatch
            {
                GenTimesNo = "GT2601010001", ProductCode = "8930001001", ProductName = "Gạo ST25 túi 5kg",
                QrType = QrType.ProdId, ConfigDomain = "P", Qty = 5, FlagPIN = true, FlagMap = false,
                ProductionLotNo = "L2026-001", ProductionDate = "2026-01-05", ShiftInCode = "CA1", UserKCS = "Nguyễn Văn KCS",
                Remark = "Lần sinh tem mẫu cho lô ST25"
            };
            db.StampBatches.Add(batch); await db.SaveChangesAsync();
            for (int i = 1; i <= 5; i++)
            {
                var idNo = $"P{i:D6}";
                var pin = $"PIN{i:D5}";
                db.Stamps.Add(new Stamp
                {
                    BatchId = batch.Id, IDNo = idNo, QR_ID = idNo, SecretNo = idNo, PIN = pin,
                    HashInformation = Md5($"{idNo}|{pin}"), FlagMap = false, FlagUsed = false
                });
            }
            await db.SaveChangesAsync();
        }

        // Đóng hộp / gán tem vào hộp (Inv_InventoryGenBox + Map_IDInBox của InBrandCloud eTEM) — hộp mẫu gom 3 tem.
        if (!await db.Boxes.AnyAsync())
        {
            var box = new Box
            {
                BoxNo = "B2601010001", QR_BoxNo = "B2601010001", GenTimesNo = "GT2601010001",
                ProductCode = "8930001001", ProductName = "Gạo ST25 túi 5kg",
                Remark = "Hộp mẫu gom 3 tem sản phẩm", FlagMap = true, FlagUsed = false
            };
            db.Boxes.Add(box); await db.SaveChangesAsync();
            db.BoxItems.AddRange(
                new BoxItem { BoxId = box.Id, BoxNo = box.BoxNo, IDNo = "P000001", ProductCode = "8930001001", InvCode = "KHO-FG-ST", FlagActive = true },
                new BoxItem { BoxId = box.Id, BoxNo = box.BoxNo, IDNo = "P000002", ProductCode = "8930001001", InvCode = "KHO-FG-ST", FlagActive = true },
                new BoxItem { BoxId = box.Id, BoxNo = box.BoxNo, IDNo = "P000003", ProductCode = "8930001001", InvCode = "KHO-FG-ST", FlagActive = true });
            await db.SaveChangesAsync();
        }

        // Đóng thùng / gán hộp vào thùng (Inv_InventoryGenCarton + Map_BoxInCarton của InBrandCloud eTEM) — thùng mẫu gom 1 hộp.
        if (!await db.Cartons.AnyAsync())
        {
            var carton = new Carton
            {
                CanNo = "C2601010001", QR_CanNo = "C2601010001", GenTimesNo = "GT2601010001",
                ProductCode = "8930001001", ProductName = "Gạo ST25 túi 5kg",
                Remark = "Thùng mẫu gom hộp B2601010001", FlagMap = true, FlagUsed = false
            };
            db.Cartons.Add(carton); await db.SaveChangesAsync();
            db.CartonItems.Add(
                new CartonItem { CartonId = carton.Id, CanNo = carton.CanNo, BoxNo = "B2601010001", ProductCode = "8930001001", InvCode = "KHO-FG-ST", FlagActive = true });
            await db.SaveChangesAsync();
        }

        // Hàng đợi đồng bộ dữ liệu truy xuất (MstSv_QueSync của InBrandCloud eTEM) — bản ghi mẫu chờ đẩy lên eTEM/ELTS.
        if (!await db.QueSyncs.AnyAsync())
        {
            db.QueSyncs.AddRange(
                new QueSync { NetworkId = "Manufacturer", QueSyncNo = "PRODUCTION_IN", TableCode = "Mst_CTE", FlagSync = true, FlagSyncBL = false, Status = QueSyncStatus.Pending, Remark = "Đẩy danh mục sự kiện sản xuất lên eTEM" },
                new QueSync { NetworkId = "Manufacturer", QueSyncNo = "LOT_NO", TableCode = "Mst_KDE", FlagSync = true, FlagSyncBL = false, Status = QueSyncStatus.Pending, Remark = "Đẩy thành phần dữ liệu số lô" },
                new QueSync { NetworkId = "Manufacturer", QueSyncNo = "EV2601010001A", TableCode = "Event_Event", FlagSync = false, FlagSyncBL = true, Status = QueSyncStatus.Synced, SyncedAt = DateTime.Now.AddDays(-1), Remark = "Sự kiện sản xuất đã đẩy + ghi blockchain" },
                new QueSync { NetworkId = "Distributor", QueSyncNo = "SALE_TO_DISTRIBUTOR", TableCode = "Mst_CTE", FlagSync = true, FlagSyncBL = false, Status = QueSyncStatus.Failed, RetryCount = 2, ErrorDetail = "eTEM timeout khi đẩy sự kiện xuất kho", Remark = "Cần thử lại" });
            await db.SaveChangesAsync();
        }

        // Danh mục dữ liệu gốc (Mst_MasterData của InBrandCloud eTEM) — "từ điển" các bảng tham chiếu eTEM tra cứu động.
        if (!await db.MasterDatas.AnyAsync())
        {
            db.MasterDatas.AddRange(
                new MasterData { Code = "MD_CTE", NetworkId = "Manufacturer", TableName = "Mst_CTE", Active = true, Remark = "Danh mục sự kiện truy xuất trọng yếu" },
                new MasterData { Code = "MD_KDE", NetworkId = "Manufacturer", TableName = "Mst_KDE", Active = true, Remark = "Danh mục thành phần dữ liệu trọng yếu" },
                new MasterData { Code = "MD_GLN", NetworkId = "Warehouse", TableName = "Mst_GLN", Active = true, Remark = "Danh mục địa điểm toàn cầu" },
                new MasterData { Code = "MD_FARM", NetworkId = "Manufacturer", TableName = "Mst_Farm", Active = true, Remark = "Danh mục nông trại / vùng trồng" },
                new MasterData { Code = "MD_EVENT", NetworkId = "Distributor", TableName = "Event_Event", Active = false, Remark = "Bảng sự kiện chuỗi cung ứng (tạm ngưng tra cứu)" });
            await db.SaveChangesAsync();
        }

        // Tổ chức tham gia mạng lưới truy xuất (Mst_NNT của InBrandCloud eTEM) — doanh nghiệp đăng ký tham gia chuỗi.
        if (!await db.NetworkOrgs.AnyAsync())
        {
            db.NetworkOrgs.AddRange(
                new NetworkOrg { Mst = "MST-NXSX-ST", FullName = "HTX Lúa gạo ST25 Sóc Trăng", NetworkType = "Manufacturer", OrgCode = "ORG-NXSX-ST",
                    Address = "Sóc Trăng", Mobile = "0901111222", ContactName = "Nguyễn Văn A", ContactEmail = "a@htxst.vn", Gln = "8930001000001",
                    EltsMstId = "ELTSMST.DEMO00000001", Status = NetworkOrgStatus.Approved, Active = true, Remark = "Nhà sản xuất chính" },
                new NetworkOrg { Mst = "MST-DL-HCM", FullName = "Đại lý phân phối TP.HCM", NetworkType = "Distributor", OrgCode = "ORG-DL-HCM",
                    Address = "TP.HCM", Mobile = "0903333444", ContactName = "Trần Thị B", ContactEmail = "b@dailyhcm.vn", Gln = "8930001000003",
                    Status = NetworkOrgStatus.New, Active = true, Remark = "Đại lý cấp 1 — chờ đăng ký mạng" },
                new NetworkOrg { Mst = "MST-BANLE-COOP", FullName = "Siêu thị Co.opmart Q.1", NetworkType = "Dealer", OrgCode = "ORG-BANLE-COOP",
                    Address = "Q.1 TP.HCM", Mobile = "0905555666", ContactName = "Lê Văn C", Gln = "8930001000004",
                    Status = NetworkOrgStatus.New, Active = false, Remark = "Điểm bán lẻ — tạm ngưng" });
            await db.SaveChangesAsync();
        }

        // Kho số bí mật (Inv_InventorySecret của InBrandCloud eTEM) — số bí mật in lên tem cào chống giả.
        if (!await db.Secrets.AnyAsync())
        {
            db.Secrets.AddRange(
                new Secret { SerialNo = "P000001", SecretNo = "SEC000001", QR_SerialNo = "QR-P000001", NetworkId = "Manufacturer",
                    Mst = "MST-NXSX-ST", OrgCode = "ORG-NXSX-ST", GenTimesNo = "GT2601010001", FlagMap = true, FlagUsed = false, Remark = "Số bí mật cho tem P000001" },
                new Secret { SerialNo = "P000002", SecretNo = "SEC000002", QR_SerialNo = "QR-P000002", NetworkId = "Manufacturer",
                    Mst = "MST-NXSX-ST", OrgCode = "ORG-NXSX-ST", GenTimesNo = "GT2601010001", FlagMap = true, FlagUsed = false, Remark = "Số bí mật cho tem P000002" },
                new Secret { SerialNo = "P000003", SecretNo = "SEC000003", QR_SerialNo = "QR-P000003", NetworkId = "Manufacturer",
                    Mst = "MST-NXSX-ST", OrgCode = "ORG-NXSX-ST", GenTimesNo = "GT2601010001", FlagMap = true, FlagUsed = true, Remark = "Đã phát hành in tem" },
                new Secret { SerialNo = "P000004", SecretNo = "SEC000004", QR_SerialNo = "QR-P000004", NetworkId = "Manufacturer",
                    Mst = "MST-NXSX-ST", OrgCode = "ORG-NXSX-ST", GenTimesNo = "GT2601010001", FlagMap = false, FlagUsed = false, Remark = "Chưa ghép serial" });
            await db.SaveChangesAsync();
        }

        // Ánh xạ cặp tem (Map_StampPair của InBrandCloud eTEM) — ghép tem sản phẩm (IDNo) với tem hộp (BoxNo).
        if (!await db.StampPairs.AnyAsync())
        {
            db.StampPairs.AddRange(
                new StampPair { IDNo = "P000001", BoxNo = "B2601010001", NetworkId = "Manufacturer", PIN = "PIN00001",
                    QR_ID = "P000001", QR_BoxNo = "B2601010001", Remark = "Cặp tem mẫu 1 — tem SP P000001 ↔ tem hộp B2601010001" },
                new StampPair { IDNo = "P000002", BoxNo = "B2601010002", NetworkId = "Manufacturer", PIN = "PIN00002",
                    QR_ID = "P000002", QR_BoxNo = "B2601010002", Remark = "Cặp tem mẫu 2" });
            await db.SaveChangesAsync();
        }

        // Định danh sản phẩm (Prd_ProductID của InBrandCloud ProductCenter) — sản phẩm đã bán gắn mã định danh + bảo hành.
        if (!await db.ProductIds.AnyAsync())
        {
            db.ProductIds.AddRange(
                new ProductId { ProductID = "PID-ST25-0001", SpecCode = "8930001001", ProductionDate = "2026-01-05", LotNo = "L2026-001",
                    BuyDate = "2026-01-20", SecretNo = "SEC000001", WarrantyStartDate = "2026-01-20", WarrantyExpiredDate = "2027-01-20",
                    WarrantyDuration = "12 tháng", Buyer = "Nguyễn Văn A", NetworkProductIdCode = "ELTS.PID.0001", Status = ProductIdStatus.Ok,
                    RefNo1 = "HD26012001", RefBiz1 = "Hóa đơn bán", Remark = "Túi gạo ST25 bán lẻ — còn bảo hành" },
                new ProductId { ProductID = "PID-ST25-0002", SpecCode = "8930001001", ProductionDate = "2026-01-05", LotNo = "L2026-001",
                    BuyDate = "2026-02-02", SecretNo = "SEC000002", WarrantyStartDate = "2026-02-02", WarrantyExpiredDate = "2027-02-02",
                    WarrantyDuration = "12 tháng", Buyer = "Trần Thị B", Status = ProductIdStatus.Checking,
                    RefNo1 = "HD26020215", RefBiz1 = "Hóa đơn bán", Remark = "Khách báo lỗi bao bì — đang kiểm tra" },
                new ProductId { ProductID = "PID-ST25-0003", SpecCode = "8930001001", ProductionDate = "2025-11-10", LotNo = "L2025-088",
                    BuyDate = "2025-11-25", SecretNo = "SEC000003", WarrantyStartDate = "2025-11-25", WarrantyExpiredDate = "2026-11-25",
                    WarrantyDuration = "12 tháng", Buyer = "Lê Văn C", Status = ProductIdStatus.Repairing,
                    RefNo1 = "HD25112533", RefBiz1 = "Hóa đơn bán", Remark = "Đang sửa chữa/đổi trả" });
            await db.SaveChangesAsync();
        }

        // Cấu hình trường hiển thị khi tra cứu (Mst_ConfigColumnSearch của InBrandCloud eTEM) — "từ điển" cột hiển thị cho màn tra cứu.
        if (!await db.ConfigColumnSearches.AnyAsync())
        {
            db.ConfigColumnSearches.AddRange(
                new ConfigColumnSearch { CoumnID = "ProductName", TabID = "TAB_PRODUCT", TabName = "Thông tin sản phẩm", NetworkId = "Manufacturer", TypeId = "Mst_Product", IdxInTab = 1, ColumnDesc = "Tên sản phẩm", FlagView = true, FlagOsOrgView = true, FlagShow = true, EsColumnId = "productName" },
                new ConfigColumnSearch { CoumnID = "ProductOrigin", TabID = "TAB_PRODUCT", TabName = "Thông tin sản phẩm", NetworkId = "Manufacturer", TypeId = "Mst_Product", IdxInTab = 2, ColumnDesc = "Xuất xứ", FlagView = true, FlagOsOrgView = true, FlagShow = true, EsColumnId = "productOrigin" },
                new ConfigColumnSearch { CoumnID = "ProductionLotNo", TabID = "TAB_TRACE", TabName = "Hành trình truy xuất", NetworkId = "Manufacturer", TypeId = "Inv_InventoryVerifiedID", IdxInTab = 1, ColumnDesc = "Số lô sản xuất", FlagView = true, FlagOsOrgView = true, FlagShow = true, EsColumnId = "productionLotNo" },
                new ConfigColumnSearch { CoumnID = "ProductionDate", TabID = "TAB_TRACE", TabName = "Hành trình truy xuất", NetworkId = "Manufacturer", TypeId = "Inv_InventoryVerifiedID", IdxInTab = 2, ColumnDesc = "Ngày sản xuất", FlagView = true, FlagOsOrgView = true, FlagShow = true, EsColumnId = "productionDate" },
                new ConfigColumnSearch { CoumnID = "VerifyCount", TabID = "TAB_TRACE", TabName = "Hành trình truy xuất", NetworkId = "Manufacturer", TypeId = "Inv_InventoryVerifiedID", IdxInTab = 3, ColumnDesc = "Số lần quét", FlagView = true, FlagOsOrgView = false, FlagShow = true, EsColumnId = "verifyCount" },
                new ConfigColumnSearch { CoumnID = "CustomerName", TabID = "TAB_DISTRIBUTION", TabName = "Lịch sử phân phối", NetworkId = "Distributor", TypeId = "InvF_InventoryHistInOutID", IdxInTab = 1, ColumnDesc = "Khách hàng nhận", FlagView = true, FlagOsOrgView = false, FlagShow = true, EsColumnId = "customerName" },
                new ConfigColumnSearch { CoumnID = "AreaName", TabID = "TAB_DISTRIBUTION", TabName = "Lịch sử phân phối", NetworkId = "Distributor", TypeId = "InvF_InventoryHistInOutID", IdxInTab = 2, ColumnDesc = "Vùng thị trường", FlagView = true, FlagOsOrgView = false, FlagShow = true, EsColumnId = "areaName" },
                new ConfigColumnSearch { CoumnID = "DriverName", TabID = "TAB_DISTRIBUTION", TabName = "Lịch sử phân phối", NetworkId = "Distributor", TypeId = "InvF_InventoryHistInOutID", IdxInTab = 3, ColumnDesc = "Tài xế vận chuyển", FlagView = true, FlagOsOrgView = false, FlagShow = false, EsColumnId = "driverName" });
            await db.SaveChangesAsync();
        }

        // Sản phẩm đã sản xuất / Dãy sản xuất (Inv_InventoryManufacturedID của InBrandCloud eTEM) — ghi nhận tem đã sản xuất.
        if (!await db.ManufacturedIds.AnyAsync())
        {
            db.ManufacturedIds.AddRange(
                new ManufacturedId { IDNo = "P000001", IManufacturedIDNo = "MFG2601010001", NetworkId = "Manufacturer", BoxNo = "B2601010001",
                    LineCode = "LINE-A1", LineRootCode = "LINE-A", ShiftCode = "CA1", ProductionLotNo = "L2026-001", RefNoLine = "REF-A1-001",
                    ProductCode = "8930001001", InvCode = "KHO-FG-ST", ManufactureStartDTime = DateTime.Now.AddDays(-20), MobileScanDTime = DateTime.Now.AddDays(-20),
                    MobileIndex = 1, FlagMap = true, Status = ManufacturedStatus.Completed, CompleteDTime = DateTime.Now.AddDays(-20), CompleteBy = "Nguyễn Văn KCS",
                    Remark = "Tem P000001 sản xuất trên dây chuyền A1 ca 1" },
                new ManufacturedId { IDNo = "P000002", IManufacturedIDNo = "MFG2601010001", NetworkId = "Manufacturer", BoxNo = "B2601010001",
                    LineCode = "LINE-A1", LineRootCode = "LINE-A", ShiftCode = "CA1", ProductionLotNo = "L2026-001", RefNoLine = "REF-A1-002",
                    ProductCode = "8930001001", InvCode = "KHO-FG-ST", ManufactureStartDTime = DateTime.Now.AddDays(-20), MobileScanDTime = DateTime.Now.AddDays(-20),
                    MobileIndex = 2, FlagMap = true, Status = ManufacturedStatus.Completed, CompleteDTime = DateTime.Now.AddDays(-20), CompleteBy = "Nguyễn Văn KCS",
                    Remark = "Tem P000002 sản xuất trên dây chuyền A1 ca 1" },
                new ManufacturedId { IDNo = "P000003", IManufacturedIDNo = "MFG2601010002", NetworkId = "Manufacturer", BoxNo = "B2601010001",
                    LineCode = "LINE-A2", LineRootCode = "LINE-A", ShiftCode = "CA2", ProductionLotNo = "L2026-001", RefNoLine = "REF-A2-001",
                    ProductCode = "8930001001", InvCode = "KHO-FG-ST", ManufactureStartDTime = DateTime.Now.AddDays(-19), MobileScanDTime = DateTime.Now.AddDays(-19),
                    MobileIndex = 1, FlagMap = true, Status = ManufacturedStatus.InProgress, Remark = "Tem P000003 đang sản xuất trên dây chuyền A2 ca 2" });
            await db.SaveChangesAsync();
        }

        // Danh mục mạng lưới / môi trường (MstSv_Mst_Network của InBrandCloud eTEM) — "từ điển" mạng dùng để định tuyến đồng bộ eTEM/ELTS.
        if (!await db.NetworkMasters.AnyAsync())
        {
            db.NetworkMasters.AddRange(
                new NetworkMaster { NetworkID = "4341766000", NetworkName = "Mạng sản xuất chính (Real)", GroupNetworkID = "REAL",
                    CoreAddr = "https://core.inos.vn", PingAddr = "https://ping.inos.vn", XSysAddr = "https://xsys.inos.vn",
                    WSUrlAddr = "https://syscm01.inos.vn/idocNet.Real.Skycic.InBrand.V20.4341766000.WA/",
                    WSUrlAddrNew = "https://syscm01.inos.vn/idocNet.Real.Skycic.InBrand.V20.4341766000.New.WA/",
                    DBUrlAddr = "192.168.1.228\\SQLSV2016", Mst = "MST-NXSX-ST", OrgIdSln = "ORG-NXSX-ST", MinVersion = "20201209", Active = true },
                new NetworkMaster { NetworkID = "9452386000", NetworkName = "Mạng Bình Điền - Ninh Bình", GroupNetworkID = "REAL",
                    WSUrlAddr = "https://syscm01.inos.vn/idocNet.Real.Skycic.InBrand.V20.9452386000.WA/",
                    Mst = "2700664419", MinVersion = "20201209", Active = true },
                new NetworkMaster { NetworkID = "TEST0001", NetworkName = "Mạng kiểm thử (Test)", GroupNetworkID = "TEST",
                    WSUrlAddr = "https://syscm-test.inos.vn/idocNet.Test.Skycic.InBrand.V20.TEST0001.WA/",
                    Mst = "MST-DL-HCM", MinVersion = "20210611", Active = false, Remark = "Môi trường test — tạm ngưng" });
            await db.SaveChangesAsync();
        }

        // Lịch sử phân phối / nhập-xuất kho theo tem (InvF_InventoryHistInOutID của InBrandCloud eTEM) — hành trình phân phối mẫu.
        if (!await db.DistributionHistories.AnyAsync())
        {
            db.DistributionHistories.AddRange(
                new DistributionHistory { IF_InvInHistNo = "H2601010001", IDNo = "P000001", NetworkId = "Manufacturer", RefNo = "PN2601010001",
                    RefType = HistRefType.In, IF_InvInNo = "PN2601010001", InvCode = "KHO-FG-ST", ProductionDate = "2026-01-05", PackageDate = "2026-01-06",
                    ProductionLotNo = "L2026-001", ShiftInCode = "CA1", PrintDate = "2026-01-06", BoxNo = "B2601010001", UserKCS = "Nguyễn Văn KCS",
                    IVerifiedIDInOutNo = "IV2601010001", FlagIsError = false, Remark = "Nhập kho thành phẩm tem P000001" },
                new DistributionHistory { IF_InvInHistNo = "H2601100002", IDNo = "P000001", NetworkId = "Distributor", RefNo = "PX2601100002",
                    RefType = HistRefType.Out, IF_InvOutNo = "PX2601100002", InvFOutType = "SALE", InvCode = "KHO-FG-ST", ProductionLotNo = "L2026-001",
                    BoxNo = "B2601010001", OrgID_Customer = "MST-DL-HCM", CustomerCode = "DL-HCM-01", CustomerName = "Đại lý phân phối TP.HCM",
                    PlateNo = "51C-12345", MoocNo = "MOOC-01", DriverName = "Trần Văn Tài", DriverPhoneNo = "0903333444", AreaName = "MA-TPHCM",
                    IVerifiedIDInOutNo = "IV2601100002", FlagIsError = false, Remark = "Xuất kho → Đại lý cấp 1 TP.HCM" },
                new DistributionHistory { IF_InvInHistNo = "H2601120003", IDNo = "P000002", NetworkId = "Distributor", RefNo = "PX2601120003",
                    RefType = HistRefType.Out, IF_InvOutNo = "PX2601120003", InvFOutType = "SALE", InvCode = "KHO-FG-ST", ProductionLotNo = "L2026-001",
                    BoxNo = "B2601010001", OrgID_Customer = "MST-BANLE-COOP", CustomerCode = "COOP-Q1", CustomerName = "Siêu thị Co.opmart Q.1",
                    PlateNo = "51C-67890", DriverName = "Lê Văn Giao", DriverPhoneNo = "0905555666", AreaName = "MA-MIENN",
                    IVerifiedIDInOutNo = "IV2601120003", FlagIsError = false, Remark = "Xuất kho → điểm bán lẻ" },
                new DistributionHistory { IF_InvInHistNo = "H2601150004", IDNo = "P000003", NetworkId = "Distributor", RefNo = "PX2601150004",
                    RefType = HistRefType.Out, IF_InvOutNo = "PX2601150004", InvFOutType = "SALE", InvCode = "KHO-FG-ST", ProductionLotNo = "L2026-001",
                    BoxNo = "B2601010001", OrgID_Customer = "MST-DL-HCM", CustomerCode = "DL-HCM-01", CustomerName = "Đại lý phân phối TP.HCM",
                    PlateNo = "51C-12345", DriverName = "Trần Văn Tài", DriverPhoneNo = "0903333444", AreaName = "MA-TPHCM",
                    IVerifiedIDInOutNo = "IV2601150004", FlagIsError = true, Remark = "Ghép tem lỗi — cần kiểm tra lại" });
            await db.SaveChangesAsync();
        }

        // Danh mục đại lý / đơn vị phân phối (Mst_Dealer của InBrandCloud) — "từ điển" đại lý trong chuỗi cung ứng.
        if (!await db.Dealers.AnyAsync())
        {
            db.Dealers.AddRange(
                new Dealer { DLCode = "DL-HCM-01", DLName = "Đại lý phân phối TP.HCM", NetworkId = "Distributor", DLLevel = "1",
                    ProvinceCode = "79", DLType = "Cấp 1", DLAddress = "Q.1, TP.HCM", DLPresentBy = "Trần Thị B",
                    DLGovIDNumber = "0312345678", DLEmail = "b@dailyhcm.vn", DLPhoneNo = "0903333444", Active = true,
                    Remark = "Đại lý cấp 1 — nhận hàng trực tiếp từ nhà máy" },
                new Dealer { DLCode = "DL-CT-01", DLName = "Đại lý Cần Thơ", NetworkId = "Distributor", DLLevel = "1",
                    ProvinceCode = "92", DLType = "Cấp 1", DLAddress = "Ninh Kiều, Cần Thơ", DLPresentBy = "Nguyễn Văn D",
                    DLEmail = "d@dailyct.vn", DLPhoneNo = "0907777888", Active = true, Remark = "Đại lý khu vực ĐBSCL" },
                new Dealer { DLCode = "DL-HN-01", DLName = "Đại lý Hà Nội", NetworkId = "Distributor", DLLevel = "2",
                    ProvinceCode = "01", DLType = "Cấp 2", DLAddress = "Hoàng Mai, Hà Nội", DLPresentBy = "Lê Văn E",
                    DLEmail = "e@dailyhn.vn", DLPhoneNo = "0909999000", Active = false, Remark = "Đại lý cấp 2 — tạm ngưng" });
            await db.SaveChangesAsync();
        }

        // Danh mục dây chuyền sản xuất (Mst_ManufactureLine của InBrandCloud) — "từ điển" dây chuyền/máy sản xuất.
        if (!await db.ManufactureLines.AnyAsync())
        {
            db.ManufactureLines.AddRange(
                new ManufactureLine { LineCode = "LINE-01", LineName = "Dây chuyền chiết rót số 1", NetworkId = "Manufacturer",
                    LineRootCode = "LINE-01", LinePositionValue = "1", FlagRoot = true, Active = true,
                    LastCompletedID = "P000003" },
                new ManufactureLine { LineCode = "LINE-02", LineName = "Dây chuyền đóng gói số 2", NetworkId = "Manufacturer",
                    LineRootCode = "LINE-01", LinePositionValue = "2", FlagRoot = false, Active = true },
                new ManufactureLine { LineCode = "LINE-03", LineName = "Dây chuyền dán nhãn số 3", NetworkId = "Manufacturer",
                    LineRootCode = "LINE-01", LinePositionValue = "3", FlagRoot = false, Active = false });
            await db.SaveChangesAsync();
        }

        // Cảnh báo đồng bộ ElasticSearch (Wrn_WarningSyncES của InBrandCloud eTEM) — tem xuất kho nhưng chưa có trên chỉ mục ES.
        if (!await db.WarningSyncESs.AnyAsync())
        {
            db.WarningSyncESs.AddRange(
                new WarningSyncES { IVerifiedIDInOutNo = "IV2601100002", OrgCode = "MST-NXSX-ST", ProductCode = "8930001001",
                    RefNoSys = "PX2601100002", IDNo = "P000001", Remark = "CSC.SyncELTS.PXK", SyncStatus = WarningSyncStatus.Pending },
                new WarningSyncES { IVerifiedIDInOutNo = "IV2601120003", OrgCode = "MST-NXSX-ST", ProductCode = "8930001001",
                    RefNoSys = "PX2601120003", IDNo = "P000002", Remark = "CSC.SyncELTS.PXK", SyncStatus = WarningSyncStatus.Pending },
                new WarningSyncES { IVerifiedIDInOutNo = "IV2601150004", OrgCode = "MST-NXSX-ST", ProductCode = "8930001001",
                    RefNoSys = "PX2601150004", IDNo = "P000003", Remark = "Ghép tem lỗi — đã đồng bộ lại",
                    SyncStatus = WarningSyncStatus.Synced, SyncedAt = DateTime.Now.AddDays(-1) });
            await db.SaveChangesAsync();
        }

        // Danh mục tỉnh/thành phố (Mst_Province của InBrandCloud) — "từ điển" đơn vị hành chính cấp tỉnh.
        if (!await db.Provinces.AnyAsync())
        {
            db.Provinces.AddRange(
                new Province { Code = "79", Name = "TP. Hồ Chí Minh", CountryCode = "VN", Active = true },
                new Province { Code = "92", Name = "Cần Thơ", CountryCode = "VN", Active = true },
                new Province { Code = "01", Name = "Hà Nội", CountryCode = "VN", Active = true },
                new Province { Code = "89", Name = "An Giang", CountryCode = "VN", Active = true },
                new Province { Code = "94", Name = "Sóc Trăng", CountryCode = "VN", Active = false });
            await db.SaveChangesAsync();
        }
    }

    // Hash MD5 của "IDNo|PIN" (tương đương Inv_InventoryGenID_HashMD5 của InBrandCloud eTEM).
    private static string Md5(string input)
    {
        var bytes = System.Security.Cryptography.MD5.HashData(System.Text.Encoding.ASCII.GetBytes(input));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    private static async Task MigratePostgresAsync(AppDbContext db)
    {
        if (!db.Database.IsNpgsql()) return;
        var def = TenantContext.DefaultOrgId;
        var tables = new[] { "Products", "Units", "Events", "Verifications", "Ctes", "Kdes", "DataTypes", "CteKdes", "Glns", "OrgGlns", "Farms", "MarketAreas", "Templates", "TplNwtCtes", "TplNwtKdes", "TplNwtCteKdes", "TplViewEvents", "Records", "RecordSpecs", "StampBatches", "Stamps", "Boxes", "BoxItems", "Cartons", "CartonItems", "QueSyncs", "MasterDatas", "NetworkOrgs", "Secrets", "StampPairs", "ProductIds", "ConfigColumnSearches", "ManufacturedIds", "NetworkMasters", "DistributionHistories", "Dealers", "ManufactureLines", "WarningSyncESs", "Provinces" };
        var sql = new List<string>
        {
            "CREATE TABLE IF NOT EXISTS minitrace.\"Orgs\" (\"Id\" uuid PRIMARY KEY, \"Name\" text NOT NULL DEFAULT '', \"ApiKey\" text NOT NULL DEFAULT '', \"CreatedAt\" timestamp NOT NULL DEFAULT now())",
            "CREATE UNIQUE INDEX IF NOT EXISTS \"IX_Orgs_ApiKey\" ON minitrace.\"Orgs\" (\"ApiKey\")",
        };
        foreach (var t in tables) sql.Add($"ALTER TABLE minitrace.\"{t}\" ADD COLUMN IF NOT EXISTS \"OrgId\" uuid NOT NULL DEFAULT '{def}'");
        foreach (var s in sql) try { await db.Database.ExecuteSqlRawAsync(s); } catch { }
    }
}
