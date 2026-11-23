using System.Net;
using System.Net.Http.Json;
using Erp.Api.Approvals;
using Erp.Api.Procurement;
using Erp.Api.Procurement.PurchaseOrders;
using Erp.Api.Procurement.PurchaseRequisitions;
using Erp.Tests.Infrastructure;
using static Erp.Tests.Api.ProcurementTestData;

namespace Erp.Tests.Api;

[Collection(ApiCollection.Name)]
public sealed class PurchaseOrderTests(ApiFactory factory)
{
    private const string Url = "/api/erp/procurement/purchase-orders";
    private const string RequisitionsUrl = "/api/erp/procurement/purchase-requisitions";

    [Fact]
    public async Task An_order_gets_the_next_number_prices_its_lines_and_shows_only_in_its_unit()
    {
        var admin = await AdminAsync(factory.DhakaId);
        var (taxed, plain) = (await ItemAsync(factory.DhakaId, 100m, 10m), await ItemAsync(factory.DhakaId, 40m));
        var tag = Unique();

        var created = await ReadAsync<PurchaseOrderDetail>(await admin.PostAsJsonAsync(Url, await OrderAsync(factory.DhakaId) with
        {
            ReferenceNo = tag,
            HeaderDiscount = 8m,
            DeliveryCharge = 10m,
            Lines = [Line(taxed, 2) with { Discount = 20m }, Line(plain, 1) with { Price = 50m }],
        }));
        var here = await admin.GetFromJsonAsync<PurchaseOrderList>($"{Url}?search={tag}");
        var chattogram = await (await AdminAsync(factory.ChattogramId)).GetFromJsonAsync<PurchaseOrderList>($"{Url}?search={tag}");

        Assert.StartsWith("PO-", created.Code);
        Assert.Equal("pending", created.Status);
        Assert.Equal([(200m, 18m, 198m), (50m, 0m, 50m)], created.Lines.Select(l => (l.TotalAmount, l.VatAmount, l.NetAmount)));
        Assert.Equal((3m, 18m, 248m, 250m), (created.TotalQuantity, created.TotalVatAmount, created.ItemTotalAmount, created.NetAmount));
        Assert.Equal(2, Assert.Single(here!.Items).ItemCount);
        Assert.Empty(chattogram!.Items);
    }

    [Fact]
    public async Task Vat_inclusive_prices_keep_the_line_amount()
    {
        var admin = await AdminAsync(factory.DhakaId);
        var item = await ItemAsync(factory.DhakaId, 115m, 15m);

        var created = await ReadAsync<PurchaseOrderDetail>(await admin.PostAsJsonAsync(Url, await OrderAsync(factory.DhakaId) with
        {
            IsVatInclusive = true,
            Lines = [Line(item, 1)],
        }));

        var line = Assert.Single(created.Lines);
        Assert.Equal((115m, 15m, 115m), (line.TotalAmount, line.VatAmount, line.NetAmount));
        Assert.Equal(115m, created.NetAmount);
    }

    [Fact]
    public async Task An_order_needs_a_unit_supplier_fair_discounts_and_an_approved_requisition()
    {
        var admin = await AdminAsync(factory.DhakaId);
        var item = await ItemAsync(factory.DhakaId, 10m);
        var pending = await ReadAsync<RequisitionDetail>(await admin.PostAsJsonAsync(RequisitionsUrl, await RequisitionAsync(item)));
        var request = await OrderAsync(factory.DhakaId);

        var wrong = await admin.PostAsJsonAsync(Url, request with
        {
            SupplierId = await SupplierAsync(factory.ChattogramId),
            RequisitionId = pending.Id,
            Lines = [Line(item, 1)],
        });
        var discounts = await admin.PostAsJsonAsync(Url, request with { HeaderDiscount = 50m, Lines = [Line(item, 1)] });
        var lineDiscount = await admin.PostAsJsonAsync(Url, request with { Lines = [Line(item, 1) with { Discount = 11m }] });

        Assert.Equal(["requisitionId", "supplierId"], (await ErrorsAsync(wrong)).Order());
        Assert.Equal(["headerDiscount"], await ErrorsAsync(discounts));
        Assert.Equal(["lines"], await ErrorsAsync(lineDiscount));
    }

    [Fact]
    public async Task An_approved_requisition_is_offered_and_an_order_from_it_is_approved_then_closed()
    {
        var admin = await AdminAsync(factory.DhakaId);
        var item = await ItemAsync(factory.DhakaId, 10m, 5m);
        var requisition = await ReadAsync<RequisitionDetail>(await admin.PostAsJsonAsync(RequisitionsUrl, await RequisitionAsync(item)));
        await ReadAsync<RequisitionDetail>(
            await admin.PostAsJsonAsync($"{RequisitionsUrl}/{requisition.Id}/approve", new ReviewRequest(null)));

        var offered = (await admin.GetFromJsonAsync<List<OrderRequisitionOption>>($"{Url}/requisitions"))!
            .Single(r => r.Id == requisition.Id);
        var request = await OrderAsync(factory.DhakaId) with { RequisitionId = requisition.Id, Lines = [Line(item, 4)] };
        var created = await ReadAsync<PurchaseOrderDetail>(await admin.PostAsJsonAsync(Url, request));
        var approved = await ReadAsync<PurchaseOrderDetail>(await admin.PostAsJsonAsync($"{Url}/{created.Id}/approve", new ReviewRequest("OK")));
        var edit = await admin.PutAsJsonAsync($"{Url}/{created.Id}", request);
        var closed = await ReadAsync<PurchaseOrderDetail>(await admin.PostAsync($"{Url}/{created.Id}/close", null));

        Assert.Equal((item, 5m), (Assert.Single(offered.Lines).ItemId, offered.Lines[0].VatPercentage));
        Assert.Equal(requisition.Code, created.RequisitionName);
        Assert.Equal("approved", approved.Status);
        Assert.Equal(HttpStatusCode.Conflict, edit.StatusCode);
        Assert.Equal("closed", closed.Status);
    }

    [Fact]
    public async Task Ordering_from_a_requisition_counts_what_is_ordered_and_offers_only_the_rest()
    {
        var admin = await AdminAsync(factory.DhakaId);
        var item = await ItemAsync(factory.DhakaId, 10m);
        var requisition = await ReadAsync<RequisitionDetail>(await admin.PostAsJsonAsync(RequisitionsUrl, await RequisitionAsync(item)));
        await ReadAsync<RequisitionDetail>(
            await admin.PostAsJsonAsync($"{RequisitionsUrl}/{requisition.Id}/approve", new ReviewRequest(null)));
        var request = await OrderAsync(factory.DhakaId) with { RequisitionId = requisition.Id };

        var first = await ReadAsync<PurchaseOrderDetail>(await admin.PostAsJsonAsync(Url, request with { Lines = [Line(item, 3)] }));
        var ordered = await admin.GetFromJsonAsync<RequisitionDetail>($"{RequisitionsUrl}/{requisition.Id}");
        var offered = (await admin.GetFromJsonAsync<List<OrderRequisitionOption>>($"{Url}/requisitions"))!.Single(r => r.Id == requisition.Id);
        var tooMuch = await admin.PostAsJsonAsync(Url, request with { Lines = [Line(item, 2)] });
        var rest = await ReadAsync<PurchaseOrderDetail>(await admin.PostAsJsonAsync(Url, request with { Lines = [Line(item, 1)] }));
        var full = await admin.GetFromJsonAsync<List<OrderRequisitionOption>>($"{Url}/requisitions");
        await admin.DeleteAsync($"{Url}/{first.Id}");
        var afterDelete = await admin.GetFromJsonAsync<RequisitionDetail>($"{RequisitionsUrl}/{requisition.Id}");

        Assert.Equal(3m, Assert.Single(ordered!.Lines).OrderedQuantity);
        Assert.Equal(1m, Assert.Single(offered.Lines).Quantity);
        Assert.Equal(["lines"], await ErrorsAsync(tooMuch));
        Assert.Equal(1m, rest.TotalQuantity);
        Assert.DoesNotContain(full!, r => r.Id == requisition.Id);
        Assert.Equal(1m, Assert.Single(afterDelete!.Lines).OrderedQuantity);
    }

    private async Task<HttpClient> AdminAsync(Guid unitId)
    {
        var login = await factory.ErpAdminLoginAsync();
        return factory.CreateClient(login.AccessToken, unitId);
    }

    private async Task<SaveRequisitionRequest> RequisitionAsync(int itemId) => new()
    {
        Code = null,
        RequestDate = Today,
        DueDate = null,
        WarehouseId = await WarehouseAsync(factory.DhakaId),
        RequestTypeId = null,
        PurchaseMethodId = null,
        ItemTypeId = null,
        DepartmentId = null,
        SectionId = null,
        EmployeeId = null,
        BuyerId = null,
        ProjectName = null,
        Remarks = null,
        Lines = [new SaveRequisitionLine { ItemId = itemId, Quantity = 4, Rate = null, Remarks = null }],
    };

    private static async Task<SavePurchaseOrderRequest> OrderAsync(Guid unitId) => new()
    {
        Code = null,
        OrderDate = Today,
        SupplierId = await SupplierAsync(unitId),
        WarehouseId = await WarehouseAsync(unitId),
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
        Lines = [],
    };

    private static SavePurchaseOrderLine Line(int itemId, decimal quantity) => new()
    {
        ItemId = itemId,
        Quantity = quantity,
        Price = null,
        Discount = null,
        VatPercentage = null,
        Specification = null,
    };
}
