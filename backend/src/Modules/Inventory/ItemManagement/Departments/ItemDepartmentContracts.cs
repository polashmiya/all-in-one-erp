using System.Text.Json;
using FluentValidation;

namespace Erp.Modules.Inventory.ItemManagement.Departments;

public sealed record ItemDepartmentRow(
    int Id,
    string Code,
    string Name,
    int? Serial,
    string? ImageSource,
    bool IsActive,
    int Categories,
    DateTime? CreatedAt,
    DateTime? ModifiedAt,
    Dictionary<string, JsonElement>? CustomData);

public sealed record ItemDepartmentList(IReadOnlyList<ItemDepartmentRow> Items, int Total, StatusCounts Counts);

public sealed record SaveItemDepartmentRequest(string Code, string Name, int? Serial, string? ImageSource, bool IsActive,
    Dictionary<string, JsonElement>? CustomData = null);

internal sealed class SaveItemDepartmentRequestValidator : AbstractValidator<SaveItemDepartmentRequest>
{
    public SaveItemDepartmentRequestValidator()
    {
        RuleFor(x => x.Code).RecordCode(20);
        RuleFor(x => x.Name).RecordName(100);
        RuleFor(x => x.Serial).InclusiveBetween(0, 999_999);
        RuleFor(x => x.ImageSource).ImageSource();
    }
}
