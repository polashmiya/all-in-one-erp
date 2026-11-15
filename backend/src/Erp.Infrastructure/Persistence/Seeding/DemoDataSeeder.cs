using Erp.Core.Authorization;
using Erp.Core.Domain.Identity;
using Erp.Core.Domain.Inventory;
using Erp.Core.Domain.Tenancy;
using Erp.Core.Security;
using Erp.Infrastructure.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Erp.Infrastructure.Persistence.Seeding;

/// <summary>
/// Idempotent demo data: the Panel super admin, "Demo Pharma Ltd" with Dhaka and Chattogram depots (each with
/// offices and warehouses), Administrator / Viewer roles and a primary ERP admin with access to both depots.
/// Every lookup is by natural key, so re-running only fills in what is missing.
/// </summary>
public sealed class DemoDataSeeder(
    ErpDbContext db,
    IPasswordHasher hasher,
    PermissionCatalog catalog,
    IOptions<SeedOptions> options,
    ILogger<DemoDataSeeder> logger)
{
    public const string AccountCode = "DEMO-PHARMA";
    public const string AdministratorRole = SystemRoles.Administrator;
    public const string ViewerRole = SystemRoles.Viewer;

    private sealed record DemoOffice(string Code, string Name, string Address);

    private sealed record DemoWarehouse(string Code, string Name, string? OfficeCode);

    private sealed record DemoUnit(string Code, string Name, string Address, DemoOffice[] Offices, DemoWarehouse[] Warehouses);

    private static readonly DemoUnit[] Units =
    [
        new("DHK", "Dhaka Depot", "Plot 12, Tejgaon I/A, Dhaka 1208",
            [new("HO", "Dhaka Head Office", "Plot 12, Tejgaon I/A, Dhaka 1208"), new("MIR", "Mirpur Branch", "Section 10, Mirpur, Dhaka 1216")],
            [new("CWH", "Central Warehouse", "HO"), new("MWH", "Mirpur Store", "MIR"), new("TRN", "Transit Warehouse", null)]),
        new("CTG", "Chattogram Depot", "Agrabad C/A, Chattogram 4100",
            [new("AGR", "Agrabad Office", "Agrabad C/A, Chattogram 4100")],
            [new("CTW", "Chattogram Warehouse", "AGR"), new("PBW", "Port Bonded Warehouse", null)]),
    ];

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        var seed = options.Value;

        await EnsurePanelAdminAsync(seed.PanelAdmin, cancellationToken);

        var account = await EnsureAccountAsync(cancellationToken);
        var units = await EnsureBusinessUnitsAsync(account, cancellationToken);
        var administrator = await EnsureRoleAsync(account, AdministratorRole, () => SystemRoles.CreateAdministrator(account.Id, catalog), [], cancellationToken);
        await EnsureRoleAsync(account, ViewerRole, () => SystemRoles.CreateViewer(account.Id, catalog), SystemRoles.ViewerPermissions(catalog), cancellationToken);
        await EnsureErpAdminAsync(seed.ErpAdmin, account, units, administrator, cancellationToken);
        await EnsureFeaturesAsync(account, cancellationToken);
        await EnsureItemMasterDataAsync(account, units, cancellationToken);

        await db.SaveChangesAsync(cancellationToken);
    }

    private async Task EnsureItemMasterDataAsync(Account account, List<BusinessUnit> units, CancellationToken ct)
    {
        foreach (var unit in units)
        {
            if (!ItemMasterDemoData.ByUnit.TryGetValue(unit.Code, out var departments)) continue;
            if (await db.ItemDepartments.AcrossTenants().AnyAsync(d => d.BusinessUnitId == unit.Id, ct)) continue;

            var serial = 0;
            foreach (var demo in departments)
            {
                var department = new ItemDepartment
                {
                    AccountId = account.Id,
                    BusinessUnitId = unit.Id,
                    Code = demo.Code,
                    Name = demo.Name,
                    Serial = ++serial,
                    IsActive = demo.IsActive,
                };
                db.ItemDepartments.Add(department);

                foreach (var demoCategory in demo.Categories)
                {
                    var category = new ItemCategory
                    {
                        AccountId = account.Id,
                        BusinessUnitId = unit.Id,
                        Department = department,
                        Code = demoCategory.Code,
                        Name = demoCategory.Name,
                        IsActive = demo.IsActive,
                    };
                    db.ItemCategories.Add(category);

                    foreach (var demoSub in demoCategory.SubCategories)
                    {
                        var subCategory = new ItemSubCategory
                        {
                            AccountId = account.Id,
                            BusinessUnitId = unit.Id,
                            Department = department,
                            Category = category,
                            Name = demoSub.Name,
                            IsActive = demo.IsActive && demoSub.IsActive,
                        };
                        db.ItemSubCategories.Add(subCategory);

                        db.ItemMinorCategories.AddRange(demoSub.MinorCategories.Select(name => new ItemMinorCategory
                        {
                            AccountId = account.Id,
                            BusinessUnitId = unit.Id,
                            Category = category,
                            SubCategory = subCategory,
                            Name = name,
                            IsActive = subCategory.IsActive,
                        }));
                    }
                }
            }

            logger.LogInformation("Seeded item departments, categories, sub and minor categories for {Unit}", unit.Name);
        }

        if (await db.UnitsOfMeasure.AcrossTenants().AnyAsync(u => u.AccountId == account.Id, ct)) return;

        db.UnitsOfMeasure.AddRange(ItemMasterDemoData.Uoms.Select(u => new UnitOfMeasure
        {
            AccountId = account.Id,
            Code = u.Code,
            Name = u.Name,
            Description = u.Description,
            IsDecimalAllowed = u.Decimals is not null,
            RoundingLength = u.Decimals ?? 0,
            IsActive = u.IsActive,
        }));
        logger.LogInformation("Seeded {Count} units of measure for {Account}", ItemMasterDemoData.Uoms.Length, account.Name);
    }

    /// <summary>The demo account gets every ERP feature added since its last grant; Panel removals of older ones are kept.</summary>
    private async Task EnsureFeaturesAsync(Account account, CancellationToken ct)
    {
        var lastGrant = await db.AccountFeatures.AcrossTenants()
            .Where(af => af.AccountId == account.Id)
            .MaxAsync(af => (DateTimeOffset?)af.CreatedAt, ct);

        var features = await db.ErpFeatures
            .Where(f => f.IsActive && (lastGrant == null || f.CreatedAt > lastGrant))
            .Select(f => f.Id)
            .ToListAsync(ct);
        db.AccountFeatures.AddRange(features.Select(id => new AccountFeature { AccountId = account.Id, FeatureId = id }));
        if (features.Count > 0) logger.LogInformation("Granted {Count} ERP feature(s) to {Account}", features.Count, account.Name);
    }

    private async Task EnsurePanelAdminAsync(SeedUserOptions seed, CancellationToken ct)
    {
        var email = Require(seed, "Seed:PanelAdmin");
        if (await db.PanelUsers.AnyAsync(u => u.Email == email, ct)) return;

        db.PanelUsers.Add(new PanelUser
        {
            Email = email,
            FullName = seed.FullName,
            PasswordHash = hasher.Hash(seed.Password),
            Role = PanelRole.SuperAdmin,
        });
        logger.LogInformation("Seeded Panel super admin {Email}", email);
    }

    private async Task<Account> EnsureAccountAsync(CancellationToken ct)
    {
        var account = await db.Accounts.SingleOrDefaultAsync(a => a.Code == AccountCode, ct);
        if (account is null)
        {
            account = new Account { Code = AccountCode, Name = "Demo Pharma Ltd" };
            db.Accounts.Add(account);
            logger.LogInformation("Seeded account {Account}", account.Name);
        }

        // Profile fields arrived after the first seed; fill them for older databases too.
        account.Email ??= "info@demopharma.com";
        account.Phone ??= "+8801700000000";
        account.Website ??= "https://demopharma.com";
        account.Industry ??= "Pharmaceuticals";
        account.Address ??= "Plot 12, Tejgaon I/A";
        account.City ??= "Dhaka";
        account.Country ??= "BD";
        return account;
    }

    private async Task<List<BusinessUnit>> EnsureBusinessUnitsAsync(Account account, CancellationToken ct)
    {
        var existing = await db.BusinessUnits.AcrossTenants()
            .Include(b => b.Offices)
            .Include(b => b.Warehouses)
            .Where(b => b.AccountId == account.Id)
            .ToListAsync(ct);

        foreach (var demo in Units)
        {
            var unit = existing.SingleOrDefault(e => e.Code == demo.Code);
            if (unit is null)
            {
                unit = new BusinessUnit { AccountId = account.Id, Code = demo.Code, Name = demo.Name };
                db.BusinessUnits.Add(unit);
                existing.Add(unit);
            }

            unit.Address ??= demo.Address;

            // Offices and warehouses are only seeded into a unit that has none, so user edits are left alone.
            if (unit.Offices.Count > 0 || unit.Warehouses.Count > 0) continue;

            var offices = demo.Offices.ToDictionary(
                o => o.Code,
                o => new Office { AccountId = account.Id, BusinessUnit = unit, Code = o.Code, Name = o.Name, Address = o.Address });
            db.Offices.AddRange(offices.Values);
            db.Warehouses.AddRange(demo.Warehouses.Select(w => new Warehouse
            {
                AccountId = account.Id,
                BusinessUnit = unit,
                Office = w.OfficeCode is null ? null : offices[w.OfficeCode],
                Code = w.Code,
                Name = w.Name,
            }));
        }

        return existing.Where(e => Units.Any(u => u.Code == e.Code)).ToList();
    }

    private async Task<Role> EnsureRoleAsync(
        Account account, string name, Func<Role> create, IReadOnlyCollection<string> permissions, CancellationToken ct)
    {
        var role = await db.Roles.AcrossTenants()
            .Include(r => r.Permissions)
            .SingleOrDefaultAsync(r => r.AccountId == account.Id && r.Name == name, ct);

        if (role is null)
        {
            role = create();
            db.Roles.Add(role);
            return role;
        }

        // Keep system roles in step with the catalog as modules add permissions.
        foreach (var code in permissions.Where(code => role.Permissions.All(p => p.PermissionCode != code)))
        {
            role.Permissions.Add(new RolePermission { PermissionCode = code });
        }

        return role;
    }

    private async Task EnsureErpAdminAsync(
        SeedUserOptions seed, Account account, List<BusinessUnit> units, Role role, CancellationToken ct)
    {
        var email = Require(seed, "Seed:ErpAdmin");
        var user = await db.ErpUsers.AcrossTenants()
            .Include(u => u.BusinessUnits)
            .SingleOrDefaultAsync(u => u.Email == email, ct);

        if (user is null)
        {
            user = new ErpUser
            {
                AccountId = account.Id,
                Username = Username(seed, email),
                Email = email,
                FullName = seed.FullName,
                PasswordHash = hasher.Hash(seed.Password),
                IsAccountAdmin = true,
            };
            db.ErpUsers.Add(user);
            logger.LogInformation("Seeded ERP admin {Email}", email);
        }

        foreach (var unit in units.Where(unit => user.BusinessUnits.All(x => x.BusinessUnitId != unit.Id)))
        {
            user.BusinessUnits.Add(new UserBusinessUnit { AccountId = account.Id, BusinessUnitId = unit.Id, Role = role });
        }
    }

    private static string Username(SeedUserOptions seed, string email) =>
        (string.IsNullOrWhiteSpace(seed.Username) ? email[..email.IndexOf('@')] : seed.Username).Trim().ToLowerInvariant();

    private static string Require(SeedUserOptions seed, string section)
    {
        if (string.IsNullOrWhiteSpace(seed.Email) || string.IsNullOrWhiteSpace(seed.Password))
        {
            throw new InvalidOperationException($"Demo seeding is enabled but {section}:Email/Password is not configured.");
        }

        return seed.Email.Trim().ToLowerInvariant();
    }
}
