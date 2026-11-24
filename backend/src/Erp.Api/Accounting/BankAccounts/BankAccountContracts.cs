using System.Text.Json;
using Erp.Api.Accounting.ChartOfAccounts;
using Erp.Core.Domain.Accounting;
using FluentValidation;

namespace Erp.Api.Accounting.BankAccounts;

public sealed record BankAccountRow(
    int Id,
    string Name,
    string Type,
    string? BankName,
    string? BranchName,
    string AccountNo,
    string? RoutingNo,
    int ChartOfAccountId,
    string ChartOfAccountName,
    bool IsActive,
    DateTime? CreatedAt,
    DateTime? ModifiedAt,
    Dictionary<string, JsonElement>? CustomData);

public sealed record BankAccountList(IReadOnlyList<BankAccountRow> Items, int Total, ChartOfAccountCounts Counts);

public sealed record BankAccountOption(int Id, string Code, string Name, string Type, bool IsActive);

public sealed record SaveBankAccountRequest
{
    public required string Name { get; init; }
    public required string Type { get; init; }
    public required string? BankName { get; init; }
    public required string? BranchName { get; init; }
    public required string AccountNo { get; init; }
    public required string? RoutingNo { get; init; }
    public required int? ChartOfAccountId { get; init; }
    public required bool IsActive { get; init; }
    public Dictionary<string, JsonElement>? CustomData { get; init; }
}

internal sealed class SaveBankAccountRequestValidator : AbstractValidator<SaveBankAccountRequest>
{
    public SaveBankAccountRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.AccountNo).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Type).Must(t => Enum.TryParse<BankAccountType>(t, true, out var type) && Enum.IsDefined(type))
            .WithMessage("Choose bank or mobile banking.");
    }
}
