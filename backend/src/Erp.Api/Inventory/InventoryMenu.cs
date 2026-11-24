using Erp.Core.Modules;

namespace Erp.Api.Inventory;

public sealed class InventoryMenu : IErpMenuProvider
{
    public const string Module = "inventory";

    public const string StockAdjustment = "inventory.stock-adjustment";
    public const string StockTransfer = "inventory.stock-transfer";
    public const string StockBalance = "inventory.stock-balance";
    public const string StockLedger = "inventory.stock-ledger";

    public IEnumerable<MenuModule> GetModules() =>
    [
        new(Module, new("Inventory", "মজুত ব্যবস্থাপনা"), "box", 30,
        [
            new("stock", new("Stock", "মজুত"), "box", 10,
            [
                new("stock-adjustment", new("Stock Adjustment", "মজুত সমন্বয়"), 10, FeatureActions.Document),
                new("stock-transfer", new("Stock Transfer", "মজুত স্থানান্তর"), 20, FeatureActions.Document),
            ]),
            new("reports", new("Reports", "রিপোর্ট"), "bar-chart", 20,
            [
                new("stock-balance", new("Stock Balance", "বর্তমান মজুত"), 10, FeatureActions.Report),
                new("stock-ledger", new("Stock Ledger", "মজুত খতিয়ান"), 20, FeatureActions.Report),
            ]),
        ]),
    ];
}
