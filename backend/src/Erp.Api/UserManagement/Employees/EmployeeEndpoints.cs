using Erp.Api.Auth;
using Erp.Api.Panel.Configuration;
using Erp.Api.Inventory;
using Erp.Api.PartnerManagement;
using Erp.Api.Procurement;
using Erp.Api.Sales;
using Erp.Api.Users;
using Erp.Core.Authorization;
using Erp.Core.Domain.Identity;
using Erp.Core.Security;
using Erp.Core.Tenancy;
using Erp.Core.Validation;
using Erp.Infrastructure.Metadata;
using Erp.Infrastructure.Persistence;
using Erp.Metadata;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace Erp.Api.UserManagement.Employees;

internal static class EmployeeEndpoints
{
    private const string Feature = UserManagementMenu.Employee;
    private const string CodePrefix = "EMP-";
    private const int CodeDigits = 5;
    private const int DefaultPageSize = 25;
    private const int MaxPageSize = 1000;
    private const string ChooseFromList = "Choose an active value from the list.";

    public static void Map(IEndpointRouteBuilder group)
    {
        var employees = group.MapGroup("/employees");

        employees.MapGet("", List)
            .WithName("ListEmployees")
            .WithSummary("Employees whose home unit is the selected business unit")
            .RequirePermission($"{Feature}.view");

        employees.MapGet("/options", Options)
            .WithName("GetEmployeeOptions")
            .WithSummary("Every employee of the account, for dropdowns")
            .RequireAnyPermission([$"{Feature}.view", $"{UserManagementMenu.User}.create", $"{UserManagementMenu.User}.edit", $"{PartnerManagementMenu.Customer}.view", .. ProcurementModule.Editors, .. SalesModule.Editors]);

        employees.MapGet("/{id:int}", Get)
            .WithName("GetEmployee")
            .RequirePermission($"{Feature}.view")
            .ProducesProblem(StatusCodes.Status404NotFound);

        employees.MapPost("", Create)
            .WithName("CreateEmployee")
            .WithSummary("Create an employee; with `user` it also creates the employee's ERP user")
            .RequirePermission($"{Feature}.create")
            .Validate<SaveEmployeeRequest>();

        employees.MapPut("/{id:int}", Update)
            .WithName("UpdateEmployee")
            .RequirePermission($"{Feature}.edit")
            .Validate<SaveEmployeeRequest>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        employees.MapPost("/status", SetStatus)
            .WithName("SetEmployeesStatus")
            .RequirePermission($"{Feature}.edit")
            .Validate<EmployeeStatusRequest>();

        var structure = group.MapGroup("/structure");

        structure.MapGet("/offices", Offices)
            .WithName("GetOfficeOptions")
            .WithSummary("Offices of the account, each with its business unit as parent")
            .RequireAnyPermission([$"{Feature}.view", $"{UserManagementMenu.User}.view", .. StockModule.Readers, .. SalesModule.Editors]);

        structure.MapGet("/warehouses", Warehouses)
            .WithName("GetWarehouseOptions")
            .WithSummary("Warehouses of the account, each with its business unit as parent")
            .RequireAnyPermission([$"{Feature}.view", $"{UserManagementMenu.User}.view", .. ProcurementModule.Editors, .. StockModule.Readers]);
    }

    private static async Task<EmployeeList> List(
        string? search,
        string? status,
        int? designationId,
        int? departmentId,
        Guid? officeId,
        string? user,
        string? sort,
        int? page,
        int? pageSize,
        ICurrentContext current,
        ErpDbContext db,
        CancellationToken cancellationToken)
    {
        var employees = db.Employees.AsNoTracking().Where(e => e.HomeBusinessUnitId == current.BusinessUnitId);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            employees = employees.Where(e => e.Name.Contains(term) || e.Code.Contains(term) ||
                (e.NameBn != null && e.NameBn.Contains(term)) || (e.Mobile != null && e.Mobile.Contains(term)));
        }

        if (designationId is not null) employees = employees.Where(e => e.DesignationId == designationId);
        if (departmentId is not null) employees = employees.Where(e => e.DepartmentId == departmentId);
        if (officeId is not null) employees = employees.Where(e => e.OfficeId == officeId);
        employees = user switch
        {
            "yes" => employees.Where(e => db.ErpUsers.Any(u => u.EmployeeId == e.Id)),
            "no" => employees.Where(e => !db.ErpUsers.Any(u => u.EmployeeId == e.Id)),
            _ => employees,
        };

        var counts = new EmployeeCounts(
            await employees.CountAsync(cancellationToken),
            await employees.CountAsync(e => e.IsActive, cancellationToken),
            await employees.CountAsync(e => !e.IsActive, cancellationToken));
        employees = status switch
        {
            "active" => employees.Where(e => e.IsActive),
            "inactive" => employees.Where(e => !e.IsActive),
            _ => employees,
        };

        var total = await employees.CountAsync(cancellationToken);
        var size = Math.Clamp(pageSize ?? DefaultPageSize, 1, MaxPageSize);
        var sorted = sort switch
        {
            "-name" => employees.OrderByDescending(e => e.Name),
            "code" => employees.OrderBy(e => e.Code),
            "-code" => employees.OrderByDescending(e => e.Code),
            "joiningDate" => employees.OrderBy(e => e.JoiningDate),
            "-joiningDate" => employees.OrderByDescending(e => e.JoiningDate),
            "modifiedAt" => employees.OrderBy(e => e.ModifiedAt ?? e.CreatedAt),
            "-modifiedAt" => employees.OrderByDescending(e => e.ModifiedAt ?? e.CreatedAt),
            _ => employees.OrderBy(e => e.Name),
        };

        var rows = await sorted
            .Skip((Math.Max(1, page ?? 1) - 1) * size)
            .Take(size)
            .Select(e => new
            {
                Row = new EmployeeRow(
                    e.Id,
                    e.Code,
                    e.Name,
                    e.NameBn,
                    e.ImageSource,
                    e.DesignationId,
                    db.LookupValues.Where(v => v.Id == e.DesignationId).Select(v => v.NameEn).FirstOrDefault(),
                    e.DepartmentId,
                    db.LookupValues.Where(v => v.Id == e.DepartmentId).Select(v => v.NameEn).FirstOrDefault(),
                    e.OfficeId,
                    db.Offices.AcrossTenants().Where(o => o.Id == e.OfficeId).Select(o => o.Name).FirstOrDefault(),
                    e.Mobile,
                    e.JoiningDate,
                    e.IsActive,
                    db.ErpUsers.Where(u => u.EmployeeId == e.Id).Select(u => (Guid?)u.Id).FirstOrDefault(),
                    db.ErpUsers.Where(u => u.EmployeeId == e.Id).Select(u => u.Username).FirstOrDefault(),
                    e.CreatedAt,
                    e.ModifiedAt,
                    null),
                e.CustomData,
            })
            .ToListAsync(cancellationToken);

        return new EmployeeList(rows.Select(r => r.Row with { CustomData = CustomFieldData.Parse(r.CustomData) }).ToList(), total, counts);
    }

    private static Task<List<EmployeeOption>> Options(ErpDbContext db, CancellationToken cancellationToken) =>
        db.Employees.AsNoTracking()
            .OrderBy(e => e.Name)
            .Select(e => new EmployeeOption(e.Id, e.Code, e.Name, e.NameBn, e.IsActive))
            .ToListAsync(cancellationToken);

    private static Task<List<StructureOption>> Offices(ErpDbContext db, CancellationToken cancellationToken) =>
        db.Offices.AcrossTenants().AsNoTracking()
            .Where(o => o.AccountId == db.CurrentAccountId)
            .OrderBy(o => o.Name)
            .Select(o => new StructureOption(o.Id, o.Code, o.Name, o.BusinessUnitId, o.IsActive, null))
            .ToListAsync(cancellationToken);

    private static Task<List<StructureOption>> Warehouses(ErpDbContext db, CancellationToken cancellationToken) =>
        db.Warehouses.AcrossTenants().AsNoTracking()
            .Where(w => w.AccountId == db.CurrentAccountId)
            .OrderBy(w => w.Name)
            .Select(w => new StructureOption(w.Id, w.Code, w.Name, w.BusinessUnitId, w.IsActive, w.OfficeId))
            .ToListAsync(cancellationToken);

    private static async Task<Results<Ok<EmployeeDetail>, NotFound>> Get(int id, ErpDbContext db, CancellationToken cancellationToken) =>
        await LoadAsync(db, id, cancellationToken) is { } detail ? TypedResults.Ok(detail) : TypedResults.NotFound();

    private static async Task<Results<Ok<EmployeeDetail>, NotFound, ValidationProblem, ProblemHttpResult>> Create(
        SaveEmployeeRequest request,
        ICurrentContext current,
        ErpDbContext db,
        FeatureConfigService configs,
        IPermissionService permissions,
        IPasswordHasher hasher,
        CancellationToken cancellationToken)
    {
        var employee = new Employee { AccountId = current.AccountId!.Value, Code = "", Name = "" };
        return await SaveAsync(employee, request, current, db, configs, permissions, hasher, cancellationToken);
    }

    private static async Task<Results<Ok<EmployeeDetail>, NotFound, ValidationProblem, ProblemHttpResult>> Update(
        int id,
        SaveEmployeeRequest request,
        ICurrentContext current,
        ErpDbContext db,
        FeatureConfigService configs,
        IPermissionService permissions,
        IPasswordHasher hasher,
        CancellationToken cancellationToken)
    {
        var employee = await db.Employees.SingleOrDefaultAsync(e => e.Id == id, cancellationToken);
        if (employee is null) return TypedResults.NotFound();
        return await SaveAsync(employee, request, current, db, configs, permissions, hasher, cancellationToken);
    }

    private static async Task<Results<NoContent, ValidationProblem>> SetStatus(
        EmployeeStatusRequest request, ICurrentContext current, ErpDbContext db, CancellationToken cancellationToken)
    {
        var employees = await db.Employees
            .Where(e => request.Ids.Contains(e.Id) && e.HomeBusinessUnitId == current.BusinessUnitId)
            .ToListAsync(cancellationToken);
        if (employees.Count != request.Ids.Distinct().Count()) return Error("ids", "Every employee must be in the selected unit.");

        foreach (var employee in employees) employee.IsActive = request.IsActive;
        await db.SaveChangesAsync(cancellationToken);
        return TypedResults.NoContent();
    }

    private static async Task<Results<Ok<EmployeeDetail>, NotFound, ValidationProblem, ProblemHttpResult>> SaveAsync(
        Employee employee,
        SaveEmployeeRequest sent,
        ICurrentContext current,
        ErpDbContext db,
        FeatureConfigService configs,
        IPermissionService permissions,
        IPasswordHasher hasher,
        CancellationToken cancellationToken)
    {
        var existing = employee.Id > 0 ? ToRequest(employee) : null;
        var (request, errors) = await configs.ApplyRulesAsync(
            current.AccountId!.Value, current.BusinessUnitId!.Value, Feature, sent, existing, cancellationToken);

        var code = Trimmed(request.Code) ?? (employee.Id > 0 ? employee.Code : null);
        if (code is not null && await db.Employees.AnyAsync(e => e.Id != employee.Id && e.Code == code, cancellationToken))
        {
            errors["code"] = ["Another employee already uses this code."];
        }

        await CheckLookupsAsync(request, existing, db, errors, cancellationToken);
        await CheckStructureAsync(request, employee.Id, current, db, errors, cancellationToken);

        ErpUser? user = null;
        if (sent.User is not null)
        {
            if (!(await permissions.GetCurrentAsync(cancellationToken)).Has($"{UserManagementMenu.User}.create"))
            {
                return TypedResults.Problem(title: "Permission denied", detail: "You cannot create users.", statusCode: StatusCodes.Status403Forbidden);
            }

            if (employee.Id > 0 && await db.ErpUsers.AnyAsync(u => u.EmployeeId == employee.Id, cancellationToken))
            {
                errors["user.username"] = ["This employee already has a user."];
            }

            user = new ErpUser
            {
                AccountId = employee.AccountId,
                Username = "",
                Email = "",
                FullName = "",
                PasswordHash = hasher.Hash(sent.User.Password),
                MustChangePassword = true,
            };
            var profile = new EmployeeProfile(request.Name, sent.User.Username, request.Email ?? "", request.Mobile);
            foreach (var (key, messages) in await ErpUserProfiles.CheckUniqueAsync(db, profile, null, cancellationToken))
            {
                errors[key == "email" ? "email" : "user.username"] = messages;
            }

            ErpUserProfiles.Apply(user, profile);
        }

        if (errors.Count > 0) return TypedResults.ValidationProblem(errors);

        if (employee.Id == 0) db.Employees.Add(employee);
        employee.Code = code ?? await NextCodeAsync(db, cancellationToken);
        Apply(employee, request);

        if (user is not null)
        {
            await db.SaveChangesAsync(cancellationToken);
            user.EmployeeId = employee.Id;
            user.BusinessUnits.Add(new UserBusinessUnit { AccountId = employee.AccountId, BusinessUnitId = employee.HomeBusinessUnitId });
            db.ErpUsers.Add(user);
            ConfigurationHistory.Record(db, employee.AccountId, employee.HomeBusinessUnitId, null, AccessAreas.User, user.Id.ToString(),
                null, new { user.FullName, user.Username, user.Email, employee = employee.Code });
        }

        await db.SaveChangesAsync(cancellationToken);
        return TypedResults.Ok((await LoadAsync(db, employee.Id, cancellationToken))!);
    }

    private sealed record EmployeeProfile(string FullName, string Username, string Email, string? Phone) : IErpUserProfile;

    private static async Task CheckLookupsAsync(
        SaveEmployeeRequest request, SaveEmployeeRequest? existing, ErpDbContext db, Dictionary<string, string[]> errors, CancellationToken cancellationToken)
    {
        (string Field, int? Id, int? Current, string Type)[] choices =
        [
            ("designationId", request.DesignationId, existing?.DesignationId, EmployeeLookups.Designation),
            ("departmentId", request.DepartmentId, existing?.DepartmentId, EmployeeLookups.Department),
            ("departmentSectionId", request.DepartmentSectionId, existing?.DepartmentSectionId, EmployeeLookups.DepartmentSection),
            ("employeeTypeId", request.EmployeeTypeId, existing?.EmployeeTypeId, EmployeeLookups.EmployeeType),
            ("genderId", request.GenderId, existing?.GenderId, EmployeeLookups.Gender),
            ("religionId", request.ReligionId, existing?.ReligionId, EmployeeLookups.Religion),
            ("bloodGroupId", request.BloodGroupId, existing?.BloodGroupId, EmployeeLookups.BloodGroup),
        ];
        var ids = choices.Where(c => c.Id is not null).Select(c => c.Id).ToList();
        var values = await db.LookupValues.AsNoTracking()
            .Where(v => ids.Contains(v.Id))
            .Select(v => new { v.Id, Type = v.LookupType!.Key, v.IsActive })
            .ToListAsync(cancellationToken);

        foreach (var choice in choices.Where(c => c.Id is not null))
        {
            var value = values.Find(v => v.Id == choice.Id && v.Type == choice.Type);
            if (value is null || (!value.IsActive && choice.Id != choice.Current)) errors[choice.Field] = [ChooseFromList];
        }
    }

    private static async Task CheckStructureAsync(
        SaveEmployeeRequest request, int employeeId, ICurrentContext current, ErpDbContext db, Dictionary<string, string[]> errors,
        CancellationToken cancellationToken)
    {
        var unitId = request.HomeBusinessUnitId;
        var unitOk = await db.BusinessUnits.AnyAsync(b => b.Id == unitId && b.IsActive, cancellationToken) &&
            (current.IsAccountAdmin || await db.UserBusinessUnits.AnyAsync(
                x => x.UserId == current.UserId && x.BusinessUnitId == unitId && x.IsActive && x.RoleId != null, cancellationToken));
        if (!unitOk) errors["homeBusinessUnitId"] = ["Choose a business unit you work in."];

        if (request.OfficeId is { } officeId &&
            !await db.Offices.AcrossTenants().AnyAsync(o => o.Id == officeId && o.BusinessUnitId == unitId, cancellationToken))
        {
            errors["officeId"] = ["Choose an office of the home business unit."];
        }

        if (request.WarehouseId is { } warehouseId &&
            !await db.Warehouses.AcrossTenants().AnyAsync(w => w.Id == warehouseId && w.BusinessUnitId == unitId, cancellationToken))
        {
            errors["warehouseId"] = ["Choose a warehouse of the home business unit."];
        }

        foreach (var (field, id) in new[] { ("supervisorId", request.SupervisorId), ("lineManagerId", request.LineManagerId) })
        {
            if (id is { } value && (value == employeeId || !await db.Employees.AnyAsync(e => e.Id == value, cancellationToken)))
            {
                errors[field] = [ChooseFromList];
            }
        }
    }

    private static async Task<string> NextCodeAsync(ErpDbContext db, CancellationToken cancellationToken)
    {
        var codes = await db.Employees.Where(e => e.Code.StartsWith(CodePrefix)).Select(e => e.Code).ToListAsync(cancellationToken);
        var last = codes.Select(c => int.TryParse(c[CodePrefix.Length..], out var n) ? n : 0).DefaultIfEmpty(0).Max();
        return $"{CodePrefix}{(last + 1).ToString($"D{CodeDigits}")}";
    }

    private static void Apply(Employee employee, SaveEmployeeRequest request)
    {
        employee.Name = request.Name.Trim();
        employee.NameBn = Trimmed(request.NameBn);
        employee.ImageSource = Trimmed(request.ImageSource);
        employee.DesignationId = request.DesignationId;
        employee.DepartmentId = request.DepartmentId;
        employee.DepartmentSectionId = request.DepartmentSectionId;
        employee.EmployeeTypeId = request.EmployeeTypeId;
        employee.Mobile = Trimmed(request.Mobile);
        employee.Email = Trimmed(request.Email)?.ToLowerInvariant();
        employee.OfficialContact = Trimmed(request.OfficialContact);
        employee.EmergencyMobile = Trimmed(request.EmergencyMobile);
        employee.FatherName = Trimmed(request.FatherName);
        employee.MotherName = Trimmed(request.MotherName);
        employee.DateOfBirth = request.DateOfBirth;
        employee.GenderId = request.GenderId;
        employee.ReligionId = request.ReligionId;
        employee.BloodGroupId = request.BloodGroupId;
        employee.Nid = Trimmed(request.Nid);
        employee.PresentAddress = Trimmed(request.PresentAddress);
        employee.PermanentAddress = Trimmed(request.PermanentAddress);
        employee.SignatureImage = Trimmed(request.SignatureImage);
        employee.HomeBusinessUnitId = request.HomeBusinessUnitId;
        employee.OfficeId = request.OfficeId;
        employee.WarehouseId = request.WarehouseId;
        employee.Location = Trimmed(request.Location);
        employee.JoiningDate = request.JoiningDate;
        employee.ConfirmationDate = request.ConfirmationDate;
        employee.TerminationDate = request.TerminationDate;
        employee.SupervisorId = request.SupervisorId;
        employee.LineManagerId = request.LineManagerId;
        employee.AttendanceId = Trimmed(request.AttendanceId);
        employee.CardNumber = Trimmed(request.CardNumber);
        employee.IsSalesRepresentative = request.IsSalesRepresentative;
        employee.IsServiceStaff = request.IsServiceStaff;
        employee.IsActive = request.IsActive;
        employee.CustomData = CustomFieldData.ToJson(request.CustomData);
    }

    private static SaveEmployeeRequest ToRequest(Employee e) => new()
    {
        Code = e.Code,
        Name = e.Name,
        NameBn = e.NameBn,
        ImageSource = e.ImageSource,
        DesignationId = e.DesignationId,
        DepartmentId = e.DepartmentId,
        DepartmentSectionId = e.DepartmentSectionId,
        EmployeeTypeId = e.EmployeeTypeId,
        Mobile = e.Mobile,
        Email = e.Email,
        OfficialContact = e.OfficialContact,
        EmergencyMobile = e.EmergencyMobile,
        FatherName = e.FatherName,
        MotherName = e.MotherName,
        DateOfBirth = e.DateOfBirth,
        GenderId = e.GenderId,
        ReligionId = e.ReligionId,
        BloodGroupId = e.BloodGroupId,
        Nid = e.Nid,
        PresentAddress = e.PresentAddress,
        PermanentAddress = e.PermanentAddress,
        SignatureImage = e.SignatureImage,
        HomeBusinessUnitId = e.HomeBusinessUnitId,
        OfficeId = e.OfficeId,
        WarehouseId = e.WarehouseId,
        Location = e.Location,
        JoiningDate = e.JoiningDate,
        ConfirmationDate = e.ConfirmationDate,
        TerminationDate = e.TerminationDate,
        SupervisorId = e.SupervisorId,
        LineManagerId = e.LineManagerId,
        AttendanceId = e.AttendanceId,
        CardNumber = e.CardNumber,
        IsSalesRepresentative = e.IsSalesRepresentative,
        IsServiceStaff = e.IsServiceStaff,
        IsActive = e.IsActive,
        CustomData = CustomFieldData.Parse(e.CustomData),
    };

    private static async Task<EmployeeDetail?> LoadAsync(ErpDbContext db, int id, CancellationToken cancellationToken)
    {
        var e = await db.Employees.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (e is null) return null;

        int?[] lookupIds = [e.DesignationId, e.DepartmentId, e.DepartmentSectionId, e.EmployeeTypeId, e.GenderId, e.ReligionId, e.BloodGroupId];
        var lookups = await db.LookupValues.AsNoTracking()
            .Where(v => lookupIds.Contains(v.Id))
            .ToDictionaryAsync(v => v.Id, v => v.NameEn, cancellationToken);
        int?[] people = [e.SupervisorId, e.LineManagerId];
        var names = await db.Employees.AsNoTracking()
            .Where(x => people.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, x => x.Name, cancellationToken);
        var user = await db.ErpUsers.AsNoTracking()
            .Where(u => u.EmployeeId == id)
            .Select(u => new { u.Id, u.Username })
            .FirstOrDefaultAsync(cancellationToken);

        string? Lookup(int? key) => key is { } k ? lookups.GetValueOrDefault(k) : null;
        string? Person(int? key) => key is { } k ? names.GetValueOrDefault(k) : null;

        return new EmployeeDetail
        {
            Id = e.Id,
            Code = e.Code,
            Name = e.Name,
            NameBn = e.NameBn,
            ImageSource = e.ImageSource,
            DesignationId = e.DesignationId,
            DesignationName = Lookup(e.DesignationId),
            DepartmentId = e.DepartmentId,
            DepartmentName = Lookup(e.DepartmentId),
            DepartmentSectionId = e.DepartmentSectionId,
            DepartmentSectionName = Lookup(e.DepartmentSectionId),
            EmployeeTypeId = e.EmployeeTypeId,
            EmployeeTypeName = Lookup(e.EmployeeTypeId),
            Mobile = e.Mobile,
            Email = e.Email,
            OfficialContact = e.OfficialContact,
            EmergencyMobile = e.EmergencyMobile,
            FatherName = e.FatherName,
            MotherName = e.MotherName,
            DateOfBirth = e.DateOfBirth,
            GenderId = e.GenderId,
            GenderName = Lookup(e.GenderId),
            ReligionId = e.ReligionId,
            ReligionName = Lookup(e.ReligionId),
            BloodGroupId = e.BloodGroupId,
            BloodGroupName = Lookup(e.BloodGroupId),
            Nid = e.Nid,
            PresentAddress = e.PresentAddress,
            PermanentAddress = e.PermanentAddress,
            SignatureImage = e.SignatureImage,
            HomeBusinessUnitId = e.HomeBusinessUnitId,
            HomeBusinessUnitName = await db.BusinessUnits.AcrossTenants().Where(b => b.Id == e.HomeBusinessUnitId).Select(b => b.Name).FirstOrDefaultAsync(cancellationToken),
            OfficeId = e.OfficeId,
            OfficeName = await db.Offices.AcrossTenants().Where(o => o.Id == e.OfficeId).Select(o => o.Name).FirstOrDefaultAsync(cancellationToken),
            WarehouseId = e.WarehouseId,
            WarehouseName = await db.Warehouses.AcrossTenants().Where(w => w.Id == e.WarehouseId).Select(w => w.Name).FirstOrDefaultAsync(cancellationToken),
            Location = e.Location,
            JoiningDate = e.JoiningDate,
            ConfirmationDate = e.ConfirmationDate,
            TerminationDate = e.TerminationDate,
            SupervisorId = e.SupervisorId,
            SupervisorName = Person(e.SupervisorId),
            LineManagerId = e.LineManagerId,
            LineManagerName = Person(e.LineManagerId),
            AttendanceId = e.AttendanceId,
            CardNumber = e.CardNumber,
            IsSalesRepresentative = e.IsSalesRepresentative,
            IsServiceStaff = e.IsServiceStaff,
            IsActive = e.IsActive,
            CustomData = CustomFieldData.Parse(e.CustomData),
            UserId = user?.Id,
            Username = user?.Username,
            CreatedAt = e.CreatedAt,
            ModifiedAt = e.ModifiedAt,
        };
    }

    private static string? Trimmed(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static ValidationProblem Error(string field, string message) =>
        TypedResults.ValidationProblem(new Dictionary<string, string[]> { [field] = [message] });
}
