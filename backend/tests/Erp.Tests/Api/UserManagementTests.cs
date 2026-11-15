using System.Net;
using System.Net.Http.Json;
using Erp.Api.Auth;
using Erp.Api.UserManagement.Employees;
using Erp.Api.UserManagement.Roles;
using Erp.Api.UserManagement.Users;
using Erp.Infrastructure.Persistence;
using Erp.Infrastructure.Persistence.Seeding;
using Erp.Modules.Inventory;
using Erp.Tests.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Erp.Tests.Api;

[Collection(ApiCollection.Name)]
public sealed class UserManagementTests(ApiFactory factory)
{
    private const string EmployeesUrl = "/api/erp/user-management/employees";
    private const string UsersUrl = "/api/erp/user-management/users";
    private const string RolesUrl = "/api/erp/user-management/roles";

    [Fact]
    public async Task An_employee_saved_with_is_user_gets_a_code_and_a_user_without_a_role()
    {
        var admin = await AdminAsync(factory.DhakaId);
        var username = Unique("emp");

        var response = await admin.PostAsJsonAsync(EmployeesUrl, Employee(factory.DhakaId) with
        {
            Email = $"{username}@demopharma.com",
            User = new EmployeeUserInput(username, ApiFactory.TestPassword),
        });
        var employee = (await response.Content.ReadFromJsonAsync<EmployeeDetail>())!;
        var users = await admin.GetFromJsonAsync<UserList>($"{UsersUrl}?search={username}");

        Assert.StartsWith("EMP-", employee.Code);
        Assert.Equal(username, employee.Username);
        var user = Assert.Single(users!.Items);
        Assert.Null(user.RoleId);
        Assert.Equal(employee.Id, user.EmployeeId);
        Assert.True((await admin.GetFromJsonAsync<UserDetail>($"{UsersUrl}/{user.Id}"))!.MustChangePassword);
    }

    [Fact]
    public async Task The_employee_list_shows_only_the_selected_units_employees()
    {
        var admin = await AdminAsync(factory.ChattogramId);
        var created = await admin.PostAsJsonAsync(EmployeesUrl, Employee(factory.ChattogramId));
        var code = (await created.Content.ReadFromJsonAsync<EmployeeDetail>())!.Code;

        var dhaka = await (await AdminAsync(factory.DhakaId)).GetFromJsonAsync<EmployeeList>($"{EmployeesUrl}?search={code}");
        var chattogram = await admin.GetFromJsonAsync<EmployeeList>($"{EmployeesUrl}?search={code}");

        Assert.Empty(dhaka!.Items);
        Assert.Single(chattogram!.Items);
    }

    [Fact]
    public async Task An_employee_rejects_a_value_from_the_wrong_list_and_an_office_of_another_unit()
    {
        var admin = await AdminAsync(factory.DhakaId);
        await using var db = ApiFactory.CreateDbContext();
        var gender = await db.LookupValues.AcrossTenants()
            .Where(v => db.Accounts.Any(a => a.Id == v.AccountId && a.Code == DemoDataSeeder.AccountCode) && v.LookupType!.Key == EmployeeLookups.Gender)
            .Select(v => v.Id)
            .FirstAsync();
        var office = await db.Offices.AcrossTenants().Where(o => o.BusinessUnitId == factory.ChattogramId).Select(o => o.Id).FirstAsync();

        var response = await admin.PostAsJsonAsync(EmployeesUrl, Employee(factory.DhakaId) with { DesignationId = gender, OfficeId = office });
        var errors = (await response.Content.ReadFromJsonAsync<ValidationProblemDetails>())!.Errors;

        Assert.Contains("designationId", errors.Keys);
        Assert.Contains("officeId", errors.Keys);
    }

    [Fact]
    public async Task A_user_made_for_two_units_gets_a_role_in_one_unit_only()
    {
        var dhaka = await AdminAsync(factory.DhakaId);
        var chattogram = await AdminAsync(factory.ChattogramId);
        var role = await CreateRoleAsync(dhaka, [$"{ItemManagementMenu.ItemProfile}.view"]);
        var user = await CreateUserAsync(dhaka, [factory.DhakaId, factory.ChattogramId]);

        var set = await dhaka.PutAsJsonAsync($"{UsersUrl}/{user.User.Id}/role", new UserRoleRequest(role.Id));
        var inDhaka = await dhaka.GetFromJsonAsync<UserDetail>($"{UsersUrl}/{user.User.Id}");
        var noRoleInChattogram = await chattogram.GetFromJsonAsync<UserList>($"{UsersUrl}?role=none&search={user.User.Username}");

        Assert.Equal(HttpStatusCode.NoContent, set.StatusCode);
        Assert.Equal(role.Id, inDhaka!.User.RoleId);
        Assert.Equal(2, inDhaka.Units.Count);
        Assert.Single(noRoleInChattogram!.Items);
    }

    [Fact]
    public async Task Turning_a_user_off_in_a_unit_takes_that_unit_away()
    {
        var admin = await AdminAsync(factory.DhakaId);
        var role = await CreateRoleAsync(admin, [$"{ItemManagementMenu.ItemProfile}.view"]);
        var user = await CreateUserAsync(admin, [factory.DhakaId]);
        await admin.PutAsJsonAsync($"{UsersUrl}/{user.User.Id}/role", new UserRoleRequest(role.Id));

        var off = await admin.PutAsJsonAsync($"{UsersUrl}/{user.User.Id}/status", new UserStatusRequest(false));
        var login = await factory.CreateClient().PostAsJsonAsync("/api/erp/auth/login", new LoginRequest(user.User.Username, ApiFactory.TestPassword));

        Assert.Equal(HttpStatusCode.NoContent, off.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, login.StatusCode);
    }

    [Fact]
    public async Task Account_admins_are_read_only_and_nobody_changes_their_own_access()
    {
        var admin = await AdminAsync(factory.DhakaId);
        var me = (await admin.GetFromJsonAsync<UserList>($"{UsersUrl}?search={factory.Seed.ErpAdmin.Email}"))!.Items.Single();

        var update = await admin.PutAsJsonAsync($"{UsersUrl}/{me.Id}", new SaveUserRequest("New name", me.Username, me.Email, null, null));
        var status = await admin.PutAsJsonAsync($"{UsersUrl}/{me.Id}/status", new UserStatusRequest(false));

        Assert.True(me.IsAccountAdmin);
        Assert.Equal(HttpStatusCode.BadRequest, update.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, status.StatusCode);
    }

    [Fact]
    public async Task A_new_user_needs_unique_sign_in_details_and_units_you_may_add_to()
    {
        var admin = await AdminAsync(factory.DhakaId);
        var first = await CreateUserAsync(admin, [factory.DhakaId]);

        var duplicate = await admin.PostAsJsonAsync(UsersUrl, NewUser(first.User.Username, [factory.DhakaId]));
        var otherAccount = await admin.PostAsJsonAsync(UsersUrl, NewUser(Unique("u"), [factory.OtherAccountUnitId]));

        Assert.Contains("username", (await duplicate.Content.ReadFromJsonAsync<ValidationProblemDetails>())!.Errors.Keys);
        Assert.Contains("businessUnitIds", (await otherAccount.Content.ReadFromJsonAsync<ValidationProblemDetails>())!.Errors.Keys);
    }

    private static string Unique(string prefix) => $"{prefix}.{Guid.NewGuid():N}"[..20];

    private static CreateUserRequest NewUser(string username, Guid[] units) =>
        new("Test User", username, $"{username}@demopharma.com", null, null, ApiFactory.TestPassword, units);

    private static async Task<UserDetail> CreateUserAsync(HttpClient admin, Guid[] units)
    {
        var response = await admin.PostAsJsonAsync(UsersUrl, NewUser(Unique("u"), units));
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<UserDetail>())!;
    }

    private static async Task<RoleDetail> CreateRoleAsync(HttpClient admin, string[] permissions)
    {
        var response = await admin.PostAsJsonAsync(RolesUrl, new SaveRoleRequest(Unique("Role"), null, true, permissions));
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<RoleDetail>())!;
    }

    private async Task<HttpClient> AdminAsync(Guid unitId)
    {
        var login = await factory.ErpAdminLoginAsync();
        return factory.CreateClient(login.AccessToken, unitId);
    }

    private static SaveEmployeeRequest Employee(Guid unitId) => new()
    {
        Code = null,
        Name = "Test Employee",
        NameBn = null,
        ImageSource = null,
        DesignationId = null,
        DepartmentId = null,
        DepartmentSectionId = null,
        EmployeeTypeId = null,
        Mobile = null,
        Email = null,
        OfficialContact = null,
        EmergencyMobile = null,
        FatherName = null,
        MotherName = null,
        DateOfBirth = null,
        GenderId = null,
        ReligionId = null,
        BloodGroupId = null,
        Nid = null,
        PresentAddress = null,
        PermanentAddress = null,
        SignatureImage = null,
        HomeBusinessUnitId = unitId,
        OfficeId = null,
        WarehouseId = null,
        Location = null,
        JoiningDate = null,
        ConfirmationDate = null,
        TerminationDate = null,
        SupervisorId = null,
        LineManagerId = null,
        AttendanceId = null,
        CardNumber = null,
        IsSalesRepresentative = false,
        IsServiceStaff = false,
        IsActive = true,
    };
}
