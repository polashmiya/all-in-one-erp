using System.Net;
using System.Net.Http.Json;
using Erp.Api.Accounting.AccountingReports;
using Erp.Api.Approvals;
using Erp.Api.Sales.SalesCollections;
using Erp.Api.Sales.SalesDeliveries;
using Erp.Api.Sales.SalesOrders;
using Erp.Api.Sales.SalesReports;
using Erp.Api.Sales.SalesReturns;
using Erp.Tests.Infrastructure;
using static Erp.Tests.Api.ProcurementTestData;
using static Erp.Tests.Api.SalesTestData;

namespace Erp.Tests.Api;

[Collection(ApiCollection.Name)]
public sealed class SalesCollectionTests(ApiFactory factory)
{
    [Fact]
    public async Task A_collection_settles_bills_with_deductions_and_posts_them()
    {
        var admin = await SalesAdminAsync(factory, factory.DhakaId);
        var item = await StockedItemAsync(factory, admin, 10, 40m);
        var first = await DeliveredAsync(admin, await DeliveryAsync(factory.DhakaId, DeliveryLine(item, 2, 100m)));
        var second = await DeliveredAsync(
            admin, await DeliveryAsync(factory.DhakaId, DeliveryLine(item, 1, 100m)) with { CustomerId = first.CustomerId });

        var before = await BillsAsync(admin, first.CustomerId);
        var created = await ReadAsync<SalesCollectionDetail>(await admin.PostAsJsonAsync(CollectionsUrl, Collection(
            first.CustomerId,
            Bill(first.Id, 150m) with { Vds = 10m, Ait = 5m, DiscountAmount = 20m, ExpenseAmount = 5m },
            Bill(second.Id, 40m))));
        var after = await BillsAsync(admin, first.CustomerId);
        await ApproveAsync<SalesCollectionDetail>(admin, CollectionsUrl, created.Id);
        var entries = await EntriesAsync(admin, created.Code);
        var delivery = await admin.GetFromJsonAsync<SalesDeliveryDetail>($"{DeliveriesUrl}/{first.Id}");

        Assert.Equal([(first.Id, 200m), (second.Id, 100m)], before.Bills.Select(b => (b.Id, b.Due)));
        Assert.StartsWith("SC-", created.Code);
        Assert.Equal((190m, 230m), (created.CollectionAmount, created.SettledAmount));
        Assert.Equal([(first.Id, 10m), (second.Id, 60m)], after.Bills.Select(b => (b.Id, b.Due)));
        Assert.Equal((190m, 10m, 5m, 20m, 5m, -230m),
            (entries["cash"], entries["vds-receivable"], entries["ait-receivable"], entries["sales-discount"], entries["bank-charge"],
                entries["accounts-receivable"]));
        Assert.Equal((190m, 10m), (delivery!.CollectedAmount, delivery.PendingAmount));
        Assert.Equal(created.Code, Assert.Single(delivery.FollowUps).Code);
    }

    [Fact]
    public async Task An_advance_is_collected_used_on_a_bill_and_cannot_be_cancelled_while_used()
    {
        var admin = await SalesAdminAsync(factory, factory.DhakaId);
        var item = await StockedItemAsync(factory, admin, 10, 40m);
        var delivery = await DeliveredAsync(admin, await DeliveryAsync(factory.DhakaId, DeliveryLine(item, 1, 100m)));
        var advance = await ReadAsync<SalesCollectionDetail>(await admin.PostAsJsonAsync(
            CollectionsUrl, Collection(delivery.CustomerId) with { IsAdvance = true, AdvanceAmount = 70m }));
        var early = await BillsAsync(admin, delivery.CustomerId);
        await ApproveAsync<SalesCollectionDetail>(admin, CollectionsUrl, advance.Id);
        var ready = await BillsAsync(admin, delivery.CustomerId);

        var tooMuch = await ErrorsAsync(await admin.PostAsJsonAsync(
            CollectionsUrl, Collection(delivery.CustomerId, Bill(delivery.Id, 0m) with { AdvanceAdjusted = 71m })));
        var used = await ReadAsync<SalesCollectionDetail>(await admin.PostAsJsonAsync(
            CollectionsUrl, Collection(delivery.CustomerId, Bill(delivery.Id, 30m) with { AdvanceAdjusted = 70m })));
        await ApproveAsync<SalesCollectionDetail>(admin, CollectionsUrl, used.Id);
        var entries = await EntriesAsync(admin, used.Code);
        var refused = await admin.PostAsJsonAsync($"{CollectionsUrl}/{advance.Id}/cancel", new ReviewRequest("No"));
        await ReadAsync<SalesCollectionDetail>(await admin.PostAsJsonAsync($"{CollectionsUrl}/{used.Id}/cancel", new ReviewRequest("Bounced")));
        var back = await BillsAsync(admin, delivery.CustomerId);

        Assert.Equal((0m, 70m), (early.AvailableAdvance, ready.AvailableAdvance));
        Assert.Equal((70m, -70m), ((await EntriesAsync(admin, advance.Code))["cash"], (await EntriesAsync(admin, advance.Code))["customer-advance"]));
        Assert.Equal(["lines"], tooMuch);
        Assert.Equal((30m, 70m, -100m), (entries["cash"], entries["customer-advance"], entries["accounts-receivable"]));
        Assert.Equal(HttpStatusCode.Conflict, refused.StatusCode);
        Assert.Equal((70m, 100m), (back.AvailableAdvance, back.Bills[0].Due));
    }

    [Fact]
    public async Task A_collection_stays_within_the_due_and_needs_bills_of_the_customer()
    {
        var admin = await SalesAdminAsync(factory, factory.DhakaId);
        var item = await StockedItemAsync(factory, admin, 10, 40m);
        var delivery = await DeliveredAsync(admin, await DeliveryAsync(factory.DhakaId, DeliveryLine(item, 1, 100m)));
        var other = await DeliveredAsync(admin, await DeliveryAsync(factory.DhakaId, DeliveryLine(item, 1, 100m)));

        async Task<IEnumerable<string>> Errors(SaveSalesCollectionRequest wrong) =>
            await ErrorsAsync(await admin.PostAsJsonAsync(CollectionsUrl, wrong));

        Assert.Equal(["lines"], await Errors(Collection(delivery.CustomerId, Bill(delivery.Id, 90m) with { Vds = 11m })));
        Assert.Equal(["lines"], await Errors(Collection(delivery.CustomerId, Bill(other.Id, 10m))));
        Assert.Equal(["lines"], await Errors(Collection(delivery.CustomerId, Bill(delivery.Id, 0m))));
        Assert.Equal(["lines"], await Errors(Collection(delivery.CustomerId, Bill(delivery.Id, 1m), Bill(delivery.Id, 1m))));
    }

    [Fact]
    public async Task The_advance_left_on_a_closed_order_can_be_used()
    {
        var admin = await SalesAdminAsync(factory, factory.DhakaId);
        var item = await StockedItemAsync(factory, admin, 10, 40m);
        var order = await ReadAsync<SalesOrderDetail>(await admin.PostAsJsonAsync(
            OrdersUrl, await OrderAsync(factory.DhakaId, OrderLine(item, 1, 100m)) with { AdvanceAmount = 25m }));
        await ApproveAsync<SalesOrderDetail>(admin, OrdersUrl, order.Id);
        var open = await BillsAsync(admin, order.CustomerId);
        await ReadAsync<SalesOrderDetail>(await admin.PostAsync($"{OrdersUrl}/{order.Id}/close", null));
        var closed = await BillsAsync(admin, order.CustomerId);

        Assert.Equal((0m, 25m), (open.AvailableAdvance, closed.AvailableAdvance));
    }

    [Fact]
    public async Task The_reports_show_sales_returns_profit_collections_due_and_the_customer_ledger()
    {
        var admin = await SalesAdminAsync(factory, factory.DhakaId);
        var item = await StockedItemAsync(factory, admin, 10, 40m);
        var delivery = await DeliveredAsync(
            admin, await DeliveryAsync(factory.DhakaId, DeliveryLine(item, 5, 100m) with { VatPercentage = 10m }) with { PaymentTermDays = 7 });
        var returned = await ReadAsync<SalesReturnDetail>(await admin.PostAsJsonAsync(ReturnsUrl, Return(delivery, 1m)));
        await ApproveAsync<SalesReturnDetail>(admin, ReturnsUrl, returned.Id);
        var collected = await ReadAsync<SalesCollectionDetail>(
            await admin.PostAsJsonAsync(CollectionsUrl, Collection(delivery.CustomerId, Bill(delivery.Id, 300m))));
        await ApproveAsync<SalesCollectionDetail>(admin, CollectionsUrl, collected.Id);
        var filter = $"customerId={delivery.CustomerId}";

        foreach (var view in (string[])["delivery", "item", "category", "customer", "salesForce", "month"])
        {
            var report = (await admin.GetFromJsonAsync<SalesReport>($"/api/erp/sales/sales-report?view={view}&{filter}"))!;
            var row = Assert.Single(report.Items);
            Assert.Equal((5m, 1m, 550m, 110m, 440m, 160m, 240m),
                (row.Quantity, row.ReturnedQuantity, row.NetAmount, row.ReturnAmount, row.SalesAmount, row.Cost, row.Profit));
            Assert.Equal(view is "item" or "category" ? null : 140m, row.Due);
            Assert.Equal(240m, report.Totals.Profit);
        }

        foreach (var view in (string[])["collection", "customer", "method", "collector", "date"])
        {
            var report = (await admin.GetFromJsonAsync<CollectionReport>($"/api/erp/sales/collection-report?view={view}&{filter}"))!;
            Assert.Equal((300m, 300m), (Assert.Single(report.Items).Collected, report.Totals.Settled));
        }

        foreach (var view in (string[])["customer", "bill"])
        {
            var due = (await admin.GetFromJsonAsync<CustomerDueReport>($"/api/erp/sales/customer-due?view={view}&{filter}"))!;
            var row = Assert.Single(due.Items);
            Assert.Equal((550m, 110m, 300m, 140m, 140m), (row.BillAmount, row.ReturnAmount, row.Received, row.Due, row.NotDue));
        }

        var ledger = (await admin.GetFromJsonAsync<LedgerReport>($"/api/erp/accounting/reports/customer-ledger?{filter}"))!;
        Assert.Equal(3, ledger.Items.Count);
        Assert.Equal(140m, ledger.Items[^1].Balance);
    }

    private static async Task<CustomerReceivable> BillsAsync(HttpClient admin, int customerId) =>
        (await admin.GetFromJsonAsync<CustomerReceivable>($"{CollectionsUrl}/bills?customerId={customerId}"))!;
}
