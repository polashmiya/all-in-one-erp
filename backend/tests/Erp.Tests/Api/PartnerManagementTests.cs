using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Erp.Api.PartnerManagement;
using Erp.Api.PartnerManagement.Customers;
using Erp.Api.PartnerManagement.Suppliers;
using Erp.Infrastructure.Persistence;
using Erp.Infrastructure.Persistence.Seeding;
using Erp.Metadata;
using Erp.Tests.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Erp.Tests.Api;

[Collection(ApiCollection.Name)]
public sealed class PartnerManagementTests(ApiFactory factory)
{
    private const string CustomersUrl = "/api/erp/partner-management/customers";
    private const string SuppliersUrl = "/api/erp/partner-management/suppliers";

    [Fact]
    public async Task A_customer_gets_the_next_code_and_shows_only_in_its_unit()
    {
        var chattogram = await AdminAsync(factory.ChattogramId);
        var mobile = Unique();
        var created = await ReadAsync<CustomerDetail>(await chattogram.PostAsJsonAsync(CustomersUrl, Customer() with { Mobile = mobile }));

        var here = await chattogram.GetFromJsonAsync<CustomerList>($"{CustomersUrl}?search={mobile}");
        var dhaka = await (await AdminAsync(factory.DhakaId)).GetFromJsonAsync<CustomerList>($"{CustomersUrl}?search={mobile}");

        Assert.StartsWith("CUS-", created.Code);
        Assert.Single(here!.Items);
        Assert.Empty(dhaka!.Items);
    }

    [Fact]
    public async Task A_customer_rejects_a_value_from_another_list_and_a_used_code()
    {
        var admin = await AdminAsync(factory.DhakaId);
        var supplierGroup = await LookupValueAsync(PartnerLookups.SupplierGroup);
        var first = await ReadAsync<CustomerDetail>(await admin.PostAsJsonAsync(CustomersUrl, Customer()));

        var response = await admin.PostAsJsonAsync(CustomersUrl, Customer() with { Code = first.Code, GroupId = supplierGroup });
        var errors = (await ReadAsync<ValidationProblemDetails>(response, HttpStatusCode.BadRequest)).Errors;

        Assert.Contains("code", errors.Keys);
        Assert.Contains("groupId", errors.Keys);
    }

    [Fact]
    public async Task A_supplier_follows_the_configuration_and_keeps_custom_fields()
    {
        var admin = await AdminAsync(factory.DhakaId);
        var district = (await PlaceAsync("Dhaka", "Gulshan")).District;
        var existing = await ReadAsync<SupplierDetail>(await admin.PostAsJsonAsync(SuppliersUrl, Supplier() with { DistrictId = district }));
        await using var settings = await FieldSettingsScope.ApplyAsync(factory.DhakaId, PartnerManagementMenu.Supplier,
            ("districtId", s => s.IsVisible = false),
            ("binNumber", s => s.IsRequired = true));
        await using var fields = await CustomFieldScope.ApplyAsync(factory.DhakaId, PartnerManagementMenu.Supplier,
            ("bankAccount", FieldTypes.Text, null));

        var missing = await admin.PostAsJsonAsync(SuppliersUrl, Supplier());
        Assert.Contains("binNumber", (await ReadAsync<ValidationProblemDetails>(missing, HttpStatusCode.BadRequest)).Errors.Keys);

        var updated = await ReadAsync<SupplierDetail>(await admin.PutAsJsonAsync($"{SuppliersUrl}/{existing.Id}", Supplier() with
        {
            Code = existing.Code,
            BinNumber = "BIN-1",
            CustomData = Data("""{"bankAccount":"0123"}"""),
        }));

        Assert.Equal(district, updated.DistrictId);
        Assert.Equal("0123", updated.CustomData!["bankAccount"].GetString());
    }

    [Fact]
    public async Task Suppliers_are_deactivated_in_bulk_and_counted_by_status()
    {
        var admin = await AdminAsync(factory.DhakaId);
        var tag = Unique();
        var a = await ReadAsync<SupplierDetail>(await admin.PostAsJsonAsync(SuppliersUrl, Supplier() with { Name = $"Bulk {tag} A" }));
        var b = await ReadAsync<SupplierDetail>(await admin.PostAsJsonAsync(SuppliersUrl, Supplier() with { Name = $"Bulk {tag} B" }));

        var status = await admin.PostAsJsonAsync($"{SuppliersUrl}/status", new PartnerStatusRequest([a.Id, b.Id], false));
        var list = await admin.GetFromJsonAsync<SupplierList>($"{SuppliersUrl}?search={tag}&status=active");

        Assert.Equal(HttpStatusCode.NoContent, status.StatusCode);
        Assert.Empty(list!.Items);
        Assert.Equal(new PartnerCounts(2, 0, 2), list.Counts);
    }

    [Fact]
    public async Task A_customer_thana_must_belong_to_its_district()
    {
        var admin = await AdminAsync(factory.DhakaId);
        var dhaka = await PlaceAsync("Dhaka", "Gulshan");
        var gazipur = await PlaceAsync("Gazipur", "Kaliakair");

        var wrong = await admin.PostAsJsonAsync(CustomersUrl, Customer() with { DistrictId = dhaka.District, ThanaId = gazipur.Thana });
        var created = await ReadAsync<CustomerDetail>(await admin.PostAsJsonAsync(CustomersUrl,
            Customer() with { DistrictId = dhaka.District, ThanaId = dhaka.Thana }));

        Assert.Equal(["thanaId"], (await ReadAsync<ValidationProblemDetails>(wrong, HttpStatusCode.BadRequest)).Errors.Keys);
        Assert.Equal(("Dhaka", "Gulshan"), (created.DistrictName, created.ThanaName));
    }

    private static async Task<(int District, int Thana)> PlaceAsync(string district, string thana)
    {
        await using var db = ApiFactory.CreateDbContext();
        return await db.Thanas
            .Where(t => t.Name == thana && db.Districts.Any(d => d.Id == t.DistrictId && d.Name == district))
            .Select(t => ValueTuple.Create(t.DistrictId, t.Id))
            .SingleAsync();
    }

    private async Task<HttpClient> AdminAsync(Guid unitId)
    {
        var login = await factory.ErpAdminLoginAsync();
        return factory.CreateClient(login.AccessToken, unitId);
    }

    private static async Task<T> ReadAsync<T>(HttpResponseMessage response, HttpStatusCode expected = HttpStatusCode.OK)
    {
        Assert.Equal(expected, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<T>())!;
    }

    private static async Task<int> LookupValueAsync(string type)
    {
        await using var db = ApiFactory.CreateDbContext();
        return await db.LookupValues.AcrossTenants()
            .Where(v => db.Accounts.Any(a => a.Id == v.AccountId && a.Code == DemoDataSeeder.AccountCode) && v.LookupType!.Key == type)
            .Select(v => v.Id)
            .FirstAsync();
    }

    private static string Unique() => Guid.NewGuid().ToString("N")[..8];

    private static Dictionary<string, JsonElement> Data(string json) =>
        JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json)!;

    private static SaveCustomerRequest Customer() => new()
    {
        Code = null,
        Name = $"Customer {Unique()}",
        BillingName = null,
        GroupId = null,
        PriceTypeId = null,
        Mobile = null,
        Email = null,
        ContactPerson = null,
        ContactMobile = null,
        Address = null,
        City = null,
        DistrictId = null,
        ThanaId = null,
        TerritoryId = null,
        IsForeign = false,
        CreditLimit = null,
        PaymentDays = null,
        SalesRepresentativeId = null,
        BinNumber = null,
        TinNumber = null,
        TradeLicenseNumber = null,
        Nid = null,
        CentralVatRegistrationNo = null,
        IsVatNotApplicable = false,
        IsVdsApplicable = false,
        DateOfBirth = null,
        GenderId = null,
        Profession = null,
        MarriageDate = null,
        Remarks = null,
        IsActive = true,
    };

    internal static SaveSupplierRequest Supplier() => new()
    {
        Code = null,
        Name = $"Supplier {Unique()}",
        BillingName = null,
        GroupId = null,
        Mobile = null,
        Email = null,
        ContactPerson = null,
        ContactMobile = null,
        Address = null,
        City = null,
        DistrictId = null,
        ThanaId = null,
        IsForeign = false,
        CreditLimit = null,
        PaymentDays = null,
        Tolerance = null,
        BinNumber = null,
        TinNumber = null,
        TradeLicenseNumber = null,
        Nid = null,
        CentralVatRegistrationNo = null,
        IsVatNotApplicable = false,
        IsVdsApplicable = false,
        Remarks = null,
        IsActive = true,
    };
}
