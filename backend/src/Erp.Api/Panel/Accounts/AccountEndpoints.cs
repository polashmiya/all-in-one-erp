using Erp.Core.Validation;
using Erp.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace Erp.Api.Panel.Accounts;

/// <summary>
/// Panel account management. The Panel works across tenants, so every query starts from AcrossTenants();
/// the Panel audience on the route group is what restricts these endpoints to the internal team.
/// </summary>
public static class AccountEndpoints
{
    public static IEndpointRouteBuilder MapAccountEndpoints(this IEndpointRouteBuilder panel)
    {
        var accounts = panel.MapGroup("/accounts").WithTags("PanelAccounts");

        accounts.MapGet("", List)
            .WithName("ListAccounts")
            .WithSummary("Accounts with contact info and structure counts, plus overall totals");

        accounts.MapGet("/availability", Availability)
            .WithName("CheckAccountAvailability")
            .WithSummary("Whether an account code and an admin email are still free");

        accounts.MapGet("/{id:guid}", Get)
            .WithName("GetAccount")
            .WithSummary("Account profile, ERP admin and the business unit / office / warehouse tree")
            .ProducesProblem(StatusCodes.Status404NotFound);

        accounts.MapPost("/setup", Setup)
            .WithName("SetupAccount")
            .WithSummary("Create an account with its business units, offices, warehouses, ERP features and ERP admin in one transaction")
            .Validate<AccountSetupRequest>()
            .ProducesProblem(StatusCodes.Status409Conflict);

        accounts.MapGet("/{id:guid}/features", GetFeatures)
            .WithName("GetAccountFeatures")
            .WithSummary("Codes of the ERP features (menus) the account has")
            .ProducesProblem(StatusCodes.Status404NotFound);

        accounts.MapPut("/{id:guid}/features", UpdateFeatures)
            .WithName("UpdateAccountFeatures")
            .WithSummary("Replace the account's ERP features; its users' menus and permissions follow immediately")
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound);

        return panel;
    }

    private static async Task<Results<Ok<AccountFeaturesResponse>, NotFound>> GetFeatures(
        Guid id, ErpDbContext db, AccountFeatureService features, CancellationToken cancellationToken)
    {
        if (!await db.Accounts.AnyAsync(a => a.Id == id, cancellationToken)) return TypedResults.NotFound();
        return TypedResults.Ok(new AccountFeaturesResponse(await features.CodesAsync(id, cancellationToken)));
    }

    private static async Task<Results<Ok<AccountFeaturesResponse>, ValidationProblem, NotFound>> UpdateFeatures(
        Guid id, AccountFeaturesRequest request, ErpDbContext db, AccountFeatureService features, CancellationToken cancellationToken)
    {
        if (!await db.Accounts.AnyAsync(a => a.Id == id, cancellationToken)) return TypedResults.NotFound();

        var (featureIds, unknown) = await features.ResolveAsync(request.FeatureCodes ?? [], cancellationToken);
        if (unknown.Count > 0)
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                [AccountFeatureService.ErrorKey] = [AccountFeatureService.UnknownMessage(unknown)],
            });
        }

        await features.ReplaceAsync(id, featureIds, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        return TypedResults.Ok(new AccountFeaturesResponse(await features.CodesAsync(id, cancellationToken)));
    }

    /// <param name="status">all (default), active or inactive.</param>
    /// <param name="sort">name, -name, code, -code, createdAt or -createdAt (default).</param>
    private static async Task<AccountListResponse> List(
        string? search,
        string? status,
        string? sort,
        int? page,
        int? pageSize,
        ErpDbContext db,
        CancellationToken cancellationToken)
    {
        var accounts = db.Accounts.AcrossTenants().AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            accounts = accounts.Where(a =>
                a.Name.Contains(term) || a.Code.Contains(term) ||
                (a.Email != null && a.Email.Contains(term)) ||
                (a.Phone != null && a.Phone.Contains(term)) ||
                (a.City != null && a.City.Contains(term)));
        }

        accounts = status?.ToLowerInvariant() switch
        {
            "active" => accounts.Where(a => a.IsActive),
            "inactive" => accounts.Where(a => !a.IsActive),
            _ => accounts,
        };

        var total = await accounts.CountAsync(cancellationToken);
        var pageNumber = Math.Max(1, page ?? 1);
        var size = Math.Clamp(pageSize ?? 10, 1, 100);

        var sorted = sort switch
        {
            "name" => accounts.OrderBy(a => a.Name),
            "-name" => accounts.OrderByDescending(a => a.Name),
            "code" => accounts.OrderBy(a => a.Code),
            "-code" => accounts.OrderByDescending(a => a.Code),
            "createdAt" => accounts.OrderBy(a => a.CreatedAt),
            _ => accounts.OrderByDescending(a => a.CreatedAt),
        };

        var items = await sorted
            .Skip((pageNumber - 1) * size)
            .Take(size)
            .Select(a => new AccountListItem(
                a.Id,
                a.Code,
                a.Name,
                a.Email,
                a.Phone,
                a.City,
                a.Country,
                a.IsActive,
                a.CreatedAt,
                db.BusinessUnits.Count(b => b.AccountId == a.Id),
                db.Offices.Count(o => o.AccountId == a.Id),
                db.Warehouses.Count(w => w.AccountId == a.Id),
                db.ErpUsers.Count(u => u.AccountId == a.Id),
                db.AccountFeatures.Count(f => f.AccountId == a.Id),
                db.ErpUsers.Where(u => u.AccountId == a.Id && u.IsPrimaryAdmin)
                    .OrderBy(u => u.CreatedAt).Select(u => u.Email).FirstOrDefault()))
            .ToListAsync(cancellationToken);

        var summary = new AccountSummary(
            await db.Accounts.CountAsync(cancellationToken),
            await db.Accounts.CountAsync(a => a.IsActive, cancellationToken),
            await db.BusinessUnits.AcrossTenants().CountAsync(cancellationToken),
            await db.Offices.AcrossTenants().CountAsync(cancellationToken),
            await db.Warehouses.AcrossTenants().CountAsync(cancellationToken));

        return new AccountListResponse(items, total, summary);
    }

    private static async Task<AvailabilityResponse> Availability(
        string? accountCode, string? adminEmail, ErpDbContext db, CancellationToken cancellationToken)
    {
        bool? codeFree = null;
        if (!string.IsNullOrWhiteSpace(accountCode))
        {
            var code = AccountSetupRequestValidator.Normalize(accountCode);
            codeFree = !await db.Accounts.AnyAsync(a => a.Code == code, cancellationToken);
        }

        bool? emailFree = null;
        if (!string.IsNullOrWhiteSpace(adminEmail))
        {
            var email = AccountSetupService.NormalizeEmail(adminEmail);
            emailFree = !await db.ErpUsers.AcrossTenants().AnyAsync(u => u.Email == email, cancellationToken);
        }

        return new AvailabilityResponse(codeFree, emailFree);
    }

    private static async Task<Results<Ok<AccountDetail>, NotFound>> Get(
        Guid id, ErpDbContext db, AccountFeatureService features, CancellationToken cancellationToken)
    {
        var account = await db.Accounts.AcrossTenants().AsNoTracking().AsSplitQuery()
            .Include(a => a.BusinessUnits).ThenInclude(b => b.Offices).ThenInclude(o => o.Warehouses)
            .Include(a => a.BusinessUnits).ThenInclude(b => b.Warehouses)
            .SingleOrDefaultAsync(a => a.Id == id, cancellationToken);

        if (account is null) return TypedResults.NotFound();

        var admin = await db.ErpUsers.AcrossTenants().AsNoTracking()
            .Where(u => u.AccountId == id && u.IsPrimaryAdmin)
            .OrderBy(u => u.CreatedAt)
            .Select(u => new AccountAdminDto(u.Id, u.FullName, u.Email, u.IsActive, u.LastLoginAt))
            .FirstOrDefaultAsync(cancellationToken);

        static WarehouseDetail ToWarehouse(Core.Domain.Tenancy.Warehouse w) => new(w.Id, w.Code, w.Name, w.Address, w.IsActive);

        var units = account.BusinessUnits
            .OrderBy(b => b.Code)
            .Select(b => new BusinessUnitDetail(
                b.Id, b.Code, b.Name, b.Email, b.Phone, b.Address, b.IsActive,
                b.Offices.OrderBy(o => o.Code)
                    .Select(o => new OfficeDetail(o.Id, o.Code, o.Name, o.Email, o.Phone, o.Address, o.IsActive,
                        o.Warehouses.OrderBy(w => w.Code).Select(ToWarehouse).ToList()))
                    .ToList(),
                b.Warehouses.Where(w => w.OfficeId is null).OrderBy(w => w.Code).Select(ToWarehouse).ToList()))
            .ToList();

        return TypedResults.Ok(new AccountDetail(
            account.Id, account.Code, account.Name, account.Email, account.Phone, account.Website, account.Industry,
            account.TaxId, account.Address, account.City, account.Country, account.BaseCurrency, account.TimeZone,
            account.IsActive, account.CreatedAt, admin, units, await features.CodesAsync(id, cancellationToken),
            AccountTheme.Parse(account.Theme)));
    }

    private static async Task<Results<Created<AccountSetupResult>, ValidationProblem, ProblemHttpResult>> Setup(
        AccountSetupRequest request, AccountSetupService service, CancellationToken cancellationToken)
    {
        try
        {
            var outcome = await service.SetupAsync(request, cancellationToken);
            if (outcome.Errors is not null) return TypedResults.ValidationProblem(outcome.Errors);

            var result = outcome.Result!;
            return TypedResults.Created($"/api/panel/accounts/{result.AccountId}", result);
        }
        catch (DbUpdateException e) when (e.InnerException is SqlException { Number: 2601 or 2627 })
        {
            // Another setup took the same code or email between our check and the insert.
            return TypedResults.Problem(
                title: "Account already exists",
                detail: "An account with this code, or a user with this admin email, was created a moment ago.",
                statusCode: StatusCodes.Status409Conflict);
        }
    }
}
