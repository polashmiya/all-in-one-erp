using System.Net;
using System.Net.Http.Json;
using Erp.Api.Approvals;
using Erp.Api.Sales.SalesOrders;
using Erp.Api.Sales.SalesQuotations;
using Erp.Tests.Infrastructure;
using static Erp.Tests.Api.ProcurementTestData;
using static Erp.Tests.Api.SalesTestData;

namespace Erp.Tests.Api;

[Collection(ApiCollection.Name)]
public sealed class SalesOrderTests(ApiFactory factory)
{
    [Fact]
    public async Task An_order_gets_the_next_number_prices_its_lines_and_charges()
    {
        var admin = await SalesAdminAsync(factory, factory.DhakaId);
        var (item, free) = (await ItemAsync(factory.DhakaId, 1m), await ItemAsync(factory.DhakaId, 1m));
        var tag = Unique();

        var created = await ReadAsync<SalesOrderDetail>(await admin.PostAsJsonAsync(OrdersUrl, await OrderAsync(
            factory.DhakaId,
            OrderLine(item, 10, 100m) with { VatPercentage = 15m },
            OrderLine(item, 1, 100m) with { IsOfferItem = true },
            OrderLine(free, 2, 50m) with { IsDiscountPerQuantity = true, DiscountAmount = 5m }) with
        {
            CustomerPo = tag,
            OthersCost = 30m,
            ExtraCharge = 20m,
            DeductionValue = 10m,
            SpecialDiscountAmount = 40m,
            AdvanceAmount = 500m,
        }));
        var here = await admin.GetFromJsonAsync<SalesOrderList>($"{OrdersUrl}?search={tag}");
        var elsewhere = await (await SalesAdminAsync(factory, factory.ChattogramId)).GetFromJsonAsync<SalesOrderList>($"{OrdersUrl}?search={tag}");

        Assert.StartsWith("SO-", created.Code);
        Assert.Equal([1150m, 0m, 90m], created.Lines.Select(l => l.TotalValue));
        Assert.Equal((13m, 1100m, 150m, 1240m), (created.TotalQuantity, created.ItemTotalAmount, created.TotalVat, created.NetAmount));
        Assert.Equal((50m, 500m, 740m), (created.NetDiscount, created.ReceiveAmount, created.PendingAmount));
        Assert.Equal(3, Assert.Single(here!.Items).ItemCount);
        Assert.Empty(elsewhere!.Items);
    }

    [Fact]
    public async Task An_order_needs_a_unit_customer_fair_amounts_and_each_item_once()
    {
        var admin = await SalesAdminAsync(factory, factory.DhakaId);
        var item = await ItemAsync(factory.DhakaId, 1m);
        var request = await OrderAsync(factory.DhakaId, OrderLine(item, 1, 100m));

        async Task<IEnumerable<string>> Errors(SaveSalesOrderRequest wrong) => await ErrorsAsync(await admin.PostAsJsonAsync(OrdersUrl, wrong));

        Assert.Equal(["customerId"], await Errors(request with { CustomerId = await CustomerAsync(factory.ChattogramId) }));
        Assert.Equal(["advanceAmount"], await Errors(request with { AdvanceAmount = 101m }));
        Assert.Equal(["specialDiscountAmount"], await Errors(request with { SpecialDiscountAmount = 101m }));
        Assert.Equal(["headerDiscount"], await Errors(request with { IsGrossDiscount = true, HeaderDiscount = 101m }));
        Assert.Equal(["lines"], await Errors(request with { Lines = [OrderLine(item, 1, 1m), OrderLine(item, 2, 1m)] }));
    }

    [Fact]
    public async Task An_approved_quotation_is_ordered_once_and_offered_again_when_the_order_is_rejected()
    {
        var admin = await SalesAdminAsync(factory, factory.DhakaId);
        var item = await ItemAsync(factory.DhakaId, 1m);
        var quotation = await ReadAsync<SalesQuotationDetail>(await admin.PostAsJsonAsync(
            QuotationsUrl, await QuotationAsync(factory.DhakaId) with { Lines = [Line(item, 4, 25m)] }));
        var early = await Options();
        await ApproveAsync<SalesQuotationDetail>(admin, QuotationsUrl, quotation.Id);
        var offered = (await Options()).Single(q => q.Id == quotation.Id);

        var request = await OrderAsync(factory.DhakaId, OrderLine(item, 4, 25m)) with
        {
            CustomerId = quotation.CustomerId,
            SalesQuotationId = quotation.Id,
        };
        var other = await ErrorsAsync(await admin.PostAsJsonAsync(OrdersUrl, request with { CustomerId = await CustomerAsync(factory.DhakaId) }));
        var order = await ReadAsync<SalesOrderDetail>(await admin.PostAsJsonAsync(OrdersUrl, request));
        var again = await ErrorsAsync(await admin.PostAsJsonAsync(OrdersUrl, request));
        var ordered = await admin.GetFromJsonAsync<SalesQuotationDetail>($"{QuotationsUrl}/{quotation.Id}");
        var cancel = await admin.PostAsJsonAsync($"{QuotationsUrl}/{quotation.Id}/cancel", new ReviewRequest("Wrong"));
        await ReadAsync<SalesOrderDetail>(await admin.PostAsJsonAsync($"{OrdersUrl}/{order.Id}/reject", new ReviewRequest("No")));
        var released = await Options();

        Assert.DoesNotContain(early, q => q.Id == quotation.Id);
        Assert.Equal((item, 4m, 25m), (offered.Lines[0].ItemId, offered.Lines[0].Quantity, offered.Lines[0].Price));
        Assert.Equal(["customerId"], other);
        Assert.Equal($"{quotation.Code} v1", order.SalesQuotationName);
        Assert.Equal(["salesQuotationId"], again);
        Assert.True(ordered!.IsOrdered);
        Assert.False(ordered.CanRevise);
        Assert.Equal(HttpStatusCode.Conflict, cancel.StatusCode);
        Assert.Contains(released, q => q.Id == quotation.Id);

        async Task<List<OrderQuotationOption>> Options() =>
            (await admin.GetFromJsonAsync<List<OrderQuotationOption>>($"{OrdersUrl}/quotations"))!;
    }

    [Fact]
    public async Task The_advance_is_posted_on_approval_and_reversed_on_cancel()
    {
        var admin = await SalesAdminAsync(factory, factory.DhakaId);
        var item = await ItemAsync(factory.DhakaId, 1m);
        var order = await ReadAsync<SalesOrderDetail>(await admin.PostAsJsonAsync(
            OrdersUrl, await OrderAsync(factory.DhakaId, OrderLine(item, 1, 100m)) with { AdvanceAmount = 60m }));

        var approved = await ApproveAsync<SalesOrderDetail>(admin, OrdersUrl, order.Id);
        var posted = await EntriesAsync(admin, order.Code);
        var edit = await admin.PutAsJsonAsync($"{OrdersUrl}/{order.Id}", await OrderAsync(factory.DhakaId, OrderLine(item, 1, 90m)));
        var cancelled = await ReadAsync<SalesOrderDetail>(
            await admin.PostAsJsonAsync($"{OrdersUrl}/{order.Id}/cancel", new ReviewRequest("Customer left")));
        var reversed = await EntriesAsync(admin, order.Code);

        Assert.Equal(("approved", 60m), (approved.Status, approved.AdvanceRemainingAmount));
        Assert.Equal((60m, -60m), (posted["cash"], posted["customer-advance"]));
        Assert.Equal(HttpStatusCode.Conflict, edit.StatusCode);
        Assert.Equal("cancelled", cancelled.Status);
        Assert.All(reversed.Values, value => Assert.Equal(0m, value));
    }
}
