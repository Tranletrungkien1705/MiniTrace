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

    public static (string text, string css, string icon) Qr(QrType t) => t switch
    {
        QrType.ProdId => ("Tem sản phẩm", "primary", "bi-qr-code"),
        QrType.Box => ("Tem hộp", "info", "bi-box"),
        QrType.Carton => ("Tem thùng", "warning", "bi-boxes"),
        QrType.Tem => ("Tem thường", "secondary", "bi-tag"),
        _ => (t.ToString(), "secondary", "bi-dot")
    };

    public static (string text, string css, string icon) QueSync(QueSyncStatus s) => s switch
    {
        QueSyncStatus.Pending => ("Chờ đồng bộ", "warning", "bi-hourglass-split"),
        QueSyncStatus.Synced => ("Đã đồng bộ", "success", "bi-cloud-check"),
        QueSyncStatus.Failed => ("Đồng bộ lỗi", "danger", "bi-cloud-slash"),
        _ => (s.ToString(), "secondary", "bi-dot")
    };

    public static (string text, string css, string icon) NetworkOrg(NetworkOrgStatus s) => s switch
    {
        NetworkOrgStatus.New => ("Mới đăng ký", "warning", "bi-hourglass-split"),
        NetworkOrgStatus.Approved => ("Đã duyệt mạng", "success", "bi-patch-check"),
        NetworkOrgStatus.Rejected => ("Bị từ chối", "danger", "bi-x-circle"),
        _ => (s.ToString(), "secondary", "bi-dot")
    };

    public static (string text, string css, string icon) Secret(bool used) => used
        ? ("Đã dùng", "success", "bi-check2-circle")
        : ("Chưa dùng", "warning", "bi-hourglass-split");

    public static (string text, string css, string icon) ProductId(ProductIdStatus s) => s switch
    {
        ProductIdStatus.Ok => ("OK", "success", "bi-check2-circle"),
        ProductIdStatus.Ng => ("NG", "danger", "bi-x-circle"),
        ProductIdStatus.Repairing => ("Đang sửa chữa", "warning", "bi-tools"),
        ProductIdStatus.Checking => ("Đang kiểm tra", "info", "bi-search"),
        _ => (s.ToString(), "secondary", "bi-dot")
    };

    public static (string text, string css, string icon) Manufactured(ManufacturedStatus s) => s switch
    {
        ManufacturedStatus.InProgress => ("Đang sản xuất", "warning", "bi-gear-wide-connected"),
        ManufacturedStatus.Completed => ("Đã hoàn tất", "success", "bi-check2-circle"),
        _ => (s.ToString(), "secondary", "bi-dot")
    };

    public static (string text, string css, string icon) WarningSync(WarningSyncStatus s) => s switch
    {
        WarningSyncStatus.Pending => ("Chưa đồng bộ ES", "warning", "bi-cloud-slash"),
        WarningSyncStatus.Synced => ("Đã đồng bộ ES", "success", "bi-cloud-check"),
        _ => (s.ToString(), "secondary", "bi-dot")
    };

    public static (string text, string css, string icon) VerifiedInOut(VerifiedInOutStatus s) => s switch
    {
        VerifiedInOutStatus.Active => ("Đang hiệu lực", "success", "bi-check2-circle"),
        VerifiedInOutStatus.Cancelled => ("Đã hủy", "secondary", "bi-x-circle"),
        _ => (s.ToString(), "secondary", "bi-dot")
    };
}
