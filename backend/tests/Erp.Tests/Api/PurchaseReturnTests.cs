using System.Net.Http.Json;
using Erp.Api.Approvals;
using Erp.Api.Procurement.PurchaseOrders;
using Erp.Api.Procurement.PurchaseReceives;
using Erp.Api.Procurement.PurchaseReturns;
using Erp.Tests.Infrastructure;
using static Erp.Tests.Api.ProcurementTestData;

namespace Erp.Tests.Api;

[Collection(ApiCollection.Name)]
public sealed class PurchaseReturnTests(ApiFactory factory)
{
    private const string Url = "/api/erp/procurement/purchase-returns";
    private const string ReceivesUrl = "/api/erp/procurement/purchase-receives";

    [Fact]
    public async Task A_return_prices_its_lines_and_updates_what_the_receive_has_returned()
    {
        var admin = await AdminAsync();
        var item = await ItemAsync(factory.DhakaId, 100m, 10m);
        var receive = await ApprovedReceiveAsync(factory, admin, item, 5);

        var created = await ReadAsync<PurchaseReturnDetail>(await admin.PostAsJsonAsync(Url, await ReturnAsync(receive.Id) with
        {
            Lines = [Line(item, 2)],
        }));
        var afterReceive = await admin.GetFromJsonAsync<PurchaseReceiveDetail>($"{ReceivesUrl}/{receive.Id}");
        var offered = (await admin.GetFromJsonAsync<List<ReturnReceiveOption>>($"{Url}/receives"))!.Single(r => r.Id == receive.Id);

        Assert.StartsWith("RTN-", created.Code);
        Assert.Equal("pending", created.Status);
        Assert.Equal((receive.SupplierId, receive.PurchaseOrderId), (created.SupplierId, created.PurchaseOrderId));
        Assert.Equal((200m, 20m, 220m), (created.Lines[0].TotalAmount, created.Lines[0].VatAmount, created.Lines[0].NetAmount));
        Assert.Equal(220m, created.NetAmount);
        Assert.Equal(2m, Assert.Single(afterReceive!.Lines).ReturnedQuantity);
        Assert.Equal(220m, afterReceive.ReturnAmount);
        Assert.Equal(3m, Assert.Single(offered.Lines).Remaining);
    }

    [Fact]
    public async Task A_return_stays_within_the_receive_and_needs_an_approved_one()
    {
        var admin = await AdminAsync();
        var item = await ItemAsync(factory.DhakaId, 10m);
        var other = await ItemAsync(factory.DhakaId, 10m);
        var receive = await ApprovedReceiveAsync(factory, admin, item, 4);
        var pending = await ReadAsync<PurchaseReceiveDetail>(await admin.PostAsJsonAsync(ReceivesUrl, await ReceiveAsync(factory,
            (await ApprovedOrderAsync(factory, admin, item, 2)).Id, item, 1)));

        var tooMuch = await admin.PostAsJsonAsync(Url, await ReturnAsync(receive.Id) with { Lines = [Line(item, 5)] });
        var notOnReceive = await admin.PostAsJsonAsync(Url, await ReturnAsync(receive.Id) with { Lines = [Line(other, 1)] });
        var fromPending = await admin.PostAsJsonAsync(Url, await ReturnAsync(pending.Id) with { Lines = [Line(item, 1)] });

        Assert.Equal(["lines"], await ErrorsAsync(tooMuch));
        Assert.Equal(["lines"], await ErrorsAsync(notOnReceive));
        Assert.Equal(["purchaseReceiveId"], await ErrorsAsync(fromPending));
    }

    [Fact]
    public async Task A_rejected_return_gives_the_quantity_back()
    {
        var admin = await AdminAsync();
        var item = await ItemAsync(factory.DhakaId, 10m);
        var receive = await ApprovedReceiveAsync(factory, admin, item, 3);

        var created = await ReadAsync<PurchaseReturnDetail>(await admin.PostAsJsonAsync(Url, await ReturnAsync(receive.Id) with { Lines = [Line(item, 3)] }));
        await ReadAsync<PurchaseReturnDetail>(await admin.PostAsJsonAsync($"{Url}/{created.Id}/reject", new ReviewRequest("Supplier refused")));
        var afterReject = await admin.GetFromJsonAsync<PurchaseReceiveDetail>($"{ReceivesUrl}/{receive.Id}");

        Assert.Equal(0m, Assert.Single(afterReject!.Lines).ReturnedQuantity);
        Assert.Equal(0m, afterReject.ReturnAmount);
    }

    private async Task<HttpClient> AdminAsync()
    {
        var login = await factory.ErpAdminLoginAsync();
        return factory.CreateClient(login.AccessToken, factory.DhakaId);
    }

    private async Task<SavePurchaseReturnRequest> ReturnAsync(int receiveId) => new()
    {
        Code = null,
        ReturnDate = Today,
        PurchaseReceiveId = receiveId,
        WarehouseId = await WarehouseAsync(factory.DhakaId),
        ReasonId = null,
        IsVatInclusive = false,
        Remarks = null,
        Lines = [],
    };

    private static SavePurchaseReturnLine Line(int itemId, decimal quantity) => new()
    {
        ItemId = itemId,
        Quantity = quantity,
        Price = null,
        Discount = null,
        VatPercentage = null,
        Batch = null,
    };
}
