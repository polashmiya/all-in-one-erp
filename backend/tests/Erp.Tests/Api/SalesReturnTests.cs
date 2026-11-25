using System.Net;
using System.Net.Http.Json;
using Erp.Api.Approvals;
using Erp.Api.Sales;
using Erp.Api.Sales.SalesCollections;
using Erp.Api.Sales.SalesDeliveries;
using Erp.Api.Sales.SalesReports;
using Erp.Api.Sales.SalesReturns;
using Erp.Tests.Infrastructure;
using static Erp.Tests.Api.ProcurementTestData;
using static Erp.Tests.Api.SalesTestData;

namespace Erp.Tests.Api;

[Collection(ApiCollection.Name)]
public sealed class SalesReturnTests(ApiFactory factory)
{
    [Fact]
    public async Task A_return_puts_stock_back_at_cost_reverses_sales_and_tax_and_reduces_the_due()
    {
        var admin = await SalesAdminAsync(factory, factory.DhakaId);
        var item = await StockedItemAsync(factory, admin, 10, 40m);
        var delivery = await DeliveredAsync(admin, await DeliveryAsync(
            factory.DhakaId, DeliveryLine(item, 4, 100m) with { VatPercentage = 15m, IsAmountWiseDiscount = true, DiscountAmount = 20m }) with
        { SpecialDiscountAmount = 43.7m, OthersCost = 20m });

        var offered = (await OptionsAsync(admin)).Single(d => d.Id == delivery.Id);
        var created = await ReadAsync<SalesReturnDetail>(await admin.PostAsJsonAsync(ReturnsUrl, Return(delivery, 1m) with
        {
            ChargeRefundAmount = 5m,
            DeductionValue = 3m,
            CreditNoteNo = "CN-1",
        }));
        var pending = await admin.GetFromJsonAsync<SalesDeliveryDetail>($"{DeliveriesUrl}/{delivery.Id}");
        var approved = await ApproveAsync<SalesReturnDetail>(admin, ReturnsUrl, created.Id);
        var entries = await EntriesAsync(admin, created.Code);

        Assert.Equal((4m, 413.3m, 20m), (offered.Lines[0].Remaining, offered.Due, offered.ChargeRefundable));
        Assert.StartsWith("SR-", created.Code);
        Assert.Equal((5m, 14.25m, 109.25m), (created.Lines[0].DiscountAmount, created.Lines[0].VatAmount, created.Lines[0].TotalValue));
        Assert.Equal((10.93m, 100.32m, 100.32m, 0m), (created.SpecialDiscountAmount, created.NetAmount, created.DueAdjusted, created.AdvanceAmount));
        Assert.Equal((1m, 100.32m, 312.98m), (pending!.Lines[0].ReturnedQuantity, pending.ReturnAmount, pending.PendingAmount));
        Assert.Equal(40m, approved.TotalCost);
        Assert.Equal(7m, await OnHandAsync(admin, item, delivery.WarehouseId));
        Assert.Equal((95m, 14.25m, 2m), (entries["sales-return"], entries["vat-payable"], entries["other-income"]));
        Assert.Equal((-10.93m, -100.32m), (entries["sales-discount"], entries["accounts-receivable"]));
        Assert.Equal((40m, -40m), (entries["inventory"], entries["cost-of-goods-sold"]));
    }

    [Fact]
    public async Task A_paid_return_is_refunded_or_kept_as_advance_and_damaged_goods_become_a_loss()
    {
        var admin = await SalesAdminAsync(factory, factory.DhakaId);
        var item = await StockedItemAsync(factory, admin, 10, 40m);
        var delivery = await DeliveredAsync(admin, await DeliveryAsync(factory.DhakaId, DeliveryLine(item, 2, 100m)) with { ReceiveAmount = 150m });

        var over = await ErrorsAsync(await admin.PostAsJsonAsync(ReturnsUrl, Return(delivery, 2m) with { RefundAmount = 151m }));
        var created = await ReadAsync<SalesReturnDetail>(await admin.PostAsJsonAsync(
            ReturnsUrl, Return(delivery, 2m, damaged: true) with { RefundAmount = 100m }));
        await ApproveAsync<SalesReturnDetail>(admin, ReturnsUrl, created.Id);
        var entries = await EntriesAsync(admin, created.Code);
        var advance = await BillsAsync(admin, delivery.CustomerId);

        Assert.Equal(["refundAmount"], over);
        Assert.Equal((200m, 50m, 100m, 50m, 80m),
            (created.NetAmount, created.DueAdjusted, created.RefundAmount, created.AdvanceAmount, created.LossAmount));
        Assert.Equal(8m, await OnHandAsync(admin, item, delivery.WarehouseId));
        Assert.Equal((200m, -50m, -100m, -50m),
            (entries["sales-return"], entries["accounts-receivable"], entries["cash"], entries["customer-advance"]));
        Assert.Equal((80m, -80m), (entries["stock-loss"], entries["cost-of-goods-sold"]));
        Assert.Equal((50m, 0), (advance.AvailableAdvance, advance.Bills.Count));
    }

    [Fact]
    public async Task A_return_needs_an_approved_delivery_and_not_more_than_was_delivered()
    {
        var admin = await SalesAdminAsync(factory, factory.DhakaId);
        var item = await StockedItemAsync(factory, admin, 10, 40m);
        var pending = await ReadAsync<SalesDeliveryDetail>(
            await admin.PostAsJsonAsync(DeliveriesUrl, await DeliveryAsync(factory.DhakaId, DeliveryLine(item, 1, 100m))));
        var delivery = await DeliveredAsync(admin, await DeliveryAsync(factory.DhakaId, DeliveryLine(item, 3, 100m)) with { OthersCost = 10m });

        async Task<IEnumerable<string>> Errors(SaveSalesReturnRequest wrong) => await ErrorsAsync(await admin.PostAsJsonAsync(ReturnsUrl, wrong));

        Assert.Equal(["deliveryId"], await Errors(Return(pending, 1m)));
        Assert.Equal(["returnDate"], await Errors(Return(delivery, 1m) with { ReturnDate = Today.AddDays(-1) }));
        Assert.Equal(["chargeRefundAmount"], await Errors(Return(delivery, 1m) with { ChargeRefundAmount = 11m }));
        Assert.Equal(["deductionValue"], await Errors(Return(delivery, 1m) with { DeductionValue = 101m }));
        await ReadAsync<SalesReturnDetail>(await admin.PostAsJsonAsync(ReturnsUrl, Return(delivery, 2m)));
        Assert.Equal(["lines"], await Errors(Return(delivery, 2m)));
        Assert.Equal(1m, (await OptionsAsync(admin)).Single(d => d.Id == delivery.Id).Lines[0].Remaining);
    }

    [Fact]
    public async Task A_cancelled_return_takes_stock_and_accounts_back_and_a_returned_delivery_cannot_be_cancelled()
    {
        var admin = await SalesAdminAsync(factory, factory.DhakaId);
        var item = await StockedItemAsync(factory, admin, 5, 10m);
        var delivery = await DeliveredAsync(admin, await DeliveryAsync(factory.DhakaId, DeliveryLine(item, 2, 100m)));
        var created = await ReadAsync<SalesReturnDetail>(await admin.PostAsJsonAsync(ReturnsUrl, Return(delivery, 2m)));
        await ApproveAsync<SalesReturnDetail>(admin, ReturnsUrl, created.Id);

        var refused = await admin.PostAsJsonAsync($"{DeliveriesUrl}/{delivery.Id}/cancel", new ReviewRequest("No"));
        var cancelled = await ReadAsync<SalesReturnDetail>(
            await admin.PostAsJsonAsync($"{ReturnsUrl}/{created.Id}/cancel", new ReviewRequest("Wrong")));
        var after = await admin.GetFromJsonAsync<SalesDeliveryDetail>($"{DeliveriesUrl}/{delivery.Id}");

        Assert.Equal(HttpStatusCode.Conflict, refused.StatusCode);
        Assert.Equal("cancelled", cancelled.Status);
        Assert.Equal(3m, await OnHandAsync(admin, item, delivery.WarehouseId));
        Assert.All((await EntriesAsync(admin, created.Code)).Values, value => Assert.Equal(0m, value));
        Assert.Equal((0m, 200m), (after!.ReturnAmount, after.PendingAmount));
    }

    [Fact]
    public async Task A_return_without_reference_takes_old_goods_back_and_keeps_the_money_as_advance_or_refund()
    {
        var admin = await SalesAdminAsync(factory, factory.DhakaId);
        var item = await StockedItemAsync(factory, admin, 5, 40m);
        var delivery = await DeliveredAsync(admin, await DeliveryAsync(factory.DhakaId, DeliveryLine(item, 1, 100m)));
        var old = Return(delivery, 1m) with
        {
            Source = "withoutReference",
            DeliveryId = null,
            ReturnRefNo = "OLD-77",
            RefundAmount = 100m,
            Lines =
            [
                ReturnLine(item, 2m) with { Price = 100m, VatPercentage = 15m, IsDiscountPerQuantity = true, DiscountAmount = 10m, CostPrice = 35m },
                ReturnLine(await ItemAsync(factory.DhakaId, 12m), 1m) with { Price = 50m, IsDamaged = true, CostPrice = 12m },
            ],
        };

        var created = await ReadAsync<SalesReturnDetail>(await admin.PostAsJsonAsync(ReturnsUrl, old));
        var edited = await ReadAsync<SalesReturnDetail>(await admin.PutAsJsonAsync($"{ReturnsUrl}/{created.Id}", old with { RefundAmount = 57m }));
        await ApproveAsync<SalesReturnDetail>(admin, ReturnsUrl, created.Id);
        var entries = await EntriesAsync(admin, created.Code);
        var after = await admin.GetFromJsonAsync<SalesDeliveryDetail>($"{DeliveriesUrl}/{delivery.Id}");
        var listed = (await admin.GetFromJsonAsync<SalesReturnList>($"{ReturnsUrl}?source=withoutReference&search={created.Code}"))!;

        Assert.Equal(("withoutReference", null, 257m), (created.Source, created.DeliveryId, created.NetAmount));
        Assert.Equal((0m, 57m, 200m), (edited.DueAdjusted, edited.RefundAmount, edited.AdvanceAmount));
        Assert.Equal((82m, 12m), (edited.TotalCost, edited.LossAmount));
        Assert.Equal(6m, await OnHandAsync(admin, item, delivery.WarehouseId));
        Assert.Equal((230m, 27m, -57m, -200m),
            (entries["sales-return"], entries["vat-payable"], entries["cash"], entries["customer-advance"]));
        Assert.Equal((70m, 12m, -82m), (entries["inventory"], entries["stock-loss"], entries["cost-of-goods-sold"]));
        Assert.Equal((0m, 100m), (after!.ReturnAmount, after.PendingAmount));
        Assert.Equal(200m, (await BillsAsync(admin, delivery.CustomerId)).AvailableAdvance);
        Assert.Equal(created.Code, Assert.Single(listed.Items).Code);

        var report = (await admin.GetFromJsonAsync<SalesReport>($"/api/erp/sales/sales-report?customerId={delivery.CustomerId}"))!;
        Assert.Equal((100m, 257m, -157m), (report.Totals.NetAmount, report.Totals.ReturnAmount, report.Totals.SalesAmount));
        Assert.Equal(created.Id, report.Items.Single(r => r.Code == created.Code).ReturnId);
        var byItem = (await admin.GetFromJsonAsync<SalesReport>($"/api/erp/sales/sales-report?view=item&customerId={delivery.CustomerId}"))!;
        Assert.Equal((2, 257m, 3m), (byItem.Total, byItem.Totals.ReturnAmount, byItem.Totals.ReturnedQuantity));
    }

    [Fact]
    public async Task A_return_names_its_source_and_a_return_without_reference_has_no_delivery()
    {
        var admin = await SalesAdminAsync(factory, factory.DhakaId);
        var item = await StockedItemAsync(factory, admin, 5, 40m);
        var delivery = await DeliveredAsync(admin, await DeliveryAsync(factory.DhakaId, DeliveryLine(item, 1, 100m)));
        var right = Return(delivery, 1m);

        async Task<IEnumerable<string>> Errors(SaveSalesReturnRequest wrong) => await ErrorsAsync(await admin.PostAsJsonAsync(ReturnsUrl, wrong));

        Assert.Equal(["deliveryId"], await Errors(right with { DeliveryId = null }));
        Assert.Equal(["deliveryId"], await Errors(right with { Source = "withoutReference" }));
        Assert.Equal(["source"], await Errors(right with { Source = "other" }));
        Assert.Equal(["customerId"], await Errors(right with { Source = "withoutReference", DeliveryId = null, CustomerId = 0 }));
        Assert.Equal(["lines"], await Errors(right with { Source = "withoutReference", DeliveryId = null, Lines = [ReturnLine(999999999, 1m)] }));
    }

    [Fact]
    public async Task Print_parties_give_the_company_and_the_customer_of_the_unit()
    {
        var admin = await SalesAdminAsync(factory, factory.DhakaId);
        var customerId = await CustomerAsync(factory.DhakaId);

        var parties = (await admin.GetFromJsonAsync<PrintParties>($"/api/erp/sales/print-parties/{customerId}"))!;
        var missing = await admin.GetAsync("/api/erp/sales/print-parties/999999999");

        Assert.NotEmpty(parties.Company.Name);
        Assert.NotEmpty(parties.Company.UnitName);
        Assert.StartsWith("Customer ", parties.Customer.Name);
        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
    }

    private static async Task<List<ReturnDeliveryOption>> OptionsAsync(HttpClient admin) =>
        (await admin.GetFromJsonAsync<List<ReturnDeliveryOption>>($"{ReturnsUrl}/deliveries"))!;

    private static async Task<CustomerReceivable> BillsAsync(HttpClient admin, int customerId) =>
        (await admin.GetFromJsonAsync<CustomerReceivable>($"{CollectionsUrl}/bills?customerId={customerId}"))!;
}
