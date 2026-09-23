namespace MiniTrace.Models;

public class Org
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = "";
    public string ApiKey { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}
public interface IOrgOwned { Guid OrgId { get; set; } }

/// <summary>Loại sự kiện truy xuất (Critical Tracking Event — chuẩn GS1).</summary>
public enum EventType
{
    Produced = 0,      // Sản xuất (commissioning)
    QualityChecked = 1,// Kiểm định chất lượng
    Packed = 2,        // Đóng gói
    Warehoused = 3,    // Nhập kho
    Shipped = 4,       // Vận chuyển/xuất kho
    Received = 5,      // Đại lý nhận hàng
    Retailed = 6,      // Bày bán tại điểm bán lẻ
    Sold = 7           // Bán cho người tiêu dùng
}

public class Product : IOrgOwned
{
    public int Id { get; set; }
    public Guid OrgId { get; set; }
    public string Code { get; set; } = "";       // GTIN
    public string Name { get; set; } = "";
    public string? Origin { get; set; }           // xuất xứ
    public string? Manufacturer { get; set; }
}

/// <summary>Đơn vị truy xuất (lô/serial) — mã duy nhất toàn cục để tra cứu công khai.</summary>
public class TraceUnit : IOrgOwned
{
    public int Id { get; set; }
    public Guid OrgId { get; set; }
    public string Code { get; set; } = "";        // duy nhất TOÀN CỤC (GS1 serial/lot)
    public int ProductId { get; set; }
    public string LotNo { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public Product Product { get; set; } = null!;
    public List<TraceEvent> Events { get; set; } = [];

    public EventType? LastStage => Events.Count == 0 ? null : Events.OrderBy(e => e.OccurredAt).Last().Type;
}

/// <summary>Sự kiện trong chuỗi truy xuất (CTE + KDE: ai/ở đâu/khi nào).</summary>
public class TraceEvent : IOrgOwned
{
    public int Id { get; set; }
    public Guid OrgId { get; set; }
    public int UnitId { get; set; }
    public EventType Type { get; set; }
    public string Location { get; set; } = "";    // KDE: địa điểm
    public string Actor { get; set; } = "";        // KDE: đơn vị thực hiện
    public DateTime OccurredAt { get; set; } = DateTime.Now;
    public string? Note { get; set; }
    public TraceUnit Unit { get; set; } = null!;
}

/// <summary>
/// Danh mục Sự kiện truy xuất trọng yếu (GS1 Critical Tracking Event — CTE).
/// Tương đương bảng Mst_CTE của InBrandCloud eTEM: định nghĩa "từ điển" các loại sự kiện
/// mà chuỗi cung ứng dùng để ghi hành trình (Sản xuất, Kiểm định, Vận chuyển…).
/// </summary>
public class Cte : IOrgOwned
{
    public int Id { get; set; }
    public Guid OrgId { get; set; }
    public string Code { get; set; } = "";        // CTECode — mã sự kiện (vd: PRODUCTION_IN)
    public string Description { get; set; } = "";  // CTEDesc — diễn giải
    public string? NetworkType { get; set; }        // TplNWType — loại mạng/đối tác áp dụng
    public string? ApiLink { get; set; }            // APIsLink — API đích khi đẩy sự kiện
    public bool Active { get; set; } = true;        // FlagActive
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}

/// <summary>
/// Danh mục Thành phần dữ liệu trọng yếu (GS1 Key Data Element — KDE).
/// Tương đương bảng Mst_KDE của InBrandCloud eTEM: "từ điển" các trường dữ liệu
/// mà chuỗi cung ứng phải thu thập tại mỗi sự kiện truy xuất (vd: Số lô, Ngày sản xuất,
/// Hạn sử dụng, Số serial, Nhiệt độ bảo quản…).
/// </summary>
public class Kde : IOrgOwned
{
    public int Id { get; set; }
    public Guid OrgId { get; set; }
    public string Code { get; set; } = "";        // KDECode — mã thành phần (vd: LOT_NO)
    public string Description { get; set; } = "";  // KDEDesc — mô tả thành phần
    public string? DataType { get; set; }           // DataType — kiểu dữ liệu (Text/Number/Date/List)
    public string? RefNoList { get; set; }          // RefNoList — danh sách giá trị để chọn khi tạo sự kiện
    public string? NetworkType { get; set; }        // TplNWType — loại tổ chức áp dụng
    public bool FlagList { get; set; }              // FlagList — cờ danh sách (chọn 1 giá trị)
    public bool FlagQuery { get; set; }             // FlagQuery — cờ truy vấn
    public bool Active { get; set; } = true;        // FlagActive
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}

/// <summary>
/// Danh mục Kiểu dữ liệu (GS1 Data Type — Mst_DataType của InBrandCloud eTEM).
/// "Từ điển" các kiểu dữ liệu mà một thành phần dữ liệu (KDE) có thể nhận
/// (Text/Number/Date/List…). Khi tạo/sửa KDE có khai báo DataType, hệ thống
/// kiểm tra kiểu đó phải tồn tại và đang hoạt động trong danh mục này.
/// </summary>
public class DataType : IOrgOwned
{
    public int Id { get; set; }
    public Guid OrgId { get; set; }
    public string Code { get; set; } = "";        // DataType — mã kiểu dữ liệu (vd: Text)
    public string Description { get; set; } = "";  // DataTypeDesc — diễn giải kiểu dữ liệu
    public string? NetworkType { get; set; }        // NetworkID — loại mạng/đối tác áp dụng
    public bool Active { get; set; } = true;        // FlagActive
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}

/// <summary>
/// Ánh xạ Sự kiện trọng yếu ↔ Thành phần dữ liệu (GS1 CTE_KDE).
/// Tương đương bảng CTE_KDE của InBrandCloud eTEM: định nghĩa mỗi sự kiện (CTE)
/// cần thu thập những thành phần dữ liệu (KDE) nào, và thành phần nào là "Key".
/// </summary>
public class CteKde : IOrgOwned
{
    public int Id { get; set; }
    public Guid OrgId { get; set; }
    public string CteCode { get; set; } = "";      // CTECode — mã sự kiện
    public string KdeCode { get; set; } = "";      // KDECode — mã thành phần dữ liệu
    public string? NetworkType { get; set; }        // TplNWType — loại tổ chức
    public bool FlagOsOrgView { get; set; }         // FlagOSOrgView — cho phép user ngoài org xem
    public bool FlagKey { get; set; }               // FlagKey — KDE là Key (bắt buộc) của sự kiện
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}

/// <summary>
/// Danh mục Địa điểm toàn cầu (GS1 Global Location Number — GLN).
/// Tương đương bảng Mst_GLN của InBrandCloud eTEM: định danh "từ điển" các địa điểm
/// trong chuỗi cung ứng (nhà máy, kho, đại lý, cửa hàng…) kèm toạ độ GPS để gắn
/// vào sự kiện truy xuất (ai/ở đâu/khi nào).
/// </summary>
public class Gln : IOrgOwned
{
    public int Id { get; set; }
    public Guid OrgId { get; set; }
    public string Code { get; set; } = "";        // GLNCode — mã địa điểm (duy nhất trong tenant)
    public string Name { get; set; } = "";        // GLNName — tên địa điểm
    public string? GpsLat { get; set; }             // GPSLat — vĩ độ
    public string? GpsLong { get; set; }            // GPSLong — kinh độ
    public string? Remark { get; set; }             // Remark — ghi chú
    public bool Active { get; set; } = true;        // FlagActive
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}

/// <summary>
/// Ánh xạ Tổ chức ↔ Địa điểm (Mst_OrgIDMapGLN của InBrandCloud eTEM).
/// Gắn một tổ chức (OrgID) với một địa điểm (GLNCode) trong chuỗi cung ứng —
/// cho biết tổ chức đó hoạt động tại những địa điểm nào. Khi truy vấn, hệ thống
/// join sang Mst_GLN để lấy tên địa điểm + toạ độ GPS (mg_GLNName/mg_GPSLat/mg_GPSLong).
/// </summary>
public class OrgGln : IOrgOwned
{
    public int Id { get; set; }
    public Guid OrgId { get; set; }
    public string OrgCode { get; set; } = "";      // OrgID — mã tổ chức (duy nhất trong tenant)
    public string GlnCode { get; set; } = "";      // GLNCode — mã địa điểm (tham chiếu Mst_GLN)
    public string? Remark { get; set; }             // Remark — ghi chú
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}

/// <summary>
/// Danh mục Nông trại / Trang trại (GS1 Farm — Mst_Farm của InBrandCloud eTEM).
/// Tương đương bảng Mst_Farm: định danh "từ điển" các nông trại/vùng trồng
/// trong chuỗi truy xuất nguồn gốc (nơi sản phẩm được nuôi trồng/thu hoạch),
/// gắn với loại mạng (NetworkType) để phân biệt nhà sản xuất/đại lý.
/// </summary>
public class Farm : IOrgOwned
{
    public int Id { get; set; }
    public Guid OrgId { get; set; }
    public string Code { get; set; } = "";        // FarmCode — mã nông trại (duy nhất trong tenant)
    public string Name { get; set; } = "";        // FarmName — tên nông trại
    public string? NetworkType { get; set; }        // NetworkID — loại mạng/đối tác áp dụng
    public bool Active { get; set; } = true;        // FlagActive
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}

/// <summary>
/// Danh mục Vùng thị trường (GS1 Market Area — Mst_MarketArea của InBrandCloud eTEM).
/// Tương đương bảng Mst_MarketArea: định danh "từ điển" các vùng thị trường
/// (miền/khu vực phân phối) mà chuỗi truy xuất dùng để gắn vào sự kiện/hồ sơ phân phối.
/// Khi đồng bộ, hệ thống join sang Mst_MarketArea để làm giàu tên vùng thị trường
/// (MarketAreaName) cho bản ghi phân phối (doc 09 §2.1/§3).
/// </summary>
public class MarketArea : IOrgOwned
{
    public int Id { get; set; }
    public Guid OrgId { get; set; }
    public string Code { get; set; } = "";        // MarketAreaCode — mã vùng thị trường (duy nhất trong tenant)
    public string Name { get; set; } = "";        // MarketAreaName — tên vùng thị trường
    public string? AreaType { get; set; }           // MarketAreaType — loại vùng (miền/khu vực)
    public string? Description { get; set; }        // MarketAreaDesc — diễn giải vùng thị trường
    public bool Active { get; set; } = true;        // FlagActive — đang hoạt động
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}

/// <summary>
/// Trạng thái mẫu loại tổ chức (TplNWTStatus của InBrandCloud eTEM).
/// PENDING = chờ duyệt (mới tạo/sửa), APPROVE = đã duyệt, CANCEL = đã hủy.
/// </summary>
public enum TplNwtStatus
{
    Pending = 0,   // PENDING — chờ duyệt
    Approve = 1,   // APPROVE — đã duyệt
    Cancel = 2     // CANCEL — đã hủy
}

/// <summary>
/// Mẫu loại tổ chức (GS1 Network Type Template — Mst_TemplateNWType của InBrandCloud eTEM).
/// Định nghĩa "bộ khung" sự kiện (CTE) + thành phần dữ liệu (KDE) + ánh xạ CTE_KDE
/// áp dụng cho một loại tổ chức trong chuỗi cung ứng (Nhà sản xuất, Kho, Đại lý, Cửa hàng…).
/// Khi một tổ chức thuộc loại này tham gia chuỗi, hệ thống lấy mẫu làm chuẩn để ghi sự kiện.
/// </summary>
public class TemplateNWType : IOrgOwned
{
    public int Id { get; set; }
    public Guid OrgId { get; set; }
    public string TplNWType { get; set; } = "";     // TplNWType — mã loại tổ chức (vd: MANUFACTURER)
    public string Description { get; set; } = "";     // TplNWTDesc — tên loại tổ chức
    public TplNwtStatus Status { get; set; } = TplNwtStatus.Pending;  // TplNWTStatus
    public string? Remark { get; set; }               // Remark — ghi chú
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public List<TplNwtCte> Ctes { get; set; } = [];
    public List<TplNwtKde> Kdes { get; set; } = [];
    public List<TplNwtCteKde> CteKdes { get; set; } = [];
}

/// <summary>Sự kiện (CTE) thuộc một mẫu loại tổ chức (TplNWT_Mst_CTE của InBrandCloud eTEM).</summary>
public class TplNwtCte : IOrgOwned
{
    public int Id { get; set; }
    public Guid OrgId { get; set; }
    public int TemplateId { get; set; }
    public string CteCode { get; set; } = "";        // CTECode
    public string? CteDesc { get; set; }              // CTEDesc — diễn giải (snapshot)
    public string? ApiLink { get; set; }              // APIsLink — API đích
    public bool Active { get; set; } = true;          // FlagActive
    public TemplateNWType Template { get; set; } = null!;
}

/// <summary>Thành phần dữ liệu (KDE) thuộc một mẫu loại tổ chức (TplNWT_Mst_KDE của InBrandCloud eTEM).</summary>
public class TplNwtKde : IOrgOwned
{
    public int Id { get; set; }
    public Guid OrgId { get; set; }
    public int TemplateId { get; set; }
    public string KdeCode { get; set; } = "";        // KDECode
    public string? KdeDesc { get; set; }              // KDEDesc — mô tả (snapshot)
    public string? DataType { get; set; }             // DataType
    public string? RefNoList { get; set; }            // RefNoList
    public bool FlagList { get; set; }                // FlagList
    public bool FlagQuery { get; set; }               // FlagQuery
    public bool Active { get; set; } = true;          // FlagActive
    public TemplateNWType Template { get; set; } = null!;
}

/// <summary>Ánh xạ CTE↔KDE trong một mẫu loại tổ chức (TplNWT_CTE_KDE của InBrandCloud eTEM).</summary>
public class TplNwtCteKde : IOrgOwned
{
    public int Id { get; set; }
    public Guid OrgId { get; set; }
    public int TemplateId { get; set; }
    public string CteCode { get; set; } = "";        // CTECode
    public string KdeCode { get; set; } = "";        // KDECode
    public string? ApiLink { get; set; }              // APIsLink
    public bool FlagOsOrgView { get; set; }           // FlagOSOrgView
    public bool FlagKey { get; set; }                 // FlagKey
    public TemplateNWType Template { get; set; } = null!;
}

/// <summary>
/// Mẫu hiển thị sự kiện truy xuất (GS1 Template View Event — Mst_TplViewEvent của InBrandCloud eTEM).
/// Định nghĩa "khuôn hiển thị" cho một sự kiện (CTE): mô tả + chi tiết bố cục (TplVEDetail)
/// dùng để render hành trình truy xuất cho người tiêu dùng/đối tác. Mỗi sự kiện có thể gắn
/// một mẫu hiển thị đang hoạt động (FlagActive) và một mẫu nền (FlagBG).
/// </summary>
public class TplViewEvent : IOrgOwned
{
    public int Id { get; set; }
    public Guid OrgId { get; set; }
    public string Code { get; set; } = "";        // TplVECode — mã mẫu hiển thị
    public string Description { get; set; } = "";  // TplVEDesc — tên/mô tả mẫu
    public string Detail { get; set; } = "";        // TplVEDetail — chi tiết bố cục hiển thị
    public string? CteCode { get; set; }            // CTECode — sự kiện áp dụng (tuỳ chọn)
    public string? Remark { get; set; }             // Remark — ghi chú
    public bool Active { get; set; } = true;        // FlagActive — đang sử dụng
    public bool FlagBG { get; set; }                // FlagBG — mẫu nền (background)
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}

/// <summary>
/// Sự kiện truy xuất theo chuẩn GS1 (Event_Event của InBrandCloud eTEM).
/// Một "bản ghi hành trình" gắn với một sự kiện trọng yếu (CTE) và tập giá trị
/// thành phần dữ liệu (KDE) thu thập tại sự kiện đó. Bộ giá trị của các KDE "Key"
/// tạo thành "dấu vân tay" (EventNo) để nhận diện trùng lặp: ghi lại cùng bộ Key
/// → cập nhật bản ghi cũ thay vì tạo mới (chống trùng hành trình).
/// </summary>
public class TraceRecord : IOrgOwned
{
    public int Id { get; set; }
    public Guid OrgId { get; set; }
    public string EventNo { get; set; } = "";        // EventNo — mã bản ghi sự kiện (duy nhất trong tenant)
    public string CteCode { get; set; } = "";        // CTECode — sự kiện trọng yếu áp dụng
    public string? TplVECode { get; set; }            // TplVECode — mẫu hiển thị đang hoạt động (snapshot)
    public string? TplVEDetail { get; set; }          // TplVEDetail — chi tiết bố cục hiển thị (snapshot)
    public string? UIStyleCode { get; set; }          // UIStyleCode — kiểu hiển thị
    public string? GlnOrgCode { get; set; }           // GLNOrgCode — mã địa điểm (GLN) nơi xảy ra
    public string? GlnOrgName { get; set; }           // GLNOrgName — tên địa điểm
    public string? GpsLat { get; set; }               // GPSLat — vĩ độ
    public string? GpsLong { get; set; }              // GPSLong — kinh độ
    public string? Remark { get; set; }               // Remark — ghi chú
    public bool Active { get; set; } = true;          // EventStatus — đang hiệu lực
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime UpdatedAt { get; set; } = DateTime.Now;

    public List<TraceRecordSpec> Specs { get; set; } = [];
}

/// <summary>
/// Giá trị thành phần dữ liệu của một sự kiện truy xuất (Event_EventSpec của InBrandCloud eTEM).
/// Mỗi dòng = 1 KDE (thành phần dữ liệu trọng yếu) + giá trị thu thập tại sự kiện.
/// KDE có cờ Key (FlagKey) là "khoá" định danh bản ghi; KDE loại danh sách (FlagList) chỉ được 1/sự kiện.
/// </summary>
public class TraceRecordSpec : IOrgOwned
{
    public int Id { get; set; }
    public Guid OrgId { get; set; }
    public int RecordId { get; set; }
    public string CteCode { get; set; } = "";        // CTECode — sự kiện trọng yếu
    public string KdeCode { get; set; } = "";        // KDECode — mã thành phần dữ liệu
    public string? KdeValue { get; set; }             // KDEValue — giá trị thu thập
    public bool FlagKey { get; set; }                 // ctekde_FlagKey — thành phần là Key (bắt buộc)
    public bool FlagList { get; set; }                // mkde_FlagList — thành phần loại danh sách
    public bool FlagOsOrgView { get; set; }           // FlagOSOrgView — cho user ngoài org xem
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public TraceRecord Record { get; set; } = null!;
}

/// <summary>
/// Loại tem sinh số (GS1 QR Type — TConst.QRType của InBrandCloud eTEM).
/// PRODID = tem định danh sản phẩm (mỗi tem 1 số), BOX = tem hộp, CARTON = tem thùng, TEM = tem thường.
/// </summary>
public enum QrType
{
    ProdId = 0,   // PRODID — tem định danh sản phẩm (sinh IDNo/PIN)
    Box = 1,      // BOX — tem hộp
    Carton = 2,   // CARTON — tem thùng
    Tem = 3       // TEM — tem thường
}

/// <summary>
/// Lần sinh tem (GS1 Stamp Generation — Inv_GenTimes của InBrandCloud eTEM).
/// Mỗi lần "chạy số" tem cho một sản phẩm: khai báo loại tem (QRType), số lượng (Qty),
/// có sinh kèm PIN bí mật hay không (FlagPIN), lô/ngày sản xuất, mẫu in…
/// Sinh ra một lô số tem (Stamp) tương ứng trong kho số.
/// </summary>
public class StampBatch : IOrgOwned
{
    public int Id { get; set; }
    public Guid OrgId { get; set; }
    public string GenTimesNo { get; set; } = "";     // GenTimesNo — mã lần sinh tem (duy nhất trong tenant)
    public string ProductCode { get; set; } = "";     // ProductCode — mã hàng hoá
    public string? ProductName { get; set; }           // ProductName — tên hàng hoá
    public QrType QrType { get; set; } = QrType.ProdId; // QRType — loại tem
    public string? ConfigDomain { get; set; }           // ConfigDomain — tiền tố mã (ConfigName theo loại tem)
    public int Qty { get; set; }                        // Qty — số lượng tem sinh ra
    public bool FlagPIN { get; set; }                   // FlagPIN — 1: sinh kèm PIN, 0: không PIN
    public bool FlagMap { get; set; }                   // FlagMap — cờ ghép thông tin sản phẩm
    public string? ProductionLotNo { get; set; }        // ProductionLotNo — lô sản xuất
    public string? ProductionDate { get; set; }         // ProductionDate — ngày sản xuất
    public string? ShiftInCode { get; set; }            // ShiftInCode — ca sản xuất
    public string? UserKCS { get; set; }                // UserKCS — người KCS
    public string? Remark { get; set; }                 // Remark — ghi chú
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public List<Stamp> Stamps { get; set; } = [];
}

/// <summary>
/// Số tem trong kho số (GS1 Stamp — Inv_InventoryGenID của InBrandCloud eTEM).
/// Mỗi dòng = 1 tem đã sinh: IDNo (số định danh), QR_ID (mã in trên tem = tiền tố + IDNo),
/// PIN (mã bí mật để xác thực), SecretNo, HashInformation (MD5 của "IDNo|PIN" — chống giả),
/// FlagMap (đã ghép sản phẩm chưa), FlagUsed (đã dùng/in chưa).
/// </summary>
public class Stamp : IOrgOwned
{
    public int Id { get; set; }
    public Guid OrgId { get; set; }
    public int BatchId { get; set; }
    public string IDNo { get; set; } = "";           // IDNo — số định danh tem (duy nhất trong tenant)
    public string QR_ID { get; set; } = "";           // QR_ID — mã in trên tem (ConfigDomain + IDNo)
    public string? PIN { get; set; }                   // PIN — mã bí mật (khi FlagPIN)
    public string? SecretNo { get; set; }              // SecretNo — số bí mật
    public string? HashInformation { get; set; }       // HashInformation — MD5(IDNo|PIN)
    public bool FlagMap { get; set; }                  // FlagMap — 0 chưa ghép / 1 đã ghép sản phẩm
    public bool FlagUsed { get; set; }                 // FlagUsed — 0 chưa dùng / 1 đã dùng
    public string? Remark { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public StampBatch Batch { get; set; } = null!;
}

/// <summary>
/// Hộp đóng gói (GS1 Box — Inv_InventoryGenBox của InBrandCloud eTEM).
/// Một hộp gom nhiều tem sản phẩm (IDNo) lại thành một đơn vị đóng gói để vận chuyển.
/// BoxNo là mã hộp (duy nhất trong tenant), QR_BoxNo là mã in trên tem hộp.
/// </summary>
public class Box : IOrgOwned
{
    public int Id { get; set; }
    public Guid OrgId { get; set; }
    public string BoxNo { get; set; } = "";          // BoxNo — mã hộp (duy nhất trong tenant)
    public string QR_BoxNo { get; set; } = "";        // QR_BoxNo — mã in trên tem hộp
    public string? GenTimesNo { get; set; }            // GenTimesNo — lần sinh số hộp
    public string? ProductCode { get; set; }           // ProductCode — mã chủng loại SP
    public string? ProductName { get; set; }           // ProductName — tên chủng loại SP
    public string? Remark { get; set; }                // Remark — ghi chú
    public bool FlagMap { get; set; }                  // FlagMap — 0 chưa gán tem / 1 đã gán tem
    public bool FlagUsed { get; set; }                 // FlagUsed — 0 chưa dùng / 1 đã dùng
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public List<BoxItem> Items { get; set; } = [];
}

/// <summary>
/// Ánh xạ tem sản phẩm vào hộp (GS1 Map_IDInBox của InBrandCloud eTEM).
/// Mỗi dòng = 1 tem (IDNo) được gán vào 1 hộp (BoxNo). Quy tắc: tem phải tồn tại
/// trong kho số tem và chỉ được nằm trong MỘT hộp (chống gán trùng).
/// </summary>
public class BoxItem : IOrgOwned
{
    public int Id { get; set; }
    public Guid OrgId { get; set; }
    public int BoxId { get; set; }
    public string BoxNo { get; set; } = "";          // BoxNo — mã hộp
    public string IDNo { get; set; } = "";            // IDNo — số định danh tem sản phẩm
    public string? ProductCode { get; set; }           // ProductCode — mã chủng loại SP
    public string? InvCode { get; set; }               // InvCode — vị trí kho
    public bool FlagActive { get; set; } = true;       // FlagActive — đang hiệu lực
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public Box Box { get; set; } = null!;
}

/// <summary>
/// Thùng đóng gói (GS1 Carton — Inv_InventoryGenCarton của InBrandCloud eTEM).
/// Cấp cao nhất trong hierarchy đóng gói Thùng→Hộp→Sản phẩm (doc 09 §3.3):
/// một thùng gom nhiều hộp (BoxNo) lại thành một đơn vị vận chuyển.
/// CanNo là mã thùng (duy nhất trong tenant), QR_CanNo là mã in trên tem thùng.
/// </summary>
public class Carton : IOrgOwned
{
    public int Id { get; set; }
    public Guid OrgId { get; set; }
    public string CanNo { get; set; } = "";          // CanNo — mã thùng (duy nhất trong tenant)
    public string QR_CanNo { get; set; } = "";        // QR_CanNo — mã in trên tem thùng
    public string? GenTimesNo { get; set; }            // GenTimesNo — lần sinh số thùng
    public string? ProductCode { get; set; }           // ProductCode — mã chủng loại SP
    public string? ProductName { get; set; }           // ProductName — tên chủng loại SP
    public string? Remark { get; set; }                // Remark — ghi chú
    public bool FlagMap { get; set; }                  // FlagMap — 0 chưa gán hộp / 1 đã gán hộp
    public bool FlagUsed { get; set; }                 // FlagUsed — 0 chưa dùng / 1 đã dùng
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public List<CartonItem> Items { get; set; } = [];
}

/// <summary>
/// Ánh xạ hộp vào thùng (GS1 Map_BoxInCarton của InBrandCloud eTEM).
/// Mỗi dòng = 1 hộp (BoxNo) được gán vào 1 thùng (CanNo). Quy tắc: hộp phải tồn tại
/// trong kho số hộp và chỉ được nằm trong MỘT thùng (chống gán trùng).
/// </summary>
public class CartonItem : IOrgOwned
{
    public int Id { get; set; }
    public Guid OrgId { get; set; }
    public int CartonId { get; set; }
    public string CanNo { get; set; } = "";          // CanNo — mã thùng
    public string BoxNo { get; set; } = "";           // BoxNo — mã hộp được gán vào thùng
    public string? ProductCode { get; set; }           // ProductCode — mã chủng loại SP
    public string? InvCode { get; set; }               // InvCode — vị trí kho
    public bool FlagActive { get; set; } = true;       // FlagActive — đang hiệu lực
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public Carton Carton { get; set; } = null!;
}

/// <summary>
/// Trạng thái đồng bộ của một bản ghi trong hàng đợi (MstSv_QueSync của InBrandCloud eTEM).
/// PENDING = chờ đẩy lên máy chủ eTEM/ELTS, SYNCED = đã đẩy thành công, FAILED = đẩy lỗi.
/// </summary>
public enum QueSyncStatus
{
    Pending = 0,   // PENDING — chờ đồng bộ
    Synced = 1,    // SYNCED — đã đồng bộ
    Failed = 2     // FAILED — đồng bộ lỗi
}

/// <summary>
/// Hàng đợi đồng bộ dữ liệu truy xuất (MstSv_QueSync của InBrandCloud eTEM).
/// Mỗi dòng = 1 bản ghi danh mục/sự kiện (TableCode) cần đẩy lên máy chủ eTEM/ELTS
/// theo một môi trường (NetworkID). QueSyncNo là mã bản ghi nguồn (vd: mã CTE, mã KDE,
/// mã sự kiện EventNo…). FlagSync = đang chờ đẩy; FlagSyncBL = có đẩy lên blockchain không.
/// Cơ chế: khi danh mục/sự kiện thay đổi → tạo 1 dòng hàng đợi; worker đẩy lên eTEM rồi
/// đánh dấu đã đồng bộ (doc 06 §6.2).
/// </summary>
public class QueSync : IOrgOwned
{
    public int Id { get; set; }
    public Guid OrgId { get; set; }
    public string NetworkId { get; set; } = "";      // NetworkID — môi trường/loại mạng đồng bộ
    public string QueSyncNo { get; set; } = "";       // QueSyncNo — mã bản ghi nguồn cần đồng bộ
    public string TableCode { get; set; } = "";       // TableCode — bảng/loại dữ liệu (vd: Mst_CTE, Event_Event)
    public bool FlagSync { get; set; } = true;         // FlagSync — đang chờ đồng bộ
    public bool FlagSyncBL { get; set; }               // FlagSyncBL — đồng bộ lên blockchain
    public QueSyncStatus Status { get; set; } = QueSyncStatus.Pending;  // trạng thái đồng bộ
    public int RetryCount { get; set; }                // số lần thử lại
    public string? ErrorDetail { get; set; }           // chi tiết lỗi (nếu đồng bộ lỗi)
    public DateTime? SyncedAt { get; set; }            // thời điểm đồng bộ thành công
    public string? Remark { get; set; }                // ghi chú
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}

/// <summary>
/// Danh mục dữ liệu gốc (GS1 Master Data — Mst_MasterData của InBrandCloud eTEM).
/// "Từ điển" các bảng/danh mục tham chiếu mà hệ thống eTEM dùng để tra cứu động:
/// mỗi dòng gắn một mã danh mục (MDCode) với tên bảng dữ liệu (TableName) và môi trường
/// mạng áp dụng (NetworkID). Khi một danh mục được khai báo ở đây và đang hoạt động
/// (FlagActive), hệ thống mới cho phép tra cứu/đồng bộ dữ liệu của bảng đó.
/// </summary>
public class MasterData : IOrgOwned
{
    public int Id { get; set; }
    public Guid OrgId { get; set; }
    public string Code { get; set; } = "";        // MDCode — mã danh mục dữ liệu gốc (duy nhất trong tenant)
    public string? NetworkId { get; set; }          // NetworkID — môi trường/loại mạng áp dụng
    public string TableName { get; set; } = "";     // TableName — tên bảng dữ liệu tham chiếu
    public bool Active { get; set; } = true;        // FlagActive — đang hoạt động
    public string? Remark { get; set; }             // ghi chú
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}

/// <summary>
/// Trạng thái đăng ký tổ chức vào mạng lưới truy xuất (RegisterStatus của Mst_NNT).
/// NEW = mới đăng ký, APPROVED = đã được duyệt tham gia mạng, REJECTED = bị từ chối.
/// </summary>
public enum NetworkOrgStatus
{
    New = 0,        // NEW — mới đăng ký
    Approved = 1,   // APPROVED — đã duyệt tham gia mạng
    Rejected = 2    // REJECTED — bị từ chối
}

/// <summary>
/// Tổ chức tham gia mạng lưới truy xuất (GS1 Network Organization — Mst_NNT của InBrandCloud eTEM).
/// Mỗi dòng = 1 doanh nghiệp/tổ chức (nhà sản xuất, kho, đại lý, điểm bán lẻ…) đăng ký tham gia
/// chuỗi truy xuất theo một loại mạng (NetworkType). Khi đăng ký, hệ thống cấp một mã định danh
/// ngoài mạng (ELTSMSTId) nếu chưa có, rồi đưa tổ chức vào hàng đợi đồng bộ (QueSync_Mst_NNT)
/// để đẩy lên máy chủ eTEM/ELTS (tương đương Mst_NNT_QueSync của InBrandCloud).
/// </summary>
public class NetworkOrg : IOrgOwned
{
    public int Id { get; set; }
    public Guid OrgId { get; set; }
    public string Mst { get; set; } = "";            // MST — mã số thuế / mã định danh tổ chức (duy nhất trong tenant)
    public string FullName { get; set; } = "";        // NNTFullName — tên đầy đủ của tổ chức
    public string? NetworkType { get; set; }            // NetworkID — loại mạng/đối tác áp dụng
    public string? OrgCode { get; set; }                // OrgID — mã tổ chức nội bộ (Mst_Org)
    public string? Address { get; set; }                // NNTAddress — địa chỉ
    public string? Mobile { get; set; }                 // NNTMobile — điện thoại di động
    public string? ContactName { get; set; }            // ContactName — người liên hệ
    public string? ContactEmail { get; set; }           // ContactEmail — email liên hệ
    public string? Gln { get; set; }                    // GLN — mã địa điểm toàn cầu (nếu có)
    public string? EltsMstId { get; set; }              // ELTSMSTId — mã định danh ngoài mạng (cấp khi đăng ký)
    public NetworkOrgStatus Status { get; set; } = NetworkOrgStatus.New;  // RegisterStatus
    public bool Active { get; set; } = true;            // FlagActive
    public string? Remark { get; set; }                 // Remark — ghi chú
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}

/// <summary>
/// Kho số bí mật (GS1 Secret Inventory — Inv_InventorySecret của InBrandCloud eTEM).
/// Mỗi dòng = 1 số bí mật (SecretNo) gắn với một serial sản phẩm (SerialNo) để in lên tem
/// cào/nhãn bảo mật — dùng cho cơ chế chống hàng giả: người tiêu dùng cào tem nhập số bí mật
/// để đối chiếu. QR_SerialNo là mã QR in kèm số bí mật. FlagMap = đã ghép với serial sản phẩm
/// chưa; FlagUsed = số bí mật đã được phát hành/dùng chưa (khi xuất excel phát hành tem → đánh dấu đã dùng).
/// </summary>
public class Secret : IOrgOwned
{
    public int Id { get; set; }
    public Guid OrgId { get; set; }
    public string SerialNo { get; set; } = "";       // SerialNo — số serial sản phẩm gắn với số bí mật
    public string? NetworkId { get; set; }             // NetworkID — môi trường/loại mạng áp dụng
    public string? Mst { get; set; }                   // MST — mã số thuế / định danh tổ chức
    public string? OrgCode { get; set; }               // OrgID — mã tổ chức nội bộ
    public string? GenTimesNo { get; set; }            // GenTimesNo — lần sinh số (lô số bí mật)
    public string SecretNo { get; set; } = "";        // SecretNo — số bí mật (duy nhất trong tenant)
    public string? QR_SerialNo { get; set; }           // QR_SerialNo — mã QR in kèm số bí mật
    public bool FlagMap { get; set; }                  // FlagMap — 0 chưa ghép serial / 1 đã ghép
    public bool FlagUsed { get; set; }                 // FlagUsed — 0 chưa dùng / 1 đã phát hành-dùng
    public string? Remark { get; set; }                // Remark — ghi chú
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}

/// <summary>
/// Danh mục định mức số lượng trong hộp (Mst_QuantityInBox của InBrandCloud eTEM).
/// "Từ điển" các mức số lượng đóng gói chuẩn (vd: Mười=10, Một trăm=100, Một nghìn=1000)
/// dùng khi sinh tem/đóng hộp để chọn nhanh số lượng sản phẩm trong một hộp/thùng.
/// QuantityCode là mã mức (duy nhất trong tenant), QuantityValue là giá trị số tương ứng.
/// </summary>
public class QuantityInBox : IOrgOwned
{
    public int Id { get; set; }
    public Guid OrgId { get; set; }
    public string QuantityCode { get; set; } = "";   // QuantityCode — mã mức số lượng (duy nhất trong tenant)
    public int QuantityValue { get; set; }            // QuantityValue — giá trị số lượng
    public string? NetworkId { get; set; }            // NetworkID — môi trường/loại mạng áp dụng
    public bool Active { get; set; } = true;          // FlagActive — đang hoạt động
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}

/// <summary>
/// Ánh xạ cặp tem (GS1 Stamp Pair — Map_StampPair của InBrandCloud eTEM).
/// Ghép 1 tem sản phẩm (IDNo) với 1 tem hộp (BoxNo) thành một "cặp tem" để đẩy lên
/// máy chủ eTEM/ELTS (Index etem_tem). Mỗi IDNo và mỗi BoxNo chỉ được xuất hiện trong
/// MỘT cặp (1-1) — hỗ trợ cơ chế 2 cuộn tem trong/ngoài khi dán tem lên sản phẩm.
/// PIN là mã bí mật in kèm tem sản phẩm (đối chiếu chống giả).
/// </summary>
public class StampPair : IOrgOwned
{
    public int Id { get; set; }
    public Guid OrgId { get; set; }
    public string IDNo { get; set; } = "";          // IDNo — số định danh tem sản phẩm (duy nhất trong tenant)
    public string BoxNo { get; set; } = "";          // BoxNo — mã tem hộp (duy nhất trong tenant)
    public string? NetworkId { get; set; }             // NetworkID — môi trường/loại mạng đồng bộ
    public string? PIN { get; set; }                   // PIN — mã bí mật in kèm tem sản phẩm
    public string? QR_ID { get; set; }                 // QR_ID — nội dung QR gốc của tem sản phẩm
    public string? QR_BoxNo { get; set; }              // QR_BoxNo — nội dung QR gốc của tem hộp
    public string? Remark { get; set; }                // Remark — ghi chú
    public bool Active { get; set; } = true;           // FlagActive — đang hiệu lực
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}

/// <summary>Kết quả xác thực khi người tiêu dùng quét mã (chống hàng giả).</summary>
public enum VerifyStatus
{
    Genuine = 0,   // Chính hãng (quét lần đầu)
    Warning = 1,   // Cảnh báo: đã quét nhiều lần
    Suspect = 2    // Nghi hàng giả: quét nhiều nơi / sau khi đã bán
}

/// <summary>
/// Lần quét xác thực sản phẩm (tương đương Inv_InventoryVerifiedID của InBrandCloud).
/// Mỗi lần NTD quét QR → ghi 1 bản ghi: ai/ở đâu/khi nào + đếm số lần quét để phát hiện hàng giả.
/// </summary>
public class Verification : IOrgOwned
{
    public int Id { get; set; }
    public Guid OrgId { get; set; }
    public int UnitId { get; set; }
    public string Code { get; set; } = "";        // mã truy xuất đã quét
    public int VerifyCount { get; set; } = 1;      // số lần mã này đã bị quét (tính đến lần này)
    public VerifyStatus Status { get; set; } = VerifyStatus.Genuine;
    public string? IpAddress { get; set; }         // IP máy khi quét
    public string? Location { get; set; }          // vị trí (GPS/địa danh)
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public string? Phone { get; set; }             // SĐT người quét (nếu nhập)
    public DateTime ScannedAt { get; set; } = DateTime.Now;
    public string? Note { get; set; }
    public TraceUnit Unit { get; set; } = null!;
}

/// <summary>
/// Trạng thái định danh sản phẩm (ProductIDStatus của InBrandCloud Prd_ProductID).
/// OK = đạt, NG = không đạt, REPAIRING = đang sửa chữa, CHECKING = đang kiểm tra.
/// </summary>
public enum ProductIdStatus
{
    Ok = 0,         // OK — đạt
    Ng = 1,         // NG — không đạt
    Repairing = 2,  // REPAIRING — đang sửa chữa
    Checking = 3    // CHECKING — đang kiểm tra
}

/// <summary>
/// Định danh sản phẩm (GS1 Product ID — Prd_ProductID của InBrandCloud).
/// Mỗi dòng = 1 sản phẩm đã bán/đã giao gắn với một mã định danh (ProductID) duy nhất,
/// kèm thông tin truy xuất nguồn gốc (lô, ngày sản xuất, số bí mật) và bảo hành
/// (ngày bắt đầu/hết hạn, thời hạn bảo hành) — dùng để tra cứu lịch sử sản phẩm,
/// xác thực chính hãng và xử lý bảo hành. Tương đương bảng Prd_ProductID của
/// InBrandCloud ProductCenter (OS_PrdCenter_Prd_ProductID_Create/Update/Delete).
/// </summary>
public class ProductId : IOrgOwned
{
    public int Id { get; set; }
    public Guid OrgId { get; set; }
    public string ProductID { get; set; } = "";       // ProductID — mã định danh sản phẩm (duy nhất trong tenant)
    public string? SpecCode { get; set; }               // SpecCode — mã quy cách / chủng loại
    public string? ProductionDate { get; set; }         // ProductionDate — ngày sản xuất
    public string? LotNo { get; set; }                  // LOTNo — số lô
    public string? BuyDate { get; set; }                // BuyDate — ngày mua
    public string? SecretNo { get; set; }               // SecretNo — số bí mật (chống giả)
    public string? WarrantyStartDate { get; set; }      // WarrantyStartDate — ngày bắt đầu bảo hành
    public string? WarrantyExpiredDate { get; set; }    // WarrantyExpiredDate — ngày hết hạn bảo hành
    public string? WarrantyDuration { get; set; }       // WarrantyDuration — thời hạn bảo hành
    public string? RefNo1 { get; set; }                 // RefNo1 — tham chiếu 1 (số)
    public string? RefBiz1 { get; set; }                // RefBiz1 — tham chiếu 1 (nghiệp vụ)
    public string? RefNo2 { get; set; }                 // RefNo2 — tham chiếu 2 (số)
    public string? RefBiz2 { get; set; }                // RefBiz2 — tham chiếu 2 (nghiệp vụ)
    public string? RefNo3 { get; set; }                 // RefNo3 — tham chiếu 3 (số)
    public string? RefBiz3 { get; set; }                // RefBiz3 — tham chiếu 3 (nghiệp vụ)
    public string? Buyer { get; set; }                  // Buyer — người mua / khách hàng
    public string? NetworkProductIdCode { get; set; }   // NetworkProductIDCode — mã định danh ngoài mạng
    public ProductIdStatus Status { get; set; } = ProductIdStatus.Ok;  // ProductIDStatus
    public string? CustomField1 { get; set; }           // CustomField1 — trường mở rộng 1
    public string? CustomField2 { get; set; }           // CustomField2 — trường mở rộng 2
    public string? CustomField3 { get; set; }           // CustomField3 — trường mở rộng 3
    public string? CustomField4 { get; set; }           // CustomField4 — trường mở rộng 4
    public string? CustomField5 { get; set; }           // CustomField5 — trường mở rộng 5
    public string? Remark { get; set; }                 // Remark — ghi chú
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime UpdatedAt { get; set; } = DateTime.Now;
}

/// <summary>
/// Cấu hình trường hiển thị khi tra cứu (Mst_ConfigColumnSearch của InBrandCloud eTEM).
/// "Từ điển" cấu hình cột hiển thị cho màn tra cứu truy xuất: mỗi dòng gắn một trường
/// (CoumnID) vào một Tab tra cứu (TabID/TabName) theo loại bảng dữ liệu (TypeID) và môi
/// trường mạng (NetworkID). IdxInTab quyết định thứ tự hiển thị; FlagView = hiển thị cho
/// người dùng trong Org, FlagOSOrgView = hiển thị cho người dùng ngoài Org (người tiêu dùng).
/// Bộ ba (CoumnID, NetworkID, TypeID) duy nhất trong tenant — tương đương Mst_ConfigColumnSearch_CheckDB.
/// </summary>
public class ConfigColumnSearch : IOrgOwned
{
    public int Id { get; set; }
    public Guid OrgId { get; set; }
    public string CoumnID { get; set; } = "";       // CoumnID — mã trường thuộc tab tra cứu
    public string TabID { get; set; } = "";          // TabID — mã Tab trong tra cứu
    public string? TabName { get; set; }              // TabName — tên Tab trong tra cứu
    public string? NetworkId { get; set; }            // NetworkID — môi trường/loại mạng áp dụng
    public string TypeId { get; set; } = "";         // TypeID — loại bảng dữ liệu tra ra
    public int IdxInTab { get; set; }                 // IdxInTab — số thứ tự hiển thị trong Tab
    public string? ColumnDesc { get; set; }           // ColumnDesc — mô tả trường
    public bool FlagView { get; set; } = true;        // FlagView — 1: hiển thị cho user trong Org
    public bool FlagOsOrgView { get; set; }           // FlagOSOrgView — hiển thị cho user ngoài Org
    public bool FlagShow { get; set; } = true;        // FlagShow — cờ hiển thị
    public string? EsColumnId { get; set; }           // ESColumnID — mã ElasticSearch của cột
    public string? EltsObjectId { get; set; }         // ELTSObjectId — mã ElasticSearch
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}

/// <summary>
/// Trạng thái ghi nhận sản phẩm đã sản xuất (FlagCompleted của Inv_InventoryManufacturedID).
/// InProgress = đang sản xuất (đã gắn tem vào dây chuyền), Completed = đã hoàn tất dãy sản xuất.
/// </summary>
public enum ManufacturedStatus
{
    InProgress = 0,   // Đang sản xuất
    Completed = 1     // Đã hoàn tất
}

/// <summary>
/// Sản phẩm đã sản xuất / Dãy sản xuất (GS1 Manufactured ID — Inv_InventoryManufacturedID của InBrandCloud eTEM).
/// Mỗi dòng = 1 tem sản phẩm (IDNo) được ghi nhận đã sản xuất trên một dây chuyền (LineCode) theo ca
/// (ShiftCode) và lô sản xuất (ProductionLotNo). Đây là mắt xích "sản xuất" trong chuỗi truy xuất:
/// nối tem số (kho số tem) với dây chuyền/ca/lô thực tế. IManufacturedIDNo là mã dãy sản xuất (nhóm tem
/// cùng một lần chạy máy). Áp quy tắc InBrandCloud (Inv_InventoryManufacturedID_AddMultiX):
///  (1) IDNo phải tồn tại trong kho số tem (Inv_InventoryGenID);
///  (2) IDNo chưa được ghi nhận trong dãy sản xuất nào (chống trùng);
///  (3) IDNo không thuộc bảng tem rách vỡ khu sản xuất;
///  (4) không trùng IDNo trong cùng một lần nhập.
/// </summary>
public class ManufacturedId : IOrgOwned
{
    public int Id { get; set; }
    public Guid OrgId { get; set; }
    public string IDNo { get; set; } = "";              // IDNo — mã tem sản phẩm (tham chiếu kho số tem)
    public string IManufacturedIDNo { get; set; } = ""; // IManufacturedIDNo — mã dãy sản xuất (nhóm tem cùng lần chạy máy)
    public string? NetworkId { get; set; }              // NetworkID — môi trường/loại mạng áp dụng
    public string? BoxNo { get; set; }                  // BoxNo — mã hộp (nếu tem đã đóng hộp)
    public string? LineCode { get; set; }               // LineCode — mã dây chuyền sản xuất
    public string? LineRootCode { get; set; }           // LineRootCode — mã dây chuyền gốc (dây chuyền cha)
    public string? ShiftCode { get; set; }              // ShiftCode — ca sản xuất
    public string? ProductionLotNo { get; set; }        // ProductionLotNo — mã lô sản xuất
    public string? RefNoLine { get; set; }              // RefNoLine — mã đối chiếu dãy
    public string? ProductCode { get; set; }            // ProductCode — mã loại sản phẩm
    public string? InvCode { get; set; }                // InvCode — mã kho
    public DateTime? ManufactureStartDTime { get; set; }// ManufactureStartDTime — thời điểm bắt đầu sản xuất
    public DateTime? MobileScanDTime { get; set; }      // MobileScanDTime — thời điểm quét tem (theo máy quét)
    public int MobileIndex { get; set; }                // MobileIndex — số thứ tự theo máy quét
    public bool FlagMap { get; set; }                   // FlagMap — đã ghép thông tin sản phẩm chưa
    public ManufacturedStatus Status { get; set; } = ManufacturedStatus.InProgress;  // FlagCompleted
    public DateTime? CompleteDTime { get; set; }        // CompleteDTimeUTC — thời điểm hoàn tất dãy
    public string? CompleteBy { get; set; }             // CompleteBy — người hoàn tất
    public string? Remark { get; set; }                 // Remark — ghi chú
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}

/// <summary>
/// Danh mục mạng lưới / môi trường (GS1 Network Master — MstSv_Mst_Network của InBrandCloud eTEM).
/// "Từ điển" các mạng lưới (môi trường) mà hệ thống truy xuất dùng để định tuyến đồng bộ dữ liệu
/// lên máy chủ eTEM/ELTS: mỗi dòng gắn một mã mạng (NetworkID) với tên mạng (NetworkName), nhóm mạng
/// (GroupNetworkID), các địa chỉ kết nối (CoreAddr/PingAddr/XSysAddr/WSUrlAddr/DBUrlAddr) và tổ chức
/// sở hữu (MST). Khi một tổ chức đăng ký tham gia mạng (Mst_NNT), hệ thống tra MstSv_OrgInNetwork để
/// tìm NetworkID của tổ chức rồi lấy WSUrlAddr từ danh mục này để gọi API đồng bộ (doc 09 §5).
/// </summary>
public class NetworkMaster : IOrgOwned
{
    public int Id { get; set; }
    public Guid OrgId { get; set; }
    public string NetworkID { get; set; } = "";      // NetworkID — mã mạng/môi trường (duy nhất trong tenant)
    public string NetworkName { get; set; } = "";    // NetworkName — tên mạng/môi trường
    public string? GroupNetworkID { get; set; }      // GroupNetworkID — nhóm mạng
    public string? CoreAddr { get; set; }            // CoreAddr — địa chỉ core
    public string? PingAddr { get; set; }            // PingAddr — địa chỉ ping/health
    public string? XSysAddr { get; set; }            // XSysAddr — địa chỉ hệ thống ngoài
    public string? WSUrlAddr { get; set; }           // WSUrlAddr — địa chỉ Web API đồng bộ
    public string? WSUrlAddrNew { get; set; }        // WSUrlAddrNew — địa chỉ Web API mới
    public string? DBUrlAddr { get; set; }           // DBUrlAddr — địa chỉ CSDL
    public string? Mst { get; set; }                 // MST — mã số thuế / định danh tổ chức sở hữu
    public string? OrgIdSln { get; set; }            // OrgIDSln — mã tổ chức giải pháp
    public string? MinVersion { get; set; }          // MinVersion — phiên bản nhỏ nhất còn hỗ trợ
    public bool Active { get; set; } = true;         // FlagActive — đang hoạt động
    public string? Remark { get; set; }              // Remark — ghi chú
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}
