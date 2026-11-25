using System.Text.Json;
using Erp.Api.Documents;
using FluentValidation;

namespace Erp.Api.Sales.SalesCollections;

public sealed record SalesCollectionRow(
    int Id,
    string Code,
    DateOnly CollectionDate,
    int CustomerId,
    string CustomerName,
    bool IsAdvance,
    int? PaymentMethodId,
    string? PaymentMethodName,
    string? MoneyReceiptNo,
    int? CollectedById,
    string? CollectedByName,
    string Status,
    int BillCount,
    decimal CollectionAmount,
    decimal SettledAmount,
    DateTime? CreatedAt,
    DateTime? ModifiedAt);

public sealed record SalesCollectionList(IReadOnlyList<SalesCollectionRow> Items, int Total, ApprovalCounts Counts);

public sealed record CollectableBill(
    int Id, string Code, DateOnly DeliveryDate, string? OrderCode, string? ChallanNo, int PaymentTermDays, DateOnly DueDate,
    decimal BillAmount, decimal ReturnAmount, decimal Collected, decimal Due);

public sealed record CustomerReceivable(IReadOnlyList<CollectableBill> Bills, decimal AvailableAdvance);

public sealed record SaveSalesCollectionLine
{
    public required int DeliveryId { get; init; }
    public required decimal CollectionAmount { get; init; }
    public required decimal? Vds { get; init; }
    public required decimal? Ait { get; init; }
    public required decimal? DiscountAmount { get; init; }
    public required decimal? ExpenseAmount { get; init; }
    public required decimal? AdvanceAdjusted { get; init; }
    public required string? Remarks { get; init; }
}

public sealed record SaveSalesCollectionRequest
{
    public required string? Code { get; init; }
    public required DateOnly CollectionDate { get; init; }
    public required int CustomerId { get; init; }
    public required bool IsAdvance { get; init; }
    public required decimal? AdvanceAmount { get; init; }
    public required Guid? OfficeId { get; init; }
    public required int? CollectedById { get; init; }
    public required string? MoneyReceiptNo { get; init; }
    public required string? ReferenceNo { get; init; }
    public required int? PaymentMethodId { get; init; }
    public required int? BankAccountId { get; init; }
    public required string? CustomerBankName { get; init; }
    public required string? CustomerBankBranch { get; init; }
    public required int? InstrumentTypeId { get; init; }
    public required string? InstrumentNo { get; init; }
    public required DateOnly? InstrumentDate { get; init; }
    public required DateOnly? DepositDate { get; init; }
    public required string? TransactionNo { get; init; }
    public required string? VdsCertificateNo { get; init; }
    public required DateOnly? VdsCertificateDate { get; init; }
    public required string? AitCertificateNo { get; init; }
    public required DateOnly? AitCertificateDate { get; init; }
    public required int? CurrencyId { get; init; }
    public required decimal? ExchangeRate { get; init; }
    public required string? Remarks { get; init; }
    public required string? Narration { get; init; }
    public required IReadOnlyList<SaveSalesCollectionLine> Lines { get; init; }
    public Dictionary<string, JsonElement>? CustomData { get; init; }
}

public sealed record SalesCollectionLineDetail(
    int Id,
    int DeliveryId,
    string DeliveryCode,
    DateOnly DeliveryDate,
    string? OrderCode,
    decimal BillAmount,
    decimal Due,
    decimal CollectionAmount,
    decimal Vds,
    decimal Ait,
    decimal DiscountAmount,
    decimal ExpenseAmount,
    decimal AdvanceAdjusted,
    decimal SettledAmount,
    string? Remarks);

public sealed record SalesCollectionDetail
{
    public required int Id { get; init; }
    public required string Code { get; init; }
    public required DateOnly CollectionDate { get; init; }
    public required int CustomerId { get; init; }
    public required string CustomerName { get; init; }
    public required bool IsAdvance { get; init; }
    public required decimal? AdvanceAmount { get; init; }
    public required Guid? OfficeId { get; init; }
    public required string? OfficeName { get; init; }
    public required int? CollectedById { get; init; }
    public required string? CollectedByName { get; init; }
    public required string? MoneyReceiptNo { get; init; }
    public required string? ReferenceNo { get; init; }
    public required int? PaymentMethodId { get; init; }
    public required string? PaymentMethodName { get; init; }
    public required int? BankAccountId { get; init; }
    public required string? BankAccountName { get; init; }
    public required string? CustomerBankName { get; init; }
    public required string? CustomerBankBranch { get; init; }
    public required int? InstrumentTypeId { get; init; }
    public required string? InstrumentTypeName { get; init; }
    public required string? InstrumentNo { get; init; }
    public required DateOnly? InstrumentDate { get; init; }
    public required DateOnly? DepositDate { get; init; }
    public required string? TransactionNo { get; init; }
    public required string? VdsCertificateNo { get; init; }
    public required DateOnly? VdsCertificateDate { get; init; }
    public required string? AitCertificateNo { get; init; }
    public required DateOnly? AitCertificateDate { get; init; }
    public required int? CurrencyId { get; init; }
    public required string? CurrencyName { get; init; }
    public required decimal ExchangeRate { get; init; }
    public required string? Remarks { get; init; }
    public required string? Narration { get; init; }
    public required string Status { get; init; }
    public required DateTime? ReviewedAt { get; init; }
    public required string? ReviewedByName { get; init; }
    public required string? ReviewComment { get; init; }
    public required decimal CollectionAmount { get; init; }
    public required decimal Vds { get; init; }
    public required decimal Ait { get; init; }
    public required decimal DiscountAmount { get; init; }
    public required decimal ExpenseAmount { get; init; }
    public required decimal AdvanceAdjusted { get; init; }
    public required decimal SettledAmount { get; init; }
    public required IReadOnlyList<SalesCollectionLineDetail> Lines { get; init; }
    public required Dictionary<string, JsonElement>? CustomData { get; init; }
    public required DateTime? CreatedAt { get; init; }
    public required DateTime? ModifiedAt { get; init; }
}

internal sealed class SaveSalesCollectionRequestValidator : AbstractValidator<SaveSalesCollectionRequest>
{
    public SaveSalesCollectionRequestValidator()
    {
        RuleFor(x => x.CustomerId).GreaterThan(0).WithMessage("Customer is required.");
        RuleFor(x => x.AdvanceAmount).NotNull().GreaterThan(0).When(x => x.IsAdvance).WithMessage("Enter the advance amount.");
        RuleFor(x => x.Lines).Empty().When(x => x.IsAdvance).WithMessage("An advance has no bills.");
        RuleFor(x => x.Lines.Count).InclusiveBetween(1, DocumentData.MaxLines).When(x => !x.IsAdvance).OverridePropertyName("lines")
            .WithMessage("Add at least one bill.");
        RuleForEach(x => x.Lines).ChildRules(line =>
        {
            line.RuleFor(l => l.DeliveryId).GreaterThan(0);
            line.RuleFor(l => l.CollectionAmount).GreaterThanOrEqualTo(0);
            line.RuleFor(l => l.Vds).GreaterThanOrEqualTo(0).When(l => l.Vds is not null);
            line.RuleFor(l => l.Ait).GreaterThanOrEqualTo(0).When(l => l.Ait is not null);
            line.RuleFor(l => l.DiscountAmount).GreaterThanOrEqualTo(0).When(l => l.DiscountAmount is not null);
            line.RuleFor(l => l.ExpenseAmount).GreaterThanOrEqualTo(0).When(l => l.ExpenseAmount is not null);
            line.RuleFor(l => l.AdvanceAdjusted).GreaterThanOrEqualTo(0).When(l => l.AdvanceAdjusted is not null);
            line.RuleFor(l => l.Remarks).MaximumLength(500);
        });
    }
}
