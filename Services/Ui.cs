using MiniTrace.Models;

namespace MiniTrace.Services;

public static class Ui
{
    public static (string text, string css, string icon) Stage(EventType t) => t switch
    {
        EventType.Produced => ("Sản xuất", "primary", "bi-gear-wide-connected"),
        EventType.QualityChecked => ("Kiểm định", "info", "bi-patch-check"),
        EventType.Packed => ("Đóng gói", "info", "bi-box-seam"),
        EventType.Warehoused => ("Nhập kho", "secondary", "bi-hdd-stack"),
        EventType.Shipped => ("Vận chuyển", "warning", "bi-truck"),
        EventType.Received => ("Đại lý nhận", "secondary", "bi-shop"),
        EventType.Retailed => ("Bày bán", "info", "bi-basket"),
        EventType.Sold => ("Đã bán", "success", "bi-bag-check"),
        _ => (t.ToString(), "secondary", "bi-dot")
    };
}
