namespace Erp.Core.Domain.Inventory;

public class Item : MasterRecord, IBusinessUnitOwned
{
    public Guid BusinessUnitId { get; set; }

    public string Code { get; set; } = "";

    public string BarCode { get; set; } = "";

    public string HsCode { get; set; } = "";

    public string Name { get; set; } = "";

    public string? Description { get; set; }

    public int? ItemTypeId { get; set; }

    public string? ItemTypeName { get; set; }

    public int DepartmentId { get; set; }

    public ItemDepartment? Department { get; set; }

    public int CategoryId { get; set; }

    public ItemCategory? Category { get; set; }

    public int SubCategoryId { get; set; }

    public ItemSubCategory? SubCategory { get; set; }

    public int MinorCategoryId { get; set; }

    public ItemMinorCategory? MinorCategory { get; set; }

    public int? SupplierId { get; set; }

    public string? SupplierName { get; set; }

    public int UomId { get; set; }

    public UnitOfMeasure? Uom { get; set; }

    public string UomName { get; set; } = "";

    public int? AltUomId { get; set; }

    public UnitOfMeasure? AltUom { get; set; }

    public string? AltUomName { get; set; }

    public decimal? AltUomConversionQty { get; set; }

    public bool IsNoDecimalQty { get; set; }

    public decimal? AverageCostPrice { get; set; }

    public decimal? LastCostPrice { get; set; }

    public decimal? SalesPrice { get; set; }

    public int? PurchaseSupplyTypeId { get; set; }

    public string? PurchaseSupplyTypeName { get; set; }

    public int? PurchaseVatId { get; set; }

    public decimal? PurchaseVatPercentage { get; set; }

    public int? SalesSupplyTypeId { get; set; }

    public string? SalesSupplyTypeName { get; set; }

    public int? SalesVatId { get; set; }

    public decimal? SalesVatPercentage { get; set; }

    public decimal? SdPercentage { get; set; }

    public bool IsVatInclusive { get; set; }

    public bool IsVatRebatable { get; set; }

    public bool IsSaleable { get; set; }

    public bool IsPurchase { get; set; }

    public bool IsNegativeSales { get; set; }

    public bool IsMultipleSalesPrice { get; set; }

    public bool IsAutoBarcode { get; set; }

    public bool IsChildBarcode { get; set; }

    public int? MasterItemId { get; set; }

    public int? BarCodeSerial { get; set; }

    public string? ImageSource { get; set; }

    public string? CustomData { get; set; }
}
