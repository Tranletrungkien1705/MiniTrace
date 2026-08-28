using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MiniTrace.Data;
using MiniTrace.Models;
using MiniTrace.Services;

namespace MiniTrace.Controllers;

public class HomeController : Controller
{
    // SPA React (admin) ở "/". Trang tra cứu công khai /Trace (Razor) giữ nguyên cho người tiêu dùng.
    public IActionResult Index() => Redirect("/index.html");
}

public class LegacyController(ITraceService svc) : Controller
{
    public async Task<IActionResult> Index() { ViewBag.Dash = await svc.DashboardAsync(); return View("~/Views/Home/Index.cshtml"); }
}

public class ProductController(ITraceService svc) : Controller
{
    public async Task<IActionResult> Index() => View(await svc.ProductsAsync());
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(string name, string? code, string? origin, string? manufacturer)
    {
        if (string.IsNullOrWhiteSpace(name)) { TempData["Error"] = "Cần tên sản phẩm."; return RedirectToAction(nameof(Index)); }
        await svc.CreateProductAsync(new Product { Name = name.Trim(), Code = code ?? "", Origin = origin, Manufacturer = manufacturer });
        TempData["Success"] = "Đã thêm sản phẩm.";
        return RedirectToAction(nameof(Index));
    }
}

public class UnitController(ITraceService svc) : Controller
{
    public async Task<IActionResult> Index(string? q) { ViewBag.Q = q; ViewBag.Products = await svc.ProductsAsync(); return View(await svc.UnitsAsync(q)); }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(int productId, string lotNo)
    {
        if (productId <= 0) { TempData["Error"] = "Chọn sản phẩm."; return RedirectToAction(nameof(Index)); }
        var id = await svc.CreateUnitAsync(productId, lotNo ?? "");
        TempData["Success"] = "Đã tạo đơn vị truy xuất.";
        return RedirectToAction(nameof(Detail), new { id });
    }

    public async Task<IActionResult> Detail(int id)
    {
        var u = await svc.GetUnitAsync(id);
        if (u == null) return NotFound();
        return View(u);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> AddEvent(int id, EventType type, string location, string actor, string? note)
    {
        var (ok, msg) = await svc.AddEventAsync(id, type, location ?? "", actor ?? "", note);
        TempData[ok ? "Success" : "Error"] = msg;
        return RedirectToAction(nameof(Detail), new { id });
    }
}

/// <summary>Trang truy xuất CÔNG KHAI (người tiêu dùng quét mã) — không cần đăng nhập, xuyên tenant.</summary>
public class TraceController(ITraceService svc) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(string? code)
    {
        if (string.IsNullOrWhiteSpace(code)) return View((TraceUnit?)null);
        ViewBag.Code = code;
        return View(await svc.PublicLookupAsync(code));
    }
}

public class OrgController(AppDbContext db) : Controller
{
    public async Task<IActionResult> Index()
    {
        var orgs = await db.Orgs.IgnoreQueryFilters().OrderBy(o => o.CreatedAt).ToListAsync();
        Request.Cookies.TryGetValue(TenantContext.CookieName, out var curKey);
        ViewBag.CurrentKey = curKey ?? TenantContext.DefaultApiKey;
        return View(orgs);
    }
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) { TempData["Error"] = "Cần tên tổ chức."; return RedirectToAction(nameof(Index)); }
        var org = new Org { Name = name.Trim(), ApiKey = "trc_" + Guid.NewGuid().ToString("N") };
        db.Orgs.Add(org); await db.SaveChangesAsync();
        SetCookies(org.ApiKey, org.Name);
        TempData["Success"] = $"Đã tạo & chuyển sang \"{org.Name}\".";
        return RedirectToAction("Index", "Home");
    }
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Switch(string apiKey)
    {
        var org = await db.Orgs.IgnoreQueryFilters().FirstOrDefaultAsync(o => o.ApiKey == apiKey);
        if (org == null) { TempData["Error"] = "Không tìm thấy."; return RedirectToAction(nameof(Index)); }
        SetCookies(org.ApiKey, org.Name);
        return RedirectToAction("Index", "Home");
    }
    public IActionResult Reset()
    {
        Response.Cookies.Delete(TenantContext.CookieName); Response.Cookies.Delete("org_name");
        return RedirectToAction("Index", "Home");
    }
    private void SetCookies(string k, string n)
    {
        var o = new CookieOptions { IsEssential = true, Expires = DateTimeOffset.UtcNow.AddDays(30) };
        Response.Cookies.Append(TenantContext.CookieName, k, o); Response.Cookies.Append("org_name", n, o);
    }
}
