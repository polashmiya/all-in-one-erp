using System.Net.Http.Json;
using Erp.Api.Approvals;
using Erp.Api.Procurement.PurchaseOrders;
using Erp.Api.Procurement.PurchaseReceives;
using Erp.Tests.Infrastructure;
using static Erp.Tests.Api.ProcurementTestData;

namespace Erp.Tests.Api;

[Collection(ApiCollection.Name)]
public sealed class PurchaseReceiveTests(ApiFactory factory)
{
    private const string Url = "/api/erp/procurement/purchase-receives";
    private const string OrdersUrl = "/api/erp/procurement/purchase-orders";

    [Fact]
    public async Task A_receive_prices_its_lines_and_updates_what_the_order_has_received()
    {
        var admin = await AdminAsync();
        var item = await ItemAsync(factory.DhakaId, 100m, 10m);
        var order = await ApprovedOrderAsync(admin, await SupplierAsync(factory.DhakaId), item, 5);

        var created = await ReadAsync<PurchaseReceiveDetail>(await admin.PostAsJsonAsync(Url, await ReceiveAsync(order.Id) with
        {
            ChallanNo = "CH-1",
            DeliveryCharge = 20m,
            Lines = [Line(item, 3) with { Batch = "B-01" }],
        }));
        var afterOrder = await admin.GetFromJsonAsync<PurchaseOrderDetail>($"{OrdersUrl}/{order.Id}");
        var offered = (await admin.GetFromJsonAsync<List<ReceiveOrderOption>>($"{Url}/orders"))!.Single(o => o.Id == order.Id);

        Assert.StartsWith("GRN-", created.Code);
        Assert.Equal("pending", created.Status);
        Assert.Equal(order.SupplierId, created.SupplierId);
        Assert.Equal((300m, 30m, 330m, "B-01"), (created.Lines[0].TotalAmount, created.Lines[0].VatAmount, created.Lines[0].NetAmount,
            created.Lines[0].Batch));
        Assert.Equal(350m, created.NetAmount);
        Assert.Equal(3m, Assert.Single(afterOrder!.Lines).ReceivedQuantity);
        Assert.Equal(3m, afterOrder.ReceivedQuantity);
        Assert.Equal(2m, Assert.Single(offered.Lines).Remaining);
    }

    [Fact]
    public async Task A_receive_stays_within_the_order_and_the_supplier_tolerance()
    {
        var admin = await AdminAsync();
        var item = await ItemAsync(factory.DhakaId, 10m);
        var other = await ItemAsync(factory.DhakaId, 10m);
        var order = await ApprovedOrderAsync(admin, await SupplierAsync(factory.DhakaId, 10m), item, 10);

        var tooMuch = await admin.PostAsJsonAsync(Url, await ReceiveAsync(order.Id) with { Lines = [Line(item, 12)] });
        var notOnOrder = await admin.PostAsJsonAsync(Url, await ReceiveAsync(order.Id) with { Lines = [Line(other, 1)] });
        var withTolerance = await admin.PostAsJsonAsync(Url, await ReceiveAsync(order.Id) with { Lines = [Line(item, 11)] });
        var nothingLeft = await admin.PostAsJsonAsync(Url, await ReceiveAsync(order.Id) with { Lines = [Line(item, 1)] });

        Assert.Equal(["lines"], await ErrorsAsync(tooMuch));
        Assert.Equal(["lines"], await ErrorsAsync(notOnOrder));
        Assert.Equal(11m, (await ReadAsync<PurchaseReceiveDetail>(withTolerance)).TotalQuantity);
        Assert.Equal(["lines"], await ErrorsAsync(nothingLeft));
    }

    [Fact]
    public async Task A_rejected_or_deleted_receive_gives_the_quantity_back_and_a_pending_order_cannot_be_received()
    {
        var admin = await AdminAsync();
        var item = await ItemAsync(factory.DhakaId, 10m);
        var supplier = await SupplierAsync(factory.DhakaId);
        var order = await ApprovedOrderAsync(admin, supplier, item, 4);
        var pending = await ReadAsync<PurchaseOrderDetail>(await admin.PostAsJsonAsync(OrdersUrl, await OrderAsync(supplier, item, 1)));

        var receive = await ReadAsync<PurchaseReceiveDetail>(await admin.PostAsJsonAsync(Url, await ReceiveAsync(order.Id) with { Lines = [Line(item, 4)] }));
        await ReadAsync<PurchaseReceiveDetail>(await admin.PostAsJsonAsync($"{Url}/{receive.Id}/reject", new ReviewRequest("Damaged")));
        var afterReject = await admin.GetFromJsonAsync<PurchaseOrderDetail>($"{OrdersUrl}/{order.Id}");
        var again = await ReadAsync<PurchaseReceiveDetail>(await admin.PostAsJsonAsync(Url, await ReceiveAsync(order.Id) with { Lines = [Line(item, 4)] }));
        await admin.DeleteAsync($"{Url}/{again.Id}");
        var afterDelete = await admin.GetFromJsonAsync<PurchaseOrderDetail>($"{OrdersUrl}/{order.Id}");
        var fromPending = await admin.PostAsJsonAsync(Url, await ReceiveAsync(pending.Id) with { Lines = [Line(item, 1)] });

        Assert.Equal(0m, afterReject!.ReceivedQuantity);
        Assert.Equal(0m, afterDelete!.ReceivedQuantity);
        Assert.Equal(["purchaseOrderId"], await ErrorsAsync(fromPending));
    }

    private async Task<HttpClient> AdminAsync()
    {
        var login = await factory.ErpAdminLoginAsync();
        return factory.CreateClient(login.AccessToken, factory.DhakaId);
    }

    private async Task<PurchaseOrderDetail> ApprovedOrderAsync(HttpClient admin, int supplierId, int itemId, decimal quantity)
    {
        var order = await ReadAsync<PurchaseOrderDetail>(await admin.PostAsJsonAsync(OrdersUrl, await OrderAsync(supplierId, itemId, quantity)));
        return await ReadAsync<PurchaseOrderDetail>(await admin.PostAsJsonAsync($"{OrdersUrl}/{order.Id}/approve", new ReviewRequest(null)));
    }

    private async Task<SavePurchaseOrderRequest> OrderAsync(int supplierId, int itemId, decimal quantity) => new()
    {
        Code = null,
        OrderDate = Today,
        SupplierId = supplierId,
        WarehouseId = await WarehouseAsync(factory.DhakaId),
        RequisitionId = null,
        ReferenceNo = null,
        ExpectedReceiveDate = null,
        PaymentTypeId = null,
        PaymentDays = null,
        IsVatInclusive = false,
        ItemTypeId = null,
        ShippingAddress = null,
        HeaderDiscount = null,
        DeliveryCharge = null,
        LaborCharge = null,
        OtherCharge = null,
        Remarks = null,
        Lines = [new SavePurchaseOrderLine
        {
            ItemId = itemId,
            Quantity = quantity,
            Price = null,
            Discount = null,
            VatPercentage = null,
            Specification = null,
        }],
    };

    private async Task<SavePurchaseReceiveRequest> ReceiveAsync(int orderId) => new()
    {
        Code = null,
        ReceiveDate = Today,
        PurchaseOrderId = orderId,
        WarehouseId = await WarehouseAsync(factory.DhakaId),
        ChallanNo = null,
        ChallanDate = null,
        VatChallanNo = null,
        PaymentTypeId = null,
        PaymentDays = null,
        IsVatInclusive = false,
        HeaderDiscount = null,
        DeliveryCharge = null,
        LaborCharge = null,
        OtherCharge = null,
        Remarks = null,
        Lines = [],
    };

    private static SavePurchaseReceiveLine Line(int itemId, decimal quantity) => new()
    {
        ItemId = itemId,
        Quantity = quantity,
        Price = null,
        Discount = null,
        VatPercentage = null,
        Batch = null,
    };
}
