using Microsoft.EntityFrameworkCore;
using MiniTrace.Data;
using MiniTrace.Models;

namespace MiniTrace.Services;

// =====================================================================================
// Port 1:1 ba màn ghép tem của 2017.A.iNOS.InBrand — cụm Views/Inv_InventoryBalanceSerial.
//
// Mỗi màn có ĐÚNG 3 bước, giống nguồn:
//   B1 ScanParse*  : đọc dòng máy quét, phán lỗi phía client
//                    (nguồn: Inv_InventoryBalanceSerialController.cs, hàm Import*)
//   B2 Calc*       : phán lỗi phía CSDL
//                    (nguồn: idn.iNOS.InBrand.V10.biz/Persistance/SqlServer/InvInvBalSerial*Provider.cs)
//   B3 Save*       : ghi các dòng người dùng tích chọn
//                    (nguồn: idn.iNOS.InBrand.V10.biz/Services/InvInventoryBalanceSerialManager.cs)
//
// LƯU Ý THUẬT NGỮ (đã kiểm ở nguồn, KHÔNG suy diễn):
//   BoxNo = mã HỘP   (InvInventoryBalanceSerial.cs:42)
//   CanNo = mã THÙNG (InvInventoryBalanceSerial.cs:44 và region
//                     Inv_InventoryBalanceSerialController.cs:1838 "Map Serial No - Mã thùng")
// =====================================================================================

public interface IStampMapService
{
    // Màn 1 — MapSerialNoPartCode.cshtml
    Task<List<ScanRow>> ScanParseSerialPartAsync(string[] lines);
    Task<(int saved, string msg)> SaveMapSerialPartAsync(List<ScanRow> rows);

    // Màn 2 — MapSerialNoCanNo.cshtml
    Task<List<ScanRow>> ScanParseSerialCanAsync(string[] lines);
    Task<(int saved, string msg)> SaveMapSerialCanAsync(List<ScanRow> rows, string userCan, string userKcs);

    // Màn 3 — MapCanNoAgentCode.cshtml
    Task<List<ScanRow>> ScanParseCanAgentAsync(string[] lines);
    Task<(int saved, string msg)> SaveOutInvAsync(List<ScanRow> rows);

    Task<string> ErrNameAsync(string? code);
}

public class StampMapService(AppDbContext db) : IStampMapService
{
    // Tiền tố nhận dạng tem serial — nguồn đọc AppSettings["IsSerial"]
    // (Inv_InventoryBalanceSerialController.cs:712-721), mặc định cứng trong code nguồn
    // là "http://pms.viglacera.vn/pi?qr="; Web.config đang bật bản test (Web.config:26).
    public const string PrefixSerial = "https://testvgc.inos.vn:12289/pi?qr=";

    // Tiền tố nhận dạng mã THÙNG — nguồn đọc AppSettings["IsCask"], mặc định cứng "THU|VGC"
    // (Inv_InventoryBalanceSerialController.cs:3893-3899; Web.config:31).
    public const string PrefixCask = "THU|VGC";

    // Mã kho của người dùng. Nguồn lấy từ Sys_User_GetListAbilityViewOfUser(...)[0].MDInvCode
    // (InvInventoryBalanceSerialManager.cs:4930 tương ứng dòng 4844-4846 trong bản đang đọc).
    // Đích chưa có bảng phân quyền kho/đơn vị ⇒ dùng 1 kho mặc định (đã khai ở mục 5 báo cáo).
    public const string DefaultInvCode = "HUNGYEN";
    public const string DefaultDlCode = "HUNGYEN";
    public const string DefaultPmType = "SU";
    private string UserId => "minitrace";

    private static string Now() => DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

    // BAY DA CHAN (xem bao cao muc 5): nguon chay SQL Server voi collation KHONG phan biet
    // hoa/thuong, con SQLite so chuoi PHAN BIET hoa/thuong (BINARY) => cung mot phep join
    // se tra 0 dong o dich. Vi vay moi phep so MA deu chuan hoa ve chu HOA o CA HAI phia.
    private static string U(string? v) => (v ?? "").Trim().ToUpperInvariant();

    public async Task<string> ErrNameAsync(string? code)
    {
        if (string.IsNullOrWhiteSpace(code)) return "";
        var c = code.Trim().ToUpperInvariant();
        // Nguồn: BaseController.cs:878-895 — tra bảng Inv_ErrMapInInventory, không có thì trả rỗng.
        var row = await db.ErrMaps.FirstOrDefaultAsync(e => e.ErrMapCode.ToUpper() == c);
        return row?.ErrMapName ?? "";
    }

    // =================================================================================
    // MÀN 1 — "Ghép Mã QR - Thông tin SP bộ"
    // Nguồn trang: Views/Inv_InventoryBalanceSerial/MapSerialNoPartCode.cshtml:3 (tiêu đề)
    //            + partial SerialNoPartCode.cshtml (lưới 5 cột)
    // Thứ tự quét ĐÚNG: Mã sản phẩm → Serial (Inv_InventoryBalanceSerialController.cs:623)
    // =================================================================================
    public async Task<List<ScanRow>> ScanParseSerialPartAsync(string[] lines)
    {
        var rows = new List<ScanRow>();
        var seenPart = new List<string>();
        var seenSerial = new List<string>();
        string partcode = "";
        // "nhóm hiện tại": tương ứng đối tượng SerialNoPartCode của nguồn
        // (ModelsUI/InvInventoryBalanceSerialUI.cs:20) — 1 mã SP kèm danh sách serial.
        string groupPart = ""; string groupFlagPart = ""; string groupErr = "";
        bool hasGroup = false; int groupCount = 0;
        int idx = 0;

        foreach (var raw in lines)
        {
            if (string.IsNullOrWhiteSpace(raw)) continue;
            var line = raw.Trim();

            // Nguồn dòng 732-733: chỉ coi là serial khi tiền tố nằm ở VỊ TRÍ 0, không phải "có chứa".
            var isSerial = line.ToLowerInvariant().IndexOf(PrefixSerial, StringComparison.Ordinal) == 0;

            if (isSerial)
            {
                var r = new ScanRow { Idx = idx.ToString(), PartCode = partcode, SerialNo_Actual = line };
                if (!hasGroup)
                {
                    // Nguồn dòng 738-761: chưa quét mã SP nào ⇒ QUETSAITHUTU.
                    r.InputStatus = SerialFlag.No;
                    r.ErrMapCode = ErrMapCode.QuetSaiThuTu;
                    hasGroup = true; groupPart = ""; groupFlagPart = SerialFlag.No;
                    groupErr = ErrMapCode.QuetSaiThuTu;
                }
                else if (!string.IsNullOrEmpty(groupPart) && groupFlagPart == SerialFlag.Yes)
                {
                    // Nguồn dòng 775-800: nhóm hợp lệ ⇒ chỉ còn kiểm trùng serial trong lô quét.
                    if (seenSerial.Contains(line))
                    {
                        r.InputStatus = SerialFlag.No;
                        r.ErrMapCode = ErrMapCode.TrungMa;
                    }
                    else { seenSerial.Add(line); r.InputStatus = SerialFlag.Yes; }
                }
                else
                {
                    // Nguồn dòng 801-816: nhóm đã sai ⇒ dòng con sai theo, kế thừa mã lỗi của nhóm.
                    r.InputStatus = SerialFlag.No;
                    r.ErrMapCode = string.IsNullOrEmpty(groupErr) ? ErrMapCode.QuetSaiThuTu : groupErr;
                }
                r.ErrMapName = await ErrNameAsync(r.ErrMapCode);
                rows.Add(r); groupCount++; idx++;
            }
            else
            {
                // Dòng không phải serial ⇒ coi là MÃ SẢN PHẨM (nguồn dòng 825-830).
                partcode = line;
                if (hasGroup && groupCount == 0)
                {
                    // Nguồn dòng 849-869: nhóm trước không có serial nào ⇒ sinh 1 dòng lỗi QUETTHIEU.
                    rows.Add(new ScanRow
                    {
                        Idx = idx.ToString(), PartCode = groupPart, SerialNo_Actual = "",
                        InputStatus = SerialFlag.No,
                        ErrMapCode = ErrMapCode.QuetThieu,
                        ErrMapName = await ErrNameAsync(ErrMapCode.QuetThieu)
                    });
                    idx++;
                }

                // Nguồn dòng 888-912: trùng mã SP trong cùng lô quét ⇒ cả nhóm TRUNGMA.
                var dup = seenPart.Contains(partcode);
                if (!dup) seenPart.Add(partcode);
                hasGroup = true; groupCount = 0; groupPart = partcode;
                groupFlagPart = dup ? SerialFlag.No : SerialFlag.Yes;
                groupErr = dup ? ErrMapCode.TrungMa : "";
            }
        }

        // Nguồn dòng 922-945: sau vòng lặp, nhóm cuối không có serial ⇒ thêm dòng QUETTHIEU.
        if (hasGroup && groupCount == 0)
            rows.Add(new ScanRow
            {
                Idx = idx.ToString(), PartCode = groupPart, SerialNo_Actual = "",
                InputStatus = SerialFlag.No,
                ErrMapCode = ErrMapCode.QuetThieu,
                ErrMapName = await ErrNameAsync(ErrMapCode.QuetThieu)
            });

        await CalcMapSerialPartAsync(rows);
        return rows;
    }

    /// <summary>
    /// Phán lỗi phía CSDL cho màn 1. Port 1:1 SQL sống
    /// <c>InvInvBalSerialMapSerialPart</c> — InvInvBalSerialMapSerialPartProvider.cs:246-484.
    /// (bản <c>_Old20180205</c> ở dòng 38 là MÃ CHẾT, không dùng.)
    /// Thứ tự ưu tiên mã lỗi theo CASE ở dòng 421-427.
    /// </summary>
    private async Task CalcMapSerialPartAsync(List<ScanRow> rows)
    {
        // Nguồn chỉ đẩy các dòng InputStatus="1" xuống biz
        // (Inv_InventoryBalanceSerialController.cs:987-1013).
        var candidates = rows.Where(r => r.InputStatus == SerialFlag.Yes).ToList();
        if (candidates.Count == 0) return;

        var parts = candidates.Select(r => U(r.PartCode)).Distinct().ToList();
        var serials = candidates.Select(r => U(r.SerialNo_Actual)).Distinct().ToList();

        // #tbl_Mst_Part_Filter — Provider dòng 300-310 (join Mst_Part theo PMType được xem).
        var partOk = await db.Parts.Where(p => parts.Contains(p.PartCode.ToUpper())).Select(p => p.PartCode.ToUpper()).ToListAsync();
        // #tbl_Inv_InventorySecret_Filter — Provider dòng 314-323.
        var secretOk = await db.Secrets.Where(s => serials.Contains(s.SerialNo_Actual.ToUpper())).Select(s => s.SerialNo_Actual.ToUpper()).ToListAsync();
        // #tbl_Inv_InventoryBalanceSerial_Filter — Provider dòng 326-335 (serial ĐÃ ghép rồi).
        var already = await db.BalanceSerials.Where(b => b.SerialNo_Actual != null && serials.Contains(b.SerialNo_Actual.ToUpper()))
                                             .Select(b => b.SerialNo_Actual!.ToUpper()).ToListAsync();

        foreach (var r in candidates)
        {
            string? err = null;
            if (!partOk.Contains(U(r.PartCode))) err = ErrMapCode.SPKhongCo;              // Provider dòng 423
            else if (!secretOk.Contains(U(r.SerialNo_Actual))) err = ErrMapCode.SeriKhongCo; // Provider dòng 424
            else if (already.Contains(U(r.SerialNo_Actual))) err = ErrMapCode.SeriDaCo;      // Provider dòng 425
            if (err == null) continue;
            r.InputStatus = SerialFlag.No; r.Status = SerialFlag.No;
            r.ErrMapCode = err; r.ErrMapName = await ErrNameAsync(err);
        }
    }

    /// <summary>
    /// Ghi màn 1. Port 1:1 <c>BuilTempInvInventoryBalanceSerialAddMultiMapSerialPart_New20180120</c>
    /// — InvInventoryBalanceSerialManager.cs:4821-5064 (gọi từ dòng 961-1010).
    /// </summary>
    public async Task<(int saved, string msg)> SaveMapSerialPartAsync(List<ScanRow> rows)
    {
        if (rows == null || rows.Count == 0) return (0, "Chưa chọn dữ liệu!");   // Controller nguồn dòng 649
        var now = Now();
        int n = 0;
        foreach (var r in rows)
        {
            var serial = (r.SerialNo_Actual ?? "").Trim();
            var part = (r.PartCode ?? "").Trim();

            // Guard 1 — Manager.cs:4888-4899: serial rỗng ⇒ InvalidSerialNo_Actual.
            if (string.IsNullOrEmpty(serial)) return (n, "Số serial không hợp lệ (rỗng).");

            // Guard 2 — Manager.cs:4901-4925: serial PHẢI có ĐÚNG 1 dòng trong kho sinh số,
            // và SecretNo được lấy từ đó (không tự sinh).
            var su = U(serial);
            var secrets = await db.Secrets.Where(s => s.SerialNo_Actual.ToUpper() == su).ToListAsync();
            if (secrets.Count != 1)
                return (n, $"Serial \"{serial}\" không có trong kho sinh số ({ErrMapCode.SeriKhongCo}).");

            // Guard 3 — Manager.cs:4863-4876 InvInventoryBalanceSerialCheckDB(strFlagExist = No):
            // serial CHƯA được phép tồn tại trong Inv_InventoryBalanceSerial.
            if (await db.BalanceSerials.AnyAsync(b => b.SerialNo_Actual != null && b.SerialNo_Actual.ToUpper() == su))
                return (n, $"Serial \"{serial}\" đã ghép trước đó ({ErrMapCode.SeriDaCo}).");

            // Guard 4 — Manager.cs:4879-4886 MstPartCheckDB(FlagExist = Yes, FlagActive = Active).
            var pu = U(part);
            if (!await db.Parts.AnyAsync(p => p.PartCode.ToUpper() == pu && p.FlagActive == SerialFlag.Active))
                return (n, $"Mã sản phẩm \"{part}\" không tồn tại/đã ngưng ({ErrMapCode.SPKhongCo}).");

            // Gán trường — bám ĐÚNG khối Manager.cs:4927-4979, kể cả các trường gán null.
            db.BalanceSerials.Add(new InvBalanceSerial
            {
                InvCode = DefaultInvCode,                 // Manager.cs:4930
                SerialNo = serial,                        // Manager.cs:4931 — SerialNo = SerialNo_Actual
                PartCode = part,
                SerialNo_Actual = serial,
                SecretNo = secrets[0].SecretNo,           // Manager.cs:4925
                PMType = DefaultPmType,                   // Manager.cs:4932
                DLCode = DefaultDlCode,                   // Manager.cs:4933
                PartLotNo = null,                         // Manager.cs:4934
                BoxNo = serial,                           // Manager.cs:4935 — mặc định = serial
                CanNo = serial,                           // Manager.cs:4936 — mặc định = serial
                AgentCode = null,                         // Manager.cs:4937
                WarrantyDateStart = null,                 // Manager.cs:4939
                PackageDate = now,                        // Manager.cs:4940
                ProductionDate = null,                    // Manager.cs:4941
                UserBox = null, UserCan = null, UserKCS = null, UserCheckPart = null, // Manager.cs:4942-4945
                BlockStatus = SerialFlag.No,              // Manager.cs:4946
                FlagNG = SerialFlag.No,                   // Manager.cs:4947
                FlagMap = SerialFlag.Yes,                 // Manager.cs:4948
                FlagUI = SerialFlag.Yes,                  // Manager.cs:4949
                FlagSales = SerialFlag.No,                // Manager.cs:4950
                FlagBox = SerialFlag.No,                  // Manager.cs:4951
                FlagCan = SerialFlag.No,                  // Manager.cs:4952
                FormInType = FormInType.Manual,           // Manager.cs:4953 — TConst.FormInType.Manual = "MANUAL"
                IF_InvInFGNo = null,                      // Manager.cs:4954
                FormOutType = null, IF_InvOutFGNo = null, // Manager.cs:4955-4956
                LUDTime = now, LUBy = UserId,             // Manager.cs:4957-4958
                CreateDTimeSv = now, CreateBySv = UserId, // Manager.cs:4959-4960
                InvDTime = now, InvBy = UserId,           // Manager.cs:4961-4962
                BoxDTime = null, BoxBy = null,            // Manager.cs:4963-4964
                CanDTime = null, CanBy = null,            // Manager.cs:4965-4966
                OutDTime = null, OutBy = null,            // Manager.cs:4967-4968
                RefNo_Type = null, RefNo_PK = null,       // Manager.cs:4969-4970
                LogLUDTime = now, LogLUBy = UserId,       // Manager.cs:4971-4972
                CreateBy = UserId, UpdateBy = UserId,     // Manager.cs:4974-4977
                OriginalInvCode = DefaultInvCode
            });
            n++;
        }
        await db.SaveChangesAsync();
        // Thông báo nguồn: MapSerialNoPartCode.cshtml:204.
        return (n, $"Kết nối Serial No - Thông tin sản phẩm thành công! ({n} tem)");
    }

    // =================================================================================
    // MÀN 2 — "Map Serial No - Mã thùng"
    // Nguồn trang: MapSerialNoCanNo.cshtml + partial SerialNoCanNo.cshtml (lưới 5 cột)
    // Thứ tự quét ĐÚNG: SerialNo → Thùng (Inv_InventoryBalanceSerialController.cs:1840)
    //   ⇒ NGƯỢC chiều màn 1 (màn 1 quét SP trước). Đây là điểm dễ port sai.
    // =================================================================================
    public async Task<List<ScanRow>> ScanParseSerialCanAsync(string[] lines)
    {
        var rows = new List<ScanRow>();
        var seenSerial = new List<string>();
        var pending = new List<ScanRow>();   // các serial đang chờ gán mã thùng
        int idx = 0;

        foreach (var raw in lines)
        {
            if (string.IsNullOrWhiteSpace(raw)) continue;
            var line = raw.Trim();
            var isSerial = line.ToLowerInvariant().IndexOf(PrefixSerial, StringComparison.Ordinal) == 0;

            if (isSerial)
            {
                var r = new ScanRow { Idx = idx.ToString(), SerialNo_Actual = line };
                if (seenSerial.Contains(line))
                {
                    r.InputStatus = SerialFlag.No; r.ErrMapCode = ErrMapCode.TrungMa;
                    r.ErrMapName = await ErrNameAsync(ErrMapCode.TrungMa);
                }
                else { seenSerial.Add(line); pending.Add(r); }
                rows.Add(r); idx++;
            }
            else
            {
                // Dòng không phải serial ⇒ MÃ THÙNG. Nguồn nhận dạng thùng bằng tiền tố
                // AppSettings["IsCask"] tách theo "|" (Controller nguồn dòng 1941-1952).
                var canno = line.ToUpperInvariant();
                if (pending.Count == 0)
                {
                    // Quét mã thùng mà chưa có serial nào chờ ⇒ QUETSAITHUTU.
                    rows.Add(new ScanRow
                    {
                        Idx = idx.ToString(), CanNo = canno, SerialNo_Actual = "",
                        InputStatus = SerialFlag.No,
                        ErrMapCode = ErrMapCode.QuetSaiThuTu,
                        ErrMapName = await ErrNameAsync(ErrMapCode.QuetSaiThuTu)
                    });
                    idx++;
                }
                else
                {
                    foreach (var p in pending) p.CanNo = canno;
                    pending.Clear();
                }
            }
        }
        // Serial còn treo, không có mã thùng ⇒ QUETTHIEU (tương ứng khối
        // "Kiểm tra thùng liền trước có detail không" — Controller nguồn dòng 2269-2320).
        foreach (var p in pending)
        {
            p.InputStatus = SerialFlag.No;
            p.ErrMapCode = ErrMapCode.QuetThieu;
            p.ErrMapName = await ErrNameAsync(ErrMapCode.QuetThieu);
        }

        await CalcMapSerialCanAsync(rows);
        return rows;
    }

    /// <summary>
    /// Phán lỗi CSDL màn 2. Port 1:1 SQL sống <c>InvInvBalSerialMapSerialCanCalc</c>
    /// — InvInvBalSerialMapSerialCanProvider.cs:255-520 (bản <c>_Old20180205</c> dòng 37 là mã chết).
    /// Thứ tự CASE: Provider dòng 457-462.
    /// </summary>
    private async Task CalcMapSerialCanAsync(List<ScanRow> rows)
    {
        var candidates = rows.Where(r => r.InputStatus == SerialFlag.Yes).ToList();
        if (candidates.Count == 0) return;

        var serials = candidates.Select(r => U(r.SerialNo_Actual)).Distinct().ToList();
        var cans = candidates.Select(r => U(r.CanNo)).Distinct().ToList();

        // #tbl_Inv_InventoryBalanceSerial_Filter — Provider dòng 300-312: serial phải CÓ trong kho.
        var inStock = await db.BalanceSerials
            .Where(b => b.SerialNo_Actual != null && serials.Contains(b.SerialNo_Actual.ToUpper()))
            .Select(b => new { Serial = b.SerialNo_Actual!.ToUpper(), b.FlagCan, b.FlagSales })
            .ToListAsync();

        // #tbl_Inv_InventoryBalanceSerial_CanNoDB — Provider dòng 340-345: mã thùng đã dùng.
        // GHI CHÚ NGUỒN (Provider dòng 345): 3 đơn vị SUBINHDUONG/SUVIETTRI/HUNGYEN được
        // MIỄN luật trùng mã thùng (vá 20220831 + 20230202, xử lý xuất điều chuyển).
        var exemptDl = new[] { "SUBINHDUONG", "SUVIETTRI", "HUNGYEN" };
        var canUsed = await db.BalanceSerials
            .Where(b => b.CanNo != null && cans.Contains(b.CanNo.ToUpper()) && (b.DLCode == null || !exemptDl.Contains(b.DLCode.ToUpper())))
            .Select(b => b.CanNo!.ToUpper()).ToListAsync();

        foreach (var r in candidates)
        {
            var hit = inStock.FirstOrDefault(x => x.Serial == U(r.SerialNo_Actual));
            string? err = null;
            if (canUsed.Contains(U(r.CanNo))) err = ErrMapCode.THUDaCo;               // Provider dòng 458
            else if (hit == null) err = ErrMapCode.SeriKoCoKhoXT;                        // Provider dòng 459
            else if (hit.FlagCan == SerialFlag.Active) err = ErrMapCode.SeriDaMap;       // Provider dòng 460
            else if (hit.FlagSales == SerialFlag.Active) err = ErrMapCode.SeriDaBan;     // Provider dòng 461
            if (err == null) continue;
            r.InputStatus = SerialFlag.No; r.Status = SerialFlag.No;
            r.ErrMapCode = err; r.ErrMapName = await ErrNameAsync(err);
        }
    }

    /// <summary>
    /// Ghi màn 2. Port 1:1 <c>InvInventoryBalanceSerialUpdMultiMapSerialCan_New20180120</c>
    /// — InvInventoryBalanceSerialManager.cs:3439-3580 (gọi từ dòng 1165-1210).
    /// </summary>
    public async Task<(int saved, string msg)> SaveMapSerialCanAsync(List<ScanRow> rows, string userCan, string userKcs)
    {
        if (rows == null || rows.Count == 0) return (0, "Chưa chọn dữ liệu!");
        userCan = (userCan ?? "").Trim();   // Manager.cs:3483
        userKcs = (userKcs ?? "").Trim();   // Manager.cs:3484
        var now = Now();
        int n = 0;

        foreach (var r in rows)
        {
            var serial = (r.SerialNo_Actual ?? "").Trim();
            var canno = (r.CanNo ?? "").Trim();

            // Guard 1 — Manager.cs:3503-3520: CanNo rỗng ⇒ lỗi.
            if (string.IsNullOrEmpty(canno)) return (n, "Mã thùng không hợp lệ (rỗng).");
            // Guard 2 — Manager.cs:3521-3535: UserCan (người đóng thùng) bắt buộc.
            if (string.IsNullOrEmpty(userCan)) return (n, "Chưa nhập người đóng thùng (UserCan).");

            // Guard 3 — Manager.cs:3489-3499 InvInventoryBalanceSerialCheckDB:
            // serial PHẢI tồn tại, FlagSales = 0 và FlagCan = 0.
            var su = U(serial);
            var db_row = await db.BalanceSerials.FirstOrDefaultAsync(b => b.SerialNo_Actual != null && b.SerialNo_Actual.ToUpper() == su);
            if (db_row == null) return (n, $"Serial \"{serial}\" không có trong kho ({ErrMapCode.SeriKoCoKhoXT}).");
            if (db_row.FlagCan == SerialFlag.Active) return (n, $"Serial \"{serial}\" đã ghép thùng ({ErrMapCode.SeriDaMap}).");
            if (db_row.FlagSales == SerialFlag.Active) return (n, $"Serial \"{serial}\" đã bán ({ErrMapCode.SeriDaBan}).");

            // Guard 4 — Manager.cs:3540-3555: phiếu nhập tay (FormInType = M) thì UserKCS bắt buộc.
            if (db_row.FormInType == FormInType.Manual && string.IsNullOrEmpty(userKcs))
                return (n, "Phiếu nhập tay: phải nhập User KCS.");

            // Cập nhật 6 trường — Manager.cs:3572-3577.
            db_row.CanNo = canno;
            db_row.UserCan = userCan;
            db_row.UserKCS = userKcs;
            db_row.CanDTime = now;
            db_row.CanBy = UserId;
            db_row.FlagCan = SerialFlag.Active;
            db_row.LUDTime = now; db_row.LUBy = UserId;
            db_row.UpdateDTime = DateTime.Now; db_row.UpdateBy = UserId;
            n++;
        }
        await db.SaveChangesAsync();
        return (n, $"Ghép Serial No - Mã thùng thành công! ({n} tem)");
    }

    // =================================================================================
    // MÀN 3 — "Xuất hàng (Trừ kho thành phẩm)"
    // Nguồn trang: MapCanNoAgentCode.cshtml + partial CanNoAgentCode.cshtml
    // Thứ tự quét ĐÚNG: Mã thùng → Mã đại lý (Inv_InventoryBalanceSerialController.cs:3802)
    // =================================================================================
    public async Task<List<ScanRow>> ScanParseCanAgentAsync(string[] lines)
    {
        var rows = new List<ScanRow>();
        // Nguồn dòng 3893-3902: tiền tố thùng lấy từ AppSettings["IsCask"], tách theo "|",
        // và dòng nào có tiền tố ở VỊ TRÍ 0 thì là thùng.
        var prefixes = PrefixCask.Split('|', StringSplitOptions.RemoveEmptyEntries);
        var pending = new List<ScanRow>();
        var seenCan = new List<string>();
        int idx = 0;

        foreach (var raw in lines)
        {
            if (string.IsNullOrWhiteSpace(raw)) continue;
            var line = raw.ToUpperInvariant().Trim();   // nguồn dòng 3906: ToUpper().Trim()
            var isCask = prefixes.Any(p => line.IndexOf(p, StringComparison.Ordinal) == 0);

            if (isCask)
            {
                var r = new ScanRow { Idx = idx.ToString(), CanNo = line };
                if (seenCan.Contains(line))
                {
                    r.InputStatus = SerialFlag.No; r.ErrMapCode = ErrMapCode.TrungMa;
                    r.ErrMapName = await ErrNameAsync(ErrMapCode.TrungMa);
                }
                else { seenCan.Add(line); pending.Add(r); }
                rows.Add(r); idx++;
            }
            else
            {
                // Không phải thùng ⇒ MÃ ĐẠI LÝ, gán cho mọi thùng đang chờ.
                if (pending.Count == 0)
                {
                    rows.Add(new ScanRow
                    {
                        Idx = idx.ToString(), AgentCode = line, CanNo = "",
                        InputStatus = SerialFlag.No,
                        ErrMapCode = ErrMapCode.QuetSaiThuTu,
                        ErrMapName = await ErrNameAsync(ErrMapCode.QuetSaiThuTu)
                    });
                    idx++;
                }
                else
                {
                    foreach (var p in pending) p.AgentCode = line;
                    pending.Clear();
                }
            }
        }
        foreach (var p in pending)
        {
            p.InputStatus = SerialFlag.No;
            p.ErrMapCode = ErrMapCode.QuetThieu;
            p.ErrMapName = await ErrNameAsync(ErrMapCode.QuetThieu);
        }

        await CalcOutInvAsync(rows);
        return rows;
    }

    /// <summary>
    /// Phán lỗi CSDL màn 3. Port 1:1 SQL sống <c>InvInvBalSerialOutInv</c>
    /// — InvInvBalSerialOutInvProvider.cs:1141-1627.
    /// (3 bản chết cùng file: <c>_Old20180205</c>:38, <c>_Old20180316</c>:353, <c>_Old20230206</c>:730.)
    /// Nhánh theo CanNo: CASE Provider dòng 1458-1464.
    /// </summary>
    private async Task CalcOutInvAsync(List<ScanRow> rows)
    {
        var candidates = rows.Where(r => r.InputStatus == SerialFlag.Yes).ToList();
        if (candidates.Count == 0) return;

        var cans = candidates.Select(r => U(r.CanNo)).Distinct().ToList();
        var agents = candidates.Select(r => U(r.AgentCode)).Distinct().ToList();

        // #tbl_Mst_Agent_RS — Provider dòng 1301-1303: đại lý phải có trong Mst_Agent.
        var agentOk = await db.Agents.Where(a => agents.Contains(a.AgentCode.ToUpper())).Select(a => a.AgentCode.ToUpper()).ToListAsync();
        // #tbl_Inv_InventoryBalanceSerial_CanNoDB / CanFlagSales — Provider dòng 1272-1290.
        var canRows = await db.BalanceSerials
            .Where(b => b.CanNo != null && cans.Contains(b.CanNo.ToUpper()))
            .Select(b => new { Can = b.CanNo!.ToUpper(), b.FlagSales })
            .ToListAsync();
        // Thùng đã xuất cho đại lý — Provider dòng 1524-1534 (đọc Inv_InventoryCanDlc).
        var canDlc = await db.CanDlcs.Where(c => cans.Contains(c.CanNo.ToUpper())).Select(c => c.CanNo.ToUpper()).ToListAsync();

        foreach (var r in candidates)
        {
            var can = U(r.CanNo);
            var group = canRows.Where(x => x.Can == can).ToList();
            string? err = null;
            if (group.Count == 0) err = ErrMapCode.THUKhongCo;                                     // Provider dòng 1460
            else if (!agentOk.Contains(U(r.AgentCode))) err = ErrMapCode.DLKhongCo;             // Provider dòng 1461
            else if (group.Any(x => x.FlagSales == SerialFlag.Active)) err = ErrMapCode.SeriDaBan; // Provider dòng 1462
            else if (canDlc.Contains(can)) err = ErrMapCode.THUDaCo;                               // Provider dòng 1536-1540
            if (err == null) continue;
            r.InputStatus = SerialFlag.No; r.Status = SerialFlag.No;
            r.ErrMapCode = err; r.ErrMapName = await ErrNameAsync(err);
        }
    }

    /// <summary>
    /// Ghi màn 3 (xuất kho). Port 1:1 <c>InvInventoryBalanceSerial_OutInv_New20230207</c>
    /// — InvInventoryBalanceSerialManager.cs:4014-4180 (gọi từ dòng 1216-1262;
    /// bản <c>_New20180120</c>:3836 KHÔNG còn được gọi).
    /// </summary>
    public async Task<(int saved, string msg)> SaveOutInvAsync(List<ScanRow> rows)
    {
        if (rows == null || rows.Count == 0) return (0, "Chưa chọn dữ liệu!");
        var now = Now();

        // Số phiếu xuất — nguồn sinh 1 số cho CẢ lô bằng SeqCommonGetX(SeqType.IF_InvOutFGNo)
        // (Manager.cs:4039-4041), rồi gán cho mọi dòng serial.
        var outNo = "OUT" + DateTime.Now.ToString("yyMMddHHmmss");

        var cans = rows.Select(r => (r.CanNo ?? "").Trim()).Where(c => c != "").Distinct().ToList();
        int n = 0;
        foreach (var r in rows)
        {
            var can = (r.CanNo ?? "").Trim();
            var agent = (r.AgentCode ?? "").Trim();

            // Guard — Manager.cs:4109-4121: AgentCode rỗng ⇒ InvalidAgentCode.
            if (string.IsNullOrEmpty(agent)) return (n, "Chưa quét mã đại lý (AgentCode).");
            var au = U(agent);
            if (!await db.Agents.AnyAsync(a => a.AgentCode.ToUpper() == au))
                return (n, $"Đại lý \"{agent}\" không tồn tại ({ErrMapCode.DLKhongCo}).");

            // Xuất theo MÃ THÙNG ⇒ trừ kho TOÀN BỘ tem trong thùng đó
            // (nguồn build lstInvInvBalSerialOutInvDtls theo thùng — CanNoAgentCode.cshtml:103-112).
            var cu = U(can);
            var items = await db.BalanceSerials.Where(b => b.CanNo != null && b.CanNo.ToUpper() == cu).ToListAsync();
            if (items.Count == 0) return (n, $"Mã thùng \"{can}\" không có trong kho ({ErrMapCode.THUKhongCo}).");
            if (items.Any(i => i.FlagSales == SerialFlag.Active))
                return (n, $"Mã thùng \"{can}\" có tem đã bán ({ErrMapCode.SeriDaBan}).");

            foreach (var it in items)
            {
                it.FormOutType = FormOutType.KhongMaVach;   // Manager.cs:4133
                it.IF_InvOutFGNo = outNo;         // Manager.cs:4134
                it.AgentCode = agent;             // Manager.cs:4135
                it.FlagSales = SerialFlag.Active; // xem ghi chú NEEDS-CONFIRM ở báo cáo mục 5
                it.OutDTime = now;                // Manager.cs:4137
                it.OutBy = UserId;                // Manager.cs:4138
                it.LUDTime = now; it.LUBy = UserId;             // Manager.cs:4140-4141
                it.LogLUDTime = now; it.LogLUBy = UserId;       // Manager.cs:4142-4143
                it.UpdateDTime = DateTime.Now; it.UpdateBy = UserId;  // Manager.cs:4144
                n++;
            }
        }

        // Ghi sổ mã thùng đã xuất — nguồn chèn Inv_InventoryCanDlc sau khi xuất thành công
        // (InvInvBalSerialOutInvProvider.cs:1536-1560).
        foreach (var c in cans)
            if (!await db.CanDlcs.AnyAsync(x => x.CanNo.ToUpper() == c.ToUpper()))
                db.CanDlcs.Add(new InvInventoryCanDlc { CanNo = c, LogLUBy = UserId });

        await db.SaveChangesAsync();
        // Thông báo nguồn: Inv_InventoryBalanceSerialController.cs:3825.
        return (n, $"Xuất hàng thành công! (phiếu {outNo}, {n} tem)");
    }
}
