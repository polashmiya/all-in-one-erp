using Erp.Api.Panel.Configuration;
using Erp.Core.Domain.Metadata;
using Erp.Core.Domain.Tenancy;
using Erp.Core.Validation;
using Erp.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace Erp.Api.Panel.Accounts;

public static class AccountEditEndpoints
{
    public static IEndpointRouteBuilder MapAccountEditEndpoints(this IEndpointRouteBuilder panel)
    {
        var account = panel.MapGroup("/accounts/{id:guid}").WithTags("PanelAccounts");

        account.MapPut("", Update)
            .WithName("UpdateAccount")
            .WithSummary("Change the account's profile and whether it is active; the code never changes")
            .Validate<UpdateAccountRequest>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        account.MapPost("/structure", AddNode)
            .WithName("AddStructureNode")
            .WithSummary("Add a business unit, an office or a warehouse to the account")
            .Validate<StructureNodeRequest>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        account.MapPut("/structure/{nodeId:guid}", UpdateNode)
            .WithName("UpdateStructureNode")
            .WithSummary("Rename, move or deactivate a business unit, office or warehouse; nothing is ever deleted")
            .Validate<StructureNodeRequest>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        account.MapPut("/theme", SaveTheme)
            .WithName("SaveAccountTheme")
            .WithSummary("The look the account's ERP users get when they sign in")
            .Validate<AccountTheme>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        account.MapDelete("/theme", ResetTheme)
            .WithName("ResetAccountTheme")
            .WithSummary("Go back to the default ERP look")
            .ProducesProblem(StatusCodes.Status404NotFound);

        return panel;
    }

    private static async Task<Results<NoContent, NotFound>> Update(
        Guid id, UpdateAccountRequest request, ErpDbContext db, CancellationToken ct)
    {
        if (await db.Accounts.SingleOrDefaultAsync(a => a.Id == id, ct) is not { } account) return TypedResults.NotFound();

        var info = request.Account;
        account.Name = info.Name.Trim();
        account.Email = info.Email.Trim();
        account.Phone = info.Phone.Trim();
        account.Website = Clean(info.Website);
        account.Industry = Clean(info.Industry);
        account.TaxId = Clean(info.TaxId);
        account.Address = info.Address.Trim();
        account.City = Clean(info.City);
        account.Country = info.Country.Trim().ToUpperInvariant();
        account.BaseCurrency = info.BaseCurrency.Trim().ToUpperInvariant();
        account.TimeZone = info.TimeZone.Trim();
        account.IsActive = request.IsActive;

        await db.SaveChangesAsync(ct);
        return TypedResults.NoContent();
    }

    private static Task<Results<Ok<StructureNodeResult>, NotFound, ValidationProblem>> AddNode(
        Guid id, StructureNodeRequest request, ErpDbContext db, CancellationToken ct) =>
        SaveNodeAsync(db, id, null, request, ct);

    private static Task<Results<Ok<StructureNodeResult>, NotFound, ValidationProblem>> UpdateNode(
        Guid id, Guid nodeId, StructureNodeRequest request, ErpDbContext db, CancellationToken ct) =>
        SaveNodeAsync(db, id, nodeId, request, ct);

    private static async Task<Results<Ok<StructureNodeResult>, NotFound, ValidationProblem>> SaveNodeAsync(
        ErpDbContext db, Guid accountId, Guid? nodeId, StructureNodeRequest request, CancellationToken ct)
    {
        if (!await db.Accounts.AnyAsync(a => a.Id == accountId, ct)) return TypedResults.NotFound();

        var code = AccountSetupRequestValidator.Normalize(request.Code);
        var name = request.Name.Trim();

        if (request.Kind == StructureKinds.BusinessUnit)
        {
            var units = await db.BusinessUnits.AcrossTenants().Where(b => b.AccountId == accountId).ToListAsync(ct);
            var unit = nodeId is null ? new BusinessUnit { AccountId = accountId, Code = code, Name = name } : units.Find(b => b.Id == nodeId);
            if (unit is null) return TypedResults.NotFound();
            if (units.Any(b => b != unit && b.Code == code)) return Invalid("code", "Another business unit already uses this code.");
            if (!request.IsActive && !units.Any(b => b != unit && b.IsActive))
                return Invalid("isActive", "An account needs at least one active business unit.");

            (unit.Code, unit.Name, unit.IsActive) = (code, name, request.IsActive);
            (unit.Email, unit.Phone, unit.Address) = (Clean(request.Email), Clean(request.Phone), Clean(request.Address));
            if (nodeId is null) db.BusinessUnits.Add(unit);
            await db.SaveChangesAsync(ct);
            return TypedResults.Ok(new StructureNodeResult(unit.Id));
        }

        var offices = await db.Offices.AcrossTenants().Where(o => o.AccountId == accountId).ToListAsync(ct);

        if (request.Kind == StructureKinds.Office)
        {
            var office = nodeId is null ? null : offices.Find(o => o.Id == nodeId);
            if (nodeId is not null && office is null) return TypedResults.NotFound();
            var unitId = office?.BusinessUnitId ?? request.BusinessUnitId;
            if (!await IsUnitAsync(db, accountId, unitId, ct)) return Invalid("businessUnitId", "Choose a business unit of this account.");
            if (offices.Any(o => o != office && o.BusinessUnitId == unitId && o.Code == code))
                return Invalid("code", "Another office in this business unit uses this code.");

            office ??= db.Offices.Add(new Office { AccountId = accountId, BusinessUnitId = unitId!.Value, Code = code, Name = name }).Entity;
            (office.Code, office.Name, office.IsActive) = (code, name, request.IsActive);
            (office.Email, office.Phone, office.Address) = (Clean(request.Email), Clean(request.Phone), Clean(request.Address));
            await db.SaveChangesAsync(ct);
            return TypedResults.Ok(new StructureNodeResult(office.Id));
        }

        var warehouses = await db.Warehouses.AcrossTenants().Where(w => w.AccountId == accountId).ToListAsync(ct);
        var warehouse = nodeId is null ? null : warehouses.Find(w => w.Id == nodeId);
        if (nodeId is not null && warehouse is null) return TypedResults.NotFound();
        var ownerId = warehouse?.BusinessUnitId ?? request.BusinessUnitId;
        if (!await IsUnitAsync(db, accountId, ownerId, ct)) return Invalid("businessUnitId", "Choose a business unit of this account.");
        if (request.OfficeId is not null && !offices.Any(o => o.Id == request.OfficeId && o.BusinessUnitId == ownerId))
            return Invalid("officeId", "Choose an office of this business unit.");
        if (warehouses.Any(w => w != warehouse && w.BusinessUnitId == ownerId && w.Code == code))
            return Invalid("code", "Another warehouse in this business unit uses this code.");

        warehouse ??= db.Warehouses.Add(new Warehouse { AccountId = accountId, BusinessUnitId = ownerId!.Value, Code = code, Name = name }).Entity;
        (warehouse.Code, warehouse.Name, warehouse.IsActive) = (code, name, request.IsActive);
        (warehouse.OfficeId, warehouse.Address) = (request.OfficeId, Clean(request.Address));
        await db.SaveChangesAsync(ct);
        return TypedResults.Ok(new StructureNodeResult(warehouse.Id));
    }

    private static Task<Results<NoContent, NotFound>> SaveTheme(Guid id, AccountTheme theme, ErpDbContext db, CancellationToken ct) =>
        SetThemeAsync(db, id, theme, ct);

    private static Task<Results<NoContent, NotFound>> ResetTheme(Guid id, ErpDbContext db, CancellationToken ct) =>
        SetThemeAsync(db, id, null, ct);

    private static async Task<Results<NoContent, NotFound>> SetThemeAsync(
        ErpDbContext db, Guid id, AccountTheme? theme, CancellationToken ct)
    {
        if (await db.Accounts.SingleOrDefaultAsync(a => a.Id == id, ct) is not { } account) return TypedResults.NotFound();

        ConfigurationHistory.Record(db, id, null, null, ConfigurationAreas.Theme, ConfigurationAreas.Theme, AccountTheme.Parse(account.Theme), theme);
        account.Theme = theme?.ToJson();
        await db.SaveChangesAsync(ct);
        return TypedResults.NoContent();
    }

    private static Task<bool> IsUnitAsync(ErpDbContext db, Guid accountId, Guid? unitId, CancellationToken ct) =>
        db.BusinessUnits.AcrossTenants().AnyAsync(b => b.Id == unitId && b.AccountId == accountId, ct);

    private static ValidationProblem Invalid(string field, string message) =>
        TypedResults.ValidationProblem(new Dictionary<string, string[]> { [field] = [message] });

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
