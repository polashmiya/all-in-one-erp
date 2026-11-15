using Erp.Core.Domain;
using Erp.Core.Domain.Tenancy;
using Erp.Core.Tenancy;
using Erp.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Erp.Tests.Tenancy;

/// <summary>A business-unit-owned entity that exists only in tests (no real one exists until Item in Phase 3).</summary>
public sealed class StockNote : BaseEntity, IBusinessUnitOwned
{
    public Guid AccountId { get; set; }

    public Guid BusinessUnitId { get; set; }

    public required string Text { get; set; }
}

public sealed class TenancyTestDbContext(DbContextOptions<TenancyTestDbContext> options, ICurrentContext current)
    : ErpDbContext(options, current)
{
    public DbSet<StockNote> StockNotes => Set<StockNote>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Registered before the base call so the base applies row-version and tenant filters to it too.
        modelBuilder.Entity<StockNote>(e =>
        {
            e.ToTable("StockNotes");
            e.Property(n => n.Text).HasMaxLength(100);
        });
        base.OnModelCreating(modelBuilder);
    }
}

public sealed class TestContext : ICurrentContext
{
    public bool IsAuthenticated => UserId is not null;

    public Guid? UserId { get; set; }

    public Guid? AccountId { get; set; }

    public Guid? BusinessUnitId { get; set; }

    public bool IsAccountAdmin { get; set; }
}

/// <summary>Two accounts: A with units A1 + A2, B with unit B1; two notes per unit.</summary>
public sealed class TenancyDatabase : IAsyncLifetime
{
    private const string ConnectionString =
        "Server=(localdb)\\MSSQLLocalDB;Database=myErp_Test_Tenancy;Trusted_Connection=True;TrustServerCertificate=True;Connect Timeout=30";

    public Guid AccountA { get; private set; }

    public Guid AccountB { get; private set; }

    public Guid UnitA1 { get; private set; }

    public Guid UnitA2 { get; private set; }

    public Guid UnitB1 { get; private set; }

    public TenancyTestDbContext CreateContext(ICurrentContext current, params Microsoft.EntityFrameworkCore.Diagnostics.IInterceptor[] interceptors) =>
        new(new DbContextOptionsBuilder<TenancyTestDbContext>()
            .UseSqlServer(Environment.GetEnvironmentVariable("ERP_TEST_TENANCY_CONNECTION") ?? ConnectionString)
            .AddInterceptors(interceptors)
            .Options, current);

    public async Task InitializeAsync()
    {
        await using var db = CreateContext(SystemCurrentContext.Instance);
        await db.Database.EnsureDeletedAsync();
        await db.Database.EnsureCreatedAsync();

        var a = new Account { Code = "A", Name = "Account A" };
        var b = new Account { Code = "B", Name = "Account B" };
        var a1 = new BusinessUnit { Account = a, Code = "A1", Name = "A one" };
        var a2 = new BusinessUnit { Account = a, Code = "A2", Name = "A two" };
        var b1 = new BusinessUnit { Account = b, Code = "B1", Name = "B one" };
        db.AddRange(a, b, a1, a2, b1);
        await db.SaveChangesAsync();

        foreach (var unit in new[] { a1, a2, b1 })
        {
            db.StockNotes.AddRange(
                new StockNote { AccountId = unit.AccountId, BusinessUnitId = unit.Id, Text = $"{unit.Code}-1" },
                new StockNote { AccountId = unit.AccountId, BusinessUnitId = unit.Id, Text = $"{unit.Code}-2" });
        }

        await db.SaveChangesAsync();
        (AccountA, AccountB, UnitA1, UnitA2, UnitB1) = (a.Id, b.Id, a1.Id, a2.Id, b1.Id);
    }

    public async Task DisposeAsync()
    {
        await using var db = CreateContext(SystemCurrentContext.Instance);
        await db.Database.EnsureDeletedAsync();
    }
}

public sealed class TenantFilterTests(TenancyDatabase database) : IClassFixture<TenancyDatabase>
{
    [Fact]
    public async Task Account_owned_data_is_limited_to_the_current_account()
    {
        await using var asA = database.CreateContext(new TestContext { AccountId = database.AccountA });
        await using var asB = database.CreateContext(new TestContext { AccountId = database.AccountB });

        Assert.Equal(["A1", "A2"], await asA.BusinessUnits.OrderBy(b => b.Code).Select(b => b.Code).ToListAsync());
        Assert.Equal(["B1"], await asB.BusinessUnits.Select(b => b.Code).ToListAsync());
    }

    [Fact]
    public async Task No_tenant_in_context_sees_nothing()
    {
        await using var db = database.CreateContext(SystemCurrentContext.Instance);

        Assert.Empty(await db.BusinessUnits.ToListAsync());
        Assert.Empty(await db.StockNotes.ToListAsync());
    }

    [Fact]
    public async Task IgnoreQueryFilters_is_the_explicit_opt_out()
    {
        await using var db = database.CreateContext(SystemCurrentContext.Instance);

        Assert.Equal(3, await db.BusinessUnits.IgnoreQueryFilters().CountAsync());
        Assert.Equal(6, await db.StockNotes.IgnoreQueryFilters().CountAsync());
    }

    [Fact]
    public async Task Business_unit_owned_data_is_limited_to_the_current_unit()
    {
        await using var db = database.CreateContext(new TestContext { AccountId = database.AccountA, BusinessUnitId = database.UnitA1 });

        Assert.Equal(["A1-1", "A1-2"], await db.StockNotes.OrderBy(n => n.Text).Select(n => n.Text).ToListAsync());
    }

    [Fact]
    public async Task Another_accounts_unit_id_matches_nothing()
    {
        // Even if a unit id leaks, the account part of the filter still applies.
        await using var db = database.CreateContext(new TestContext { AccountId = database.AccountA, BusinessUnitId = database.UnitB1 });

        Assert.Empty(await db.StockNotes.ToListAsync());
    }

    [Fact]
    public async Task Filter_values_are_read_per_query_not_baked_into_the_model()
    {
        var context = new TestContext { AccountId = database.AccountA, BusinessUnitId = database.UnitA1 };
        await using var db = database.CreateContext(context);

        var first = await db.StockNotes.Select(n => n.Text).ToListAsync();
        context.BusinessUnitId = database.UnitA2;
        var second = await db.StockNotes.Select(n => n.Text).ToListAsync();

        Assert.All(first, t => Assert.StartsWith("A1", t));
        Assert.All(second, t => Assert.StartsWith("A2", t));
    }

    [Fact]
    public async Task Auditing_interceptor_stamps_created_and_updated_columns()
    {
        var userId = Guid.NewGuid();
        var context = new TestContext { UserId = userId, AccountId = database.AccountA, BusinessUnitId = database.UnitA1 };
        await using var db = database.CreateContext(context, new AuditingInterceptor(context, TimeProvider.System));

        var note = new StockNote { AccountId = database.AccountA, BusinessUnitId = database.UnitA1, Text = "audit" };
        db.StockNotes.Add(note);
        await db.SaveChangesAsync();

        Assert.Equal(userId, note.CreatedBy);
        Assert.True(note.CreatedAt > DateTimeOffset.UtcNow.AddMinutes(-1));
        Assert.NotEmpty(note.RowVersion);
        Assert.Null(note.UpdatedAt);

        note.Text = "audit-2";
        await db.SaveChangesAsync();

        Assert.Equal(userId, note.UpdatedBy);
        Assert.NotNull(note.UpdatedAt);

        db.StockNotes.Remove(note);
        await db.SaveChangesAsync();
    }
}
