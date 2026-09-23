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
        var tables = new[] { "Products", "Units", "Events", "Verifications", "Ctes", "Kdes", "DataTypes", "CteKdes", "Glns", "OrgGlns", "Farms", "Templates", "TplNwtCtes", "TplNwtKdes", "TplNwtCteKdes", "TplViewEvents", "Records", "RecordSpecs", "StampBatches", "Stamps", "Boxes", "BoxItems", "QueSyncs", "MasterDatas", "NetworkOrgs" };
        var sql = new List<string>
        {
            "CREATE TABLE IF NOT EXISTS minitrace.\"Orgs\" (\"Id\" uuid PRIMARY KEY, \"Name\" text NOT NULL DEFAULT '', \"ApiKey\" text NOT NULL DEFAULT '', \"CreatedAt\" timestamp NOT NULL DEFAULT now())",
            "CREATE UNIQUE INDEX IF NOT EXISTS \"IX_Orgs_ApiKey\" ON minitrace.\"Orgs\" (\"ApiKey\")",
        };
        foreach (var t in tables) sql.Add($"ALTER TABLE minitrace.\"{t}\" ADD COLUMN IF NOT EXISTS \"OrgId\" uuid NOT NULL DEFAULT '{def}'");
        foreach (var s in sql) try { await db.Database.ExecuteSqlRawAsync(s); } catch { }
    }
}
