using Erp.Core.Domain.Metadata;
using Erp.Core.Modules;
using Erp.Api.Panel.Configuration;
using Erp.Core.Validation;
using Erp.Infrastructure.Persistence;
using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace Erp.Api.Panel.Lookups;

public sealed record LookupTypeDto(string Key, LocalizedLabel Name);

public sealed record LookupValueRow(int Id, string? Code, string NameEn, string? NameBn, int SortOrder, bool IsDefault, bool IsActive);

public sealed record SaveLookupValueRequest(string? Code, string NameEn, string? NameBn, int SortOrder, bool IsDefault, bool IsActive);

internal sealed class SaveLookupValueRequestValidator : AbstractValidator<SaveLookupValueRequest>
{
    public SaveLookupValueRequestValidator()
    {
        RuleFor(x => x.Code).MaximumLength(50);
        RuleFor(x => x.NameEn).NotEmpty().WithMessage("Enter the English name.").MaximumLength(200);
        RuleFor(x => x.NameBn).MaximumLength(200);
        RuleFor(x => x.SortOrder).GreaterThanOrEqualTo(0);
        RuleFor(x => x.IsDefault).Equal(false).WithMessage("Only an active value can be the default.").When(x => !x.IsActive);
    }
}

public static class LookupListEndpoints
{
    public static IEndpointRouteBuilder MapLookupListEndpoints(this IEndpointRouteBuilder panel)
    {
        panel.MapGet("/lookup-types", Types)
            .WithName("ListLookupTypes")
            .WithTags("PanelLookups")
            .WithSummary("Lookup lists every account has, such as item types");

        var values = panel.MapGroup("/accounts/{id:guid}/lookups/{type}").WithTags("PanelLookups");

        values.MapGet("", List)
            .WithName("ListAccountLookupValues")
            .WithSummary("The account's values of one lookup list")
            .ProducesProblem(StatusCodes.Status404NotFound);

        values.MapPost("", Create)
            .WithName("CreateLookupValue")
            .Validate<SaveLookupValueRequest>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        values.MapPut("/{valueId:int}", Update)
            .WithName("UpdateLookupValue")
            .Validate<SaveLookupValueRequest>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        return panel;
    }

    private static Task<List<LookupTypeDto>> Types(ErpDbContext db, CancellationToken ct) =>
        db.LookupTypes.AsNoTracking()
            .Where(t => t.IsActive)
            .OrderBy(t => t.NameEn)
            .Select(t => new LookupTypeDto(t.Key, new LocalizedLabel(t.NameEn, t.NameBn)))
            .ToListAsync(ct);

    private static async Task<Results<Ok<List<LookupValueRow>>, NotFound>> List(
        Guid id, string type, ErpDbContext db, CancellationToken ct)
    {
        if (await TypeIdAsync(db, id, type, ct) is not { } typeId) return TypedResults.NotFound();

        return TypedResults.Ok(await db.LookupValues.AcrossTenants().AsNoTracking()
            .Where(v => v.AccountId == id && v.LookupTypeId == typeId)
            .OrderBy(v => v.SortOrder).ThenBy(v => v.NameEn)
            .Select(v => new LookupValueRow(v.Id, v.Code, v.NameEn, v.NameBn, v.SortOrder, v.IsDefault, v.IsActive))
            .ToListAsync(ct));
    }

    private static async Task<Results<Ok<LookupValueRow>, NotFound, ValidationProblem>> Create(
        Guid id, string type, SaveLookupValueRequest request, ErpDbContext db, CancellationToken ct)
    {
        if (await TypeIdAsync(db, id, type, ct) is not { } typeId) return TypedResults.NotFound();

        var value = new LookupValue { AccountId = id, LookupTypeId = typeId, NameEn = request.NameEn };
        db.LookupValues.Add(value);
        return await SaveAsync(db, value, request, ct);
    }

    private static async Task<Results<Ok<LookupValueRow>, NotFound, ValidationProblem>> Update(
        Guid id, string type, int valueId, SaveLookupValueRequest request, ErpDbContext db, CancellationToken ct)
    {
        if (await TypeIdAsync(db, id, type, ct) is not { } typeId) return TypedResults.NotFound();

        var value = await db.LookupValues.AcrossTenants()
            .SingleOrDefaultAsync(v => v.Id == valueId && v.AccountId == id && v.LookupTypeId == typeId, ct);
        if (value is null) return TypedResults.NotFound();

        return await SaveAsync(db, value, request, ct);
    }

    private static async Task<Guid?> TypeIdAsync(ErpDbContext db, Guid accountId, string type, CancellationToken ct) =>
        await db.Accounts.AnyAsync(a => a.Id == accountId, ct)
            ? await db.LookupTypes.Where(t => t.Key == type && t.IsActive).Select(t => (Guid?)t.Id).SingleOrDefaultAsync(ct)
            : null;

    private static async Task<Results<Ok<LookupValueRow>, NotFound, ValidationProblem>> SaveAsync(
        ErpDbContext db, LookupValue value, SaveLookupValueRequest request, CancellationToken ct)
    {
        var before = value.Id > 0 ? Row(value) : null;
        value.Code = string.IsNullOrWhiteSpace(request.Code) ? null : request.Code.Trim();
        value.NameEn = request.NameEn.Trim();
        value.NameBn = string.IsNullOrWhiteSpace(request.NameBn) ? null : request.NameBn.Trim();
        value.SortOrder = request.SortOrder;
        value.IsDefault = request.IsDefault;
        value.IsActive = request.IsActive;

        if (value.IsDefault)
        {
            await db.LookupValues.AcrossTenants()
                .Where(v => v.AccountId == value.AccountId && v.LookupTypeId == value.LookupTypeId && v.Id != value.Id && v.IsDefault)
                .ExecuteUpdateAsync(s => s.SetProperty(v => v.IsDefault, false), ct);
        }

        var type = await db.LookupTypes.Where(t => t.Id == value.LookupTypeId).Select(t => t.Key).SingleAsync(ct);
        ConfigurationHistory.Record(db, value.AccountId, null, null, ConfigurationAreas.LookupValue, type, before, Row(value));

        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException e) when (e.InnerException is SqlException { Number: 2601 or 2627 })
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                ["code"] = ["Another value of this list already uses this code."],
            });
        }

        await db.AccountFeatures.AcrossTenants()
            .Where(af => af.AccountId == value.AccountId)
            .ExecuteUpdateAsync(s => s.SetProperty(af => af.ConfigVersion, af => af.ConfigVersion + 1), ct);

        return TypedResults.Ok(Row(value));
    }

    private static LookupValueRow Row(LookupValue value) =>
        new(value.Id, value.Code, value.NameEn, value.NameBn, value.SortOrder, value.IsDefault, value.IsActive);
}
