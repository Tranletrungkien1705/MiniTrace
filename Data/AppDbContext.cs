using Microsoft.EntityFrameworkCore;
using MiniTrace.Models;

namespace MiniTrace.Data;

public class AppDbContext : DbContext
{
    private readonly Guid _orgId;
    public AppDbContext(DbContextOptions<AppDbContext> options, ITenantContext tenant) : base(options) => _orgId = tenant.OrgId;

    public DbSet<Org> Orgs => Set<Org>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<TraceUnit> Units => Set<TraceUnit>();
    public DbSet<TraceEvent> Events => Set<TraceEvent>();

    // Cụm ghép tem port 1:1 tu 2017.A.iNOS.InBrand (Views/Inv_InventoryBalanceSerial).
    public DbSet<InvBalanceSerial> BalanceSerials => Set<InvBalanceSerial>();
    public DbSet<InvInventorySecret> Secrets => Set<InvInventorySecret>();
    public DbSet<MstPart> Parts => Set<MstPart>();
    public DbSet<MstAgent> Agents => Set<MstAgent>();
    public DbSet<InvInventoryCanDlc> CanDlcs => Set<InvInventoryCanDlc>();
    public DbSet<InvErrMapInInventory> ErrMaps => Set<InvErrMapInInventory>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        if (Database.IsNpgsql()) b.HasDefaultSchema("minitrace");
        b.Entity<Org>().HasIndex(x => x.ApiKey).IsUnique();
        b.Entity<Product>(e => { e.HasIndex(x => new { x.OrgId, x.Code }).IsUnique(); e.HasQueryFilter(x => x.OrgId == _orgId); });
        b.Entity<TraceUnit>(e =>
        {
            e.HasIndex(x => x.Code).IsUnique();   // duy nhất toàn cục (tra cứu công khai)
            e.Ignore(x => x.LastStage);
            e.HasOne(x => x.Product).WithMany().HasForeignKey(x => x.ProductId);
            e.HasQueryFilter(x => x.OrgId == _orgId);
        });
        b.Entity<TraceEvent>(e =>
        {
            e.HasOne(x => x.Unit).WithMany(x => x.Events).HasForeignKey(x => x.UnitId);
            e.HasQueryFilter(x => x.OrgId == _orgId);
        });

        // Khoa chinh nguon la ghep (InvCode, PartCode, SerialNo) - InvInventoryBalanceSerial.cs:18-28;
        // o dich dung khoa thay the Id + chi muc duy nhat de giu nguyen rang buoc trung.
        b.Entity<InvBalanceSerial>(e =>
        {
            e.HasIndex(x => new { x.OrgId, x.InvCode, x.PartCode, x.SerialNo }).IsUnique();
            e.HasIndex(x => x.SerialNo_Actual);
            e.HasIndex(x => x.CanNo);
            e.HasQueryFilter(x => x.OrgId == _orgId);
        });
        b.Entity<InvInventorySecret>(e => { e.HasIndex(x => new { x.OrgId, x.SerialNo_Actual }).IsUnique(); e.HasQueryFilter(x => x.OrgId == _orgId); });
        b.Entity<MstPart>(e => { e.HasIndex(x => new { x.OrgId, x.PartCode }).IsUnique(); e.HasQueryFilter(x => x.OrgId == _orgId); });
        b.Entity<MstAgent>(e => { e.HasIndex(x => new { x.OrgId, x.AgentCode }).IsUnique(); e.HasQueryFilter(x => x.OrgId == _orgId); });
        b.Entity<InvInventoryCanDlc>(e => { e.HasIndex(x => new { x.OrgId, x.CanNo }).IsUnique(); e.HasQueryFilter(x => x.OrgId == _orgId); });
        b.Entity<InvErrMapInInventory>(e => { e.HasIndex(x => new { x.OrgId, x.ErrMapCode }).IsUnique(); e.HasQueryFilter(x => x.OrgId == _orgId); });
    }

    public override int SaveChanges() { StampOrg(); return base.SaveChanges(); }
    public override Task<int> SaveChangesAsync(CancellationToken ct = default) { StampOrg(); return base.SaveChangesAsync(ct); }
    private void StampOrg()
    {
        foreach (var e in ChangeTracker.Entries<IOrgOwned>())
            if (e.State == EntityState.Added && e.Entity.OrgId == Guid.Empty) e.Entity.OrgId = _orgId;
    }
}
