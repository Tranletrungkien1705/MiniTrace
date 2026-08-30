using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using MiniTrace.Data;
using MiniTrace.Models;
using MiniTrace.Services;
using Xunit;

namespace MiniTrace.Tests;

/// <summary>Test truy xuất: tạo đơn vị sinh sự kiện Sản xuất, sự kiện forward-only, tra cứu công khai xuyên tenant.</summary>
public class TraceServiceTests
{
    private static (AppDbContext db, ITraceService svc, SqliteConnection conn) NewSvc()
    {
        var conn = new SqliteConnection("DataSource=:memory:"); conn.Open();
        var opt = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(conn).Options;
        var db = new AppDbContext(opt, new TenantContext { OrgId = TenantContext.DefaultOrgId });
        db.Database.EnsureCreated();
        return (db, new TraceService(db, new StubHttpFactory()), conn);
    }

    private sealed class StubHttpFactory : System.Net.Http.IHttpClientFactory
    {
        public System.Net.Http.HttpClient CreateClient(string name) => new(new FailHandler());
        private sealed class FailHandler : System.Net.Http.HttpMessageHandler
        {
            protected override System.Threading.Tasks.Task<System.Net.Http.HttpResponseMessage> SendAsync(System.Net.Http.HttpRequestMessage r, System.Threading.CancellationToken c)
                => throw new System.Net.Http.HttpRequestException("stub");
        }
    }

    private static async Task<int> NewUnit(ITraceService svc)
    {
        var pid = await svc.CreateProductAsync(new Product { Code = "8931", Name = "Gạo ST25", Origin = "Sóc Trăng", Manufacturer = "HTX A" });
        return await svc.CreateUnitAsync(pid, "LOT-01");
    }

    [Fact]
    public async Task CreateUnit_SeedsProducedEvent_WithGlobalCode()
    {
        var (db, svc, conn) = NewSvc(); using (conn)
        {
            var id = await NewUnit(svc);
            var u = await svc.GetUnitAsync(id);
            Assert.Single(u!.Events);
            Assert.Equal(EventType.Produced, u.Events[0].Type);
            Assert.StartsWith("89", u.Code);
        }
    }

    [Fact]
    public async Task AddEvent_ForwardOnly_Advances()
    {
        var (db, svc, conn) = NewSvc(); using (conn)
        {
            var id = await NewUnit(svc);
            var (ok, _) = await svc.AddEventAsync(id, EventType.Packed, "Kho A", "NV", null);
            Assert.True(ok);
            Assert.Equal(EventType.Packed, (await svc.GetUnitAsync(id))!.LastStage);
        }
    }

    [Fact]
    public async Task AddEvent_Backward_Blocked()
    {
        var (db, svc, conn) = NewSvc(); using (conn)
        {
            var id = await NewUnit(svc);
            await svc.AddEventAsync(id, EventType.Shipped, "X", "Y", null);    // nhảy tới Shipped
            var (ok, msg) = await svc.AddEventAsync(id, EventType.Packed, "X", "Y", null);  // lùi về Packed
            Assert.False(ok);
            Assert.Contains("phải sau", msg);
        }
    }

    [Fact]
    public async Task AddEvent_SameStage_Blocked()
    {
        var (db, svc, conn) = NewSvc(); using (conn)
        {
            var id = await NewUnit(svc);
            var (ok, _) = await svc.AddEventAsync(id, EventType.Produced, "X", "Y", null);  // trùng Produced
            Assert.False(ok);
        }
    }

    [Fact]
    public async Task PublicLookup_FindsByCode_CrossTenant()
    {
        var (db, svc, conn) = NewSvc(); using (conn)
        {
            var id = await NewUnit(svc);
            var u = await svc.GetUnitAsync(id);
            var found = await svc.PublicLookupAsync(u!.Code);
            Assert.NotNull(found);
            Assert.Equal("Gạo ST25", found!.Product.Name);
        }
    }

    [Fact]
    public async Task Dashboard_CountsCompletedWhenSold()
    {
        var (db, svc, conn) = NewSvc(); using (conn)
        {
            var id = await NewUnit(svc);
            await svc.AddEventAsync(id, EventType.Sold, "Cửa hàng", "NV", null);
            var d = await svc.DashboardAsync();
            Assert.Equal(1, d.Completed);
            Assert.Equal(1, d.Units);
        }
    }
}
