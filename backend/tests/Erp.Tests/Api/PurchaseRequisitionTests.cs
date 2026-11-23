using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Erp.Api.Procurement;
using Erp.Api.Procurement.PurchaseRequisitions;
using Erp.Core.Domain.Inventory;
using Erp.Infrastructure.Persistence;
using Erp.Metadata;
using Erp.Tests.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Erp.Tests.Api;

[Collection(ApiCollection.Name)]
public sealed class PurchaseRequisitionTests(ApiFactory factory)
{
    private const string Url = "/api/erp/procurement/purchase-requisitions";
    private static readonly DateOnly Today = DateOnly.FromDateTime(DateTime.Today);

    [Fact]
    public async Task A_requisition_gets_the_next_number_and_totals_and_shows_only_in_its_unit()
    {
        var admin = await AdminAsync(factory.DhakaId);
        var (first, second) = (await ItemAsync(factory.DhakaId, 10m), await ItemAsync(factory.DhakaId, 4m));
        var tag = Unique();

        var created = await ReadAsync<RequisitionDetail>(await admin.PostAsJsonAsync(Url, await RequisitionAsync(factory.DhakaId) with
        {
            Remarks = tag,
            Lines = [Line(first, 3), Line(second, 2) with { Rate = 5m }],
        }));
        var here = await admin.GetFromJsonAsync<RequisitionList>($"{Url}?search={tag}");
        var chattogram = await (await AdminAsync(factory.ChattogramId)).GetFromJsonAsync<RequisitionList>($"{Url}?search={tag}");

        Assert.StartsWith("PR-", created.Code);
        Assert.Equal("pending", created.Status);
        Assert.Equal([30m, 10m], created.Lines.Select(l => l.Amount));
        Assert.Equal((5m, 40m), (created.TotalQuantity, created.TotalAmount));
        Assert.Equal(2, Assert.Single(here!.Items).ItemCount);
        Assert.Empty(chattogram!.Items);
    }

    [Fact]
    public async Task A_requisition_needs_items_once_each_and_a_warehouse_of_its_unit()
    {
        var admin = await AdminAsync(factory.DhakaId);
        var item = await ItemAsync(factory.DhakaId, 1m);
        var request = await RequisitionAsync(factory.DhakaId);

        var empty = await admin.PostAsJsonAsync(Url, request with { Lines = [] });
        var wrong = await admin.PostAsJsonAsync(Url, request with
        {
            WarehouseId = await WarehouseAsync(factory.ChattogramId),
            Lines = [Line(item, 1), Line(item, 2)],
        });

        Assert.Contains("lines", await ErrorsAsync(empty));
        Assert.Equal(["lines", "warehouseId"], (await ErrorsAsync(wrong)).Order());
    }

    [Fact]
    public async Task A_requisition_is_approved_then_closed_and_is_locked_after_review()
    {
        var admin = await AdminAsync(factory.DhakaId);
        var request = await RequisitionAsync(factory.DhakaId) with { Lines = [Line(await ItemAsync(factory.DhakaId, 2m), 1)] };
        var created = await ReadAsync<RequisitionDetail>(await admin.PostAsJsonAsync(Url, request));

        var approved = await ReadAsync<RequisitionDetail>(
            await admin.PostAsJsonAsync($"{Url}/{created.Id}/approve", new ReviewRequisitionRequest("Go ahead")));
        var edit = await admin.PutAsJsonAsync($"{Url}/{created.Id}", request);
        var delete = await admin.DeleteAsync($"{Url}/{created.Id}");
        var closed = await ReadAsync<RequisitionDetail>(await admin.PostAsync($"{Url}/{created.Id}/close", null));

        Assert.Equal(("approved", "Go ahead"), (approved.Status, approved.ReviewComment));
        Assert.NotNull(approved.ReviewedByName);
        Assert.Equal(HttpStatusCode.Conflict, edit.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, delete.StatusCode);
        Assert.Equal("closed", closed.Status);
    }

    [Fact]
    public async Task A_rejection_needs_a_reason_and_a_rejected_requisition_can_be_deleted()
    {
        var admin = await AdminAsync(factory.DhakaId);
        var request = await RequisitionAsync(factory.DhakaId) with { Lines = [Line(await ItemAsync(factory.DhakaId, 2m), 1)] };
        var created = await ReadAsync<RequisitionDetail>(await admin.PostAsJsonAsync(Url, request));

        var noReason = await admin.PostAsJsonAsync($"{Url}/{created.Id}/reject", new ReviewRequisitionRequest(" "));
        var rejected = await ReadAsync<RequisitionDetail>(
            await admin.PostAsJsonAsync($"{Url}/{created.Id}/reject", new ReviewRequisitionRequest("Not in budget")));
        var deleted = await admin.DeleteAsync($"{Url}/{created.Id}");

        Assert.Equal(["comment"], await ErrorsAsync(noReason));
        Assert.Equal("rejected", rejected.Status);
        Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await admin.GetAsync($"{Url}/{created.Id}")).StatusCode);
    }

    [Fact]
    public async Task A_requisition_follows_the_configuration_and_keeps_custom_fields()
    {
        var admin = await AdminAsync(factory.DhakaId);
        var request = await RequisitionAsync(factory.DhakaId) with { Remarks = "Keep me", Lines = [Line(await ItemAsync(factory.DhakaId, 1m), 1)] };
        var existing = await ReadAsync<RequisitionDetail>(await admin.PostAsJsonAsync(Url, request));
        await using var settings = await FieldSettingsScope.ApplyAsync(factory.DhakaId, ProcurementMenu.PurchaseRequisition,
            ("remarks", s => s.IsVisible = false),
            ("dueDate", s => s.IsRequired = true));
        await using var fields = await CustomFieldScope.ApplyAsync(factory.DhakaId, ProcurementMenu.PurchaseRequisition,
            ("budgetCode", FieldTypes.Text, null));

        var missing = await admin.PostAsJsonAsync(Url, request);
        var updated = await ReadAsync<RequisitionDetail>(await admin.PutAsJsonAsync($"{Url}/{existing.Id}", request with
        {
            Code = existing.Code,
            Remarks = null,
            DueDate = Today.AddDays(7),
            CustomData = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>("""{"budgetCode":"B-7"}"""),
        }));

        Assert.Contains("dueDate", await ErrorsAsync(missing));
        Assert.Equal("Keep me", updated.Remarks);
        Assert.Equal("B-7", updated.CustomData!["budgetCode"].GetString());
    }

    private async Task<HttpClient> AdminAsync(Guid unitId)
    {
        var login = await factory.ErpAdminLoginAsync();
        return factory.CreateClient(login.AccessToken, unitId);
    }

    private static async Task<SaveRequisitionRequest> RequisitionAsync(Guid unitId) => new()
    {
        Code = null,
        RequestDate = Today,
        DueDate = null,
        WarehouseId = await WarehouseAsync(unitId),
        RequestTypeId = null,
        PurchaseMethodId = null,
        ItemTypeId = null,
        DepartmentId = null,
        SectionId = null,
        EmployeeId = null,
        BuyerId = null,
        ProjectName = null,
        Remarks = null,
        Lines = [],
    };

    private static SaveRequisitionLine Line(int itemId, decimal quantity) => new()
    {
        ItemId = itemId,
        Quantity = quantity,
        Rate = null,
        Remarks = null,
    };

    private static async Task<Guid> WarehouseAsync(Guid unitId)
    {
        await using var db = ApiFactory.CreateDbContext();
        return await db.Warehouses.AcrossTenants().Where(w => w.BusinessUnitId == unitId && w.IsActive).Select(w => w.Id).FirstAsync();
    }

    private static async Task<int> ItemAsync(Guid unitId, decimal cost)
    {
        await using var db = ApiFactory.CreateDbContext();
        var minor = await db.ItemMinorCategories.AcrossTenants()
            .Where(m => m.BusinessUnitId == unitId && m.IsActive)
            .Select(m => new { m.Id, m.AccountId, m.CategoryId, m.SubCategoryId, DepartmentId = db.ItemCategories.AcrossTenants().Where(c => c.Id == m.CategoryId).Select(c => c.DepartmentId).First() })
            .FirstAsync();
        var uom = await db.UnitsOfMeasure.AcrossTenants().Where(u => u.AccountId == minor.AccountId).FirstAsync();
        var code = Unique();
        var item = new Item
        {
            AccountId = minor.AccountId,
            BusinessUnitId = unitId,
            Code = code,
            BarCode = code,
            Name = $"Item {code}",
            DepartmentId = minor.DepartmentId,
            CategoryId = minor.CategoryId,
            SubCategoryId = minor.SubCategoryId,
            MinorCategoryId = minor.Id,
            UomId = uom.Id,
            UomName = uom.Name,
            LastCostPrice = cost,
        };
        db.Items.Add(item);
        await db.SaveChangesAsync();
        return item.Id;
    }

    private static async Task<T> ReadAsync<T>(HttpResponseMessage response, HttpStatusCode expected = HttpStatusCode.OK)
    {
        Assert.Equal(expected, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<T>())!;
    }

    private static async Task<IEnumerable<string>> ErrorsAsync(HttpResponseMessage response) =>
        (await ReadAsync<ValidationProblemDetails>(response, HttpStatusCode.BadRequest)).Errors.Keys;

    private static string Unique() => Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();
}
