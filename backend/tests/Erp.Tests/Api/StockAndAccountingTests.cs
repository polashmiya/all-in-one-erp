using System.Net;
using System.Net.Http.Json;
using Erp.Api.Accounting.AccountingReports;
using Erp.Api.Accounting.ChartOfAccounts;
using Erp.Api.Accounting.Journals;
using Erp.Api.Approvals;
using Erp.Api.Inventory.StockAdjustments;
using Erp.Api.Inventory.StockReports;
using Erp.Api.Inventory.StockTransfers;
using Erp.Api.Procurement;
using Erp.Api.Procurement.PurchasePayments;
using Erp.Api.Procurement.PurchaseReceives;
using Erp.Api.Procurement.PurchaseReports;
using Erp.Api.Procurement.PurchaseReturns;
using Erp.Core.Domain.Accounting;
using Erp.Infrastructure.Persistence;
using Erp.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using static Erp.Tests.Api.ProcurementTestData;

namespace Erp.Tests.Api;

[Collection(ApiCollection.Name)]
public sealed class StockAndAccountingTests(ApiFactory factory)
{
    private const string ReturnsUrl = "/api/erp/procurement/purchase-returns";
    private const string PaymentsUrl = "/api/erp/procurement/purchase-payments";
    private const string AdjustmentsUrl = "/api/erp/inventory/stock-adjustments";
    private const string TransfersUrl = "/api/erp/inventory/stock-transfers";
    private const string AccountsUrl = "/api/erp/accounting/chart-of-accounts";
    private const string JournalsUrl = "/api/erp/accounting/journals";
    private const string ReportsUrl = "/api/erp/accounting/reports";

    [Fact]
    public async Task An_approved_receive_adds_stock_with_its_charges_and_debits_inventory_and_vat()
    {
        var admin = await AdminAsync();
        var supplier = await SupplierAsync(factory.DhakaId);
        var item = await ItemAsync(factory.DhakaId, 100m, 10m);
        await SetRebatableAsync(item);
        var order = await ApprovedOrderAsync(factory, admin, item, 10, supplier);
        var receive = await ReadAsync<PurchaseReceiveDetail>(await admin.PostAsJsonAsync(ProcurementReceivesUrl,
            await ReceiveAsync(factory, order.Id, item, 10) with { HeaderDiscount = 100m, DeliveryCharge = 50m }));
        var beforeApproval = await LedgerAsync(admin, item);
        await ApproveAsync(admin, $"{ProcurementReceivesUrl}/{receive.Id}");

        var ledger = await LedgerAsync(admin, item);
        var journal = await JournalAsync(admin, receive.Code);

        Assert.Empty(beforeApproval.Items);
        Assert.Equal(1050m, receive.NetAmount);
        Assert.Equal((10m, 950m), (ledger.ClosingQuantity, ledger.ClosingValue));
        Assert.Equal((receive.Code, 10m, 0m), (ledger.Items[0].DocumentCode, ledger.Items[0].QuantityIn, ledger.Items[0].QuantityOut));
        Assert.Equal(
            [("Inventory", 950m, 0m), ("VAT receivable", 100m, 0m), ("Accounts payable", 0m, 1050m)],
            journal.Lines.Select(l => (l.AccountName, l.Debit, l.Credit)));
        Assert.Equal(supplier, journal.Lines[2].SupplierId);
        Assert.Equal("approved", journal.Status);
        Assert.False(journal.IsManual);
        await using var db = ApiFactory.CreateDbContext();
        var saved = await db.Items.AcrossTenants().SingleAsync(i => i.Id == item);
        Assert.Equal((95m, 95m), (saved.AverageCostPrice, saved.LastCostPrice));
    }

    [Fact]
    public async Task A_return_takes_stock_at_average_cost_and_posts_the_price_difference()
    {
        var admin = await AdminAsync();
        var supplier = await SupplierAsync(factory.DhakaId);
        var item = await ItemAsync(factory.DhakaId, 100m);
        await ApproveAsync(admin, AdjustmentsUrl, await AdjustmentAsync("opening", item, 10, 80m));
        var receive = await ApprovedReceiveAsync(factory, admin, item, 10, supplier);

        var returned = await ReadAsync<PurchaseReturnDetail>(await admin.PostAsJsonAsync(ReturnsUrl, await ReturnAsync(receive.Id, item, 4)));
        await ApproveAsync(admin, $"{ReturnsUrl}/{returned.Id}");
        var ledger = await LedgerAsync(admin, item);
        var journal = await JournalAsync(admin, returned.Code);

        Assert.Equal((16m, 1440m), (ledger.ClosingQuantity, ledger.ClosingValue));
        Assert.Equal(
            [("Accounts payable", 400m, 0m), ("Inventory", 0m, 360m), ("Purchase price difference", 0m, 40m)],
            journal.Lines.Select(l => (l.AccountName, l.Debit, l.Credit)));
    }

    [Fact]
    public async Task A_return_of_stock_that_is_gone_stays_pending()
    {
        var admin = await AdminAsync();
        var item = await ItemAsync(factory.DhakaId, 10m);
        var receive = await ApprovedReceiveAsync(factory, admin, item, 5);
        var returned = await ReadAsync<PurchaseReturnDetail>(await admin.PostAsJsonAsync(ReturnsUrl, await ReturnAsync(receive.Id, item, 5)));
        await ApproveAsync(admin, AdjustmentsUrl, await AdjustmentAsync("out", item, 2));

        var refused = await admin.PostAsJsonAsync($"{ReturnsUrl}/{returned.Id}/approve", new ReviewRequest(null));
        var after = await admin.GetFromJsonAsync<PurchaseReturnDetail>($"{ReturnsUrl}/{returned.Id}");
        var ledger = await LedgerAsync(admin, item);

        Assert.Equal(HttpStatusCode.Conflict, refused.StatusCode);
        Assert.Equal("pending", after!.Status);
        Assert.Equal(3m, ledger.ClosingQuantity);
    }

    [Fact]
    public async Task A_payment_clears_the_supplier_and_credits_cash_vds_tds_and_discount()
    {
        var admin = await AdminAsync();
        var supplier = await SupplierAsync(factory.DhakaId);
        var item = await ItemAsync(factory.DhakaId, 100m);
        var receive = await ApprovedReceiveAsync(factory, admin, item, 10, supplier);

        var payment = await ReadAsync<PurchasePaymentDetail>(await admin.PostAsJsonAsync(PaymentsUrl, new SavePurchasePaymentRequest
        {
            Code = null,
            PaymentDate = Today,
            SupplierId = supplier,
            PaymentMethodId = null,
            PayeeName = null,
            BankAccountNo = null,
            InstrumentNo = null,
            InstrumentDate = null,
            Remarks = null,
            Lines = [new SavePurchasePaymentLine { PurchaseReceiveId = receive.Id, PaymentAmount = 880m, Vds = 50m, Tds = 40m, DiscountAmount = 30m }],
        }));
        await ApproveAsync(admin, $"{PaymentsUrl}/{payment.Id}");
        var journal = await JournalAsync(admin, payment.Code);
        var ledger = await admin.GetFromJsonAsync<LedgerReport>($"{ReportsUrl}/supplier-ledger?supplierId={supplier}");

        Assert.Equal(
            [
                ("Accounts payable", 1000m, 0m), ("Cash in hand", 0m, 880m), ("VDS payable", 0m, 50m), ("TDS payable", 0m, 40m),
                ("Purchase discount received", 0m, 30m),
            ],
            journal.Lines.Select(l => (l.AccountName, l.Debit, l.Credit)));
        Assert.Equal([-1000m, 0m], ledger!.Items.Select(r => r.Balance));
        Assert.Equal((1000m, 1000m, 0m), (ledger.Debit, ledger.Credit, ledger.Closing));
    }

    [Fact]
    public async Task Adjustments_change_stock_at_average_cost_and_post_to_their_own_accounts()
    {
        var admin = await AdminAsync();
        var item = await ItemAsync(factory.DhakaId, 5m);
        var opening = await ApproveAsync(admin, AdjustmentsUrl, await AdjustmentAsync("opening", item, 10, 20m));
        var found = await ApproveAsync(admin, AdjustmentsUrl, await AdjustmentAsync("in", item, 10, 30m));
        var lost = await ApproveAsync(admin, AdjustmentsUrl, await AdjustmentAsync("out", item, 4, 999m));
        var tooMuch = await admin.PostAsJsonAsync(AdjustmentsUrl, await AdjustmentAsync("out", item, 17));
        var ledger = await LedgerAsync(admin, item);

        Assert.StartsWith("ADJ-", opening.Code);
        Assert.Equal([10m, 20m, 16m], ledger.Items.Select(r => r.BalanceQuantity));
        Assert.Equal([200m, 500m, 400m], ledger.Items.Select(r => r.BalanceValue));
        Assert.Equal(100m, lost.TotalAmount);
        Assert.Equal(["lines"], await ErrorsAsync(tooMuch));
        Assert.Equal([("Inventory", 200m), ("Opening balance", -200m)], await EntriesAsync(admin, opening.Code));
        Assert.Equal([("Inventory", 300m), ("Stock gain", -300m)], await EntriesAsync(admin, found.Code));
        Assert.Equal([("Stock loss", 100m), ("Inventory", -100m)], await EntriesAsync(admin, lost.Code));
    }

    [Fact]
    public async Task A_rejected_adjustment_posts_nothing()
    {
        var admin = await AdminAsync();
        var item = await ItemAsync(factory.DhakaId, 5m);
        var adjustment = await ReadAsync<StockAdjustmentDetail>(await admin.PostAsJsonAsync(AdjustmentsUrl, await AdjustmentAsync("in", item, 3, 5m)));
        await ReadAsync<StockAdjustmentDetail>(await admin.PostAsJsonAsync($"{AdjustmentsUrl}/{adjustment.Id}/reject", new ReviewRequest("No")));

        Assert.Empty((await LedgerAsync(admin, item)).Items);
        Assert.Empty((await admin.GetFromJsonAsync<JournalList>($"{JournalsUrl}?search={adjustment.Code}"))!.Items);
    }

    [Fact]
    public async Task A_transfer_moves_quantity_and_value_without_a_journal()
    {
        var admin = await AdminAsync();
        var item = await ItemAsync(factory.DhakaId, 5m);
        var from = await WarehouseAsync(factory.DhakaId);
        var to = await SecondWarehouseAsync();
        await ApproveAsync(admin, AdjustmentsUrl, await AdjustmentAsync("opening", item, 10, 7m));

        var request = new SaveStockTransferRequest
        {
            Code = null,
            TransferDate = Today,
            FromWarehouseId = from,
            ToWarehouseId = to,
            Remarks = null,
            Lines = [new SaveStockTransferLine { ItemId = item, Quantity = 4, Remarks = null }],
        };
        var transfer = await ReadAsync<StockTransferDetail>(await admin.PostAsJsonAsync(TransfersUrl, request));
        await ApproveAsync(admin, $"{TransfersUrl}/{transfer.Id}");
        var same = await admin.PostAsJsonAsync(TransfersUrl, request with { ToWarehouseId = from });
        var tooMuch = await admin.PostAsJsonAsync(TransfersUrl, request with { Lines = [request.Lines[0] with { Quantity = 7 }] });
        var left = await LedgerAsync(admin, item, from);
        var moved = await LedgerAsync(admin, item, to);

        Assert.Equal((6m, 42m), (left.ClosingQuantity, left.ClosingValue));
        Assert.Equal((4m, 28m), (moved.ClosingQuantity, moved.ClosingValue));
        Assert.Equal(["toWarehouseId"], await ErrorsAsync(same));
        Assert.Equal(["lines"], await ErrorsAsync(tooMuch));
        Assert.Empty((await admin.GetFromJsonAsync<JournalList>($"{JournalsUrl}?search={transfer.Code}"))!.Items);
    }

    [Fact]
    public async Task A_manual_journal_must_balance_and_counts_only_when_approved()
    {
        var admin = await AdminAsync();
        var bank = await AccountAsync(admin, PostingKeys.Bank);
        var cash = await AccountAsync(admin, PostingKeys.Cash);
        var payable = await AccountAsync(admin, PostingKeys.AccountsPayable);
        var inventory = await AccountAsync(admin, PostingKeys.Inventory);
        var before = await admin.GetFromJsonAsync<LedgerReport>($"{ReportsUrl}/account-ledger?accountId={bank}");

        var unbalanced = await admin.PostAsJsonAsync(JournalsUrl, Journal(Debit(bank, 100m), Credit(cash, 90m)));
        var noSupplier = await admin.PostAsJsonAsync(JournalsUrl, Journal(Debit(payable, 100m), Credit(cash, 100m)));
        var stock = await admin.PostAsJsonAsync(JournalsUrl, Journal(Debit(inventory, 100m), Credit(cash, 100m)));
        var oneSide = await admin.PostAsJsonAsync(JournalsUrl, Journal(Debit(bank, 100m), Debit(cash, 100m)));
        var journal = await ReadAsync<JournalDetail>(await admin.PostAsJsonAsync(JournalsUrl, Journal(Debit(bank, 100m), Credit(cash, 100m))));
        var pending = await admin.GetFromJsonAsync<LedgerReport>($"{ReportsUrl}/account-ledger?accountId={bank}");
        await ApproveAsync(admin, $"{JournalsUrl}/{journal.Id}");
        var approved = await admin.GetFromJsonAsync<LedgerReport>($"{ReportsUrl}/account-ledger?accountId={bank}");
        var deleted = await admin.DeleteAsync($"{JournalsUrl}/{journal.Id}");

        Assert.Equal(["lines"], await ErrorsAsync(unbalanced));
        Assert.Equal(["lines"], await ErrorsAsync(noSupplier));
        Assert.Equal(["lines"], await ErrorsAsync(stock));
        Assert.Equal(["lines"], await ErrorsAsync(oneSide));
        Assert.True(journal.IsManual);
        Assert.Equal(before!.Closing, pending!.Closing);
        Assert.Equal(before.Closing + 100m, approved!.Closing);
        Assert.Equal(HttpStatusCode.Conflict, deleted.StatusCode);
    }

    [Fact]
    public async Task Automatic_journals_cannot_be_changed_by_hand()
    {
        var admin = await AdminAsync();
        var adjustment = await ApproveAsync(admin, AdjustmentsUrl, await AdjustmentAsync("in", await ItemAsync(factory.DhakaId, 5m), 1, 5m));
        var journal = await JournalAsync(admin, adjustment.Code);
        var cash = await AccountAsync(admin, PostingKeys.Cash);
        var bank = await AccountAsync(admin, PostingKeys.Bank);

        var updated = await admin.PutAsJsonAsync($"{JournalsUrl}/{journal.Id}", Journal(Debit(bank, 5m), Credit(cash, 5m)));
        var deleted = await admin.DeleteAsync($"{JournalsUrl}/{journal.Id}");

        Assert.Equal(HttpStatusCode.NotFound, updated.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, deleted.StatusCode);
    }

    [Fact]
    public async Task The_books_balance_and_the_stock_value_matches_the_inventory_account()
    {
        var admin = await AdminAsync();
        await ApprovedReceiveAsync(factory, admin, await ItemAsync(factory.DhakaId, 33.33m, 7.5m), 7);

        var trial = await admin.GetFromJsonAsync<TrialBalanceReport>($"{ReportsUrl}/trial-balance");
        var sheet = await admin.GetFromJsonAsync<BalanceSheet>($"{ReportsUrl}/balance-sheet");
        var income = await admin.GetFromJsonAsync<IncomeStatement>($"{ReportsUrl}/income-statement");
        var stock = await admin.GetFromJsonAsync<StockBalanceReport>("/api/erp/inventory/stock-balance");

        Assert.Equal(trial!.Debit, trial.Credit);
        Assert.Equal(trial.ClosingDebit, trial.ClosingCredit);
        Assert.Equal(sheet!.TotalAssets, sheet.TotalLiabilities + sheet.TotalEquity);
        Assert.Equal(sheet.CurrentEarnings, income!.NetProfit);
        Assert.Equal(trial.Items.Single(r => r.Name == "Inventory").Closing, stock!.TotalValue);
    }

    [Fact]
    public async Task Accounts_in_use_keep_their_type_and_stay_active()
    {
        var admin = await AdminAsync();
        var cash = await AccountAsync(admin, PostingKeys.Cash);
        var code = Unique();
        var request = new SaveChartOfAccountRequest
        {
            Code = code,
            Name = $"Account {code}",
            Type = "expense",
            IsGroup = false,
            ParentId = null,
            Description = null,
            IsActive = true,
        };

        var created = await ReadAsync<ChartOfAccountRow>(await admin.PostAsJsonAsync(AccountsUrl, request));
        var duplicate = await admin.PostAsJsonAsync(AccountsUrl, request with { Name = "Other name" });
        var wrongParent = await admin.PutAsJsonAsync($"{AccountsUrl}/{created.Id}", request with { ParentId = cash });
        var deactivated = await admin.PostAsJsonAsync($"{AccountsUrl}/status", new ChartOfAccountStatusRequest([cash], false));

        Assert.Equal("expense", created.Type);
        Assert.Equal(["code"], await ErrorsAsync(duplicate));
        Assert.Equal(["parentId"], await ErrorsAsync(wrongParent));
        Assert.Equal(HttpStatusCode.Conflict, deactivated.StatusCode);
    }

    [Fact]
    public async Task The_purchase_report_shows_returns_payments_and_due()
    {
        var admin = await AdminAsync();
        var supplier = await SupplierAsync(factory.DhakaId);
        var item = await ItemAsync(factory.DhakaId, 100m);
        var receive = await ApprovedReceiveAsync(factory, admin, item, 10, supplier);
        var returned = await ReadAsync<PurchaseReturnDetail>(await admin.PostAsJsonAsync(ReturnsUrl, await ReturnAsync(receive.Id, item, 3)));
        await ApproveAsync(admin, $"{ReturnsUrl}/{returned.Id}");

        async Task<PurchaseReport> ReportAsync(string view) =>
            (await admin.GetFromJsonAsync<PurchaseReport>($"/api/erp/procurement/purchase-report?view={view}&supplierId={supplier}"))!;
        var byReceive = Assert.Single((await ReportAsync("receive")).Items);
        var bySupplier = Assert.Single((await ReportAsync("supplier")).Items);
        var byItem = await ReportAsync("item");

        Assert.Equal((receive.Code, 1000m, 300m, 700m, 700m), (byReceive.Code, byReceive.NetAmount, byReceive.ReturnAmount, byReceive.PurchaseAmount, byReceive.Due));
        Assert.Equal((supplier, 10m, 3m, 700m), (bySupplier.Id, bySupplier.Quantity, bySupplier.ReturnedQuantity, bySupplier.Due));
        Assert.Equal((item, 700m), (byItem.Items[0].Id, byItem.Items[0].PurchaseAmount));
        Assert.Null(byItem.Totals.Due);
    }

    [Fact]
    public async Task Purchases_approved_before_posting_existed_are_posted_once()
    {
        var admin = await AdminAsync();
        var item = await ItemAsync(factory.DhakaId, 10m);
        var receive = await ApprovedReceiveAsync(factory, admin, item, 5);
        await using (var db = ApiFactory.CreateDbContext())
        {
            await db.InventoryTransactions.AcrossTenants().Where(t => t.DocumentCode == receive.Code).ExecuteDeleteAsync();
            await db.Journals.AcrossTenants().Where(j => j.DocumentCode == receive.Code).ExecuteDeleteAsync();
            await db.StockBalances.AcrossTenants().Where(b => b.ItemId == item).ExecuteDeleteAsync();
        }

        await factory.Services.PostEarlierPurchasesAsync();
        await factory.Services.PostEarlierPurchasesAsync();
        var ledger = await LedgerAsync(admin, item);

        Assert.Equal((1, 5m, 50m), (ledger.Total, ledger.ClosingQuantity, ledger.ClosingValue));
        Assert.Equal(50m, (await JournalAsync(admin, receive.Code)).TotalAmount);
    }

    private async Task<HttpClient> AdminAsync()
    {
        var login = await factory.ErpAdminLoginAsync();
        return factory.CreateClient(login.AccessToken, factory.DhakaId);
    }

    private static async Task ApproveAsync(HttpClient admin, string url) =>
        Assert.Equal(HttpStatusCode.OK, (await admin.PostAsJsonAsync($"{url}/approve", new ReviewRequest(null))).StatusCode);

    private static async Task<StockAdjustmentDetail> ApproveAsync(HttpClient admin, string url, SaveStockAdjustmentRequest request)
    {
        var saved = await ReadAsync<StockAdjustmentDetail>(await admin.PostAsJsonAsync(url, request));
        return await ReadAsync<StockAdjustmentDetail>(await admin.PostAsJsonAsync($"{url}/{saved.Id}/approve", new ReviewRequest(null)));
    }

    private static async Task<StockLedgerReport> LedgerAsync(HttpClient admin, int itemId, Guid? warehouseId = null) =>
        (await admin.GetFromJsonAsync<StockLedgerReport>(
            $"/api/erp/inventory/stock-ledger?itemId={itemId}{(warehouseId is null ? "" : $"&warehouseId={warehouseId}")}"))!;

    private static async Task<JournalDetail> JournalAsync(HttpClient admin, string documentCode)
    {
        var row = Assert.Single((await admin.GetFromJsonAsync<JournalList>($"{JournalsUrl}?search={documentCode}"))!.Items);
        return (await admin.GetFromJsonAsync<JournalDetail>($"{JournalsUrl}/{row.Id}"))!;
    }

    private static async Task<IEnumerable<(string, decimal)>> EntriesAsync(HttpClient admin, string documentCode) =>
        (await JournalAsync(admin, documentCode)).Lines.Select(l => (l.AccountName, l.Debit - l.Credit));

    private static async Task<int> AccountAsync(HttpClient admin, string key) =>
        (await admin.GetFromJsonAsync<List<ChartOfAccountOption>>($"{AccountsUrl}/options"))!.Single(a => a.PostingKeys.Contains(key)).Id;

    private static async Task SetRebatableAsync(int itemId)
    {
        await using var db = ApiFactory.CreateDbContext();
        await db.Items.AcrossTenants().Where(i => i.Id == itemId).ExecuteUpdateAsync(s => s.SetProperty(i => i.IsVatRebatable, true));
    }

    private async Task<Guid> SecondWarehouseAsync()
    {
        var first = await WarehouseAsync(factory.DhakaId);
        await using var db = ApiFactory.CreateDbContext();
        return await db.Warehouses.AcrossTenants()
            .Where(w => w.BusinessUnitId == factory.DhakaId && w.IsActive && w.Id != first)
            .Select(w => w.Id)
            .FirstAsync();
    }

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

    private async Task<SavePurchaseReturnRequest> ReturnAsync(int receiveId, int itemId, decimal quantity) => new()
    {
        Code = null,
        ReturnDate = Today,
        PurchaseReceiveId = receiveId,
        WarehouseId = await WarehouseAsync(factory.DhakaId),
        ReasonId = null,
        IsVatInclusive = false,
        Remarks = null,
        Lines = [new SavePurchaseReturnLine { ItemId = itemId, Quantity = quantity, Price = null, Discount = null, VatPercentage = null, Batch = null }],
    };

    private static SaveJournalRequest Journal(params SaveJournalLine[] lines) => new()
    {
        Code = null,
        JournalDate = Today,
        Narration = "Test journal",
        Lines = lines,
    };

    private static SaveJournalLine Debit(int accountId, decimal amount) =>
        new() { ChartOfAccountId = accountId, SupplierId = null, Debit = amount, Credit = null, Remarks = null };

    private static SaveJournalLine Credit(int accountId, decimal amount) =>
        new() { ChartOfAccountId = accountId, SupplierId = null, Debit = null, Credit = amount, Remarks = null };
}
