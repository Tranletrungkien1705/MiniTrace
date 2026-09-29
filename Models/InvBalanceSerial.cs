namespace MiniTrace.Models;

// =====================================================================================
// Port 1:1 từ dự án nguồn 2017.A.iNOS.InBrand (bản đời đầu của hệ tem/truy xuất).
// Cụm màn nguồn: Views/Inv_InventoryBalanceSerial/* — cụm này InBrandCloud KHÔNG có
// (đã đối chiếu NGUON_MANHINH.tsv: 2019.6.InBrandCloud có 0 màn Inv_InventoryBalanceSerial).
//
// Tên cột giữ NGUYÊN VĂN theo nguồn để đo parity cột được:
//   idn.iNOS.InBrand.V10.common/Models/InvInventoryBalanceSerial.cs:11-141
// =====================================================================================

/// <summary>
/// Bảng tồn kho theo từng tem serial — bảng lõi của cả 3 màn ghép tem.
/// Nguồn: <c>[DataTable("Inv_InventoryBalanceSerial")]</c>
/// tại InvInventoryBalanceSerial.cs:11 (45 cột DataColum + Idx tạm).
/// </summary>
public class InvBalanceSerial : IOrgOwned
{
    // Nguồn dùng khoá chính ghép (InvCode, PartCode, SerialNo) — InvInventoryBalanceSerial.cs:18-28.
    // Ở đích dùng khoá thay thế Id + chỉ mục duy nhất trên 3 cột đó (đơn-giản-hoá-có-chủ-đích).
    public int Id { get; set; }
    public Guid OrgId { get; set; }

    public string InvCode { get; set; } = "";        // Mã kho — nguồn dòng 19
    public string PartCode { get; set; } = "";       // Mã sản phẩm — nguồn dòng 24
    public string SerialNo { get; set; } = "";       // Số serial (khoá) — nguồn dòng 28
    public string? PMType { get; set; }              // Nhóm sản phẩm — nguồn dòng 32
    public string? SerialNo_Actual { get; set; }     // Số serial thực quét — nguồn dòng 34
    public string? DLCode { get; set; }              // Mã đơn vị — nguồn dòng 38
    public string? PartLotNo { get; set; }           // nguồn dòng 40
    public string? BoxNo { get; set; }               // Mã hộp — nguồn dòng 42
    public string? CanNo { get; set; }               // Mã thùng — nguồn dòng 44
    public string? AgentCode { get; set; }           // Mã đại lý — nguồn dòng 48
    public string? SecretNo { get; set; }            // Mã bí mật — nguồn dòng 50
    public string? WarrantyDateStart { get; set; }   // Ngày bắt đầu bảo hành — nguồn dòng 52
    public string? PackageDate { get; set; }         // Ngày đóng gói — nguồn dòng 54
    public string? ProductionDate { get; set; }      // Ngày sản xuất — nguồn dòng 56
    public string? UserBox { get; set; }             // User đóng hộp — nguồn dòng 58
    public string? UserCan { get; set; }             // User đóng thùng — nguồn dòng 60
    public string? UserKCS { get; set; }             // User KCS — nguồn dòng 62
    public string? UserCheckPart { get; set; }       // Người kiểm tra sản phẩm — nguồn dòng 64
    public string? BlockStatus { get; set; }         // nguồn dòng 66
    public string? FlagNG { get; set; }              // nguồn dòng 68
    public string? FlagMap { get; set; }             // Trạng thái map — nguồn dòng 70
    public string? FlagUI { get; set; }              // Cờ view — nguồn dòng 72
    public string? FlagSales { get; set; }           // 1 = đã bán — nguồn dòng 74
    public string? FlagBox { get; set; }             // 1 = đã ghép hộp — nguồn dòng 76
    public string? FlagCan { get; set; }             // 1 = đã ghép thùng — nguồn dòng 78
    public string? FormInType { get; set; }          // A = hệ thống gen, M = người dùng nhập — nguồn dòng 80
    public string? IF_InvInFGNo { get; set; }        // Số phiếu nhập — nguồn dòng 82
    public string? FormOutType { get; set; }         // Loại phiếu xuất — nguồn dòng 84
    public string? IF_InvOutFGNo { get; set; }       // Số phiếu xuất — nguồn dòng 86
    public string? LUDTime { get; set; }             // nguồn dòng 90
    public string? LUBy { get; set; }                // nguồn dòng 92
    public string? CreateDTimeSv { get; set; }       // Thời gian tạo (server) — nguồn dòng 94
    public string? CreateBySv { get; set; }          // Người tạo (server) — nguồn dòng 96
    public string? InvDTime { get; set; }            // Thời gian nhập kho — nguồn dòng 98
    public string? InvBy { get; set; }               // Người nhập kho — nguồn dòng 100
    public string? BoxDTime { get; set; }            // Thời gian đóng hộp — nguồn dòng 102
    public string? BoxBy { get; set; }               // Người đóng hộp — nguồn dòng 104
    public string? CanDTime { get; set; }            // Thời gian đóng thùng — nguồn dòng 106
    public string? CanBy { get; set; }               // Người đóng thùng — nguồn dòng 108
    public string? OutDTime { get; set; }            // Thời gian xuất — nguồn dòng 110
    public string? OutBy { get; set; }               // Người xuất — nguồn dòng 112
    public string? RefNo_Type { get; set; }          // nguồn dòng 114
    public string? RefNo_PK { get; set; }            // nguồn dòng 116
    public string? LogLUDTime { get; set; }          // nguồn dòng 118
    public string? LogLUBy { get; set; }             // nguồn dòng 120
    public DateTime CreateDTime { get; set; } = DateTime.Now;   // nguồn dòng 121 (EntityBase)
    public string? CreateBy { get; set; }            // nguồn dòng 124
    public DateTime UpdateDTime { get; set; } = DateTime.Now;   // nguồn dòng 125 (EntityBase)
    public string? UpdateBy { get; set; }            // nguồn dòng 128
    public string? OriginalInvCode { get; set; }     // Mã kho gốc (20211023) — nguồn dòng 140
}

/// <summary>
/// Kho sinh số bí mật — tem chỉ được ghép nếu serial ĐÃ có sẵn ở đây.
/// Nguồn: <c>[DataTable("Inv_InventorySecret")]</c> InvInventorySecret.cs:11-37.
/// </summary>
public class InvInventorySecret : IOrgOwned
{
    public int Id { get; set; }
    public Guid OrgId { get; set; }
    public string SerialNo_Actual { get; set; } = "";  // nguồn dòng 19
    public string SecretNo { get; set; } = "";         // nguồn dòng 23
    public string? Remark { get; set; }                // nguồn dòng 25
}

/// <summary>Danh mục sản phẩm. Nguồn: <c>[DataTable("Mst_Part")]</c> MstPart.cs:11-127 (chỉ port các cột 3 màn dùng).</summary>
public class MstPart : IOrgOwned
{
    public int Id { get; set; }
    public Guid OrgId { get; set; }
    public string PartCode { get; set; } = "";   // nguồn dòng 17
    public string? PartName { get; set; }        // nguồn dòng 25
    public string? PartType { get; set; }        // nguồn dòng 36
    public string? PMType { get; set; }          // Mã nhóm vật liệu — nguồn dòng 40 (dùng để lọc quyền xem)
    public string? FlagActive { get; set; }      // nguồn dòng 127
}

/// <summary>Danh mục đại lý. Nguồn: <c>[DataTable("Mst_Agent")]</c> MstAgent.cs:11-46.</summary>
public class MstAgent : IOrgOwned
{
    public int Id { get; set; }
    public Guid OrgId { get; set; }
    public string AgentCode { get; set; } = "";  // nguồn dòng 19
    public string? ProvinceCode { get; set; }    // nguồn dòng 23
    public string? DistrictCode { get; set; }    // nguồn dòng 27
    public string? AgentName { get; set; }       // nguồn dòng 30
    public string? AgentAddress { get; set; }    // nguồn dòng 32
    public string? FlagActive { get; set; }      // nguồn dòng 34
}

/// <summary>
/// Mã thùng đã xuất cho đại lý — dùng chặn xuất lại cùng mã thùng.
/// Nguồn: <c>[DataTable("Inv_InventoryCanDlc")]</c> InvInventoryCanDlc.cs:11-23,
/// guard đọc bảng này ở InvInvBalSerialOutInvProvider.cs:1524-1534.
/// </summary>
public class InvInventoryCanDlc : IOrgOwned
{
    public int Id { get; set; }
    public Guid OrgId { get; set; }
    public string CanNo { get; set; } = "";      // nguồn dòng 17
    public string? LogLUBy { get; set; }         // nguồn dòng 23
    public DateTime LogLUDTime { get; set; } = DateTime.Now;  // nguồn dòng 20
}

/// <summary>
/// Từ điển mã lỗi ghép tem. Nguồn: <c>[DataTable("Inv_ErrMapInInventory")]</c>
/// InvErrMapInInventory.cs:11-35; biz join bảng này để lấy ErrMapName
/// (InvInvBalSerialMapSerialPartProvider.cs:448-450).
/// </summary>
public class InvErrMapInInventory : IOrgOwned
{
    public int Id { get; set; }
    public Guid OrgId { get; set; }
    public string ErrMapCode { get; set; } = ""; // nguồn dòng 19
    public string? ErrMapName { get; set; }      // nguồn dòng 21
    public string? Remark { get; set; }          // nguồn dòng 23
}

/// <summary>
/// Hằng mã lỗi — port 1:1 <c>TConst.ErrMapCode</c>
/// (idn.iNOS.InBrand.V10.common/Constants/Const.Biz.cs:51-67).
/// </summary>
public static class ErrMapCode
{
    public const string SeriKhongCo = "SERIKHONGCO";      // Const.Biz.cs:53
    public const string SeriKoCoKhoXT = "SERIKOCOKHOXT";  // Const.Biz.cs:54
    public const string SeriDaCo = "SERIDACO";            // Const.Biz.cs:55
    public const string BOXDaCo = "BOXDACO";              // Const.Biz.cs:56
    public const string THUDaCo = "THUDACO";              // Const.Biz.cs:57
    public const string SPKhongCo = "SPKHONGCO";          // Const.Biz.cs:58
    public const string DLKhongCo = "DLKHONGCO";          // Const.Biz.cs:59
    public const string TrungMa = "TRUNGMA";              // Const.Biz.cs:60
    public const string QuetSaiThuTu = "QUETSAITHUTU";    // Const.Biz.cs:61
    public const string SeriDaMap = "SERIDAMAP";          // Const.Biz.cs:62
    public const string SeriDaBan = "SERIDABAN";          // Const.Biz.cs:63
    public const string THUKhongCo = "THUKHONGCO";        // Const.Biz.cs:64
    public const string BOXKhongCo = "BOXKHONGCO";        // Const.Biz.cs:65
    public const string QuetThieu = "QUETTHIEU";          // dùng ở Inv_InventoryBalanceSerialController.cs:860
}

/// <summary>Hằng cờ — port 1:1 <c>TConst.Flag</c> (Const.Biz.cs:40-48).</summary>
public static class SerialFlag
{
    public const string Active = "1";    // Const.Biz.cs:42
    public const string Inactive = "0";  // Const.Biz.cs:43
    public const string Auto = "A";      // Const.Biz.cs:44
    public const string Manual = "M";    // Const.Biz.cs:45
    public const string Yes = Active;
    public const string No = Inactive;
}

/// <summary>
/// Hằng loại phiếu nhập — port 1:1 <c>TConst.FormInType</c> (Const.Biz.cs:307-311).
/// CẨN THẬN: giá trị là "MANUAL"/"AUTO", KHÁC hẳn <c>TConst.Flag.Manual = "M"</c> (Const.Biz.cs:44).
/// </summary>
public static class FormInType
{
    public const string Manual = "MANUAL";  // Const.Biz.cs:309
    public const string Auto = "AUTO";      // Const.Biz.cs:310
}

/// <summary>Hằng loại phiếu xuất — port 1:1 <c>TConst.FormOutType</c> (Const.Biz.cs:313-317).</summary>
public static class FormOutType
{
    public const string MaVach = "MAVACH";            // Const.Biz.cs:315
    public const string KhongMaVach = "KHONGMAVACH";  // Const.Biz.cs:316
}

/// <summary>
/// Một dòng trong lưới xem trước sau khi nạp file máy quét.
/// Port 1:1 <c>InvInvBalSerialMapSerialPart</c> (InvInventoryBalanceSerialCalc.cs:11-28)
/// gộp thêm 2 cột <c>CanNo</c>/<c>AgentCode</c> của 2 màn còn lại
/// (<c>InvInvBalSerialMapSerialCan</c>, <c>InvInvBalSerialOutInv</c>).
/// </summary>
public class ScanRow
{
    public string Idx { get; set; } = "";              // Calc.cs:16 — dùng để map kết quả biz về đúng dòng
    public string? PartCode { get; set; }              // Calc.cs:18
    public string? SerialNo_Actual { get; set; }       // Calc.cs:20
    public string? CanNo { get; set; }                 // màn MapSerialNoCanNo — SerialNoCanNo.cshtml:55
    public string? BoxNo { get; set; }                 // màn xuất hàng — CanNoAgentCode.cshtml
    public string? AgentCode { get; set; }             // màn MapCanNoAgentCode — CanNoAgentCode.cshtml:56
    public string InputStatus { get; set; } = SerialFlag.Yes;  // Calc.cs:22 — "1" chọn được, "0" lỗi
    public string? Status { get; set; }                // Calc.cs:24 — 1 đúng / 0 sai (biz trả về)
    public string? ErrMapCode { get; set; }            // Calc.cs:26
    public string? ErrMapName { get; set; }            // Calc.cs:28
}
