using System.Net;
using System.Net.Http.Json;
using Erp.Api.Approvals;
using Erp.Api.Sales.SalesQuotations;
using Erp.Tests.Infrastructure;
using static Erp.Tests.Api.ProcurementTestData;
using static Erp.Tests.Api.SalesTestData;

namespace Erp.Tests.Api;

[Collection(ApiCollection.Name)]
public sealed class SalesQuotationTests(ApiFactory factory)
{
    private const string Url = QuotationsUrl;

    [Fact]
    public async Task A_quotation_gets_the_next_number_prices_its_lines_and_shows_only_in_its_unit()
    {
        var admin = await SalesAdminAsync(factory, factory.DhakaId);
        var (taxed, plain, unit) = (await ItemAsync(factory.DhakaId, 1m), await ItemAsync(factory.DhakaId, 1m), await ItemAsync(factory.DhakaId, 1m));
        var tag = Unique();

        var created = await ReadAsync<SalesQuotationDetail>(await admin.PostAsJsonAsync(Url, await QuotationAsync(factory.DhakaId) with
        {
            Reference = tag,
            DeliveryCharge = 10m,
            ExtraCharge = 5m,
            Lines =
            [
                Line(taxed, 2, 100m) with { DiscountPercentage = 10m, SdPercentage = 10m, VatPercentage = 15m, AitPercentage = 5m },
                Line(plain, 1, 50m) with { IsAmountWiseDiscount = true, DiscountAmount = 5m },
                Line(unit, 4, 25m) with { IsDiscountPerQuantity = true, DiscountAmount = 2m, WarrantyInMonth = 12, DeliveryDate = "Within 7 days" },
            ],
        }));
        var here = await admin.GetFromJsonAsync<SalesQuotationList>($"{Url}?search={tag}");
        var chattogram = await (await SalesAdminAsync(factory, factory.ChattogramId)).GetFromJsonAsync<SalesQuotationList>($"{Url}?search={tag}");

        Assert.StartsWith("SQ-", created.Code);
        Assert.Equal(("v1", "pending", false), (created.Version, created.Status, created.IsExpired));
        Assert.Equal(
            [(20m, 18m, 29.7m, 9m, 236.7m), (5m, 0m, 0m, 0m, 45m), (8m, 0m, 0m, 0m, 92m)],
            created.Lines.Select(l => (l.DiscountAmount, l.SdAmount, l.VatAmount, l.AitAmount, l.TotalValue)));
        Assert.Equal((350m, 33m, 388.7m), (created.GrossAmount, created.DiscountAmount, created.TotalAmount));
        Assert.Equal((12, "Within 7 days"), (created.Lines[2].WarrantyInMonth, created.Lines[2].DeliveryDate));
        Assert.Equal((3, 7m), (Assert.Single(here!.Items).ItemCount, here.Items[0].TotalQuantity));
        Assert.Empty(chattogram!.Items);
    }

    [Fact]
    public async Task A_discount_on_the_total_is_shared_and_inclusive_prices_keep_the_line_amount()
    {
        var admin = await SalesAdminAsync(factory, factory.DhakaId);
        var (first, second) = (await ItemAsync(factory.DhakaId, 1m), await ItemAsync(factory.DhakaId, 1m));

        var created = await ReadAsync<SalesQuotationDetail>(await admin.PostAsJsonAsync(Url, await QuotationAsync(factory.DhakaId) with
        {
            IsVatInclusive = true,
            IsGrossDiscount = true,
            DiscountPercentage = 10m,
            Lines = [Line(first, 1, 230m) with { VatPercentage = 15m }, Line(second, 1, 100m) with { DiscountPercentage = 50m }],
        }));

        Assert.Equal([(23m, 27m, 207m), (10m, 0m, 90m)], created.Lines.Select(l => (l.DiscountAmount, l.VatAmount, l.TotalValue)));
        Assert.Equal((10m, 33m, 297m), (created.DiscountPercentage, created.DiscountAmount, created.TotalAmount));
    }

    [Fact]
    public async Task A_quotation_needs_a_unit_customer_a_valid_date_and_fair_discounts()
    {
        var admin = await SalesAdminAsync(factory, factory.DhakaId);
        var item = await ItemAsync(factory.DhakaId, 1m);
        var request = await QuotationAsync(factory.DhakaId) with { Lines = [Line(item, 1, 10m)] };

        var customer = await admin.PostAsJsonAsync(Url, request with { CustomerId = await CustomerAsync(factory.ChattogramId) });
        var date = await admin.PostAsJsonAsync(Url, request with { ValidityDate = Today.AddDays(-1) });
        var gross = await admin.PostAsJsonAsync(Url, request with { IsGrossDiscount = true, DiscountAmount = 11m });
        var line = await admin.PostAsJsonAsync(Url, request with
        {
            Lines = [Line(item, 1, 10m) with { IsAmountWiseDiscount = true, DiscountAmount = 11m }],
        });

        Assert.Equal(["customerId"], await ErrorsAsync(customer));
        Assert.Equal(["validityDate"], await ErrorsAsync(date));
        Assert.Equal(["discountAmount"], await ErrorsAsync(gross));
        Assert.Equal(["lines"], await ErrorsAsync(line));
    }

    [Fact]
    public async Task An_approved_quotation_keeps_the_approved_prices_and_is_revised_to_the_next_version()
    {
        var admin = await SalesAdminAsync(factory, factory.DhakaId);
        var item = await ItemAsync(factory.DhakaId, 1m);
        var request = await QuotationAsync(factory.DhakaId) with { Lines = [Line(item, 3, 20m)] };

        var created = await ReadAsync<SalesQuotationDetail>(await admin.PostAsJsonAsync(Url, request));
        var early = await admin.PostAsync($"{Url}/{created.Id}/revise", null);
        var approved = await ReadAsync<SalesQuotationDetail>(
            await admin.PostAsJsonAsync($"{Url}/{created.Id}/approve", new ReviewRequest(null)));
        var edit = await admin.PutAsJsonAsync($"{Url}/{created.Id}", request);
        var revised = await ReadAsync<SalesQuotationDetail>(await admin.PostAsync($"{Url}/{created.Id}/revise", null));
        var again = await admin.PostAsync($"{Url}/{created.Id}/revise", null);
        var changed = await ReadAsync<SalesQuotationDetail>(await admin.PutAsJsonAsync($"{Url}/{revised.Id}", request with
        {
            Code = revised.Code,
            Version = revised.Version,
            Lines = [Line(item, 5, 18m)],
        }));

        Assert.Equal(HttpStatusCode.Conflict, early.StatusCode);
        Assert.Equal(("approved", true), (approved.Status, approved.CanRevise));
        Assert.Equal((3m, 20m, 60m), (approved.Lines[0].ApproveQuantity, approved.Lines[0].ApprovePrice, approved.Lines[0].ApproveAmount));
        Assert.Equal(HttpStatusCode.Conflict, edit.StatusCode);
        Assert.Equal((created.Code, "v2", "pending", (int?)created.Id), (revised.Code, revised.Version, revised.Status, revised.ReferenceId));
        Assert.Equal(0m, revised.Lines[0].ApproveQuantity);
        Assert.Equal([("v2", "pending"), ("v1", "closed")], revised.Versions.Select(v => (v.Version, v.Status)));
        Assert.Equal(HttpStatusCode.Conflict, again.StatusCode);
        Assert.Equal((revised.Code, "v2", 90m), (changed.Code, changed.Version, changed.TotalAmount));
    }

    [Fact]
    public async Task A_quotation_is_rejected_cancelled_and_deleted_like_every_document()
    {
        var admin = await SalesAdminAsync(factory, factory.DhakaId);
        var item = await ItemAsync(factory.DhakaId, 1m);
        var request = await QuotationAsync(factory.DhakaId) with { Lines = [Line(item, 1, 10m)] };
        var first = await ReadAsync<SalesQuotationDetail>(await admin.PostAsJsonAsync(Url, request));
        var second = await ReadAsync<SalesQuotationDetail>(await admin.PostAsJsonAsync(Url, request));

        var noReason = await admin.PostAsJsonAsync($"{Url}/{first.Id}/reject", new ReviewRequest(null));
        var rejected = await ReadAsync<SalesQuotationDetail>(await admin.PostAsJsonAsync($"{Url}/{first.Id}/reject", new ReviewRequest("Too low")));
        var deleted = await admin.DeleteAsync($"{Url}/{first.Id}");
        var gone = await admin.GetAsync($"{Url}/{first.Id}");
        await admin.PostAsJsonAsync($"{Url}/{second.Id}/approve", new ReviewRequest(null));
        var cancelled = await ReadAsync<SalesQuotationDetail>(await admin.PostAsJsonAsync($"{Url}/{second.Id}/cancel", new ReviewRequest("Not needed")));

        Assert.Equal(HttpStatusCode.BadRequest, noReason.StatusCode);
        Assert.Equal(("rejected", "Too low"), (rejected.Status, rejected.ReviewComment));
        Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, gone.StatusCode);
        Assert.Equal("cancelled", cancelled.Status);
    }
}
