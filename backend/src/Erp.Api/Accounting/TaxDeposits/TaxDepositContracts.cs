using System.Text.Json;
using Erp.Api.Documents;
using Erp.Core.Domain.Accounting;
using FluentValidation;

namespace Erp.Api.Accounting.TaxDeposits;

public sealed record TaxDepositRow(
    int Id,
    string Code,
    DateOnly DepositDate,
    string Type,
    int? PaymentMethodId,
    string? PaymentMethodName,
    string? ChallanNo,
    string Status,
    int PaymentCount,
    decimal TotalAmount,
    DateTime? CreatedAt,
    DateTime? ModifiedAt);

public sealed record TaxDepositList(IReadOnlyList<TaxDepositRow> Items, int Total, ApprovalCounts Counts);

public sealed record PendingTax(
    int PurchasePaymentId, string PaymentCode, DateOnly PaymentDate, string? SupplierName, decimal Deducted, decimal Deposited, decimal Remaining);

public sealed record SaveTaxDepositLine
{
    public required int PurchasePaymentId { get; init; }
    public required decimal Amount { get; init; }
}

public sealed record SaveTaxDepositRequest
{
    public required string? Code { get; init; }
    public required DateOnly DepositDate { get; init; }
    public required string Type { get; init; }
    public required int? PaymentMethodId { get; init; }
    public required int? BankAccountId { get; init; }
    public required string? ChallanNo { get; init; }
    public required DateOnly? ChallanDate { get; init; }
    public required string? Remarks { get; init; }
    public required IReadOnlyList<SaveTaxDepositLine> Lines { get; init; }
    public Dictionary<string, JsonElement>? CustomData { get; init; }
}

public sealed record TaxDepositLineDetail(
    int Id, int PurchasePaymentId, string PaymentCode, DateOnly PaymentDate, string? SupplierName, decimal Deducted, decimal Remaining, decimal Amount);

public sealed record TaxDepositDetail
{
    public required int Id { get; init; }
    public required string Code { get; init; }
    public required DateOnly DepositDate { get; init; }
    public required string Type { get; init; }
    public required int? PaymentMethodId { get; init; }
    public required string? PaymentMethodName { get; init; }
    public required int? BankAccountId { get; init; }
    public required string? BankAccountName { get; init; }
    public required string? ChallanNo { get; init; }
    public required DateOnly? ChallanDate { get; init; }
    public required string? Remarks { get; init; }
    public required string Status { get; init; }
    public required DateTime? ReviewedAt { get; init; }
    public required string? ReviewedByName { get; init; }
    public required string? ReviewComment { get; init; }
    public required decimal TotalAmount { get; init; }
    public required IReadOnlyList<TaxDepositLineDetail> Lines { get; init; }
    public required Dictionary<string, JsonElement>? CustomData { get; init; }
    public required DateTime? CreatedAt { get; init; }
    public required DateTime? ModifiedAt { get; init; }
}

internal sealed class SaveTaxDepositRequestValidator : AbstractValidator<SaveTaxDepositRequest>
{
    public SaveTaxDepositRequestValidator()
    {
        RuleFor(x => x.Type).Must(t => Enum.TryParse<TaxType>(t, true, out var type) && Enum.IsDefined(type))
            .WithMessage("Choose VDS or TDS.");
        RuleFor(x => x.Lines).NotEmpty().WithMessage("Add at least one payment.");
        RuleFor(x => x.Lines.Count).LessThanOrEqualTo(DocumentData.MaxLines).OverridePropertyName("lines");
        RuleForEach(x => x.Lines).ChildRules(line =>
        {
            line.RuleFor(l => l.PurchasePaymentId).GreaterThan(0);
            line.RuleFor(l => l.Amount).GreaterThan(0).PrecisionScale(18, 2, true);
        });
    }
}
