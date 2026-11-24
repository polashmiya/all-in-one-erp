using System.Net;
using System.Net.Http.Json;
using Erp.Api.Accounting.AccountingReports;
using Erp.Api.Accounting.BankAccounts;
using Erp.Api.Accounting.ChartOfAccounts;
using Erp.Api.Accounting.Journals;
using Erp.Api.Accounting.PeriodClosings;
using Erp.Api.Accounting.TaxDeposits;
using Erp.Api.Approvals;
using Erp.Api.Inventory.StockAdjustments;
using Erp.Api.Inventory.StockReports;
using Erp.Api.Inventory.StockTransfers;
using Erp.Api.Procurement;
using Erp.Api.Procurement.PurchaseOrders;
using Erp.Api.Procurement.PurchasePayments;
using Erp.Api.Procurement.PurchaseReceives;
using Erp.Core.Domain.Accounting;
using Erp.Infrastructure.Persistence;
using Erp.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using static Erp.Tests.Api.ProcurementTestData;

namespace Erp.Tests.Api;

[Collection(ApiCollection.Name)]
public sealed class CancelAndClosingTests(ApiFactory factory)
{
    private const string PaymentsUrl = "/api/erp/procurement/purchase-payments";
    private const string AdjustmentsUrl = "/api/erp/inventory/stock-adjustments";
    private const string TransfersUrl = "/api/erp/inventory/stock-transfers";
    private const string AccountsUrl = "/api/erp/accounting/chart-of-accounts";
    private const string BanksUrl = "/api/erp/accounting/bank-accounts";
    private const string JournalsUrl = "/api/erp/accounting/journals";
    private const string DepositsUrl = "/api/erp/accounting/tax-deposits";
    private const string PeriodsUrl = "/api/erp/accounting/periods";
    private const string ReportsUrl = "/api/erp/accounting/reports";

    [Fact]
    public async Task Cancelling_a_receive_takes_its_stock_and_journal_back_and_frees_the_order()
    {
        var admin = await AdminAsync();
        var supplier = await SupplierAsync(factory.DhakaId);
        var item = await ItemAsync(factory.DhakaId, 100m);
        await ApproveAsync(admin, AdjustmentsUrl, await AdjustmentAsync("opening", item, 10, 50m));
        var receive = await ApprovedReceiveAsync(factory, admin, item, 10, supplier);

        var noReason = await CancelAsync(admin, $"{ProcurementReceivesUrl}/{receive.Id}", null);
        var cancelled = await ReadAsync<PurchaseReceiveDetail>(await CancelAsync(admin, $"{ProcurementReceivesUrl}/{receive.Id}"));
        var again = await CancelAsync(admin, $"{ProcurementReceivesUrl}/{receive.Id}");
        var ledger = await LedgerAsync(admin, item);
        var supplierLedger = await admin.GetFromJsonAsync<LedgerReport>($"{ReportsUrl}/supplier-ledger?supplierId={supplier}");
        var order = await admin.GetFromJsonAsync<PurchaseOrderDetail>($"{ProcurementOrdersUrl}/{receive.PurchaseOrderId}");
        var progress = await admin.GetFromJsonAsync<ApprovalProgress>($"{ProcurementReceivesUrl}/{receive.Id}/approval");
        var reversal = (await JournalsAsync(admin, receive.Code)).Single(j => j.Narration.StartsWith("Cancelled"));

        Assert.Equal(["comment"], await ErrorsAsync(noReason));
        Assert.Equal("cancelled", cancelled.Status);
        Assert.Equal(HttpStatusCode.Conflict, again.StatusCode);
        Assert.Equal((10m, 750m), (ledger.ClosingQuantity, ledger.ClosingValue));
        Assert.Equal(0m, supplierLedger!.Closing);
        Assert.Equal(0m, Assert.Single(order!.Lines).ReceivedQuantity);
        Assert.Equal(("cancelled", "Wrong"), (progress!.Steps[^1].Status, progress.Steps[^1].Comment));
        Assert.Equal(
            [("Accounts payable", 1000m), ("Inventory", -750m), ("Purchase price difference", -250m)],
            reversal.Lines.Select(l => (l.AccountName, l.Debit - l.Credit)));
        Assert.Equal("cancelled", (await CancelledOrderAsync(admin, receive.PurchaseOrderId)).Status);
    }

    [Fact]
    public async Task A_record_in_use_cannot_be_cancelled()
    {
        var admin = await AdminAsync();
        var supplier = await SupplierAsync(factory.DhakaId);
        var receive = await ApprovedReceiveAsync(factory, admin, await ItemAsync(factory.DhakaId, 100m), 10, supplier);
        var payment = await PayAsync(admin, Payment(supplier) with { Lines = [Line(receive.Id, 400m)] });

        var order = await CancelAsync(admin, $"{ProcurementOrdersUrl}/{receive.PurchaseOrderId}");
        var paid = await CancelAsync(admin, $"{ProcurementReceivesUrl}/{receive.Id}");
        await ReadAsync<PurchasePaymentDetail>(await CancelAsync(admin, $"{PaymentsUrl}/{payment.Id}"));
        var free = await admin.GetFromJsonAsync<PurchaseReceiveDetail>($"{ProcurementReceivesUrl}/{receive.Id}");
        var afterPayment = await CancelAsync(admin, $"{ProcurementReceivesUrl}/{receive.Id}");

        Assert.Equal(HttpStatusCode.Conflict, order.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, paid.StatusCode);
        Assert.Equal(0m, free!.PaidAmount);
        Assert.Equal(HttpStatusCode.OK, afterPayment.StatusCode);
    }

    [Fact]
    public async Task Cancelling_stock_documents_moves_the_stock_back_or_is_refused_when_it_is_used()
    {
        var admin = await AdminAsync();
        var item = await ItemAsync(factory.DhakaId, 5m);
        var (from, to) = await WarehousesAsync();
        var opening = await ApproveAsync(admin, AdjustmentsUrl, await AdjustmentAsync("opening", item, 10, 7m));
        var lost = await ApproveAsync(admin, AdjustmentsUrl, await AdjustmentAsync("out", item, 2));
        var transfer = await ReadAsync<StockTransferDetail>(await admin.PostAsJsonAsync(TransfersUrl, Transfer(from, to, item, 3)));
        await ReadAsync<StockTransferDetail>(await admin.PostAsJsonAsync($"{TransfersUrl}/{transfer.Id}/approve", new ReviewRequest(null)));

        var refused = await CancelAsync(admin, $"{AdjustmentsUrl}/{opening.Id}");
        await ReadAsync<StockTransferDetail>(await CancelAsync(admin, $"{TransfersUrl}/{transfer.Id}"));
        await ReadAsync<StockAdjustmentDetail>(await CancelAsync(admin, $"{AdjustmentsUrl}/{lost.Id}"));
        var left = await LedgerAsync(admin, item, from);
        var moved = await LedgerAsync(admin, item, to);
        await ReadAsync<StockAdjustmentDetail>(await CancelAsync(admin, $"{AdjustmentsUrl}/{opening.Id}"));
        var after = await LedgerAsync(admin, item);

        Assert.Equal(HttpStatusCode.Conflict, refused.StatusCode);
        Assert.Equal((10m, 70m), (left.ClosingQuantity, left.ClosingValue));
        Assert.Equal((0m, 0m), (moved.ClosingQuantity, moved.ClosingValue));
        Assert.Equal((0m, 0m), (after.ClosingQuantity, after.ClosingValue));
    }

    [Fact]
    public async Task Cancelling_a_manual_journal_posts_the_opposite()
    {
        var admin = await AdminAsync();
        var bank = await AccountAsync(admin, PostingKeys.Bank);
        var cash = await AccountAsync(admin, PostingKeys.Cash);
        var before = await AccountLedgerAsync(admin, bank);
        var journal = await ApprovedJournalAsync(admin, Today, Debit(bank, 100m), Credit(cash, 100m));

        await ReadAsync<JournalDetail>(await CancelAsync(admin, $"{JournalsUrl}/{journal.Id}"));
        var after = await AccountLedgerAsync(admin, bank);

        Assert.Equal(before.Closing, after.Closing);
        Assert.Equal(before.Total + 2, after.Total);
    }

    [Fact]
    public async Task Each_bank_account_has_its_own_account_and_payments_credit_it()
    {
        var admin = await AdminAsync();
        var supplier = await SupplierAsync(factory.DhakaId);
        var receive = await ApprovedReceiveAsync(factory, admin, await ItemAsync(factory.DhakaId, 100m), 10, supplier);
        var bank = await BankAsync(admin, "bank");
        var wallet = await BankAsync(admin, "mobile");
        var cheque = await MethodAsync("CHEQUE");
        var payment = Payment(supplier) with { PaymentMethodId = cheque, BankAccountId = bank.Id, Lines = [Line(receive.Id, 300m)] };

        var wrongKind = await admin.PostAsJsonAsync(PaymentsUrl, payment with { BankAccountId = wallet.Id });
        var cash = await admin.PostAsJsonAsync(PaymentsUrl, payment with { PaymentMethodId = null });
        var paid = await PayAsync(admin, payment);
        var duplicate = await admin.PostAsJsonAsync(BanksUrl, Bank("bank") with { ChartOfAccountId = bank.ChartOfAccountId });
        var journal = Assert.Single(await JournalsAsync(admin, paid.Code));

        Assert.NotEqual(bank.ChartOfAccountId, wallet.ChartOfAccountId);
        Assert.Equal(bank.Name, bank.ChartOfAccountName);
        Assert.Equal(["bankAccountId"], await ErrorsAsync(wrongKind));
        Assert.Equal(["bankAccountId"], await ErrorsAsync(cash));
        Assert.Equal(["chartOfAccountId"], await ErrorsAsync(duplicate));
        Assert.Equal([("Accounts payable", 300m), (bank.Name, -300m)], journal.Lines.Select(l => (l.AccountName, l.Debit - l.Credit)));
    }

    [Fact]
    public async Task An_advance_is_used_on_later_bills_and_never_more_than_is_left()
    {
        var admin = await AdminAsync();
        var supplier = await SupplierAsync(factory.DhakaId);
        var receive = await ApprovedReceiveAsync(factory, admin, await ItemAsync(factory.DhakaId, 100m), 10, supplier);
        var advance = await PayAsync(admin, Payment(supplier) with { IsAdvance = true, AdvanceAmount = 600m });
        var noAmount = await admin.PostAsJsonAsync(PaymentsUrl, Payment(supplier) with { IsAdvance = true });

        var payable = await admin.GetFromJsonAsync<SupplierPayable>($"{PaymentsUrl}/bills?supplierId={supplier}");
        var tooMuch = await admin.PostAsJsonAsync(PaymentsUrl, Payment(supplier) with { Lines = [Line(receive.Id, 0m, 601m)] });
        var bill = await PayAsync(admin, Payment(supplier) with { Lines = [Line(receive.Id, 400m, 500m)] });
        var left = await admin.GetFromJsonAsync<SupplierPayable>($"{PaymentsUrl}/bills?supplierId={supplier}");
        var used = await CancelAsync(admin, $"{PaymentsUrl}/{advance.Id}");
        var ledger = await admin.GetFromJsonAsync<LedgerReport>($"{ReportsUrl}/supplier-ledger?supplierId={supplier}");
        var journal = Assert.Single(await JournalsAsync(admin, bill.Code));

        Assert.True(advance.IsAdvance);
        Assert.Equal(["advanceAmount"], await ErrorsAsync(noAmount));
        Assert.Equal(600m, payable!.AvailableAdvance);
        Assert.Equal(["lines"], await ErrorsAsync(tooMuch));
        Assert.Equal((100m, 100m), (left!.AvailableAdvance, Assert.Single(left.Bills).Due));
        Assert.Equal(HttpStatusCode.Conflict, used.StatusCode);
        Assert.Equal(0m, ledger!.Closing);
        Assert.Equal(
            [("Accounts payable", 900m), ("Cash in hand", -400m), ("Advance to suppliers", -500m)],
            journal.Lines.Select(l => (l.AccountName, l.Debit - l.Credit)));
    }

    [Fact]
    public async Task Vds_is_deposited_once_and_blocks_cancelling_its_payment()
    {
        var admin = await AdminAsync();
        var supplier = await SupplierAsync(factory.DhakaId);
        var receive = await ApprovedReceiveAsync(factory, admin, await ItemAsync(factory.DhakaId, 100m), 10, supplier);
        var payment = await PayAsync(admin, Payment(supplier) with { Lines = [Line(receive.Id, 900m) with { Vds = 70m }] });
        async Task<PendingTax?> PendingAsync() =>
            (await admin.GetFromJsonAsync<List<PendingTax>>($"{DepositsUrl}/pending?type=vds"))!.Find(t => t.PurchasePaymentId == payment.Id);

        var before = await PendingAsync();
        var tooMuch = await admin.PostAsJsonAsync(DepositsUrl, Deposit(payment.Id, 71m));
        var noTds = await admin.PostAsJsonAsync(DepositsUrl, Deposit(payment.Id, 1m) with { Type = "tds" });
        var deposit = await ReadAsync<TaxDepositDetail>(await admin.PostAsJsonAsync(DepositsUrl, Deposit(payment.Id, 70m)));
        await ReadAsync<TaxDepositDetail>(await admin.PostAsJsonAsync($"{DepositsUrl}/{deposit.Id}/approve", new ReviewRequest(null)));
        var after = await PendingAsync();
        var blocked = await CancelAsync(admin, $"{PaymentsUrl}/{payment.Id}");
        var journal = Assert.Single(await JournalsAsync(admin, deposit.Code));
        await ReadAsync<TaxDepositDetail>(await CancelAsync(admin, $"{DepositsUrl}/{deposit.Id}"));
        var freed = await PendingAsync();

        Assert.Equal(70m, before!.Remaining);
        Assert.Equal(["lines"], await ErrorsAsync(tooMuch));
        Assert.Equal(["lines"], await ErrorsAsync(noTds));
        Assert.Null(after);
        Assert.Equal(HttpStatusCode.Conflict, blocked.StatusCode);
        Assert.Equal([("VDS payable", 70m), ("Cash in hand", -70m)], journal.Lines.Select(l => (l.AccountName, l.Debit - l.Credit)));
        Assert.Equal(70m, freed!.Remaining);
    }

    [Fact]
    public async Task A_closed_period_takes_no_postings_and_a_closed_year_moves_profit_to_retained_earnings()
    {
        var unitId = await OtherUnitAsync();
        var login = await factory.ErpAdminLoginAsync();
        var admin = factory.CreateClient(login.AccessToken, unitId);
        var cash = await AccountAsync(admin, PostingKeys.Cash);
        var sales = await AccountByNameAsync(admin, "Sales");
        var rent = await AccountByNameAsync(admin, "Rent");
        var retained = await AccountAsync(admin, PostingKeys.RetainedEarnings);
        await ApprovedJournalAsync(admin, new DateOnly(2020, 7, 10), Debit(cash, 1000m), Credit(sales, 1000m));
        await ApprovedJournalAsync(admin, new DateOnly(2021, 6, 20), Debit(rent, 300m), Credit(cash, 300m));
        var waiting = await ReadAsync<JournalDetail>(await admin.PostAsJsonAsync(
            JournalsUrl, Journal(new DateOnly(2020, 8, 5), Debit(cash, 5m), Credit(sales, 5m))));

        async Task<HttpResponseMessage> CloseAsync(DateOnly end, bool year = false) =>
            await admin.PostAsJsonAsync($"{PeriodsUrl}/close", new PeriodClosingRequest(end, year));
        var skipped = await CloseAsync(new DateOnly(2020, 8, 31));
        var july = await CloseAsync(new DateOnly(2020, 7, 31));
        var pendingMonth = await CloseAsync(new DateOnly(2020, 8, 31));
        Assert.Equal(HttpStatusCode.NoContent, (await admin.DeleteAsync($"{JournalsUrl}/{waiting.Id}")).StatusCode);
        var lockedEntry = await admin.PostAsJsonAsync(JournalsUrl, Journal(new DateOnly(2020, 7, 15), Debit(cash, 5m), Credit(sales, 5m)));
        var earlyYear = await CloseAsync(new DateOnly(2021, 6, 30), true);
        for (var end = new DateOnly(2020, 8, 31); end <= new DateOnly(2021, 6, 30); end = end.AddDays(1).AddMonths(1).AddDays(-1))
        {
            Assert.Equal(HttpStatusCode.OK, (await CloseAsync(end)).StatusCode);
        }

        var income = await admin.GetFromJsonAsync<IncomeStatement>($"{ReportsUrl}/income-statement?from=2020-07-01&to=2021-06-30");
        var closedYear = await ReadAsync<PeriodClosingReport>(await CloseAsync(new DateOnly(2021, 6, 30), true));
        var incomeAfter = await admin.GetFromJsonAsync<IncomeStatement>($"{ReportsUrl}/income-statement?from=2020-07-01&to=2021-06-30");
        var sheet = await admin.GetFromJsonAsync<BalanceSheet>($"{ReportsUrl}/balance-sheet?to=2021-06-30");
        var kept = await AccountLedgerAsync(admin, retained);
        var monthReopen = await admin.PostAsJsonAsync($"{PeriodsUrl}/reopen", new PeriodClosingRequest(new DateOnly(2021, 6, 30), false));
        var fiscalStart = await admin.PutAsJsonAsync($"{PeriodsUrl}/fiscal-year", new FiscalYearRequest(1));
        await ReadAsync<PeriodClosingReport>(await admin.PostAsJsonAsync($"{PeriodsUrl}/reopen", new PeriodClosingRequest(new DateOnly(2021, 6, 30), true)));
        var keptAfter = await AccountLedgerAsync(admin, retained);

        Assert.Equal(HttpStatusCode.Conflict, skipped.StatusCode);
        Assert.Equal(HttpStatusCode.OK, july.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, pendingMonth.StatusCode);
        Assert.Equal(["journalDate"], await ErrorsAsync(lockedEntry));
        Assert.Equal(HttpStatusCode.Conflict, earlyYear.StatusCode);
        Assert.Equal((1000m, 300m, 700m), (income!.Income.Total, income.Expenses.Total, income.NetProfit));
        Assert.True(closedYear.Years.Single(y => y.End == new DateOnly(2021, 6, 30)).IsClosed);
        Assert.Equal(700m, incomeAfter!.NetProfit);
        Assert.Equal((700m, 0m, 700m), (sheet!.TotalAssets, sheet.CurrentEarnings, sheet.TotalEquity));
        Assert.Equal(-700m, kept.Closing);
        Assert.Equal(HttpStatusCode.Conflict, monthReopen.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, fiscalStart.StatusCode);
        Assert.Equal(0m, keptAfter.Closing);
    }

    [Fact]
    public async Task The_chart_has_eight_main_groups_and_every_account_is_under_one()
    {
        var admin = await AdminAsync();
        var accounts = (await admin.GetFromJsonAsync<List<ChartOfAccountOption>>($"{AccountsUrl}/options"))!;
        var main = accounts.Where(a => a.ParentId is null).ToList();
        var request = new SaveChartOfAccountRequest
        {
            Code = Unique(),
            Name = $"Account {Unique()}",
            Type = "nonCurrentAsset",
            IsGroup = true,
            ParentId = null,
            Description = null,
            IsActive = true,
        };

        var newMain = await admin.PostAsJsonAsync(AccountsUrl, request);
        var subGroup = await admin.PostAsJsonAsync(AccountsUrl, request with { ParentId = main.Single(a => a.Type == "nonCurrentAsset").Id });
        var group = main.Single(a => a.Type == "equity");
        var changed = await admin.PutAsJsonAsync($"{AccountsUrl}/{group.Id}", request with { Code = group.Code, Name = group.Name });

        Assert.Equal(
            ["costOfSales", "currentAsset", "currentLiability", "equity", "expense", "income", "nonCurrentAsset", "nonCurrentLiability"],
            main.Select(a => a.Type).Order(StringComparer.Ordinal));
        Assert.All(main, a => Assert.True(a.IsGroup));
        Assert.Equal(["parentId"], await ErrorsAsync(newMain));
        Assert.Equal(HttpStatusCode.OK, subGroup.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, changed.StatusCode);
    }

    [Fact]
    public async Task A_transfer_between_offices_checks_that_the_warehouse_belongs_to_the_office()
    {
        var admin = await AdminAsync();
        var item = await ItemAsync(factory.DhakaId, 5m);
        var (from, to) = await WarehousesAsync();
        await ApproveAsync(admin, AdjustmentsUrl, await AdjustmentAsync("opening", item, 5, 7m));
        var fromOffice = await OfficeOfAsync(from);
        var toOffice = await OfficeOfAsync(to);
        var wrong = await admin.PostAsJsonAsync(TransfersUrl, Transfer(from, to, item, 1) with { FromOfficeId = Guid.NewGuid() });
        var saved = await ReadAsync<StockTransferDetail>(await admin.PostAsJsonAsync(
            TransfersUrl, Transfer(from, to, item, 1) with { FromOfficeId = fromOffice, ToOfficeId = toOffice }));

        Assert.Equal(["fromWarehouseId"], await ErrorsAsync(wrong));
        Assert.Equal((fromOffice, toOffice), (saved.FromOfficeId, saved.ToOfficeId));
    }

    private async Task<HttpClient> AdminAsync()
    {
        var login = await factory.ErpAdminLoginAsync();
        return factory.CreateClient(login.AccessToken, factory.DhakaId);
    }

    private static Task<HttpResponseMessage> CancelAsync(HttpClient admin, string url, string? reason = "Wrong") =>
        admin.PostAsJsonAsync($"{url}/cancel", new ReviewRequest(reason));

    private static async Task<PurchaseOrderDetail> CancelledOrderAsync(HttpClient admin, int orderId) =>
        await ReadAsync<PurchaseOrderDetail>(await CancelAsync(admin, $"{ProcurementOrdersUrl}/{orderId}"));

    private static async Task<StockAdjustmentDetail> ApproveAsync(HttpClient admin, string url, SaveStockAdjustmentRequest request)
    {
        var saved = await ReadAsync<StockAdjustmentDetail>(await admin.PostAsJsonAsync(url, request));
        return await ReadAsync<StockAdjustmentDetail>(await admin.PostAsJsonAsync($"{url}/{saved.Id}/approve", new ReviewRequest(null)));
    }

    private static async Task<PurchasePaymentDetail> PayAsync(HttpClient admin, SavePurchasePaymentRequest request)
    {
        var saved = await ReadAsync<PurchasePaymentDetail>(await admin.PostAsJsonAsync(PaymentsUrl, request));
        return await ReadAsync<PurchasePaymentDetail>(await admin.PostAsJsonAsync($"{PaymentsUrl}/{saved.Id}/approve", new ReviewRequest(null)));
    }

    private static async Task<JournalDetail> ApprovedJournalAsync(HttpClient admin, DateOnly date, params SaveJournalLine[] lines)
    {
        var saved = await ReadAsync<JournalDetail>(await admin.PostAsJsonAsync(JournalsUrl, Journal(date, lines)));
        return await ReadAsync<JournalDetail>(await admin.PostAsJsonAsync($"{JournalsUrl}/{saved.Id}/approve", new ReviewRequest(null)));
    }

    private static async Task<StockLedgerReport> LedgerAsync(HttpClient admin, int itemId, Guid? warehouseId = null) =>
        (await admin.GetFromJsonAsync<StockLedgerReport>(
            $"/api/erp/inventory/stock-ledger?itemId={itemId}{(warehouseId is null ? "" : $"&warehouseId={warehouseId}")}"))!;

    private static async Task<LedgerReport> AccountLedgerAsync(HttpClient admin, int accountId) =>
        (await admin.GetFromJsonAsync<LedgerReport>($"{ReportsUrl}/account-ledger?accountId={accountId}"))!;

    private static async Task<List<JournalDetail>> JournalsAsync(HttpClient admin, string documentCode)
    {
        var journals = new List<JournalDetail>();
        foreach (var row in (await admin.GetFromJsonAsync<JournalList>($"{JournalsUrl}?search={documentCode}"))!.Items)
        {
            journals.Add((await admin.GetFromJsonAsync<JournalDetail>($"{JournalsUrl}/{row.Id}"))!);
        }

        return journals;
    }

    private static async Task<int> AccountAsync(HttpClient admin, string key) =>
        (await admin.GetFromJsonAsync<List<ChartOfAccountOption>>($"{AccountsUrl}/options"))!.Single(a => a.PostingKeys.Contains(key)).Id;

    private static async Task<int> AccountByNameAsync(HttpClient admin, string name) =>
        (await admin.GetFromJsonAsync<List<ChartOfAccountOption>>($"{AccountsUrl}/options"))!.Single(a => a.Name == name).Id;

    private static async Task<int> MethodAsync(string code)
    {
        await using var db = ApiFactory.CreateDbContext();
        return await db.LookupValues.AcrossTenants()
            .Where(v => v.Code == code && v.LookupType!.Key == ProcurementLookups.PaymentMethod)
            .Join(db.BusinessUnits.AcrossTenants().Where(u => u.Code == "DHK"), v => v.AccountId, u => u.AccountId, (v, _) => v.Id)
            .FirstAsync();
    }

    private async Task<Guid> OtherUnitAsync()
    {
        await using var db = ApiFactory.CreateDbContext();
        var accountId = await db.BusinessUnits.AcrossTenants().Where(u => u.Id == factory.DhakaId).Select(u => u.AccountId).FirstAsync();
        return await db.BusinessUnits.AcrossTenants()
            .Where(u => u.AccountId == accountId && u.Id != factory.DhakaId && u.IsActive)
            .Select(u => u.Id)
            .FirstAsync();
    }

    private async Task<(Guid From, Guid To)> WarehousesAsync()
    {
        var first = await WarehouseAsync(factory.DhakaId);
        await using var db = ApiFactory.CreateDbContext();
        var second = await db.Warehouses.AcrossTenants()
            .Where(w => w.BusinessUnitId == factory.DhakaId && w.IsActive && w.Id != first)
            .Select(w => w.Id)
            .FirstAsync();
        return (first, second);
    }

    private static async Task<Guid?> OfficeOfAsync(Guid warehouseId)
    {
        await using var db = ApiFactory.CreateDbContext();
        return await db.Warehouses.AcrossTenants().Where(w => w.Id == warehouseId).Select(w => w.OfficeId).SingleAsync();
    }

    private static SaveBankAccountRequest Bank(string type) => new()
    {
        Name = $"Account {Unique()}",
        Type = type,
        BankName = "City Bank",
        BranchName = null,
        AccountNo = Unique(),
        RoutingNo = null,
        ChartOfAccountId = null,
        IsActive = true,
    };

    private static async Task<BankAccountRow> BankAsync(HttpClient admin, string type) =>
        await ReadAsync<BankAccountRow>(await admin.PostAsJsonAsync(BanksUrl, Bank(type)));

    private async Task<SaveStockAdjustmentRequest> AdjustmentAsync(string type, int itemId, decimal quantity, decimal? rate = null) => new()
    {
        Code = null,
        AdjustmentDate = Today,
        Type = type,
        WarehouseId = await WarehouseAsync(factory.DhakaId),
        ReasonId = null,
        Remarks = null,
        Lines = [new SaveStockAdjustmentLine { ItemId = itemId, Quantity = quantity, Rate = rate, Remarks = null }],
    };

    private static SaveStockTransferRequest Transfer(Guid from, Guid to, int itemId, decimal quantity) => new()
    {
        Code = null,
        TransferDate = Today,
        FromOfficeId = null,
        FromWarehouseId = from,
        ToOfficeId = null,
        ToWarehouseId = to,
        Remarks = null,
        Lines = [new SaveStockTransferLine { ItemId = itemId, Quantity = quantity, Remarks = null }],
    };

    private static SavePurchasePaymentRequest Payment(int supplierId) => new()
    {
        Code = null,
        PaymentDate = Today,
        SupplierId = supplierId,
        IsAdvance = false,
        PaymentMethodId = null,
        BankAccountId = null,
        PayeeName = null,
        BankAccountNo = null,
        InstrumentNo = null,
        InstrumentDate = null,
        Remarks = null,
        AdvanceAmount = null,
        Lines = [],
    };

    private static SavePurchasePaymentLine Line(int receiveId, decimal payment, decimal? advance = null) => new()
    {
        PurchaseReceiveId = receiveId,
        PaymentAmount = payment,
        Vds = null,
        Tds = null,
        DiscountAmount = null,
        AdvanceAdjusted = advance,
    };

    private static SaveTaxDepositRequest Deposit(int paymentId, decimal amount) => new()
    {
        Code = null,
        DepositDate = Today,
        Type = "vds",
        PaymentMethodId = null,
        BankAccountId = null,
        ChallanNo = "T-1",
        ChallanDate = null,
        Remarks = null,
        Lines = [new SaveTaxDepositLine { PurchasePaymentId = paymentId, Amount = amount }],
    };

    private static SaveJournalRequest Journal(DateOnly date, params SaveJournalLine[] lines) => new()
    {
        Code = null,
        JournalDate = date,
        Narration = "Test journal",
        Lines = lines,
    };

    private static SaveJournalLine Debit(int accountId, decimal amount) =>
        new() { ChartOfAccountId = accountId, SupplierId = null, Debit = amount, Credit = null, Remarks = null };

    private static SaveJournalLine Credit(int accountId, decimal amount) =>
        new() { ChartOfAccountId = accountId, SupplierId = null, Debit = null, Credit = amount, Remarks = null };
}
