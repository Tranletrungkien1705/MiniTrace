namespace MiniTrace.Data;

public interface ITenantContext { Guid OrgId { get; set; } }

public sealed class TenantContext : ITenantContext
{
    public static readonly Guid DefaultOrgId = new("dddddddd-dddd-dddd-dddd-dddddddddddd");
    public const string DefaultApiKey = "demo-trace";
    public const string CookieName = "org_key";
    public Guid OrgId { get; set; } = DefaultOrgId;
}
