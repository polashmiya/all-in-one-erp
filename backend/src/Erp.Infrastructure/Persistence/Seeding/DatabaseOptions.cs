namespace Erp.Infrastructure.Persistence.Seeding;

public sealed class DatabaseOptions
{
    public const string Section = "Database";

    /// <summary>Apply pending EF migrations at startup (development convenience; use migration bundles in production).</summary>
    public bool MigrateOnStartup { get; set; }

    /// <summary>Create the demo Panel admin, demo account, business units, roles and ERP admin when missing.</summary>
    public bool SeedDemoData { get; set; }
}

public sealed class SeedUserOptions
{
    public string Email { get; set; } = "";

    public string Username { get; set; } = "";

    public string FullName { get; set; } = "";

    public string Password { get; set; } = "";
}

public sealed class SeedOptions
{
    public const string Section = "Seed";

    public SeedUserOptions PanelAdmin { get; set; } = new();

    public SeedUserOptions ErpAdmin { get; set; } = new();
}
