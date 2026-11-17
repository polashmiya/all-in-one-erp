using System.Text.Json;
using Erp.Core.Domain.Partners;
using Erp.Infrastructure.Persistence;
using Erp.Metadata;
using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace Erp.Api.PartnerManagement;

public sealed record PartnerCounts(int All, int Active, int Inactive);

public sealed record PartnerStatusRequest(IReadOnlyList<int> Ids, bool IsActive);

public interface IPartnerRequest
{
    string? Code { get; }
    string Name { get; }
    string? BillingName { get; }
    int? GroupId { get; }
    string? Mobile { get; }
    string? Email { get; }
    string? ContactPerson { get; }
    string? ContactMobile { get; }
    string? Address { get; }
    string? City { get; }
    int? DistrictId { get; }
    int? ThanaId { get; }
    bool IsForeign { get; }
    decimal? CreditLimit { get; }
    int? PaymentDays { get; }
    string? BinNumber { get; }
    string? TinNumber { get; }
    string? TradeLicenseNumber { get; }
    string? Nid { get; }
    string? CentralVatRegistrationNo { get; }
    bool IsVatNotApplicable { get; }
    bool IsVdsApplicable { get; }
    string? Remarks { get; }
    bool IsActive { get; }
    Dictionary<string, JsonElement>? CustomData { get; }
}

internal sealed class PartnerStatusRequestValidator : AbstractValidator<PartnerStatusRequest>
{
    public PartnerStatusRequestValidator()
    {
        RuleFor(x => x.Ids).NotEmpty();
    }
}

internal static class PartnerData
{
    public const int DefaultPageSize = 25;
    public const int MaxPageSize = 1000;
    private const int CodeDigits = 5;
    private const string ChooseFromList = "Choose an active value from the list.";

    public static void AddRules<T>(AbstractValidator<T> validator) where T : IPartnerRequest
    {
        validator.RuleFor(x => x.Name).NotEmpty();
        validator.RuleFor(x => x.Email).EmailAddress().When(x => !string.IsNullOrWhiteSpace(x.Email));
    }

    public static IQueryable<T> Search<T>(IQueryable<T> partners, string? search) where T : PartnerRecord
    {
        if (string.IsNullOrWhiteSpace(search)) return partners;
        var term = search.Trim();
        return partners.Where(p => p.Name.Contains(term) || p.Code.Contains(term) ||
            (p.Mobile != null && p.Mobile.Contains(term)) ||
            (p.BillingName != null && p.BillingName.Contains(term)) ||
            (p.ContactPerson != null && p.ContactPerson.Contains(term)));
    }

    public static async Task<(IQueryable<T> Partners, PartnerCounts Counts)> ByStatusAsync<T>(
        IQueryable<T> partners, string? status, CancellationToken cancellationToken) where T : PartnerRecord
    {
        var counts = new PartnerCounts(
            await partners.CountAsync(cancellationToken),
            await partners.CountAsync(p => p.IsActive, cancellationToken),
            await partners.CountAsync(p => !p.IsActive, cancellationToken));
        var filtered = status switch
        {
            "active" => partners.Where(p => p.IsActive),
            "inactive" => partners.Where(p => !p.IsActive),
            _ => partners,
        };
        return (filtered, counts);
    }

    public static IQueryable<T> Page<T>(IQueryable<T> partners, string? sort, int? page, int? pageSize) where T : PartnerRecord
    {
        var size = Math.Clamp(pageSize ?? DefaultPageSize, 1, MaxPageSize);
        var sorted = sort switch
        {
            "-name" => partners.OrderByDescending(p => p.Name),
            "code" => partners.OrderBy(p => p.Code),
            "-code" => partners.OrderByDescending(p => p.Code),
            "modifiedAt" => partners.OrderBy(p => p.ModifiedAt ?? p.CreatedAt),
            "-modifiedAt" => partners.OrderByDescending(p => p.ModifiedAt ?? p.CreatedAt),
            _ => partners.OrderBy(p => p.Name),
        };
        return sorted.Skip((Math.Max(1, page ?? 1) - 1) * size).Take(size);
    }

    public static async Task<ValidationProblem?> SetStatusAsync<T>(
        IQueryable<T> partners, PartnerStatusRequest request, ErpDbContext db, CancellationToken cancellationToken) where T : PartnerRecord
    {
        var found = await partners.Where(p => request.Ids.Contains(p.Id)).ToListAsync(cancellationToken);
        if (found.Count != request.Ids.Distinct().Count())
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]> { ["ids"] = ["Every record must be in the selected unit."] });
        }

        foreach (var partner in found) partner.IsActive = request.IsActive;
        await db.SaveChangesAsync(cancellationToken);
        return null;
    }

    public static async Task<string?> CodeAsync<T>(
        IQueryable<T> partners, T partner, string? sent, Dictionary<string, string[]> errors, CancellationToken cancellationToken)
        where T : PartnerRecord
    {
        var code = Trimmed(sent)?.ToUpperInvariant() ?? (partner.Id > 0 ? partner.Code : null);
        if (code is not null && await partners.AnyAsync(p => p.Id != partner.Id && p.Code == code, cancellationToken))
        {
            errors["code"] = ["Another record already uses this code."];
        }

        return code;
    }

    public static async Task<string> NextCodeAsync<T>(IQueryable<T> partners, string prefix, CancellationToken cancellationToken)
        where T : PartnerRecord
    {
        var codes = await partners.Where(p => p.Code.StartsWith(prefix)).Select(p => p.Code).ToListAsync(cancellationToken);
        var last = codes.Select(c => int.TryParse(c[prefix.Length..], out var n) ? n : 0).DefaultIfEmpty(0).Max();
        return $"{prefix}{(last + 1).ToString($"D{CodeDigits}")}";
    }

    public static async Task CheckLookupsAsync(
        ErpDbContext db,
        IEnumerable<(string Field, int? Id, int? Current, string Type)> choices,
        Dictionary<string, string[]> errors,
        CancellationToken cancellationToken)
    {
        var chosen = choices.Where(c => c.Id is not null).ToList();
        var ids = chosen.Select(c => c.Id).ToList();
        var values = await db.LookupValues.AsNoTracking()
            .Where(v => ids.Contains(v.Id))
            .Select(v => new { v.Id, Type = v.LookupType!.Key, v.IsActive })
            .ToListAsync(cancellationToken);

        foreach (var choice in chosen)
        {
            var value = values.Find(v => v.Id == choice.Id && v.Type == choice.Type);
            if (value is null || (!value.IsActive && choice.Id != choice.Current)) errors[choice.Field] = [ChooseFromList];
        }
    }

    public static async Task CheckPlaceAsync(
        ErpDbContext db, IPartnerRequest request, Dictionary<string, string[]> errors, CancellationToken cancellationToken)
    {
        if (request.DistrictId is { } districtId && !await db.Districts.AnyAsync(d => d.Id == districtId, cancellationToken))
        {
            errors["districtId"] = [ChooseFromList];
        }

        if (request.ThanaId is { } thanaId &&
            !await db.Thanas.AnyAsync(t => t.Id == thanaId && (request.DistrictId == null || t.DistrictId == request.DistrictId), cancellationToken))
        {
            errors["thanaId"] = ["Choose a thana of the selected district."];
        }
    }

    public static async Task<(string? District, string? Thana)> PlaceNamesAsync(
        ErpDbContext db, int? districtId, int? thanaId, CancellationToken cancellationToken) =>
    (
        await db.Districts.Where(d => d.Id == districtId).Select(d => d.Name).FirstOrDefaultAsync(cancellationToken),
        await db.Thanas.Where(t => t.Id == thanaId).Select(t => t.Name).FirstOrDefaultAsync(cancellationToken)
    );

    public static Task<Dictionary<int, string>> LookupNamesAsync(ErpDbContext db, int?[] ids, CancellationToken cancellationToken) =>
        db.LookupValues.AsNoTracking()
            .Where(v => ids.Contains(v.Id))
            .ToDictionaryAsync(v => v.Id, v => v.NameEn, cancellationToken);

    public static void Apply(PartnerRecord partner, IPartnerRequest request)
    {
        partner.Name = request.Name.Trim();
        partner.BillingName = Trimmed(request.BillingName);
        partner.GroupId = request.GroupId;
        partner.Mobile = Trimmed(request.Mobile);
        partner.Email = Trimmed(request.Email)?.ToLowerInvariant();
        partner.ContactPerson = Trimmed(request.ContactPerson);
        partner.ContactMobile = Trimmed(request.ContactMobile);
        partner.Address = Trimmed(request.Address);
        partner.City = Trimmed(request.City);
        partner.DistrictId = request.DistrictId;
        partner.ThanaId = request.ThanaId;
        partner.IsForeign = request.IsForeign;
        partner.CreditLimit = request.CreditLimit;
        partner.PaymentDays = request.PaymentDays;
        partner.BinNumber = Trimmed(request.BinNumber);
        partner.TinNumber = Trimmed(request.TinNumber);
        partner.TradeLicenseNumber = Trimmed(request.TradeLicenseNumber);
        partner.Nid = Trimmed(request.Nid);
        partner.CentralVatRegistrationNo = Trimmed(request.CentralVatRegistrationNo);
        partner.IsVatNotApplicable = request.IsVatNotApplicable;
        partner.IsVdsApplicable = request.IsVdsApplicable;
        partner.Remarks = Trimmed(request.Remarks);
        partner.IsActive = request.IsActive;
        partner.CustomData = CustomFieldData.ToJson(request.CustomData);
    }

    public static string? Trimmed(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
