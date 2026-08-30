using Microsoft.EntityFrameworkCore;
using MiniTrace.Data;
using MiniTrace.Models;
using MiniTrace.Services;
using Serilog;

AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
FleetObs.ConfigureLogger("minitrace");

var builder = WebApplication.CreateBuilder(args);
builder.Host.UseSerilog();
builder.WebHost.UseUrls($"http://0.0.0.0:{Environment.GetEnvironmentVariable("PORT") ?? "8080"}");

var conn = Environment.GetEnvironmentVariable("CONNECTION_STRING")
    ?? builder.Configuration.GetConnectionString("DefaultConnection") ?? "Data Source=minitrace.db";
builder.Services.AddDbContext<AppDbContext>(o =>
{
    if (DbUtil.IsPostgres(conn)) o.UseNpgsql(DbUtil.ToNpgsql(conn));
    else o.UseSqlite(conn);
});
builder.Services.AddScoped<ITenantContext, TenantContext>();
builder.Services.AddScoped<ITraceService, TraceService>();
builder.Services.AddHttpClient();   // đồng bộ danh mục từ MiniPIM
builder.Services.AddFleetObs();
builder.Services.AddControllersWithViews();

var app = builder.Build();
using (var scope = app.Services.CreateScope())
    await Seeder.SeedAsync(scope.ServiceProvider.GetRequiredService<AppDbContext>());

app.UseFleetObs();

app.Use(async (ctx, next) =>
{
    var key = ctx.Request.Headers["X-Api-Key"].FirstOrDefault();
    if (string.IsNullOrWhiteSpace(key)) ctx.Request.Cookies.TryGetValue(TenantContext.CookieName, out key);
    if (!string.IsNullOrWhiteSpace(key))
    {
        using var lookup = app.Services.CreateScope();
        var ldb = lookup.ServiceProvider.GetRequiredService<AppDbContext>();
        var org = await ldb.Orgs.FirstOrDefaultAsync(o => o.ApiKey == key);
        if (org != null) ctx.RequestServices.GetRequiredService<ITenantContext>().OrgId = org.Id;
    }
    await next();
});

app.UseStaticFiles();
app.MapGet("/healthz", () => "ok");

// API truy xuất công khai theo mã (người tiêu dùng/ứng dụng)
app.MapGet("/api/trace", async (string code, ITraceService svc) =>
{
    var u = await svc.PublicLookupAsync(code);
    if (u == null) return Results.NotFound(new { code, found = false });
    return Results.Ok(new
    {
        code = u.Code, product = u.Product.Name, origin = u.Product.Origin, lot = u.LotNo,
        stage = u.LastStage.HasValue ? Ui.Stage(u.LastStage.Value).text : null,
        events = u.Events.OrderBy(e => e.OccurredAt).Select(e => new { stage = Ui.Stage(e.Type).text, at = e.OccurredAt.ToString("yyyy-MM-dd HH:mm"), e.Location, e.Actor, e.Note })
    });
});

// API tích hợp: MiniWMS ghi sổ phiếu kho → ghi sự kiện truy xuất cho lô hàng (mã lô = số phiếu).
app.MapPost("/api/ext/wh-event", async (WhEventDto dto, ITraceService svc, AppDbContext db, HttpContext ctx) =>
{
    if (string.IsNullOrWhiteSpace(dto.Product) || string.IsNullOrWhiteSpace(dto.LotNo))
        return Results.BadRequest(new { error = "Cần Product và LotNo." });
    var name = dto.Product.Trim();
    var prod = await db.Products.FirstOrDefaultAsync(p => p.Name == name);
    if (prod == null)
    {
        await svc.CreateProductAsync(new Product { Name = name, Origin = dto.Location, Manufacturer = "MiniWMS" });
        prod = await db.Products.FirstOrDefaultAsync(p => p.Name == name);
    }
    var unit = await db.Units.Include(u => u.Events).FirstOrDefaultAsync(u => u.ProductId == prod!.Id && u.LotNo == dto.LotNo);
    var unitId = unit?.Id ?? await svc.CreateUnitAsync(prod!.Id, dto.LotNo.Trim());
    var (ok, msg) = await svc.AddEventAsync(unitId, (EventType)dto.Stage, dto.Location ?? "Kho", "MiniWMS", dto.Note);
    var u2 = await svc.GetUnitAsync(unitId);
    var baseUrl = $"{ctx.Request.Scheme}://{ctx.Request.Host}";
    return Results.Ok(new { code = u2!.Code, ok, msg, traceUrl = $"{baseUrl}/Trace?code={u2.Code}" });
});

app.MapPost("/api/orgs/register", async (RegisterOrgDto dto, AppDbContext db) =>
{
    if (string.IsNullOrWhiteSpace(dto.Name)) return Results.BadRequest(new { error = "Cần Name." });
    var org = new Org { Name = dto.Name.Trim(), ApiKey = "trc_" + Guid.NewGuid().ToString("N") };
    db.Orgs.Add(org); await db.SaveChangesAsync();
    return Results.Ok(new { orgId = org.Id, apiKey = org.ApiKey });
});

app.MapControllerRoute(name: "default", pattern: "{controller=Home}/{action=Index}/{id?}");
app.Run();

record RegisterOrgDto(string Name);
record WhEventDto(string Product, string LotNo, int Stage, string? Location, string? Note);
