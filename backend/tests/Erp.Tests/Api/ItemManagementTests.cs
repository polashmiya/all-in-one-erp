using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Erp.Metadata;
using Erp.Modules.Inventory;
using Erp.Modules.Inventory.ItemManagement;
using Erp.Modules.Inventory.ItemManagement.Categories;
using Erp.Modules.Inventory.ItemManagement.Departments;
using Erp.Modules.Inventory.ItemManagement.MinorCategories;
using Erp.Modules.Inventory.ItemManagement.SubCategories;
using Erp.Modules.Inventory.ItemManagement.Uoms;
using Erp.Tests.Infrastructure;
using Microsoft.AspNetCore.Mvc;

namespace Erp.Tests.Api;

[Collection(ApiCollection.Name)]
public sealed class ItemManagementTests(ApiFactory factory)
{
    private const string Base = "/api/erp/item-management";

    private const string TinyPng =
        "data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNkYPhfDwAChwGA60e6kgAAAABJRU5ErkJggg==";

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

    private static async Task<ItemDepartmentRow> CreateDepartmentAsync(HttpClient client, string? code = null, string? name = null) =>
        await ReadAsync<ItemDepartmentRow>(
            await client.PostAsJsonAsync($"{Base}/departments",
                new SaveItemDepartmentRequest(code ?? Unique(), name ?? $"Dept {Unique()}", null, null, true)),
            HttpStatusCode.Created);

    private static async Task<ItemCategoryRow> CreateCategoryAsync(HttpClient client, int departmentId, string? name = null) =>
        await ReadAsync<ItemCategoryRow>(
            await client.PostAsJsonAsync($"{Base}/categories",
                new SaveItemCategoryRequest(departmentId, Unique(3), name ?? $"Cat {Unique()}", null, true)),
            HttpStatusCode.Created);

    private static async Task<ItemSubCategoryRow> CreateSubCategoryAsync(HttpClient client, int categoryId) =>
        await ReadAsync<ItemSubCategoryRow>(
            await client.PostAsJsonAsync($"{Base}/sub-categories",
                new SaveItemSubCategoryRequest(categoryId, $"Sub {Unique()}", null, true)),
            HttpStatusCode.Created);

    [Fact]
    public async Task Department_create_edit_status_and_delete_round_trip()
    {
        var client = await AdminAsync();
        var code = $"d{Unique(5)}".ToLowerInvariant();

        var created = await ReadAsync<ItemDepartmentRow>(
            await client.PostAsJsonAsync($"{Base}/departments", new SaveItemDepartmentRequest($" {code} ", $"  Pharma {code} ", null, TinyPng, true)),
            HttpStatusCode.Created);

        Assert.Equal(code.ToUpperInvariant(), created.Code);
        Assert.Equal($"Pharma {code}", created.Name);
        Assert.NotNull(created.Serial);
        Assert.Equal(TinyPng, created.ImageSource);
        Assert.Equal(DateTimeKind.Utc, created.CreatedAt!.Value.Kind);

        var updated = await ReadAsync<ItemDepartmentRow>(
            await client.PutAsJsonAsync($"{Base}/departments/{created.Id}", new SaveItemDepartmentRequest(code, $"Medicine {code}", 7, null, true)));
        Assert.Equal(($"Medicine {code}", 7, null), (updated.Name, updated.Serial, updated.ImageSource));
        Assert.NotNull(updated.ModifiedAt);

        var status = await client.PostAsJsonAsync($"{Base}/departments/status", new RecordStatusRequest([created.Id], false));
        Assert.Equal(1, (await ReadAsync<AffectedRecords>(status)).Count);

        var inactive = await client.GetFromJsonAsync<ItemDepartmentList>($"{Base}/departments?status=inactive&search={code}");
        Assert.Equal(created.Id, Assert.Single(inactive!.Items).Id);
        Assert.Equal((1, 0, 1), (inactive.Counts.All, inactive.Counts.Active, inactive.Counts.Inactive));

        Assert.Equal(HttpStatusCode.OK, (await client.DeleteAsync($"{Base}/departments/{created.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"{Base}/departments/{created.Id}")).StatusCode);
    }

    [Fact]
    public async Task Master_data_screens_follow_the_configuration_and_keep_custom_fields()
    {
        var client = await AdminAsync();
        var existing = await CreateDepartmentAsync(client);
        await using var settings = await FieldSettingsScope.ApplyAsync(factory.DhakaId, ItemManagementMenu.ItemDepartment,
            ("serial", s => s.IsVisible = false));
        await using var fields = await CustomFieldScope.ApplyAsync(factory.DhakaId, ItemManagementMenu.ItemDepartment,
            ("headName", FieldTypes.Text, null));
        await using var uomSettings = await FieldSettingsScope.ApplyAsync(factory.DhakaId, ItemManagementMenu.Uom,
            ("description", s => s.IsRequired = true));

        var created = await ReadAsync<ItemDepartmentRow>(await client.PostAsJsonAsync($"{Base}/departments",
            new SaveItemDepartmentRequest(Unique(), $"Dept {Unique()}", 1, null, true, Data("""{"headName":"Rahim"}"""))),
            HttpStatusCode.Created);
        Assert.NotEqual(1, created.Serial);
        Assert.Equal("Rahim", created.CustomData!["headName"].GetString());

        var wrongType = await ReadAsync<ValidationProblemDetails>(await client.PutAsJsonAsync($"{Base}/departments/{existing.Id}",
            new SaveItemDepartmentRequest(existing.Code, existing.Name, 999, null, true, Data("""{"headName":5}"""))),
            HttpStatusCode.BadRequest);
        Assert.Equal(["customData.headName"], wrongType.Errors.Keys);

        var kept = await ReadAsync<ItemDepartmentRow>(await client.PutAsJsonAsync($"{Base}/departments/{existing.Id}",
            new SaveItemDepartmentRequest(existing.Code, existing.Name, 999, null, true)));
        Assert.Equal(existing.Serial, kept.Serial);

        var uom = await client.PostAsJsonAsync($"{Base}/uoms", new SaveUomRequest(Unique(5), $"Unit {Unique()}", null, false, null, true));
        Assert.Equal(HttpStatusCode.BadRequest, uom.StatusCode);
    }

    private static Dictionary<string, JsonElement> Data(string json) =>
        JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json)!;

    [Fact]
    public async Task Demo_account_starts_with_item_master_data()
    {
        var dhaka = await AdminAsync(factory.DhakaId);
        var chattogram = await AdminAsync(factory.ChattogramId);

        var pharma = await dhaka.GetFromJsonAsync<ItemDepartmentList>($"{Base}/departments?search=PHARMA");
        var herbal = await dhaka.GetFromJsonAsync<ItemDepartmentList>($"{Base}/departments?search=HERB&status=inactive");
        var film = await dhaka.GetFromJsonAsync<ItemMinorCategoryList>($"{Base}/minor-categories?search=Antibiotic&pageSize=100");
        var chattogramCodes = (await chattogram.GetFromJsonAsync<List<ItemLookup>>($"{Base}/departments/options"))!.Select(d => d.Code);
        var kg = await chattogram.GetFromJsonAsync<UomList>($"{Base}/uoms?search=Kilogram");

        Assert.Equal(6, Assert.Single(pharma!.Items).Categories);
        Assert.Single(herbal!.Items);
        Assert.Contains(film!.Items, m => m.SubCategoryName == "Film coated" && m.CategoryName == "Tablet" && m.DepartmentName == "Pharmaceuticals");
        Assert.Contains("PHARMA", chattogramCodes);
        Assert.DoesNotContain("OTC", chattogramCodes);
        Assert.Equal((true, 3), (Assert.Single(kg!.Items).IsDecimalAllowed, kg.Items[0].RoundingLength));
    }

    [Fact]
    public async Task Duplicate_department_code_and_name_are_field_errors()
    {
        var client = await AdminAsync();
        var existing = await CreateDepartmentAsync(client);

        var response = await client.PostAsJsonAsync($"{Base}/departments",
            new SaveItemDepartmentRequest(existing.Code.ToLowerInvariant(), existing.Name.ToUpperInvariant(), null, null, true));

        var problem = await ReadAsync<ValidationProblemDetails>(response, HttpStatusCode.BadRequest);
        Assert.Contains("code", problem.Errors.Keys);
        Assert.Contains("name", problem.Errors.Keys);
    }

    [Fact]
    public async Task Invalid_input_is_rejected_before_saving()
    {
        var client = await AdminAsync();

        var department = await client.PostAsJsonAsync($"{Base}/departments",
            new SaveItemDepartmentRequest("BAD CODE", "", -1, "data:text/html;base64,PGgxPg==", true));
        var category = await client.PostAsJsonAsync($"{Base}/categories",
            new SaveItemCategoryRequest(0, "TOOLONG", "Name", null, true));

        var departmentErrors = (await ReadAsync<ValidationProblemDetails>(department, HttpStatusCode.BadRequest)).Errors.Keys;
        Assert.Equal(["code", "imageSource", "name", "serial"], departmentErrors.Order());
        var categoryErrors = (await ReadAsync<ValidationProblemDetails>(category, HttpStatusCode.BadRequest)).Errors.Keys;
        Assert.Equal(["code", "departmentId"], categoryErrors.Order());
    }

    [Fact]
    public async Task Hierarchy_derives_parents_and_blocks_deleting_used_records()
    {
        var client = await AdminAsync();
        var department = await CreateDepartmentAsync(client);
        var category = await CreateCategoryAsync(client, department.Id);
        var subCategory = await CreateSubCategoryAsync(client, category.Id);

        var minor = await ReadAsync<ItemMinorCategoryRow>(
            await client.PostAsJsonAsync($"{Base}/minor-categories", new SaveItemMinorCategoryRequest(subCategory.Id, "Film coated", null, true)),
            HttpStatusCode.Created);

        Assert.Equal((department.Id, department.Name), (subCategory.DepartmentId, subCategory.DepartmentName));
        Assert.Equal((department.Id, category.Id, subCategory.Id), (minor.DepartmentId, minor.CategoryId, minor.SubCategoryId));

        var blocked = await client.DeleteAsync($"{Base}/departments/{department.Id}");
        var problem = await ReadAsync<ProblemDetails>(blocked, HttpStatusCode.Conflict);
        Assert.Contains("1 category", problem.Detail);
        Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsJsonAsync($"{Base}/sub-categories/delete", new RecordIdsRequest([subCategory.Id]))).StatusCode);

        var departments = await client.GetFromJsonAsync<ItemDepartmentList>($"{Base}/departments?search={department.Code}");
        Assert.Equal(1, Assert.Single(departments!.Items).Categories);

        var filtered = await client.GetFromJsonAsync<ItemMinorCategoryList>($"{Base}/minor-categories?departmentId={department.Id}");
        Assert.Equal(minor.Id, Assert.Single(filtered!.Items).Id);
    }

    [Fact]
    public async Task Moving_a_category_or_sub_category_moves_its_children()
    {
        var client = await AdminAsync();
        var from = await CreateDepartmentAsync(client);
        var to = await CreateDepartmentAsync(client);
        var category = await CreateCategoryAsync(client, from.Id);
        var otherCategory = await CreateCategoryAsync(client, to.Id);
        var subCategory = await CreateSubCategoryAsync(client, category.Id);
        var minor = await ReadAsync<ItemMinorCategoryRow>(
            await client.PostAsJsonAsync($"{Base}/minor-categories", new SaveItemMinorCategoryRequest(subCategory.Id, $"Minor {Unique()}", null, true)),
            HttpStatusCode.Created);

        var moved = await client.PutAsJsonAsync($"{Base}/categories/{category.Id}",
            new SaveItemCategoryRequest(to.Id, category.Code, category.Name, null, true));
        Assert.Equal(to.Id, (await ReadAsync<ItemCategoryRow>(moved)).DepartmentId);
        Assert.Equal(to.Id, (await client.GetFromJsonAsync<ItemSubCategoryRow>($"{Base}/sub-categories/{subCategory.Id}"))!.DepartmentId);

        var movedSub = await client.PutAsJsonAsync($"{Base}/sub-categories/{subCategory.Id}",
            new SaveItemSubCategoryRequest(otherCategory.Id, subCategory.Name, null, true));
        Assert.Equal(otherCategory.Id, (await ReadAsync<ItemSubCategoryRow>(movedSub)).CategoryId);
        Assert.Equal(otherCategory.Id, (await client.GetFromJsonAsync<ItemMinorCategoryRow>($"{Base}/minor-categories/{minor.Id}"))!.CategoryId);
    }

    [Fact]
    public async Task Inactive_parents_cannot_take_new_children()
    {
        var client = await AdminAsync();
        var department = await CreateDepartmentAsync(client);
        await client.PostAsJsonAsync($"{Base}/departments/status", new RecordStatusRequest([department.Id], false));

        var response = await client.PostAsJsonAsync($"{Base}/categories",
            new SaveItemCategoryRequest(department.Id, Unique(3), "Late", null, true));

        Assert.Contains("departmentId", (await ReadAsync<ValidationProblemDetails>(response, HttpStatusCode.BadRequest)).Errors.Keys);
    }

    [Fact]
    public async Task Classification_data_belongs_to_one_business_unit()
    {
        var dhaka = await AdminAsync(factory.DhakaId);
        var chattogram = await AdminAsync(factory.ChattogramId);
        var department = await CreateDepartmentAsync(dhaka);

        var list = await chattogram.GetFromJsonAsync<ItemDepartmentList>($"{Base}/departments?search={department.Code}");
        var options = await chattogram.GetFromJsonAsync<List<ItemLookup>>($"{Base}/departments/options");
        var direct = await chattogram.GetAsync($"{Base}/departments/{department.Id}");
        var category = await chattogram.PostAsJsonAsync($"{Base}/categories",
            new SaveItemCategoryRequest(department.Id, Unique(3), "Cross unit", null, true));

        Assert.Empty(list!.Items);
        Assert.DoesNotContain(options!, o => o.Id == department.Id);
        Assert.Equal(HttpStatusCode.NotFound, direct.StatusCode);
        Assert.Contains("departmentId", (await ReadAsync<ValidationProblemDetails>(category, HttpStatusCode.BadRequest)).Errors.Keys);
        Assert.NotNull(await CreateDepartmentAsync(chattogram, department.Code, department.Name));
    }

    [Fact]
    public async Task Viewer_can_read_but_not_change_item_master_data()
    {
        var login = await factory.ErpLoginAsync(ApiFactory.ViewerEmail, ApiFactory.TestPassword);
        var viewer = factory.CreateClient(login.AccessToken, factory.DhakaId);

        Assert.Equal(HttpStatusCode.OK, (await viewer.GetAsync($"{Base}/categories")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await viewer.GetAsync($"{Base}/uoms/options")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await viewer.PostAsJsonAsync($"{Base}/uoms",
            new SaveUomRequest("PCS", "Pieces", null, false, null, true))).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await viewer.DeleteAsync($"{Base}/departments/1")).StatusCode);
    }

    [Fact]
    public async Task Account_without_the_features_gets_403()
    {
        var login = await factory.ErpLoginAsync(ApiFactory.OtherAdminEmail, ApiFactory.TestPassword);
        var other = factory.CreateClient(login.AccessToken, factory.OtherAccountUnitId);

        Assert.Equal(HttpStatusCode.Forbidden, (await other.GetAsync($"{Base}/departments")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await other.GetAsync($"{Base}/departments/options")).StatusCode);
    }

    [Fact]
    public async Task Uoms_are_shared_by_the_accounts_business_units_and_keep_decimal_rules()
    {
        var dhaka = await AdminAsync(factory.DhakaId);
        var chattogram = await AdminAsync(factory.ChattogramId);
        var code = $"u{Unique(4)}".ToLowerInvariant();

        var missingDecimals = await dhaka.PostAsJsonAsync($"{Base}/uoms", new SaveUomRequest(code, $"Kilo {code}", null, true, null, true));
        Assert.Contains("roundingLength", (await ReadAsync<ValidationProblemDetails>(missingDecimals, HttpStatusCode.BadRequest)).Errors.Keys);

        var created = await ReadAsync<UomRow>(
            await dhaka.PostAsJsonAsync($"{Base}/uoms", new SaveUomRequest(code, $"Kilo {code}", " Weight ", true, 3, false)),
            HttpStatusCode.Created);
        Assert.Equal((code, "Weight", 3, false), (created.Code, created.Description, created.RoundingLength, created.IsActive));

        var whole = await ReadAsync<UomRow>(
            await dhaka.PutAsJsonAsync($"{Base}/uoms/{created.Id}", new SaveUomRequest(code, created.Name, null, false, 4, true)));
        Assert.Equal((false, 0, true), (whole.IsDecimalAllowed, whole.RoundingLength, whole.IsActive));

        var fromChattogram = await chattogram.GetFromJsonAsync<UomList>($"{Base}/uoms?search={code}");
        Assert.Equal(created.Id, Assert.Single(fromChattogram!.Items).Id);

        var duplicate = await chattogram.PostAsJsonAsync($"{Base}/uoms", new SaveUomRequest(code.ToUpperInvariant(), "Other", null, false, null, true));
        Assert.Contains("code", (await ReadAsync<ValidationProblemDetails>(duplicate, HttpStatusCode.BadRequest)).Errors.Keys);
    }

    [Fact]
    public async Task Bulk_status_and_delete_are_all_or_nothing()
    {
        var client = await AdminAsync();
        var first = await CreateDepartmentAsync(client);
        var second = await CreateDepartmentAsync(client);

        var missing = await client.PostAsJsonAsync($"{Base}/departments/status", new RecordStatusRequest([first.Id, int.MaxValue], false));
        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
        Assert.True((await client.GetFromJsonAsync<ItemDepartmentRow>($"{Base}/departments/{first.Id}"))!.IsActive);

        var deleted = await client.PostAsJsonAsync($"{Base}/departments/delete", new RecordIdsRequest([first.Id, second.Id]));
        Assert.Equal(2, (await ReadAsync<AffectedRecords>(deleted)).Count);
    }
}
