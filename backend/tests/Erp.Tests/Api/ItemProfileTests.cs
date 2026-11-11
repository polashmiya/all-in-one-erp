using System.Net;
using System.Net.Http.Json;
using Erp.Api.Lookups;
using Erp.Modules.Inventory;
using Erp.Modules.Inventory.ItemManagement;
using Erp.Modules.Inventory.ItemManagement.Categories;
using Erp.Modules.Inventory.ItemManagement.Departments;
using Erp.Modules.Inventory.ItemManagement.Items;
using Erp.Modules.Inventory.ItemManagement.MinorCategories;
using Erp.Modules.Inventory.ItemManagement.SubCategories;
using Erp.Modules.Inventory.ItemManagement.Uoms;
using Erp.Tests.Infrastructure;
using Microsoft.AspNetCore.Mvc;

namespace Erp.Tests.Api;

[Collection(ApiCollection.Name)]
public sealed class ItemProfileTests(ApiFactory factory)
{
    private const string Base = "/api/erp/item-management";

    private sealed record Hierarchy(ItemCategoryRow Category, ItemMinorCategoryRow Minor);

    private sealed record Choices(int FinishedGoods, int Service, int StandardVat);

    private sealed record Setup(Hierarchy Hierarchy, UomRow Uom, UomRow AltUom, Choices Choices);

    private static string Unique(int length = 6) => Guid.NewGuid().ToString("N")[..length].ToUpperInvariant();

    private async Task<HttpClient> AdminAsync(Guid? unit = null)
    {
        var login = await factory.ErpAdminLoginAsync();
        return factory.CreateClient(login.AccessToken, unit ?? factory.DhakaId);
    }

    private static async Task<T> ReadAsync<T>(HttpResponseMessage response, HttpStatusCode expected = HttpStatusCode.OK)
    {
        Assert.Equal(expected, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<T>())!;
    }

    private static async Task<T> CreateAsync<T>(HttpClient client, string path, object body) =>
        await ReadAsync<T>(await client.PostAsJsonAsync($"{Base}/{path}", body), HttpStatusCode.Created);

    private static async Task<IEnumerable<string>> ErrorsAsync(HttpResponseMessage response) =>
        (await ReadAsync<ValidationProblemDetails>(response, HttpStatusCode.BadRequest)).Errors.Keys;

    private static async Task<Hierarchy> CreateHierarchyAsync(HttpClient client)
    {
        var department = await CreateAsync<ItemDepartmentRow>(client, "departments",
            new SaveItemDepartmentRequest(Unique(), $"Dept {Unique()}", null, null, true));
        var category = await CreateAsync<ItemCategoryRow>(client, "categories",
            new SaveItemCategoryRequest(department.Id, Unique(3), $"Cat {Unique()}", null, true));
        var subCategory = await CreateAsync<ItemSubCategoryRow>(client, "sub-categories",
            new SaveItemSubCategoryRequest(category.Id, $"Sub {Unique()}", null, true));
        var minor = await CreateAsync<ItemMinorCategoryRow>(client, "minor-categories",
            new SaveItemMinorCategoryRequest(subCategory.Id, $"Minor {Unique()}", null, true));
        return new Hierarchy(category, minor);
    }

    private static async Task<Setup> CreateSetupAsync(HttpClient client) => new(
        await CreateHierarchyAsync(client),
        await CreateAsync<UomRow>(client, "uoms", new SaveUomRequest(Unique(5), $"Piece {Unique()}", null, false, null, true)),
        await CreateAsync<UomRow>(client, "uoms", new SaveUomRequest(Unique(5), $"Box {Unique()}", null, false, null, true)),
        new Choices(
            await ChoiceAsync(client, ItemLookups.ItemType, "FG"),
            await ChoiceAsync(client, ItemLookups.ItemType, "SRV"),
            await ChoiceAsync(client, ItemLookups.SupplyType, "STD")));

    private static async Task<int> ChoiceAsync(HttpClient client, string type, string code) =>
        (await client.GetFromJsonAsync<List<LookupOption>>($"/api/erp/lookups/{type}"))!.Single(o => o.Code == code).Id;

    private static SaveItemRequest NewItem(Setup setup) => new()
    {
        Code = null,
        Name = $"Item {Unique()}",
        Description = null,
        ItemTypeId = setup.Choices.FinishedGoods,
        HsCode = "3004.90.99",
        MinorCategoryId = setup.Hierarchy.Minor.Id,
        IsAutoBarcode = true,
        BarCode = null,
        IsChildBarcode = false,
        MasterItemId = null,
        SupplierName = null,
        UomId = setup.Uom.Id,
        AltUomId = null,
        AltUomConversionQty = null,
        IsNoDecimalQty = true,
        AverageCostPrice = 10m,
        LastCostPrice = 10m,
        SalesPrice = 12.5m,
        PurchaseSupplyTypeId = setup.Choices.StandardVat,
        PurchaseVatPercentage = 15m,
        SalesSupplyTypeId = setup.Choices.StandardVat,
        SalesVatPercentage = 15m,
        SdPercentage = null,
        IsVatInclusive = false,
        IsVatRebatable = true,
        IsSaleable = true,
        IsPurchase = true,
        IsNegativeSales = false,
        IsMultipleSalesPrice = false,
        ImageSource = null,
        IsActive = true,
    };

    [Fact]
    public async Task Item_create_edit_status_and_delete_round_trip()
    {
        var client = await AdminAsync();
        var setup = await CreateSetupAsync(client);
        var category = setup.Hierarchy.Category;

        var created = await CreateAsync<ItemDetail>(client, "items", NewItem(setup) with { Name = "  Napa 500  ", Description = " Paracetamol " });

        Assert.Equal($"{category.Code}-00001", created.Code);
        Assert.Equal(("Napa 500", "Paracetamol"), (created.Name, created.Description));
        Assert.Matches("^20[0-9]{11}$", created.BarCode);
        Assert.Equal((category.DepartmentId, category.Id, setup.Hierarchy.Minor.SubCategoryId), (created.DepartmentId, created.CategoryId, created.SubCategoryId));
        Assert.Equal((setup.Uom.Name, "Finished Goods", "Standard VAT"), (created.UomName, created.ItemTypeName, created.SalesSupplyTypeName));
        Assert.NotNull(created.CreatedByName);

        var barCode = $"BC{Unique()}";
        var updated = await ReadAsync<ItemDetail>(await client.PutAsJsonAsync($"{Base}/items/{created.Id}", NewItem(setup) with
        {
            Name = created.Name,
            ItemTypeId = setup.Choices.Service,
            IsAutoBarcode = false,
            BarCode = $" {barCode} ",
            AltUomId = setup.AltUom.Id,
            AltUomConversionQty = 12m,
        }));

        Assert.Equal(created.Code, updated.Code);
        Assert.Equal((barCode, setup.AltUom.Name, 12m, "Service"), (updated.BarCode, updated.AltUomName, updated.AltUomConversionQty, updated.ItemTypeName));
        Assert.NotNull(updated.ModifiedAt);

        var next = await CreateAsync<ItemDetail>(client, "items", NewItem(setup));
        Assert.NotEqual(created.BarCode, next.BarCode);

        var status = await client.PostAsJsonAsync($"{Base}/items/status", new RecordStatusRequest([created.Id], false));
        Assert.Equal(1, (await ReadAsync<AffectedRecords>(status)).Count);

        var inactive = await client.GetFromJsonAsync<ItemList>($"{Base}/items?status=inactive&search={created.Code}");
        Assert.Equal(created.Id, Assert.Single(inactive!.Items).Id);
        Assert.Equal((1, 0, 1), (inactive.Counts.All, inactive.Counts.Active, inactive.Counts.Inactive));

        Assert.Equal(HttpStatusCode.OK, (await client.DeleteAsync($"{Base}/items/{created.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"{Base}/items/{created.Id}")).StatusCode);
    }

    [Fact]
    public async Task Invalid_item_input_is_rejected_before_saving()
    {
        var client = await AdminAsync();
        var setup = await CreateSetupAsync(client);

        var response = await client.PostAsJsonAsync($"{Base}/items", NewItem(setup) with
        {
            Name = "",
            ItemTypeId = 99,
            HsCode = "",
            IsAutoBarcode = false,
            BarCode = null,
            IsChildBarcode = true,
            AltUomId = setup.Uom.Id,
            SalesVatPercentage = 150m,
        });

        Assert.Equal(
            ["altUomConversionQty", "altUomId", "barCode", "masterItemId", "name", "salesVatPercentage"],
            (await ErrorsAsync(response)).Order());
    }

    [Fact]
    public async Task Lookup_values_must_come_from_the_right_list()
    {
        var client = await AdminAsync();
        var setup = await CreateSetupAsync(client);

        var wrongList = await client.PostAsJsonAsync($"{Base}/items", NewItem(setup) with { ItemTypeId = setup.Choices.StandardVat });
        var unknown = await client.PostAsJsonAsync($"{Base}/items", NewItem(setup) with { SalesSupplyTypeId = int.MaxValue });

        Assert.Equal(["itemTypeId"], await ErrorsAsync(wrongList));
        Assert.Equal(["salesSupplyTypeId"], await ErrorsAsync(unknown));
    }

    [Fact]
    public async Task Required_fields_come_from_the_feature_configuration()
    {
        var client = await AdminAsync();
        var setup = await CreateSetupAsync(client);

        var response = await client.PostAsJsonAsync($"{Base}/items", NewItem(setup) with
        {
            HsCode = " ",
            ItemTypeId = null,
            PurchaseSupplyTypeId = null,
        });

        Assert.Equal(["hsCode", "itemTypeId", "purchaseSupplyTypeId"], (await ErrorsAsync(response)).Order());
    }

    [Fact]
    public async Task Account_settings_hide_fields_and_change_what_is_required()
    {
        var client = await AdminAsync();
        var setup = await CreateSetupAsync(client);
        var existing = await CreateAsync<ItemDetail>(client, "items", NewItem(setup));

        await using var settings = await FieldSettingsScope.ApplyAsync(factory.DhakaId, ItemManagementMenu.ItemProfile,
            ("hsCode", s => s.IsVisible = false),
            ("itemTypeId", s => s.IsRequired = false),
            ("isSaleable", s => s.IsVisible = false),
            ("supplierName", s => (s.IsRequired, s.MaxLength) = (true, 5)));

        var missing = await client.PostAsJsonAsync($"{Base}/items", NewItem(setup) with { SupplierName = null });
        var tooLong = await client.PostAsJsonAsync($"{Base}/items", NewItem(setup) with { SupplierName = "Acme Ltd" });
        Assert.Equal(["supplierName"], await ErrorsAsync(missing));
        Assert.Equal(["supplierName"], await ErrorsAsync(tooLong));

        var created = await CreateAsync<ItemDetail>(client, "items",
            NewItem(setup) with { HsCode = "IGNORED", ItemTypeId = null, IsSaleable = false, SupplierName = "Acme" });
        Assert.Equal(("", null, null, true), (created.HsCode, created.ItemTypeId, created.ItemTypeName, created.IsSaleable));

        var updated = await ReadAsync<ItemDetail>(await client.PutAsJsonAsync($"{Base}/items/{existing.Id}", NewItem(setup) with
        {
            Name = existing.Name,
            HsCode = "CHANGED",
            IsSaleable = false,
            SupplierName = "Acme",
        }));
        Assert.Equal((existing.HsCode, true, "Acme"), (updated.HsCode, updated.IsSaleable, updated.SupplierName));
    }

    [Fact]
    public async Task Duplicate_code_and_barcode_are_field_errors()
    {
        var client = await AdminAsync();
        var setup = await CreateSetupAsync(client);
        var first = await CreateAsync<ItemDetail>(client, "items",
            NewItem(setup) with { Code = $"IT{Unique()}", IsAutoBarcode = false, BarCode = $"BC{Unique()}" });

        var response = await client.PostAsJsonAsync($"{Base}/items",
            NewItem(setup) with { Code = first.Code, IsAutoBarcode = false, BarCode = first.BarCode });

        Assert.Equal(["barCode", "code"], (await ErrorsAsync(response)).Order());
    }

    [Fact]
    public async Task Child_items_point_to_a_master_item_that_cannot_be_deleted()
    {
        var client = await AdminAsync();
        var setup = await CreateSetupAsync(client);
        var master = await CreateAsync<ItemDetail>(client, "items", NewItem(setup));
        var child = await CreateAsync<ItemDetail>(client, "items", NewItem(setup) with { IsChildBarcode = true, MasterItemId = master.Id });

        Assert.Equal((master.Id, master.Code, master.Name), (child.MasterItemId, child.MasterItemCode, child.MasterItemName));
        Assert.NotEqual(master.BarCode, child.BarCode);

        var grandChild = await client.PostAsJsonAsync($"{Base}/items", NewItem(setup) with { IsChildBarcode = true, MasterItemId = child.Id });
        Assert.Contains("masterItemId", await ErrorsAsync(grandChild));

        var options = await client.GetFromJsonAsync<List<ItemLookup>>($"{Base}/items/options?search={setup.Hierarchy.Category.Code}-");
        Assert.Equal(master.Id, Assert.Single(options!).Id);

        var blocked = await client.DeleteAsync($"{Base}/items/{master.Id}");
        Assert.Contains("master item of 1 item", (await ReadAsync<ProblemDetails>(blocked, HttpStatusCode.Conflict)).Detail);
    }

    [Fact]
    public async Task Inactive_items_cannot_become_master_items()
    {
        var client = await AdminAsync();
        var setup = await CreateSetupAsync(client);
        var master = await CreateAsync<ItemDetail>(client, "items", NewItem(setup));
        var child = await CreateAsync<ItemDetail>(client, "items", NewItem(setup) with { IsChildBarcode = true, MasterItemId = master.Id });
        await client.PostAsJsonAsync($"{Base}/items/status", new RecordStatusRequest([master.Id], false));

        var options = await client.GetFromJsonAsync<List<ItemLookup>>($"{Base}/items/options?search={setup.Hierarchy.Category.Code}-");
        var rejected = await client.PostAsJsonAsync($"{Base}/items", NewItem(setup) with { IsChildBarcode = true, MasterItemId = master.Id });
        var kept = await client.PutAsJsonAsync($"{Base}/items/{child.Id}",
            NewItem(setup) with { Name = child.Name, IsChildBarcode = true, MasterItemId = master.Id });

        Assert.Empty(options!);
        Assert.Contains("masterItemId", await ErrorsAsync(rejected));
        Assert.Equal(HttpStatusCode.OK, kept.StatusCode);
    }

    [Fact]
    public async Task Moving_categories_and_renaming_units_reach_their_items()
    {
        var client = await AdminAsync();
        var setup = await CreateSetupAsync(client);
        var other = await CreateHierarchyAsync(client);
        var item = await CreateAsync<ItemDetail>(client, "items", NewItem(setup) with { AltUomId = setup.AltUom.Id, AltUomConversionQty = 10m });
        var category = setup.Hierarchy.Category;
        var minor = setup.Hierarchy.Minor;

        await client.PutAsJsonAsync($"{Base}/categories/{category.Id}",
            new SaveItemCategoryRequest(other.Category.DepartmentId, category.Code, category.Name, null, true));
        Assert.Equal(other.Category.DepartmentId, (await client.GetFromJsonAsync<ItemDetail>($"{Base}/items/{item.Id}"))!.DepartmentId);

        await client.PutAsJsonAsync($"{Base}/minor-categories/{minor.Id}",
            new SaveItemMinorCategoryRequest(other.Minor.SubCategoryId, minor.Name, null, true));
        var newName = $"Carton {Unique()}";
        await client.PutAsJsonAsync($"{Base}/uoms/{setup.AltUom.Id}", new SaveUomRequest(setup.AltUom.Code, newName, null, false, null, true));

        var moved = (await client.GetFromJsonAsync<ItemDetail>($"{Base}/items/{item.Id}"))!;
        Assert.Equal((other.Category.DepartmentId, other.Category.Id, other.Minor.SubCategoryId), (moved.DepartmentId, moved.CategoryId, moved.SubCategoryId));
        Assert.Equal(newName, moved.AltUomName);
    }

    [Fact]
    public async Task Items_belong_to_one_business_unit()
    {
        var dhaka = await AdminAsync(factory.DhakaId);
        var chattogram = await AdminAsync(factory.ChattogramId);
        var setup = await CreateSetupAsync(dhaka);
        var item = await CreateAsync<ItemDetail>(dhaka, "items", NewItem(setup));

        var list = await chattogram.GetFromJsonAsync<ItemList>($"{Base}/items?search={item.Code}");
        var direct = await chattogram.GetAsync($"{Base}/items/{item.Id}");
        var crossUnit = await chattogram.PostAsJsonAsync($"{Base}/items", NewItem(setup));

        Assert.Empty(list!.Items);
        Assert.Equal(HttpStatusCode.NotFound, direct.StatusCode);
        Assert.Contains("minorCategoryId", await ErrorsAsync(crossUnit));
    }

    [Fact]
    public async Task Viewer_can_read_items_but_not_change_them()
    {
        var login = await factory.ErpLoginAsync(ApiFactory.ViewerEmail, ApiFactory.TestPassword);
        var viewer = factory.CreateClient(login.AccessToken, factory.DhakaId);

        var itemTypes = await ReadAsync<List<LookupOption>>(await viewer.GetAsync($"/api/erp/lookups/{ItemLookups.ItemType}"));
        var supplyTypes = await ReadAsync<List<LookupOption>>(await viewer.GetAsync($"/api/erp/lookups/{ItemLookups.SupplyType}"));
        Assert.Equal((8, 6), (itemTypes.Count, supplyTypes.Count));
        Assert.Equal("STD", Assert.Single(supplyTypes, o => o.IsDefault).Code);
        Assert.Equal(HttpStatusCode.NotFound, (await viewer.GetAsync("/api/erp/lookups/unknown-list")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await viewer.GetAsync($"{Base}/items")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await viewer.GetAsync($"{Base}/minor-categories/options")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await viewer.PostAsJsonAsync($"{Base}/items/delete", new RecordIdsRequest([1]))).StatusCode);
    }
}
