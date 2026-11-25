namespace Erp.Core.Domain.Accounting;

public static class PostingKeys
{
    public const string Inventory = "inventory";
    public const string AccountsPayable = "accounts-payable";
    public const string SupplierAdvance = "supplier-advance";
    public const string AccountsReceivable = "accounts-receivable";
    public const string CustomerAdvance = "customer-advance";
    public const string Sales = "sales";
    public const string SalesDiscount = "sales-discount";
    public const string OtherIncome = "other-income";
    public const string CostOfGoodsSold = "cost-of-goods-sold";
    public const string VatPayable = "vat-payable";
    public const string SdPayable = "sd-payable";
    public const string VatReceivable = "vat-receivable";
    public const string VdsPayable = "vds-payable";
    public const string TdsPayable = "tds-payable";
    public const string Cash = "cash";
    public const string Bank = "bank";
    public const string MobileBanking = "mobile-banking";
    public const string PurchaseDiscount = "purchase-discount";
    public const string PriceDifference = "price-difference";
    public const string StockGain = "stock-gain";
    public const string StockLoss = "stock-loss";
    public const string OpeningBalance = "opening-balance";
    public const string RetainedEarnings = "retained-earnings";

    public static readonly IReadOnlyList<string> All =
    [
        Inventory, AccountsPayable, SupplierAdvance, VatReceivable, VdsPayable, TdsPayable, Cash, Bank, MobileBanking, PurchaseDiscount,
        PriceDifference, StockGain, StockLoss, OpeningBalance, RetainedEarnings, AccountsReceivable, CustomerAdvance, Sales, SalesDiscount, OtherIncome,
        CostOfGoodsSold, VatPayable, SdPayable,
    ];
}
