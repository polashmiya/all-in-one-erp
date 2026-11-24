using Erp.Core.Modules;

namespace Erp.Api.Accounting;

public sealed class AccountingMenu : IErpMenuProvider
{
    public const string Module = "accounting";

    public const string ChartOfAccounts = "accounting.chart-of-accounts";
    public const string PostingAccounts = "accounting.posting-accounts";
    public const string Journal = "accounting.journal";
    public const string AccountLedger = "accounting.account-ledger";
    public const string SupplierLedger = "accounting.supplier-ledger";
    public const string TrialBalance = "accounting.trial-balance";
    public const string IncomeStatement = "accounting.income-statement";
    public const string BalanceSheet = "accounting.balance-sheet";

    private static readonly string[] SetupActions =
        [FeatureActions.View, FeatureActions.Create, FeatureActions.Edit, FeatureActions.Export];

    public IEnumerable<MenuModule> GetModules() =>
    [
        new(Module, new("Accounting", "হিসাবরক্ষণ"), "book", 40,
        [
            new("setup", new("Account Setup", "হিসাব সেটআপ"), "setting", 10,
            [
                new("chart-of-accounts", new("Chart of Accounts", "হিসাবের তালিকা"), 10, SetupActions),
                new("posting-accounts", new("Posting Accounts", "পোস্টিং হিসাব"), 20, [FeatureActions.View, FeatureActions.Edit]),
            ]),
            new("transactions", new("Transactions", "লেনদেন"), "file", 20,
            [
                new("journal", new("Journal Voucher", "জার্নাল ভাউচার"), 10, FeatureActions.Document),
            ]),
            new("reports", new("Reports", "রিপোর্ট"), "bar-chart", 30,
            [
                new("account-ledger", new("Account Ledger", "হিসাব খতিয়ান"), 10, FeatureActions.Report),
                new("supplier-ledger", new("Supplier Ledger", "সরবরাহকারী খতিয়ান"), 20, FeatureActions.Report),
                new("trial-balance", new("Trial Balance", "রেওয়ামিল"), 30, FeatureActions.Report),
                new("income-statement", new("Income Statement", "আয় বিবরণী"), 40, FeatureActions.Report),
                new("balance-sheet", new("Balance Sheet", "উদ্বৃত্তপত্র"), 50, FeatureActions.Report),
            ]),
        ]),
    ];
}
