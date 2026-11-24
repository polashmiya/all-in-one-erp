using System.Text.Json;
using Erp.Api.Documents;
using FluentValidation;

namespace Erp.Api.Accounting.Journals;

public sealed record JournalRow(
    int Id,
    string Code,
    DateOnly JournalDate,
    string Narration,
    string? Feature,
    int? DocumentId,
    string? DocumentCode,
    bool IsManual,
    string Status,
    decimal TotalAmount,
    DateTime? CreatedAt,
    DateTime? ModifiedAt);

public sealed record JournalList(IReadOnlyList<JournalRow> Items, int Total, ApprovalCounts Counts);

public sealed record SaveJournalLine
{
    public required int ChartOfAccountId { get; init; }
    public required int? SupplierId { get; init; }
    public required decimal? Debit { get; init; }
    public required decimal? Credit { get; init; }
    public required string? Remarks { get; init; }
}

public sealed record SaveJournalRequest
{
    public required string? Code { get; init; }
    public required DateOnly JournalDate { get; init; }
    public required string Narration { get; init; }
    public required IReadOnlyList<SaveJournalLine> Lines { get; init; }
    public Dictionary<string, JsonElement>? CustomData { get; init; }
}

public sealed record JournalLineDetail(
    int Id,
    int ChartOfAccountId,
    string AccountCode,
    string AccountName,
    int? SupplierId,
    string? SupplierName,
    decimal Debit,
    decimal Credit,
    string? Remarks);

public sealed record JournalDetail
{
    public required int Id { get; init; }
    public required string Code { get; init; }
    public required DateOnly JournalDate { get; init; }
    public required string Narration { get; init; }
    public required string? Feature { get; init; }
    public required int? DocumentId { get; init; }
    public required string? DocumentCode { get; init; }
    public required bool IsManual { get; init; }
    public required string Status { get; init; }
    public required DateTime? ReviewedAt { get; init; }
    public required string? ReviewedByName { get; init; }
    public required string? ReviewComment { get; init; }
    public required decimal TotalAmount { get; init; }
    public required IReadOnlyList<JournalLineDetail> Lines { get; init; }
    public required Dictionary<string, JsonElement>? CustomData { get; init; }
    public required DateTime? CreatedAt { get; init; }
    public required DateTime? ModifiedAt { get; init; }
}

internal sealed class SaveJournalRequestValidator : AbstractValidator<SaveJournalRequest>
{
    public SaveJournalRequestValidator()
    {
        RuleFor(x => x.Narration).NotEmpty().MaximumLength(1500);
        RuleFor(x => x.Lines.Count).InclusiveBetween(2, DocumentData.MaxLines).OverridePropertyName("lines")
            .WithMessage("Add at least one debit and one credit line.");
        RuleForEach(x => x.Lines).ChildRules(line =>
        {
            line.RuleFor(l => l.ChartOfAccountId).GreaterThan(0);
            line.RuleFor(l => l.Debit).GreaterThanOrEqualTo(0).PrecisionScale(18, 2, true).When(l => l.Debit is not null);
            line.RuleFor(l => l.Credit).GreaterThanOrEqualTo(0).PrecisionScale(18, 2, true).When(l => l.Credit is not null);
            line.RuleFor(l => (l.Debit ?? 0) > 0 != (l.Credit ?? 0) > 0).Equal(true)
                .OverridePropertyName("debit").WithMessage("Enter a debit or a credit.");
            line.RuleFor(l => l.Remarks).MaximumLength(500);
        });
    }
}
