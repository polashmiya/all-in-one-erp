using System.Net;
using System.Net.Http.Json;
using Erp.Api.Approvals;
using Erp.Api.Panel.Approvals;
using Erp.Api.Procurement;
using Erp.Api.Procurement.PurchaseRequisitions;
using Erp.Infrastructure.Persistence.Seeding;
using Erp.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using static Erp.Tests.Api.ProcurementTestData;

namespace Erp.Tests.Api;

[Collection(ApiCollection.Name)]
public sealed class ApprovalTests(ApiFactory factory)
{
    private const string RequisitionsUrl = "/api/erp/procurement/purchase-requisitions";
    private static readonly string Feature = ProcurementMenu.PurchaseRequisition;
    private static readonly string[] Approve = [$"{Feature}.view", $"{Feature}.approve", "approval.my-approvals.view"];

    [Fact]
    public async Task Levels_are_approved_in_order_by_their_role_or_user()
    {
        var first = await ApproverAsync(factory, factory.DhakaId, Approve);
        var second = await ApproverAsync(factory, factory.DhakaId, Approve);
        var panel = await PanelAsync();
        var levels = await LevelsUrlAsync(factory.DhakaId);

        try
        {
            var saved = await ReadAsync<ApprovalLevelSet>(await panel.PutAsJsonAsync(levels, new SaveApprovalLevelsRequest(
            [
                new ApprovalLevelDto("Store manager", first.RoleId, null),
                new ApprovalLevelDto("Finance", null, second.UserId),
            ])));
            var requisition = await CreateAsync();
            var url = $"{RequisitionsUrl}/{requisition.Id}";

            var early = await second.Client.PostAsJsonAsync($"{url}/approve", new ReviewRequest(null));
            var afterFirst = await ReadAsync<RequisitionDetail>(await first.Client.PostAsJsonAsync($"{url}/approve", new ReviewRequest("OK")));
            var progress = await second.Client.GetFromJsonAsync<ApprovalProgress>($"{url}/approval");
            var waiting = await second.Client.GetFromJsonAsync<ApprovalInbox>("/api/erp/approvals");
            var done = await ReadAsync<RequisitionDetail>(await second.Client.PostAsJsonAsync($"{url}/approve", new ReviewRequest(null)));
            var history = await first.Client.GetFromJsonAsync<ApprovalInbox>("/api/erp/approvals?view=approved");

            Assert.Equal(2, saved.Levels.Count);
            Assert.Equal(HttpStatusCode.Forbidden, early.StatusCode);
            Assert.Equal("pending", afterFirst.Status);
            Assert.Equal(["approved", "pending"], progress!.Steps.Select(s => s.Status));
            Assert.True(progress.CanAct);
            Assert.Contains(waiting!.Items, r => r.Id == requisition.Id && r.Level == 2 && r.Levels == 2);
            Assert.Equal("approved", done.Status);
            Assert.Contains(history!.Items, r => r.Id == requisition.Id && r.StepName == "Store manager");
        }
        finally
        {
            await panel.PutAsJsonAsync(levels, new SaveApprovalLevelsRequest([]));
        }
    }

    [Fact]
    public async Task A_change_starts_the_approval_again_and_a_rejection_ends_it()
    {
        var first = await ApproverAsync(factory, factory.DhakaId, Approve);
        var panel = await PanelAsync();
        var levels = await LevelsUrlAsync(factory.DhakaId);

        try
        {
            await ReadAsync<ApprovalLevelSet>(await panel.PutAsJsonAsync(levels, new SaveApprovalLevelsRequest(
            [
                new ApprovalLevelDto("Store manager", first.RoleId, null),
                new ApprovalLevelDto("Head office", first.RoleId, null),
            ])));
            var admin = await AdminAsync();
            var request = await RequestAsync();
            var requisition = await ReadAsync<RequisitionDetail>(await admin.PostAsJsonAsync(RequisitionsUrl, request));
            var url = $"{RequisitionsUrl}/{requisition.Id}";

            await ReadAsync<RequisitionDetail>(await first.Client.PostAsJsonAsync($"{url}/approve", new ReviewRequest(null)));
            await ReadAsync<RequisitionDetail>(await admin.PutAsJsonAsync(url, request with { Code = requisition.Code, Remarks = "Changed" }));
            var restarted = await admin.GetFromJsonAsync<ApprovalProgress>($"{url}/approval");
            var rejected = await ReadAsync<RequisitionDetail>(
                await first.Client.PostAsJsonAsync($"{url}/reject", new ReviewRequest("Wrong items")));

            Assert.Equal(["pending", "pending"], restarted!.Steps.Select(s => s.Status));
            Assert.Equal(("rejected", "Wrong items"), (rejected.Status, rejected.ReviewComment));
        }
        finally
        {
            await panel.PutAsJsonAsync(levels, new SaveApprovalLevelsRequest([]));
        }
    }

    [Fact]
    public async Task A_unit_uses_the_account_levels_until_it_has_its_own_and_each_level_needs_one_approver()
    {
        var panel = await PanelAsync();
        var account = await LevelsUrlAsync(null);
        var unit = await LevelsUrlAsync(factory.ChattogramId);
        var role = (await panel.GetFromJsonAsync<ApproverOptions>($"{await AccountUrlAsync()}/approvers"))!.Roles[0].Id;

        try
        {
            await ReadAsync<ApprovalLevelSet>(await panel.PutAsJsonAsync(account,
                new SaveApprovalLevelsRequest([new ApprovalLevelDto("Manager", role, null)])));
            var inherited = await panel.GetFromJsonAsync<ApprovalLevelSet>(unit);
            var both = await panel.PutAsJsonAsync(unit, new SaveApprovalLevelsRequest([new ApprovalLevelDto("Odd", role, Guid.NewGuid())]));
            var features = await panel.GetFromJsonAsync<List<ApprovalFeatureOption>>($"{await AccountUrlAsync()}/approval-features");

            Assert.True(inherited!.Inherited);
            Assert.Equal("Manager", Assert.Single(inherited.Levels).Name);
            Assert.Equal(HttpStatusCode.BadRequest, both.StatusCode);
            Assert.Contains(features!, f => f.Code == Feature);
            Assert.Contains(features!, f => f.Code == ProcurementMenu.PurchaseOrder);
        }
        finally
        {
            await panel.PutAsJsonAsync(account, new SaveApprovalLevelsRequest([]));
        }
    }

    private async Task<RequisitionDetail> CreateAsync() =>
        await ReadAsync<RequisitionDetail>(await (await AdminAsync()).PostAsJsonAsync(RequisitionsUrl, await RequestAsync()));

    private async Task<SaveRequisitionRequest> RequestAsync() => new()
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
        Lines = [new SaveRequisitionLine { ItemId = await ItemAsync(factory.DhakaId, 5m), Quantity = 2, Rate = null, Remarks = null }],
    };

    private async Task<HttpClient> AdminAsync()
    {
        var login = await factory.ErpAdminLoginAsync();
        return factory.CreateClient(login.AccessToken, factory.DhakaId);
    }

    private async Task<HttpClient> PanelAsync()
    {
        var login = await factory.PanelLoginAsync(factory.Seed.PanelAdmin.Email, factory.Seed.PanelAdmin.Password);
        return factory.CreateClient(login.AccessToken);
    }

    private static async Task<string> AccountUrlAsync()
    {
        await using var db = ApiFactory.CreateDbContext();
        var id = await db.Accounts.Where(a => a.Code == DemoDataSeeder.AccountCode).Select(a => a.Id).SingleAsync();
        return $"/api/panel/accounts/{id}";
    }

    private static async Task<string> LevelsUrlAsync(Guid? unitId) =>
        $"{await AccountUrlAsync()}/approval-levels?feature={Feature}" + (unitId is { } id ? $"&businessUnitId={id}" : "");
}
