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
    public DbSet<Verification> Verifications => Set<Verification>();
    public DbSet<Cte> Ctes => Set<Cte>();
    public DbSet<Kde> Kdes => Set<Kde>();
    public DbSet<DataType> DataTypes => Set<DataType>();
    public DbSet<CteKde> CteKdes => Set<CteKde>();
    public DbSet<Gln> Glns => Set<Gln>();
    public DbSet<OrgGln> OrgGlns => Set<OrgGln>();
    public DbSet<Farm> Farms => Set<Farm>();
    public DbSet<MarketArea> MarketAreas => Set<MarketArea>();
    public DbSet<TemplateNWType> Templates => Set<TemplateNWType>();
    public DbSet<TplNwtCte> TplNwtCtes => Set<TplNwtCte>();
    public DbSet<TplNwtKde> TplNwtKdes => Set<TplNwtKde>();
    public DbSet<TplNwtCteKde> TplNwtCteKdes => Set<TplNwtCteKde>();
    public DbSet<TplViewEvent> TplViewEvents => Set<TplViewEvent>();
    public DbSet<TraceRecord> Records => Set<TraceRecord>();
    public DbSet<TraceRecordSpec> RecordSpecs => Set<TraceRecordSpec>();
    public DbSet<StampBatch> StampBatches => Set<StampBatch>();
    public DbSet<Stamp> Stamps => Set<Stamp>();
    public DbSet<Box> Boxes => Set<Box>();
    public DbSet<BoxItem> BoxItems => Set<BoxItem>();
    public DbSet<Carton> Cartons => Set<Carton>();
    public DbSet<CartonItem> CartonItems => Set<CartonItem>();
    public DbSet<QueSync> QueSyncs => Set<QueSync>();
    public DbSet<MasterData> MasterDatas => Set<MasterData>();
    public DbSet<NetworkOrg> NetworkOrgs => Set<NetworkOrg>();
    public DbSet<Secret> Secrets => Set<Secret>();
    public DbSet<StampPair> StampPairs => Set<StampPair>();

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
        b.Entity<Verification>(e =>
        {
            e.HasOne(x => x.Unit).WithMany().HasForeignKey(x => x.UnitId);
            e.HasIndex(x => x.Code);
            e.HasQueryFilter(x => x.OrgId == _orgId);
        });
        b.Entity<Cte>(e =>
        {
            e.HasIndex(x => new { x.OrgId, x.Code }).IsUnique();
            e.HasQueryFilter(x => x.OrgId == _orgId);
        });
        b.Entity<Kde>(e =>
        {
            e.HasIndex(x => new { x.OrgId, x.Code }).IsUnique();
            e.HasQueryFilter(x => x.OrgId == _orgId);
        });
        b.Entity<DataType>(e =>
        {
            e.HasIndex(x => new { x.OrgId, x.Code }).IsUnique();
            e.HasQueryFilter(x => x.OrgId == _orgId);
        });
        b.Entity<CteKde>(e =>
        {
            e.HasIndex(x => new { x.OrgId, x.CteCode, x.KdeCode }).IsUnique();
            e.HasQueryFilter(x => x.OrgId == _orgId);
        });
        b.Entity<Gln>(e =>
        {
            e.HasIndex(x => new { x.OrgId, x.Code }).IsUnique();
            e.HasQueryFilter(x => x.OrgId == _orgId);
        });
        b.Entity<Farm>(e =>
        {
            e.HasIndex(x => new { x.OrgId, x.Code }).IsUnique();
            e.HasQueryFilter(x => x.OrgId == _orgId);
        });
        b.Entity<MarketArea>(e =>
        {
            e.HasIndex(x => new { x.OrgId, x.Code }).IsUnique();
            e.HasQueryFilter(x => x.OrgId == _orgId);
        });
        b.Entity<OrgGln>(e =>
        {
            e.HasIndex(x => new { x.OrgId, x.OrgCode, x.GlnCode }).IsUnique();
            e.HasQueryFilter(x => x.OrgId == _orgId);
        });
        b.Entity<TemplateNWType>(e =>
        {
            e.HasIndex(x => new { x.OrgId, x.TplNWType }).IsUnique();
            e.HasQueryFilter(x => x.OrgId == _orgId);
        });
        b.Entity<TplNwtCte>(e =>
        {
            e.HasOne(x => x.Template).WithMany(x => x.Ctes).HasForeignKey(x => x.TemplateId);
            e.HasIndex(x => new { x.OrgId, x.TemplateId, x.CteCode }).IsUnique();
            e.HasQueryFilter(x => x.OrgId == _orgId);
        });
        b.Entity<TplNwtKde>(e =>
        {
            e.HasOne(x => x.Template).WithMany(x => x.Kdes).HasForeignKey(x => x.TemplateId);
            e.HasIndex(x => new { x.OrgId, x.TemplateId, x.KdeCode }).IsUnique();
            e.HasQueryFilter(x => x.OrgId == _orgId);
        });
        b.Entity<TplNwtCteKde>(e =>
        {
            e.HasOne(x => x.Template).WithMany(x => x.CteKdes).HasForeignKey(x => x.TemplateId);
            e.HasIndex(x => new { x.OrgId, x.TemplateId, x.CteCode, x.KdeCode }).IsUnique();
            e.HasQueryFilter(x => x.OrgId == _orgId);
        });
        b.Entity<TplViewEvent>(e =>
        {
            e.HasIndex(x => new { x.OrgId, x.Code }).IsUnique();
            e.HasQueryFilter(x => x.OrgId == _orgId);
        });
        b.Entity<TraceRecord>(e =>
        {
            e.HasIndex(x => new { x.OrgId, x.EventNo }).IsUnique();
            e.HasQueryFilter(x => x.OrgId == _orgId);
        });
        b.Entity<TraceRecordSpec>(e =>
        {
            e.HasOne(x => x.Record).WithMany(x => x.Specs).HasForeignKey(x => x.RecordId);
            e.HasIndex(x => new { x.OrgId, x.RecordId, x.KdeCode }).IsUnique();
            e.HasQueryFilter(x => x.OrgId == _orgId);
        });
        b.Entity<StampBatch>(e =>
        {
            e.HasIndex(x => new { x.OrgId, x.GenTimesNo }).IsUnique();
            e.HasQueryFilter(x => x.OrgId == _orgId);
        });
        b.Entity<Stamp>(e =>
        {
            e.HasOne(x => x.Batch).WithMany(x => x.Stamps).HasForeignKey(x => x.BatchId);
            e.HasIndex(x => new { x.OrgId, x.IDNo }).IsUnique();
            e.HasQueryFilter(x => x.OrgId == _orgId);
        });
        b.Entity<Box>(e =>
        {
            e.HasIndex(x => new { x.OrgId, x.BoxNo }).IsUnique();
            e.HasQueryFilter(x => x.OrgId == _orgId);
        });
        b.Entity<BoxItem>(e =>
        {
            e.HasOne(x => x.Box).WithMany(x => x.Items).HasForeignKey(x => x.BoxId);
            e.HasIndex(x => new { x.OrgId, x.IDNo }).IsUnique();   // mỗi tem chỉ nằm trong 1 hộp
            e.HasQueryFilter(x => x.OrgId == _orgId);
        });
        b.Entity<Carton>(e =>
        {
            e.HasIndex(x => new { x.OrgId, x.CanNo }).IsUnique();
            e.HasQueryFilter(x => x.OrgId == _orgId);
        });
        b.Entity<CartonItem>(e =>
        {
            e.HasOne(x => x.Carton).WithMany(x => x.Items).HasForeignKey(x => x.CartonId);
            e.HasIndex(x => new { x.OrgId, x.BoxNo }).IsUnique();   // mỗi hộp chỉ nằm trong 1 thùng
            e.HasQueryFilter(x => x.OrgId == _orgId);
        });
        b.Entity<QueSync>(e =>
        {
            e.HasIndex(x => new { x.OrgId, x.NetworkId, x.QueSyncNo, x.TableCode }).IsUnique();
            e.HasQueryFilter(x => x.OrgId == _orgId);
        });
        b.Entity<MasterData>(e =>
        {
            e.HasIndex(x => new { x.OrgId, x.Code }).IsUnique();
            e.HasQueryFilter(x => x.OrgId == _orgId);
        });
        b.Entity<NetworkOrg>(e =>
        {
            e.HasIndex(x => new { x.OrgId, x.Mst }).IsUnique();
            e.HasQueryFilter(x => x.OrgId == _orgId);
        });
        b.Entity<Secret>(e =>
        {
            e.HasIndex(x => new { x.OrgId, x.SecretNo }).IsUnique();
            e.HasQueryFilter(x => x.OrgId == _orgId);
        });
        b.Entity<StampPair>(e =>
        {
            e.HasIndex(x => new { x.OrgId, x.IDNo }).IsUnique();   // mỗi tem sản phẩm chỉ nằm trong 1 cặp
            e.HasIndex(x => new { x.OrgId, x.BoxNo }).IsUnique();  // mỗi tem hộp chỉ nằm trong 1 cặp
            e.HasQueryFilter(x => x.OrgId == _orgId);
        });
    }

    public override int SaveChanges() { StampOrg(); return base.SaveChanges(); }
    public override Task<int> SaveChangesAsync(CancellationToken ct = default) { StampOrg(); return base.SaveChangesAsync(ct); }
    private void StampOrg()
    {
        foreach (var e in ChangeTracker.Entries<IOrgOwned>())
            if (e.State == EntityState.Added && e.Entity.OrgId == Guid.Empty) e.Entity.OrgId = _orgId;
    }
}
