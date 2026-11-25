using Erp.Core.Domain.Inventory;

namespace Erp.Core.Domain.Sales;

public abstract class SalesLine
{
    public int Id { get; set; }

    public int Sequence { get; set; }

    public int ItemId { get; set; }

    public Item? Item { get; set; }

    public string ItemName { get; set; } = "";

    public string? ItemDescription { get; set; }

    public int UomId { get; set; }

    public UnitOfMeasure? Uom { get; set; }

    public string UomName { get; set; } = "";

    public decimal Quantity { get; set; }

    public decimal Price { get; set; }

    public decimal TotalAmount { get; set; }

    public decimal DiscountPercentage { get; set; }

    public decimal DiscountAmount { get; set; }

    public bool IsAmountWiseDiscount { get; set; }

    public bool IsDiscountPerQuantity { get; set; }

    public decimal SdPercentage { get; set; }

    public decimal SdAmount { get; set; }

    public decimal VatPercentage { get; set; }

    public decimal VatAmount { get; set; }

    public decimal AitPercentage { get; set; }

    public decimal AitAmount { get; set; }

    public decimal TotalValue { get; set; }

    public int WarrantyInMonth { get; set; }
}

public abstract class SalesDocumentLine : SalesLine
{
    public decimal SellingPrice { get; set; }

    public decimal ItemBasePriceInclusive { get; set; }

    public decimal HeaderDiscountProportion { get; set; }

    public decimal HeaderCostProportion { get; set; }

    public bool IsOfferItem { get; set; }

    public int? OfferItemId { get; set; }

    public string? OfferItemName { get; set; }

    public decimal OfferItemQuantity { get; set; }

    public int? FreeTypeId { get; set; }

    public string? FreeTypeName { get; set; }

    public int? DiscountId { get; set; }

    public decimal? SpecialDiscountAmount { get; set; }

    public decimal SupplierDiscountContributionInPercent { get; set; }

    public decimal SupplierDiscountContributionInAmount { get; set; }

    public int? ProformaInvoiceId { get; set; }

    public string? PiNo { get; set; }

    public decimal BdPortQuantity { get; set; }

    public decimal ForeignPortQuantity { get; set; }

    public string? ShelfLife { get; set; }

    public int? ParentRowId { get; set; }
}
