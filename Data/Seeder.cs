using Microsoft.EntityFrameworkCore;
using MiniTrace.Models;
using MiniTrace.Services;

namespace MiniTrace.Data;

public static class Seeder
{
    public static async Task SeedAsync(AppDbContext db)
    {
        await db.Database.EnsureCreatedAsync();
        // 🔴 BẪY #28: EnsureCreated là NO-OP khi .db đã tồn tại ⇒ bảng/cột thêm
        // sau đó KHÔNG được tạo, build xanh mà chạy lên chết "no such table".
        await MigrateSqliteAuto.RunAsync(db);
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

        await SeedStampMapAsync(db);
    }

    /// <summary>
    /// Dữ liệu mồi cho cụm ghép tem port 1:1 từ 2017.A.iNOS.InBrand
    /// (Views/Inv_InventoryBalanceSerial — cụm này InBrandCloud không có).
    /// </summary>
    private static async Task SeedStampMapAsync(AppDbContext db)
    {
        // Từ điển mã lỗi. MÃ lấy nguyên văn từ TConst.ErrMapCode (Const.Biz.cs:51-67).
        // CẢNH BÁO TRUNG THỰC: phần TÊN lỗi (ErrMapName) nằm ở DỮ LIỆU bảng
        // Inv_ErrMapInInventory trong CSDL nguồn, KHÔNG có trong source code
        // (BaseController.cs:867-876 chỉ đọc từ service). Tên dưới đây do tôi đặt theo NGHĨA của mã,
        // KHÔNG trích được từ nguồn — cần lấy lại từ DB thật khi có kết nối.
        if (!await db.ErrMaps.AnyAsync())
        {
            const string rm = "Tên đặt theo nghĩa mã; chưa lấy được từ DB nguồn";
            db.ErrMaps.AddRange(
                new InvErrMapInInventory { ErrMapCode = ErrMapCode.SeriKhongCo, ErrMapName = "Serial không có trong kho sinh số", Remark = rm },
                new InvErrMapInInventory { ErrMapCode = ErrMapCode.SeriKoCoKhoXT, ErrMapName = "Serial không có trong kho xuất", Remark = rm },
                new InvErrMapInInventory { ErrMapCode = ErrMapCode.SeriDaCo, ErrMapName = "Serial đã ghép trước đó", Remark = rm },
                new InvErrMapInInventory { ErrMapCode = ErrMapCode.SeriDaMap, ErrMapName = "Serial đã ghép thùng", Remark = rm },
                new InvErrMapInInventory { ErrMapCode = ErrMapCode.SeriDaBan, ErrMapName = "Serial đã bán", Remark = rm },
                new InvErrMapInInventory { ErrMapCode = ErrMapCode.SPKhongCo, ErrMapName = "Mã sản phẩm không tồn tại", Remark = rm },
                new InvErrMapInInventory { ErrMapCode = ErrMapCode.DLKhongCo, ErrMapName = "Mã đại lý không tồn tại", Remark = rm },
                new InvErrMapInInventory { ErrMapCode = ErrMapCode.THUKhongCo, ErrMapName = "Mã thùng không tồn tại", Remark = rm },
                new InvErrMapInInventory { ErrMapCode = ErrMapCode.THUDaCo, ErrMapName = "Mã thùng đã dùng", Remark = rm },
                new InvErrMapInInventory { ErrMapCode = ErrMapCode.BOXKhongCo, ErrMapName = "Mã hộp không tồn tại", Remark = rm },
                new InvErrMapInInventory { ErrMapCode = ErrMapCode.BOXDaCo, ErrMapName = "Mã hộp đã dùng", Remark = rm },
                new InvErrMapInInventory { ErrMapCode = ErrMapCode.TrungMa, ErrMapName = "Trùng mã trong lô quét", Remark = rm },
                new InvErrMapInInventory { ErrMapCode = ErrMapCode.QuetSaiThuTu, ErrMapName = "Quét sai thứ tự", Remark = rm },
                new InvErrMapInInventory { ErrMapCode = ErrMapCode.QuetThieu, ErrMapName = "Quét thiếu", Remark = rm }
            );
            await db.SaveChangesAsync();
        }

        // Danh mục sản phẩm mồi (Mst_Part) — FlagActive = "1" để đi qua guard MstPartCheckDB
        // (InvInventoryBalanceSerialManager.cs:4879-4886).
        if (!await db.Parts.AnyAsync())
        {
            db.Parts.AddRange(
                new MstPart { PartCode = "SU-C108", PartName = "Bệt liền khối C108", PartType = "BET", PMType = StampMapService.DefaultPmType, FlagActive = SerialFlag.Active },
                new MstPart { PartCode = "SU-L501", PartName = "Chậu rửa L501", PartType = "CHAU", PMType = StampMapService.DefaultPmType, FlagActive = SerialFlag.Active },
                new MstPart { PartCode = "SU-VI88", PartName = "Vòi lavabo VI88", PartType = "VOI", PMType = StampMapService.DefaultPmType, FlagActive = SerialFlag.Active }
            );
            await db.SaveChangesAsync();
        }

        // Danh mục đại lý mồi (Mst_Agent) — cần cho guard DLKHONGCO của màn xuất hàng
        // (InvInvBalSerialOutInvProvider.cs:1301-1303).
        if (!await db.Agents.AnyAsync())
        {
            db.Agents.AddRange(
                new MstAgent { AgentCode = "DL-HN01", AgentName = "Đại lý Hà Nội 01", AgentAddress = "Hà Nội", FlagActive = SerialFlag.Active },
                new MstAgent { AgentCode = "DL-HCM02", AgentName = "Đại lý TP.HCM 02", AgentAddress = "TP.HCM", FlagActive = SerialFlag.Active }
            );
            await db.SaveChangesAsync();
        }

        // Kho sinh số bí mật (Inv_InventorySecret): serial chỉ ghép được nếu đã nằm ở đây
        // (guard tại InvInventoryBalanceSerialManager.cs:4901-4925 — phải có ĐÚNG 1 dòng).
        if (!await db.Secrets.AnyAsync())
        {
            for (int i = 1; i <= 12; i++)
                db.Secrets.Add(new InvInventorySecret
                {
                    SerialNo_Actual = $"{StampMapService.PrefixSerial}VGC2026{i:D4}",
                    SecretNo = $"{100000 + i * 7777}"
                });
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

        // Bảng mới của cụm ghép tem (port 1:1 iNOS.InBrand). EnsureCreated KHÔNG tạo thêm bảng
        // trên CSDL đã tồn tại, nên phải CREATE TABLE IF NOT EXISTS tay — cùng cách nhà đang làm ở trên.
        sql.Add($@"CREATE TABLE IF NOT EXISTS minitrace.""BalanceSerials"" (
            ""Id"" serial PRIMARY KEY, ""OrgId"" uuid NOT NULL DEFAULT '{def}',
            ""InvCode"" text NOT NULL DEFAULT '', ""PartCode"" text NOT NULL DEFAULT '', ""SerialNo"" text NOT NULL DEFAULT '',
            ""PMType"" text, ""SerialNo_Actual"" text, ""DLCode"" text, ""PartLotNo"" text, ""BoxNo"" text, ""CanNo"" text,
            ""AgentCode"" text, ""SecretNo"" text, ""WarrantyDateStart"" text, ""PackageDate"" text, ""ProductionDate"" text,
            ""UserBox"" text, ""UserCan"" text, ""UserKCS"" text, ""UserCheckPart"" text, ""BlockStatus"" text, ""FlagNG"" text,
            ""FlagMap"" text, ""FlagUI"" text, ""FlagSales"" text, ""FlagBox"" text, ""FlagCan"" text, ""FormInType"" text,
            ""IF_InvInFGNo"" text, ""FormOutType"" text, ""IF_InvOutFGNo"" text, ""LUDTime"" text, ""LUBy"" text,
            ""CreateDTimeSv"" text, ""CreateBySv"" text, ""InvDTime"" text, ""InvBy"" text, ""BoxDTime"" text, ""BoxBy"" text,
            ""CanDTime"" text, ""CanBy"" text, ""OutDTime"" text, ""OutBy"" text, ""RefNo_Type"" text, ""RefNo_PK"" text,
            ""LogLUDTime"" text, ""LogLUBy"" text, ""CreateDTime"" timestamp NOT NULL DEFAULT now(), ""CreateBy"" text,
            ""UpdateDTime"" timestamp NOT NULL DEFAULT now(), ""UpdateBy"" text, ""OriginalInvCode"" text)");
        sql.Add($@"CREATE TABLE IF NOT EXISTS minitrace.""Secrets"" (""Id"" serial PRIMARY KEY, ""OrgId"" uuid NOT NULL DEFAULT '{def}',
            ""SerialNo_Actual"" text NOT NULL DEFAULT '', ""SecretNo"" text NOT NULL DEFAULT '', ""Remark"" text)");
        sql.Add($@"CREATE TABLE IF NOT EXISTS minitrace.""Parts"" (""Id"" serial PRIMARY KEY, ""OrgId"" uuid NOT NULL DEFAULT '{def}',
            ""PartCode"" text NOT NULL DEFAULT '', ""PartName"" text, ""PartType"" text, ""PMType"" text, ""FlagActive"" text)");
        sql.Add($@"CREATE TABLE IF NOT EXISTS minitrace.""Agents"" (""Id"" serial PRIMARY KEY, ""OrgId"" uuid NOT NULL DEFAULT '{def}',
            ""AgentCode"" text NOT NULL DEFAULT '', ""ProvinceCode"" text, ""DistrictCode"" text, ""AgentName"" text,
            ""AgentAddress"" text, ""FlagActive"" text)");
        sql.Add($@"CREATE TABLE IF NOT EXISTS minitrace.""CanDlcs"" (""Id"" serial PRIMARY KEY, ""OrgId"" uuid NOT NULL DEFAULT '{def}',
            ""CanNo"" text NOT NULL DEFAULT '', ""LogLUBy"" text, ""LogLUDTime"" timestamp NOT NULL DEFAULT now())");
        sql.Add($@"CREATE TABLE IF NOT EXISTS minitrace.""ErrMaps"" (""Id"" serial PRIMARY KEY, ""OrgId"" uuid NOT NULL DEFAULT '{def}',
            ""ErrMapCode"" text NOT NULL DEFAULT '', ""ErrMapName"" text, ""Remark"" text)");

        foreach (var s in sql) try { await db.Database.ExecuteSqlRawAsync(s); } catch { }
    }
}
