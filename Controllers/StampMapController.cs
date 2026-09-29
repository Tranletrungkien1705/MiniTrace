using Microsoft.AspNetCore.Mvc;
using MiniTrace.Models;
using MiniTrace.Services;

namespace MiniTrace.Controllers;

// =====================================================================================
// Port 1:1 ba màn ghép tem của 2017.A.iNOS.InBrand.
// Nguồn controller: idn.iNOS.InBrand.V10.web/Controllers/Inv_InventoryBalanceSerialController.cs
//   - dòng 620-1046 : MapSerialNoPartCode  (GET / POST lưu / POST import file .txt)
//   - dòng 1838-2381: MapSerialNoCanNo
//   - dòng 3800-4300: MapCanNoAgentCode ("Xuất hàng (Trừ kho thành phẩm)")
//
// ĐƠN-GIẢN-HOÁ-CÓ-CHỦ-ĐÍCH: nguồn nạp dữ liệu máy quét bằng UPLOAD FILE .txt
// (HttpPostedFileWrapper + FileUtils.SaveTempFile — nguồn dòng 669-702); ở đích dùng ô
// dán văn bản nhiều dòng. Luật tách dòng/phán lỗi giữ y nguyên.
// =====================================================================================
public class StampMapController(IStampMapService svc) : Controller
{
    private static string[] SplitLines(string? text) =>
        (text ?? "").Replace("\r\n", "\n").Replace('\r', '\n')
        .Split('\n', StringSplitOptions.RemoveEmptyEntries)
        .Select(l => l.Trim()).Where(l => l.Length > 0).ToArray();

    /// <summary>Chỉ giữ các dòng người dùng tích chọn — nguồn chỉ gửi dòng có ckb:checked
    /// (MapSerialNoPartCode.cshtml:119-136).</summary>
    private static List<ScanRow> Picked(List<ScanRow> rows, int[]? pick) =>
        rows.Where((_, i) => pick != null && pick.Contains(i)).ToList();

    // ------------------------------------------------------------------ MÀN 1
    // Nguồn: Inv_InventoryBalanceSerialController.cs:625 (GET), :634 (POST lưu), :669 (import)
    [HttpGet]
    public IActionResult MapSerialNoPartCode() => View(new List<ScanRow>());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ImportSerialNoPartCode(string scanText)
    {
        var lines = SplitLines(scanText);
        if (lines.Length == 0) { TempData["Error"] = "File import không có dữ liệu!"; return View("MapSerialNoPartCode", new List<ScanRow>()); }
        ViewBag.ScanText = scanText;
        return View("MapSerialNoPartCode", await svc.ScanParseSerialPartAsync(lines));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveSerialNoPartCode(string scanText, int[] pick)
    {
        var rows = await svc.ScanParseSerialPartAsync(SplitLines(scanText));
        var chosen = Picked(rows, pick);
        if (chosen.Count == 0) { TempData["Error"] = "Chưa chọn dữ liệu!"; }
        else
        {
            var (saved, msg) = await svc.SaveMapSerialPartAsync(chosen);
            TempData[saved > 0 ? "Success" : "Error"] = msg;
        }
        return RedirectToAction(nameof(MapSerialNoPartCode));
    }

    // ------------------------------------------------------------------ MÀN 2
    // Nguồn: Inv_InventoryBalanceSerialController.cs:1843 (GET), :1853 (POST lưu), :1888 (import)
    [HttpGet]
    public IActionResult MapSerialNoCanNo() => View(new List<ScanRow>());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ImportSerialNoCanNo(string scanText, string? userCan, string? userKcs)
    {
        var lines = SplitLines(scanText);
        if (lines.Length == 0) { TempData["Error"] = "File import không có dữ liệu!"; return View("MapSerialNoCanNo", new List<ScanRow>()); }
        ViewBag.ScanText = scanText; ViewBag.UserCan = userCan; ViewBag.UserKcs = userKcs;
        return View("MapSerialNoCanNo", await svc.ScanParseSerialCanAsync(lines));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveSerialNoCanNo(string scanText, string? userCan, string? userKcs, int[] pick)
    {
        var rows = await svc.ScanParseSerialCanAsync(SplitLines(scanText));
        var chosen = Picked(rows, pick);
        if (chosen.Count == 0) { TempData["Error"] = "Chưa chọn dữ liệu!"; }
        else
        {
            var (saved, msg) = await svc.SaveMapSerialCanAsync(chosen, userCan ?? "", userKcs ?? "");
            TempData[saved > 0 ? "Success" : "Error"] = msg;
        }
        return RedirectToAction(nameof(MapSerialNoCanNo));
    }

    // ------------------------------------------------------------------ MÀN 3
    // Nguồn: Inv_InventoryBalanceSerialController.cs:3806 (GET), :3816 (POST lưu), :3851 (import)
    [HttpGet]
    public IActionResult MapCanNoAgentCode() => View(new List<ScanRow>());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ImportCanNoAgentCode(string scanText)
    {
        var lines = SplitLines(scanText);
        if (lines.Length == 0) { TempData["Error"] = "File import không có dữ liệu!"; return View("MapCanNoAgentCode", new List<ScanRow>()); }
        ViewBag.ScanText = scanText;
        return View("MapCanNoAgentCode", await svc.ScanParseCanAgentAsync(lines));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveCanNoAgentCode(string scanText, int[] pick)
    {
        var rows = await svc.ScanParseCanAgentAsync(SplitLines(scanText));
        var chosen = Picked(rows, pick);
        if (chosen.Count == 0) { TempData["Error"] = "Chưa chọn dữ liệu!"; }
        else
        {
            var (saved, msg) = await svc.SaveOutInvAsync(chosen);
            TempData[saved > 0 ? "Success" : "Error"] = msg;
        }
        return RedirectToAction(nameof(MapCanNoAgentCode));
    }
}
