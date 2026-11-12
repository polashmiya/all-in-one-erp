using System.Text.Json;
using FluentValidation;

namespace Erp.Modules.Inventory.ItemManagement.Items;

public sealed record ItemRow(
    int Id,
    string Code,
    string Name,
    string BarCode,
    string? ItemTypeName,
    string DepartmentName,
    string CategoryName,
    string SubCategoryName,
    string MinorCategoryName,
    string UomName,
    decimal? SalesPrice,
    string? ImageSource,
    bool IsActive,
    DateTime? CreatedAt,
    DateTime? ModifiedAt);

public sealed record ItemList(IReadOnlyList<ItemRow> Items, int Total, StatusCounts Counts);

public sealed record ItemDetail
{
    public required int Id { get; init; }
    public required string Code { get; init; }
    public required string Name { get; init; }
    public required string? Description { get; init; }
    public required int? ItemTypeId { get; init; }
    public required string? ItemTypeName { get; init; }
    public required string HsCode { get; init; }
    public required int DepartmentId { get; init; }
    public required string DepartmentName { get; init; }
    public required int CategoryId { get; init; }
    public required string CategoryName { get; init; }
    public required int SubCategoryId { get; init; }
    public required string SubCategoryName { get; init; }
    public required int MinorCategoryId { get; init; }
    public required string MinorCategoryName { get; init; }
    public required string BarCode { get; init; }
    public required bool IsAutoBarcode { get; init; }
    public required bool IsChildBarcode { get; init; }
    public required int? MasterItemId { get; init; }
    public required string? MasterItemCode { get; init; }
    public required string? MasterItemName { get; init; }
    public required string? SupplierName { get; init; }
    public required int UomId { get; init; }
    public required string UomName { get; init; }
    public required int? AltUomId { get; init; }
    public required string? AltUomName { get; init; }
    public required decimal? AltUomConversionQty { get; init; }
    public required bool IsNoDecimalQty { get; init; }
    public required decimal? AverageCostPrice { get; init; }
    public required decimal? LastCostPrice { get; init; }
    public required decimal? SalesPrice { get; init; }
    public required int? PurchaseSupplyTypeId { get; init; }
    public required string? PurchaseSupplyTypeName { get; init; }
    public required decimal? PurchaseVatPercentage { get; init; }
    public required int? SalesSupplyTypeId { get; init; }
    public required string? SalesSupplyTypeName { get; init; }
    public required decimal? SalesVatPercentage { get; init; }
    public required decimal? SdPercentage { get; init; }
    public required bool IsVatInclusive { get; init; }
    public required bool IsVatRebatable { get; init; }
    public required bool IsSaleable { get; init; }
    public required bool IsPurchase { get; init; }
    public required bool IsNegativeSales { get; init; }
    public required bool IsMultipleSalesPrice { get; init; }
    public required string? ImageSource { get; init; }
    public required Dictionary<string, JsonElement>? CustomData { get; init; }
    public required bool IsActive { get; init; }
    public required DateTime? CreatedAt { get; init; }
    public required string? CreatedByName { get; init; }
    public required DateTime? ModifiedAt { get; init; }
    public required string? ModifiedByName { get; init; }
}

public sealed record SaveItemRequest
{
    public required string? Code { get; init; }
    public required string Name { get; init; }
    public required string? Description { get; init; }
    public required int? ItemTypeId { get; init; }
    public required string? HsCode { get; init; }
    public required int MinorCategoryId { get; init; }
    public required bool IsAutoBarcode { get; init; }
    public required string? BarCode { get; init; }
    public required bool IsChildBarcode { get; init; }
    public required int? MasterItemId { get; init; }
    public required string? SupplierName { get; init; }
    public required int UomId { get; init; }
    public required int? AltUomId { get; init; }
    public required decimal? AltUomConversionQty { get; init; }
    public required bool IsNoDecimalQty { get; init; }
    public required decimal? AverageCostPrice { get; init; }
    public required decimal? LastCostPrice { get; init; }
    public required decimal? SalesPrice { get; init; }
    public required int? PurchaseSupplyTypeId { get; init; }
    public required decimal? PurchaseVatPercentage { get; init; }
    public required int? SalesSupplyTypeId { get; init; }
    public required decimal? SalesVatPercentage { get; init; }
    public required decimal? SdPercentage { get; init; }
    public required bool IsVatInclusive { get; init; }
    public required bool IsVatRebatable { get; init; }
    public required bool IsSaleable { get; init; }
    public required bool IsPurchase { get; init; }
    public required bool IsNegativeSales { get; init; }
    public required bool IsMultipleSalesPrice { get; init; }
    public required string? ImageSource { get; init; }
    public Dictionary<string, JsonElement>? CustomData { get; init; }
    public required bool IsActive { get; init; }
}

internal sealed class SaveItemRequestValidator : AbstractValidator<SaveItemRequest>
{
    public SaveItemRequestValidator()
    {
        RuleFor(x => x.Code!).RecordCode(50).When(x => !string.IsNullOrWhiteSpace(x.Code));
        RuleFor(x => x.Name).RecordName(200);
        RuleFor(x => x.Description).MaximumLength(2000);
        RuleFor(x => x.HsCode).MaximumLength(50);
        RuleFor(x => x.MinorCategoryId).GreaterThan(0).WithMessage("Choose a minor category.");
        RuleFor(x => x.BarCode).NotEmpty().WithMessage("Enter a barcode or turn on auto barcode.").MaximumLength(50)
            .When(x => !x.IsAutoBarcode);
        RuleFor(x => x.MasterItemId).NotNull().WithMessage("Choose the master item.").When(x => x.IsChildBarcode);
        RuleFor(x => x.SupplierName).MaximumLength(100);
        RuleFor(x => x.UomId).GreaterThan(0).WithMessage("Choose a unit of measure.");
        RuleFor(x => x.AltUomId).NotEqual(x => x.UomId).WithMessage("Choose a unit other than the main unit.")
            .When(x => x.AltUomId is not null);
        RuleFor(x => x.AltUomConversionQty)
            .NotNull().WithMessage("Enter how many main units make one alternate unit.")
            .GreaterThan(0m).WithMessage("Use a quantity above zero.")
            .When(x => x.AltUomId is not null);
        RuleFor(x => x.AverageCostPrice).GreaterThanOrEqualTo(0m).WithMessage("Use zero or more.");
        RuleFor(x => x.LastCostPrice).GreaterThanOrEqualTo(0m).WithMessage("Use zero or more.");
        RuleFor(x => x.SalesPrice).GreaterThanOrEqualTo(0m).WithMessage("Use zero or more.");
        RuleFor(x => x.PurchaseVatPercentage).InclusiveBetween(0m, 100m).WithMessage("Use 0 to 100.");
        RuleFor(x => x.SalesVatPercentage).InclusiveBetween(0m, 100m).WithMessage("Use 0 to 100.");
        RuleFor(x => x.SdPercentage).InclusiveBetween(0m, 100m).WithMessage("Use 0 to 100.");
        RuleFor(x => x.ImageSource).ImageSource();
    }
}
