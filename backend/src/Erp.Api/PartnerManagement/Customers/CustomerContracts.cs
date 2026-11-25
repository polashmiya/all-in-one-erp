using System.Text.Json;
using FluentValidation;

namespace Erp.Api.PartnerManagement.Customers;

public sealed record CustomerRow(
    int Id,
    string Code,
    string Name,
    string? BillingName,
    int? GroupId,
    string? GroupName,
    int? PriceTypeId,
    string? PriceTypeName,
    string? Mobile,
    int? DistrictId,
    string? DistrictName,
    decimal? CreditLimit,
    int? SalesRepresentativeId,
    string? SalesRepresentativeName,
    bool IsActive,
    DateTime? CreatedAt,
    DateTime? ModifiedAt,
    Dictionary<string, JsonElement>? CustomData);

public sealed record CustomerOption(
    int Id,
    string Code,
    string Name,
    string? ContactPerson,
    string? Phone,
    string? Address,
    int? SalesRepresentativeId,
    bool IsActive);

public sealed record CustomerList(IReadOnlyList<CustomerRow> Items, int Total, PartnerCounts Counts);

public sealed record SaveCustomerRequest : IPartnerRequest
{
    public required string? Code { get; init; }
    public required string Name { get; init; }
    public required string? BillingName { get; init; }
    public required int? GroupId { get; init; }
    public required int? PriceTypeId { get; init; }
    public required string? Mobile { get; init; }
    public required string? Email { get; init; }
    public required string? ContactPerson { get; init; }
    public required string? ContactMobile { get; init; }
    public required string? Address { get; init; }
    public required string? City { get; init; }
    public required int? DistrictId { get; init; }
    public required int? ThanaId { get; init; }
    public required int? TerritoryId { get; init; }
    public required bool IsForeign { get; init; }
    public required decimal? CreditLimit { get; init; }
    public required int? PaymentDays { get; init; }
    public required int? SalesRepresentativeId { get; init; }
    public required string? BinNumber { get; init; }
    public required string? TinNumber { get; init; }
    public required string? TradeLicenseNumber { get; init; }
    public required string? Nid { get; init; }
    public required string? CentralVatRegistrationNo { get; init; }
    public required bool IsVatNotApplicable { get; init; }
    public required bool IsVdsApplicable { get; init; }
    public required DateOnly? DateOfBirth { get; init; }
    public required int? GenderId { get; init; }
    public required string? Profession { get; init; }
    public required DateOnly? MarriageDate { get; init; }
    public required string? Remarks { get; init; }
    public required bool IsActive { get; init; }
    public Dictionary<string, JsonElement>? CustomData { get; init; }
}

public sealed record CustomerDetail
{
    public required int Id { get; init; }
    public required string Code { get; init; }
    public required string Name { get; init; }
    public required string? BillingName { get; init; }
    public required int? GroupId { get; init; }
    public required string? GroupName { get; init; }
    public required int? PriceTypeId { get; init; }
    public required string? PriceTypeName { get; init; }
    public required string? Mobile { get; init; }
    public required string? Email { get; init; }
    public required string? ContactPerson { get; init; }
    public required string? ContactMobile { get; init; }
    public required string? Address { get; init; }
    public required string? City { get; init; }
    public required int? DistrictId { get; init; }
    public required string? DistrictName { get; init; }
    public required int? ThanaId { get; init; }
    public required string? ThanaName { get; init; }
    public required int? TerritoryId { get; init; }
    public required string? TerritoryName { get; init; }
    public required bool IsForeign { get; init; }
    public required decimal? CreditLimit { get; init; }
    public required int? PaymentDays { get; init; }
    public required int? SalesRepresentativeId { get; init; }
    public required string? SalesRepresentativeName { get; init; }
    public required string? BinNumber { get; init; }
    public required string? TinNumber { get; init; }
    public required string? TradeLicenseNumber { get; init; }
    public required string? Nid { get; init; }
    public required string? CentralVatRegistrationNo { get; init; }
    public required bool IsVatNotApplicable { get; init; }
    public required bool IsVdsApplicable { get; init; }
    public required DateOnly? DateOfBirth { get; init; }
    public required int? GenderId { get; init; }
    public required string? GenderName { get; init; }
    public required string? Profession { get; init; }
    public required DateOnly? MarriageDate { get; init; }
    public required string? Remarks { get; init; }
    public required bool IsActive { get; init; }
    public required Dictionary<string, JsonElement>? CustomData { get; init; }
    public required DateTime? CreatedAt { get; init; }
    public required DateTime? ModifiedAt { get; init; }
}

internal sealed class SaveCustomerRequestValidator : AbstractValidator<SaveCustomerRequest>
{
    public SaveCustomerRequestValidator() => PartnerData.AddRules(this);
}
