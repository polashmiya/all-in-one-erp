using Erp.Core.Modules;

namespace Erp.Api.Sales;

public sealed class SalesMenu : IErpMenuProvider
{
    public const string Module = "sales";

    public const string SalesQuotation = "sales.sales-quotation";
    public const string SalesOrder = "sales.sales-order";
    public const string SalesDelivery = "sales.sales-delivery";
    public const string SalesReturn = "sales.sales-return";
    public const string SalesCollection = "sales.sales-collection";
    public const string SalesReport = "sales.sales-report";
    public const string CollectionReport = "sales.collection-report";
    public const string CustomerDue = "sales.customer-due";

    public IEnumerable<MenuModule> GetModules() =>
    [
        new(Module, new("Sales", "বিক্রয় ব্যবস্থাপনা"), "shop", 15,
        [
            new("sales", new("Sales", "বিক্রয়"), "shop", 10,
            [
                new("sales-quotation", new("Sales Quotation", "বিক্রয় কোটেশন"), 10, FeatureActions.Document),
                new("sales-order", new("Sales Order", "বিক্রয় আদেশ"), 20, FeatureActions.Document),
                new("sales-delivery", new("Sales Delivery", "বিক্রয় ডেলিভারি"), 30, FeatureActions.Document),
                new("sales-return", new("Sales Return", "বিক্রয় ফেরত"), 40, FeatureActions.Document),
                new("sales-collection", new("Sales Collection", "বিক্রয় আদায়"), 50, FeatureActions.Document),
            ]),
            new("reports", new("Reports", "রিপোর্ট"), "bar-chart", 20,
            [
                new("sales-report", new("Sales Report", "বিক্রয় রিপোর্ট"), 10, FeatureActions.Report),
                new("collection-report", new("Collection Report", "আদায় রিপোর্ট"), 20, FeatureActions.Report),
                new("customer-due", new("Customer Due", "গ্রাহকের বাকি"), 30, FeatureActions.Report),
            ]),
        ]),
    ];
}
