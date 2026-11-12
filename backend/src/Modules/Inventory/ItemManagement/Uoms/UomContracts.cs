using System.Text.Json;
using FluentValidation;

namespace Erp.Modules.Inventory.ItemManagement.Uoms;

public sealed record UomRow(
    int Id,
    string Code,
    string Name,
    string? Description,
    bool IsDecimalAllowed,
    int? RoundingLength,
    bool IsActive,
    DateTime? CreatedAt,
    DateTime? ModifiedAt,
    Dictionary<string, JsonElement>? CustomData);

public sealed record UomList(IReadOnlyList<UomRow> Items, int Total, StatusCounts Counts);

public sealed record SaveUomRequest(
    string Code,
    string Name,
    string? Description,
    bool IsDecimalAllowed,
    int? RoundingLength,
    bool IsActive,
    Dictionary<string, JsonElement>? CustomData = null);

internal sealed class SaveUomRequestValidator : AbstractValidator<SaveUomRequest>
{
    public const int MaxDecimals = 6;

    public SaveUomRequestValidator()
    {
        RuleFor(x => x.Code).RecordCode(10);
        RuleFor(x => x.Name).RecordName(50);
        RuleFor(x => x.Description).MaximumLength(255);
        RuleFor(x => x.RoundingLength)
            .NotNull().WithMessage("Choose how many decimal places to keep.")
            .InclusiveBetween(1, MaxDecimals).WithMessage($"Use 1 to {MaxDecimals} decimal places.")
            .When(x => x.IsDecimalAllowed);
    }
}
