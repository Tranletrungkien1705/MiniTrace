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
