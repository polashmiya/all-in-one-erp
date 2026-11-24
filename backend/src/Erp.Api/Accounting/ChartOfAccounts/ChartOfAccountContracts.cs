using System.Text.Json;
using Erp.Core.Domain.Accounting;
using FluentValidation;

namespace Erp.Api.Accounting.ChartOfAccounts;

public sealed record ChartOfAccountRow(
    int Id,
    string Code,
    string Name,
    string Type,
    bool IsGroup,
    int? ParentId,
    string? ParentName,
    string? Description,
    bool IsActive,
    DateTime? CreatedAt,
    DateTime? ModifiedAt,
    Dictionary<string, JsonElement>? CustomData);

public sealed record ChartOfAccountCounts(int All, int Active, int Inactive);

public sealed record ChartOfAccountList(IReadOnlyList<ChartOfAccountRow> Items, int Total, ChartOfAccountCounts Counts);

public sealed record ChartOfAccountOption(
    int Id, string Code, string Name, string Type, bool IsGroup, int? ParentId, bool IsActive, IReadOnlyList<string> PostingKeys);

public sealed record SaveChartOfAccountRequest
{
    public required string Code { get; init; }
    public required string Name { get; init; }
    public required string Type { get; init; }
    public required bool IsGroup { get; init; }
    public required int? ParentId { get; init; }
    public required string? Description { get; init; }
    public required bool IsActive { get; init; }
    public Dictionary<string, JsonElement>? CustomData { get; init; }
}

public sealed record ChartOfAccountStatusRequest(IReadOnlyList<int> Ids, bool IsActive);

public sealed record PostingAccountRow(string Key, int? ChartOfAccountId);

public sealed record SavePostingAccountsRequest(IReadOnlyList<PostingAccountRow> Items);

internal sealed class SaveChartOfAccountRequestValidator : AbstractValidator<SaveChartOfAccountRequest>
{
    public SaveChartOfAccountRequestValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(300);
        RuleFor(x => x.Type).Must(t => Enum.TryParse<AccountType>(t, true, out var type) && Enum.IsDefined(type))
            .WithMessage("Choose an account type.");
        RuleFor(x => x.Description).MaximumLength(500);
    }
}

internal sealed class ChartOfAccountStatusRequestValidator : AbstractValidator<ChartOfAccountStatusRequest>
{
    public ChartOfAccountStatusRequestValidator()
    {
        RuleFor(x => x.Ids).NotEmpty();
    }
}

internal sealed class SavePostingAccountsRequestValidator : AbstractValidator<SavePostingAccountsRequest>
{
    public SavePostingAccountsRequestValidator()
    {
        RuleFor(x => x.Items).NotEmpty();
    }
}
