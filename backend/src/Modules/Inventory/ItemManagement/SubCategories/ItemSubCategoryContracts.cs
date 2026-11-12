using System.Text.Json;
using FluentValidation;

namespace Erp.Modules.Inventory.ItemManagement.SubCategories;

public sealed record ItemSubCategoryRow(
    int Id,
    string Name,
    int? DepartmentId,
    string? DepartmentName,
    int CategoryId,
    string CategoryName,
    string? ImageSource,
    bool IsActive,
    int MinorCategories,
    DateTime? CreatedAt,
    DateTime? ModifiedAt,
    Dictionary<string, JsonElement>? CustomData);

public sealed record ItemSubCategoryList(IReadOnlyList<ItemSubCategoryRow> Items, int Total, StatusCounts Counts);

public sealed record SaveItemSubCategoryRequest(int CategoryId, string Name, string? ImageSource, bool IsActive,
    Dictionary<string, JsonElement>? CustomData = null);

internal sealed class SaveItemSubCategoryRequestValidator : AbstractValidator<SaveItemSubCategoryRequest>
{
    public SaveItemSubCategoryRequestValidator()
    {
        RuleFor(x => x.CategoryId).GreaterThan(0).WithMessage("Choose a category.");
        RuleFor(x => x.Name).RecordName(100);
        RuleFor(x => x.ImageSource).ImageSource();
    }
}
