using Erp.Core.Modules;

namespace Erp.Api.Procurement;

public sealed class ProcurementMenu : IErpMenuProvider
{
    public const string Module = "procurement";
    public const string Approve = "approve";

    public const string PurchaseRequisition = "procurement.purchase-requisition";
    public const string PurchaseOrder = "procurement.purchase-order";
    public const string PurchaseReceive = "procurement.purchase-receive";
    public const string PurchaseReturn = "procurement.purchase-return";
    public const string PurchasePayment = "procurement.purchase-payment";

    private static readonly string[] ApprovalActions =
        [FeatureActions.View, FeatureActions.Create, FeatureActions.Edit, FeatureActions.Delete, FeatureActions.Export, Approve];

    private static readonly string[] ReportActions = [FeatureActions.View, FeatureActions.Export];

    public IEnumerable<MenuModule> GetModules() =>
    [
        new(Module, new("Procurement", "ক্রয় ব্যবস্থাপনা"), "shopping-cart", 20,
        [
            new("purchase", new("Purchase", "ক্রয়"), "shopping-cart", 10,
            [
                new("purchase-requisition", new("Purchase Requisition", "ক্রয় চাহিদাপত্র"), 10, ApprovalActions),
                new("purchase-order", new("Purchase Order", "ক্রয় আদেশ"), 20, ApprovalActions),
                new("purchase-receive", new("Purchase Receive", "ক্রয়কৃত মাল গ্রহণ"), 30, ApprovalActions),
                new("purchase-return", new("Purchase Return", "ক্রয় ফেরত"), 40, ApprovalActions),
                new("purchase-payment", new("Purchase Payment", "ক্রয় পরিশোধ"), 50, ApprovalActions),
            ]),
            new("reports", new("Reports", "রিপোর্ট"), "bar-chart", 20,
            [
                new("purchase-report", new("Purchase Report", "ক্রয় রিপোর্ট"), 10, ReportActions),
            ]),
        ]),
    ];
}
