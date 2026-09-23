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
