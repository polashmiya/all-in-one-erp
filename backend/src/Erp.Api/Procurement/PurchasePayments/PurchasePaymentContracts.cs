using Erp.Api.Documents;
using System.Text.Json;
using FluentValidation;

namespace Erp.Api.Procurement.PurchasePayments;

public sealed record PurchasePaymentRow(
    int Id,
    string Code,
    DateOnly PaymentDate,
    int SupplierId,
    string? SupplierName,
    int? PaymentMethodId,
    string? PaymentMethodName,
    string Status,
    int BillCount,
    decimal PaymentAmount,
    decimal SettledAmount,
    DateTime? CreatedAt,
    DateTime? ModifiedAt);

public sealed record PurchasePaymentList(IReadOnlyList<PurchasePaymentRow> Items, int Total, ApprovalCounts Counts);

public sealed record PayableBill(
    int Id,
    string Code,
    DateOnly ReceiveDate,
    string? PurchaseOrderName,
    string? ChallanNo,
    decimal NetAmount,
    decimal ReturnAmount,
    decimal PaidAmount,
    decimal Due);

public sealed record SavePurchasePaymentLine
{
    public required int PurchaseReceiveId { get; init; }
    public required decimal PaymentAmount { get; init; }
    public required decimal? Vds { get; init; }
    public required decimal? Tds { get; init; }
    public required decimal? DiscountAmount { get; init; }
}

public sealed record SavePurchasePaymentRequest
{
    public required string? Code { get; init; }
    public required DateOnly PaymentDate { get; init; }
    public required int SupplierId { get; init; }
    public required int? PaymentMethodId { get; init; }
    public required string? PayeeName { get; init; }
    public required string? BankAccountNo { get; init; }
    public required string? InstrumentNo { get; init; }
    public required DateOnly? InstrumentDate { get; init; }
    public required string? Remarks { get; init; }
    public required IReadOnlyList<SavePurchasePaymentLine> Lines { get; init; }
    public Dictionary<string, JsonElement>? CustomData { get; init; }
}

public sealed record PurchasePaymentLineDetail(
    int Id,
    int PurchaseReceiveId,
    string PurchaseReceiveCode,
    DateOnly ReceiveDate,
    string? PurchaseOrderName,
    decimal BillAmount,
    decimal Due,
    decimal PaymentAmount,
    decimal Vds,
    decimal Tds,
    decimal DiscountAmount,
    decimal SettledAmount);

public sealed record PurchasePaymentDetail
{
    public required int Id { get; init; }
    public required string Code { get; init; }
    public required DateOnly PaymentDate { get; init; }
    public required int SupplierId { get; init; }
    public required string? SupplierName { get; init; }
    public required int? PaymentMethodId { get; init; }
    public required string? PaymentMethodName { get; init; }
    public required string? PayeeName { get; init; }
    public required string? BankAccountNo { get; init; }
    public required string? InstrumentNo { get; init; }
    public required DateOnly? InstrumentDate { get; init; }
    public required string? Remarks { get; init; }
    public required string Status { get; init; }
    public required DateTime? ReviewedAt { get; init; }
    public required string? ReviewedByName { get; init; }
    public required string? ReviewComment { get; init; }
    public required decimal PaymentAmount { get; init; }
    public required decimal Vds { get; init; }
    public required decimal Tds { get; init; }
    public required decimal DiscountAmount { get; init; }
    public required decimal SettledAmount { get; init; }
    public required IReadOnlyList<PurchasePaymentLineDetail> Lines { get; init; }
    public required Dictionary<string, JsonElement>? CustomData { get; init; }
    public required DateTime? CreatedAt { get; init; }
    public required DateTime? ModifiedAt { get; init; }
}

internal sealed class SavePurchasePaymentRequestValidator : AbstractValidator<SavePurchasePaymentRequest>
{
    public SavePurchasePaymentRequestValidator()
    {
        RuleFor(x => x.SupplierId).GreaterThan(0).WithMessage("Supplier is required.");
        RuleFor(x => x.Lines).NotEmpty().WithMessage("Add at least one bill.");
        RuleFor(x => x.Lines.Count).LessThanOrEqualTo(DocumentData.MaxLines).OverridePropertyName("lines");
        RuleForEach(x => x.Lines).ChildRules(line =>
        {
            line.RuleFor(l => l.PurchaseReceiveId).GreaterThan(0);
            line.RuleFor(l => l.PaymentAmount).GreaterThanOrEqualTo(0);
            line.RuleFor(l => l.Vds).GreaterThanOrEqualTo(0).When(l => l.Vds is not null);
            line.RuleFor(l => l.Tds).GreaterThanOrEqualTo(0).When(l => l.Tds is not null);
            line.RuleFor(l => l.DiscountAmount).GreaterThanOrEqualTo(0).When(l => l.DiscountAmount is not null);
            line.RuleFor(l => l.PaymentAmount + (l.Vds ?? 0) + (l.Tds ?? 0) + (l.DiscountAmount ?? 0)).GreaterThan(0)
                .OverridePropertyName("paymentAmount").WithMessage("Enter an amount for every bill.");
        });
    }
}
