using System.Text.Json;
using FluentValidation;

namespace Erp.Modules.Inventory.ItemManagement.Categories;

public sealed record ItemCategoryRow(
    int Id,
    string Code,
    string Name,
    int DepartmentId,
    string DepartmentName,
    string? ImageSource,
    bool IsActive,
    int SubCategories,
    DateTime? CreatedAt,
    DateTime? ModifiedAt,
    Dictionary<string, JsonElement>? CustomData);

public sealed record ItemCategoryList(IReadOnlyList<ItemCategoryRow> Items, int Total, StatusCounts Counts);

public sealed record SaveItemCategoryRequest(int DepartmentId, string Code, string Name, string? ImageSource, bool IsActive,
    Dictionary<string, JsonElement>? CustomData = null);

internal sealed class SaveItemCategoryRequestValidator : AbstractValidator<SaveItemCategoryRequest>
{
    public SaveItemCategoryRequestValidator()
    {
        RuleFor(x => x.DepartmentId).GreaterThan(0).WithMessage("Choose a department.");
        RuleFor(x => x.Code).RecordCode(3);
        RuleFor(x => x.Name).RecordName(100);
        RuleFor(x => x.ImageSource).ImageSource();
    }
}
