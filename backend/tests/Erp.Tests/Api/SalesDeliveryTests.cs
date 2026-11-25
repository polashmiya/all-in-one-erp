using System.Net;
using System.Net.Http.Json;
using Erp.Api.Accounting.ChartOfAccounts;
using Erp.Api.Accounting.Journals;
using Erp.Api.Approvals;
using Erp.Api.Inventory;
using Erp.Api.Sales.SalesDeliveries;
using Erp.Api.Sales.SalesOrders;
using Erp.Tests.Infrastructure;
using static Erp.Tests.Api.ProcurementTestData;
using static Erp.Tests.Api.SalesTestData;

namespace Erp.Tests.Api;

[Collection(ApiCollection.Name)]
public sealed class SalesDeliveryTests(ApiFactory factory)
{
    [Fact]
    public async Task A_direct_delivery_takes_stock_out_and_posts_sales_tax_cost_and_receivable()
    {
        var admin = await SalesAdminAsync(factory, factory.DhakaId);
        var item = await StockedItemAsync(factory, admin, 10, 40m);
        var request = await DeliveryAsync(
            factory.DhakaId,
            DeliveryLine(item, 2, 100m) with { SdPercentage = 10m, VatPercentage = 15m, WarrantyInMonth = 6, Batch = "B1" },
            DeliveryLine(item, 1, 100m) with { IsOfferItem = true }) with
        {
            OthersCost = 20m,
            DeductionValue = 3m,
            SpecialDiscountAmount = 7m,
            ReceiveAmount = 100m,
            TotalNoOfInstallment = 2,
            InterestRate = 10m,
        };

        var created = await ReadAsync<SalesDeliveryDetail>(await admin.PostAsJsonAsync(DeliveriesUrl, request));
        var before = await OnHandAsync(admin, item, request.WarehouseId);
        var approved = await ApproveAsync<SalesDeliveryDetail>(admin, DeliveriesUrl, created.Id);
        var entries = await EntriesAsync(admin, created.Code);

        Assert.StartsWith("SD-", created.Code);
        Assert.Equal(("direct", 263m, 163m), (created.Type, created.NetAmount, created.PendingAmount));
        Assert.Equal((179.3m, 89.65m), (created.NetAmountWithInterest, created.AmountPerInstallment));
        Assert.Equal((Today.AddMonths(6), "B1"), (created.Lines[0].WarrantyExpiredDate, created.Lines[0].Batch));
        Assert.Equal(10m, before);
        Assert.Equal(7m, await OnHandAsync(admin, item, request.WarehouseId));
        Assert.Equal((120m, 80m, 40m), (approved.TotalCost, approved.Lines[0].CostTotal, approved.Lines[1].CostTotal));
        Assert.Equal((163m, 100m, 10m), (entries["accounts-receivable"], entries["cash"], entries["sales-discount"]));
        Assert.Equal((-200m, -20m, -33m, -20m), (entries["sales"], entries["sd-payable"], entries["vat-payable"], entries["other-income"]));
        Assert.Equal((120m, -120m), (entries["cost-of-goods-sold"], entries["inventory"]));
    }

    [Fact]
    public async Task An_order_is_delivered_in_parts_with_its_prices_and_advance()
    {
        var admin = await SalesAdminAsync(factory, factory.DhakaId);
        var item = await StockedItemAsync(factory, admin, 20, 10m);
        var order = await ApprovedOrderAsync(admin, await OrderAsync(
            factory.DhakaId, OrderLine(item, 3, 100m) with { IsAmountWiseDiscount = true, DiscountAmount = 10m }) with
        { AdvanceAmount = 120m, OthersCost = 15m });

        var offered = await OptionAsync(admin, order.Id);
        var first = await ReadAsync<SalesDeliveryDetail>(await admin.PostAsJsonAsync(
            DeliveriesUrl, await DeliveryOfAsync(factory.DhakaId, order, 1) with { AdvanceBalanceAdjust = 80m, OthersCost = 15m }));
        var scheduled = await admin.GetFromJsonAsync<SalesOrderDetail>($"{OrdersUrl}/{order.Id}");
        var closing = await admin.PostAsync($"{OrdersUrl}/{order.Id}/close", null);
        await ApproveAsync<SalesDeliveryDetail>(admin, DeliveriesUrl, first.Id);
        var entries = await EntriesAsync(admin, first.Code);
        var partly = await OptionAsync(admin, order.Id);

        var rest = await DeliveryOfAsync(factory.DhakaId, order, 2);
        var tooMuch = await ErrorsAsync(await admin.PostAsJsonAsync(DeliveriesUrl, await DeliveryOfAsync(factory.DhakaId, order, 3)));
        var overAdvance = await ErrorsAsync(await admin.PostAsJsonAsync(DeliveriesUrl, rest with { AdvanceBalanceAdjust = 41m }));
        var second = await ReadAsync<SalesDeliveryDetail>(await admin.PostAsJsonAsync(DeliveriesUrl, rest with { AdvanceBalanceAdjust = 40m }));
        await ApproveAsync<SalesDeliveryDetail>(admin, DeliveriesUrl, second.Id);
        var done = await admin.GetFromJsonAsync<SalesOrderDetail>($"{OrdersUrl}/{order.Id}");
        var cancelOrder = await admin.PostAsJsonAsync($"{OrdersUrl}/{order.Id}/cancel", new ReviewRequest("No"));

        Assert.Equal((3m, 120m, 15m), (offered!.Lines[0].Remaining, offered.AdvanceAvailable, offered.OthersCost));
        Assert.Equal((order.CustomerId, order.Code), (first.CustomerId, first.OrderName));
        Assert.Equal((3.33m, 111.67m, 31.67m), (first.Lines[0].DiscountAmount, first.NetAmount, first.PendingAmount));
        Assert.Equal((1m, 0m), (scheduled!.Lines[0].ScheduledQuantity, scheduled.Lines[0].DeliveredQuantity));
        Assert.Equal(HttpStatusCode.Conflict, closing.StatusCode);
        Assert.Equal((80m, 31.67m, -96.67m), (entries["customer-advance"], entries["accounts-receivable"], entries["sales"]));
        Assert.Equal((2m, 40m, 0m), (partly!.Lines[0].Remaining, partly.AdvanceAvailable, partly.OthersCost));
        Assert.Equal(["lines"], tooMuch);
        Assert.Equal(["advanceBalanceAdjust"], overAdvance);
        Assert.Equal(6.67m, second.Lines[0].DiscountAmount);
        Assert.Equal((3m, true, 0m, 2), (done!.DeliveredQuantity, done.IsCompleted, done.AdvanceRemainingAmount, done.Deliveries.Count));
        Assert.Null(await OptionAsync(admin, order.Id));
        Assert.Equal(HttpStatusCode.Conflict, cancelOrder.StatusCode);
    }

    [Fact]
    public async Task A_delivery_needs_stock_an_approved_order_and_fair_amounts()
    {
        var admin = await SalesAdminAsync(factory, factory.DhakaId);
        var item = await StockedItemAsync(factory, admin, 2, 10m);
        var pending = await ReadAsync<SalesOrderDetail>(
            await admin.PostAsJsonAsync(OrdersUrl, await OrderAsync(factory.DhakaId, OrderLine(item, 1, 100m))));
        var request = await DeliveryAsync(factory.DhakaId, DeliveryLine(item, 2, 100m));

        async Task<IEnumerable<string>> Errors(SaveSalesDeliveryRequest wrong) =>
            await ErrorsAsync(await admin.PostAsJsonAsync(DeliveriesUrl, wrong));

        Assert.Equal(["lines"], await Errors(request with { Lines = [DeliveryLine(item, 3, 100m)] }));
        Assert.Equal(["orderId"], await Errors(await DeliveryOfAsync(factory.DhakaId, pending, 1)));
        Assert.Equal(["receiveAmount"], await Errors(request with { ReceiveAmount = 201m }));
        Assert.Equal(["advanceBalanceAdjust"], await Errors(request with { AdvanceBalanceAdjust = 1m }));
    }

    [Fact]
    public async Task A_cancelled_delivery_gives_stock_accounts_and_the_order_back()
    {
        var admin = await SalesAdminAsync(factory, factory.DhakaId);
        var item = await StockedItemAsync(factory, admin, 5, 10m);
        var order = await ApprovedOrderAsync(admin, await OrderAsync(factory.DhakaId, OrderLine(item, 2, 100m)));
        var delivery = await ReadAsync<SalesDeliveryDetail>(
            await admin.PostAsJsonAsync(DeliveriesUrl, await DeliveryOfAsync(factory.DhakaId, order, 2)));
        await ApproveAsync<SalesDeliveryDetail>(admin, DeliveriesUrl, delivery.Id);

        var cancelled = await ReadAsync<SalesDeliveryDetail>(
            await admin.PostAsJsonAsync($"{DeliveriesUrl}/{delivery.Id}/cancel", new ReviewRequest("Came back")));
        var open = await admin.GetFromJsonAsync<SalesOrderDetail>($"{OrdersUrl}/{order.Id}");

        Assert.Equal("cancelled", cancelled.Status);
        Assert.Equal(5m, await OnHandAsync(admin, item, delivery.WarehouseId));
        Assert.All((await EntriesAsync(admin, delivery.Code)).Values, value => Assert.Equal(0m, value));
        Assert.Equal((0m, false), (open!.DeliveredQuantity, open.IsCompleted));
        Assert.NotNull(await OptionAsync(admin, order.Id));
    }

    [Fact]
    public async Task An_approved_delivery_is_confirmed_once_and_then_stays()
    {
        var admin = await SalesAdminAsync(factory, factory.DhakaId);
        var item = await StockedItemAsync(factory, admin, 5, 10m);
        var delivery = await ReadAsync<SalesDeliveryDetail>(
            await admin.PostAsJsonAsync(DeliveriesUrl, await DeliveryAsync(factory.DhakaId, DeliveryLine(item, 1, 100m))));
        var request = new ConfirmDeliveryRequest
        {
            ActualDeliveryDate = Today,
            CollectedById = null,
            GrnNo = "GRN-9",
            GrnDate = Today,
            GrnAmount = 100m,
            GrnCollectionDate = null,
            Remarks = "Received in full",
        };

        var early = await admin.PostAsJsonAsync($"{DeliveriesUrl}/{delivery.Id}/confirm", request);
        await ApproveAsync<SalesDeliveryDetail>(admin, DeliveriesUrl, delivery.Id);
        var confirmed = await ReadAsync<SalesDeliveryDetail>(await admin.PostAsJsonAsync($"{DeliveriesUrl}/{delivery.Id}/confirm", request));
        var again = await admin.PostAsJsonAsync($"{DeliveriesUrl}/{delivery.Id}/confirm", request);
        var cancel = await admin.PostAsJsonAsync($"{DeliveriesUrl}/{delivery.Id}/cancel", new ReviewRequest("No"));

        Assert.Equal(HttpStatusCode.Conflict, early.StatusCode);
        Assert.Equal((true, "GRN-9", Today), (confirmed.IsConfirmed, confirmed.GrnNo, confirmed.ActualDeliveryDate));
        Assert.Equal(HttpStatusCode.Conflict, again.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, cancel.StatusCode);
    }

    [Fact]
    public async Task A_manual_journal_on_receivable_needs_a_customer()
    {
        var admin = await SalesAdminAsync(factory, factory.DhakaId);
        var accounts = (await admin.GetFromJsonAsync<List<ChartOfAccountOption>>("/api/erp/accounting/chart-of-accounts/options"))!;
        int Account(string key) => accounts.Single(a => a.PostingKeys.Contains(key)).Id;
        SaveJournalRequest Request(int? customerId) => new()
        {
            Code = null,
            JournalDate = Today,
            Narration = "Opening due",
            Lines =
            [
                new SaveJournalLine { ChartOfAccountId = Account("accounts-receivable"), SupplierId = null, CustomerId = customerId, Debit = 5m, Credit = null, Remarks = null },
                new SaveJournalLine { ChartOfAccountId = Account("opening-balance"), SupplierId = null, Debit = null, Credit = 5m, Remarks = null },
            ],
        };

        var without = await ErrorsAsync(await admin.PostAsJsonAsync("/api/erp/accounting/journals", Request(null)));
        var saved = await ReadAsync<JournalDetail>(
            await admin.PostAsJsonAsync("/api/erp/accounting/journals", Request(await CustomerAsync(factory.DhakaId))));

        Assert.Equal(["lines"], without);
        Assert.StartsWith("Customer ", saved.Lines[0].CustomerName);
    }

    private static async Task<SalesOrderDetail> ApprovedOrderAsync(HttpClient admin, SaveSalesOrderRequest request)
    {
        var order = await ReadAsync<SalesOrderDetail>(await admin.PostAsJsonAsync(OrdersUrl, request));
        return await ApproveAsync<SalesOrderDetail>(admin, OrdersUrl, order.Id);
    }

    private static async Task<DeliveryOrderOption?> OptionAsync(HttpClient admin, int orderId) =>
        (await admin.GetFromJsonAsync<List<DeliveryOrderOption>>($"{DeliveriesUrl}/orders"))!.Find(o => o.Id == orderId);

    private static async Task<decimal> OnHandAsync(HttpClient admin, int itemId, Guid warehouseId) =>
        (await admin.GetFromJsonAsync<List<StockOnHand>>($"/api/erp/inventory/on-hand?warehouseId={warehouseId}&itemIds={itemId}"))!
            .Single().Quantity;
}
