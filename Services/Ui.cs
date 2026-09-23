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

    public static (string text, string css, string icon) Verify(VerifyStatus s) => s switch
    {
        VerifyStatus.Genuine => ("Chính hãng", "success", "bi-patch-check-fill"),
        VerifyStatus.Warning => ("Cảnh báo", "warning", "bi-exclamation-triangle"),
        VerifyStatus.Suspect => ("Nghi hàng giả", "danger", "bi-shield-exclamation"),
        _ => (s.ToString(), "secondary", "bi-dot")
    };

    public static (string text, string css, string icon) TplStatus(TplNwtStatus s) => s switch
    {
        TplNwtStatus.Pending => ("Chờ duyệt", "warning", "bi-hourglass-split"),
        TplNwtStatus.Approve => ("Đã duyệt", "success", "bi-check2-circle"),
        TplNwtStatus.Cancel => ("Đã hủy", "secondary", "bi-x-circle"),
        _ => (s.ToString(), "secondary", "bi-dot")
    };
}
