using System.Net;
using System.Net.Http.Json;
using Erp.Api.Approvals;
using Erp.Api.Procurement.PurchasePayments;
using Erp.Api.Procurement.PurchaseReceives;
using Erp.Tests.Infrastructure;
using static Erp.Tests.Api.ProcurementTestData;

namespace Erp.Tests.Api;

[Collection(ApiCollection.Name)]
public sealed class PurchasePaymentTests(ApiFactory factory)
{
    private const string Url = "/api/erp/procurement/purchase-payments";

    [Fact]
    public async Task A_payment_settles_bills_and_updates_what_the_receive_has_paid()
    {
        var admin = await AdminAsync();
        var supplier = await SupplierAsync(factory.DhakaId);
        var item = await ItemAsync(factory.DhakaId, 100m);
        var first = await ApprovedReceiveAsync(factory, admin, item, 5, supplier);
        var second = await ApprovedReceiveAsync(factory, admin, item, 3, supplier);

        var before = await BillsAsync(admin, supplier);
        var created = await ReadAsync<PurchasePaymentDetail>(await admin.PostAsJsonAsync(Url, Payment(supplier,
            Line(first.Id, 400m, vds: 50m, discount: 50m), Line(second.Id, 100m))));
        var afterReceive = await admin.GetFromJsonAsync<PurchaseReceiveDetail>($"{ProcurementReceivesUrl}/{first.Id}");
        var after = await BillsAsync(admin, supplier);
        var editing = await BillsAsync(admin, supplier, created.Id);

        Assert.Equal([(first.Id, 500m), (second.Id, 300m)], before.Select(b => (b.Id, b.Due)));
        Assert.StartsWith("PAY-", created.Code);
        Assert.Equal("pending", created.Status);
        Assert.Equal((500m, 50m, 0m, 50m, 600m),
            (created.PaymentAmount, created.Vds, created.Tds, created.DiscountAmount, created.SettledAmount));
        Assert.Equal((500m, 500m), (created.Lines[0].Due, created.Lines[0].SettledAmount));
        Assert.Equal(500m, afterReceive!.PaidAmount);
        Assert.Equal([(second.Id, 200m)], after.Select(b => (b.Id, b.Due)));
        Assert.Equal([500m, 300m], editing.Select(b => b.Due));
    }

    [Fact]
    public async Task A_payment_stays_within_the_due_and_needs_approved_bills_of_the_supplier()
    {
        var admin = await AdminAsync();
        var supplier = await SupplierAsync(factory.DhakaId);
        var item = await ItemAsync(factory.DhakaId, 10m);
        var bill = await ApprovedReceiveAsync(factory, admin, item, 10, supplier);
        var otherSupplier = await ApprovedReceiveAsync(factory, admin, item, 1);
        var order = await ApprovedOrderAsync(factory, admin, item, 1, supplier);
        var pending = await ReadAsync<PurchaseReceiveDetail>(await admin.PostAsJsonAsync(ProcurementReceivesUrl,
            await ReceiveAsync(factory, order.Id, item, 1)));

        var tooMuch = await admin.PostAsJsonAsync(Url, Payment(supplier, Line(bill.Id, 90m, tds: 11m)));
        var wrongSupplier = await admin.PostAsJsonAsync(Url, Payment(supplier, Line(otherSupplier.Id, 1m)));
        var notApproved = await admin.PostAsJsonAsync(Url, Payment(supplier, Line(pending.Id, 1m)));
        var twice = await admin.PostAsJsonAsync(Url, Payment(supplier, Line(bill.Id, 1m), Line(bill.Id, 1m)));
        var empty = await admin.PostAsJsonAsync(Url, Payment(supplier, Line(bill.Id, 0m)));

        Assert.Equal(["lines"], await ErrorsAsync(tooMuch));
        Assert.Equal(["lines"], await ErrorsAsync(wrongSupplier));
        Assert.Equal(["lines"], await ErrorsAsync(notApproved));
        Assert.Equal(["lines"], await ErrorsAsync(twice));
        Assert.Equal(["lines[0].paymentAmount"], await ErrorsAsync(empty));
    }

    [Fact]
    public async Task A_rejected_or_deleted_payment_gives_the_amount_back()
    {
        var admin = await AdminAsync();
        var supplier = await SupplierAsync(factory.DhakaId);
        var item = await ItemAsync(factory.DhakaId, 10m);
        var bill = await ApprovedReceiveAsync(factory, admin, item, 10, supplier);

        var rejected = await ReadAsync<PurchasePaymentDetail>(await admin.PostAsJsonAsync(Url, Payment(supplier, Line(bill.Id, 100m))));
        await ReadAsync<PurchasePaymentDetail>(await admin.PostAsJsonAsync($"{Url}/{rejected.Id}/reject", new ReviewRequest("Wrong amount")));
        var afterReject = await admin.GetFromJsonAsync<PurchaseReceiveDetail>($"{ProcurementReceivesUrl}/{bill.Id}");

        var deleted = await ReadAsync<PurchasePaymentDetail>(await admin.PostAsJsonAsync(Url, Payment(supplier, Line(bill.Id, 60m))));
        var afterSave = await admin.GetFromJsonAsync<PurchaseReceiveDetail>($"{ProcurementReceivesUrl}/{bill.Id}");
        Assert.Equal(HttpStatusCode.NoContent, (await admin.DeleteAsync($"{Url}/{deleted.Id}")).StatusCode);
        var afterDelete = await admin.GetFromJsonAsync<PurchaseReceiveDetail>($"{ProcurementReceivesUrl}/{bill.Id}");

        Assert.Equal(0m, afterReject!.PaidAmount);
        Assert.Equal(60m, afterSave!.PaidAmount);
        Assert.Equal(0m, afterDelete!.PaidAmount);
    }

    private async Task<HttpClient> AdminAsync()
    {
        var login = await factory.ErpAdminLoginAsync();
        return factory.CreateClient(login.AccessToken, factory.DhakaId);
    }

    private static async Task<List<PayableBill>> BillsAsync(HttpClient admin, int supplierId, int? paymentId = null) =>
        [.. (await admin.GetFromJsonAsync<SupplierPayable>($"{Url}/bills?supplierId={supplierId}{(paymentId is null ? "" : $"&paymentId={paymentId}")}"))!.Bills];

    private static SavePurchasePaymentRequest Payment(int supplierId, params SavePurchasePaymentLine[] lines) => new()
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
        Lines = lines,
    };

    private static SavePurchasePaymentLine Line(int receiveId, decimal payment, decimal? vds = null, decimal? tds = null,
        decimal? discount = null) => new()
    {
        PurchaseReceiveId = receiveId,
        PaymentAmount = payment,
        Vds = vds,
        Tds = tds,
        DiscountAmount = discount,
        AdvanceAdjusted = null,
    };
}
