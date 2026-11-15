using System.Text.Json;
using Erp.Api.Auth;
using FluentValidation;

namespace Erp.Api.UserManagement.Employees;

public sealed record EmployeeCounts(int All, int Active, int Inactive);

public sealed record EmployeeRow(
    int Id,
    string Code,
    string Name,
    string? NameBn,
    string? ImageSource,
    int? DesignationId,
    string? DesignationName,
    int? DepartmentId,
    string? DepartmentName,
    Guid? OfficeId,
    string? OfficeName,
    string? Mobile,
    DateOnly? JoiningDate,
    bool IsActive,
    Guid? UserId,
    string? Username,
    DateTime? CreatedAt,
    DateTime? ModifiedAt,
    Dictionary<string, JsonElement>? CustomData);

public sealed record EmployeeList(IReadOnlyList<EmployeeRow> Items, int Total, EmployeeCounts Counts);

public sealed record EmployeeOption(int Id, string Code, string Name, string? NameBn, bool IsActive);

public sealed record StructureOption(Guid Id, string Code, string Name, Guid? ParentId, bool IsActive);

public sealed record EmployeeUserInput(string Username, string Password);

public sealed record SaveEmployeeRequest
{
    public required string? Code { get; init; }
    public required string Name { get; init; }
    public required string? NameBn { get; init; }
    public required string? ImageSource { get; init; }
    public required int? DesignationId { get; init; }
    public required int? DepartmentId { get; init; }
    public required int? DepartmentSectionId { get; init; }
    public required int? EmployeeTypeId { get; init; }
    public required string? Mobile { get; init; }
    public required string? Email { get; init; }
    public required string? OfficialContact { get; init; }
    public required string? EmergencyMobile { get; init; }
    public required string? FatherName { get; init; }
    public required string? MotherName { get; init; }
    public required DateOnly? DateOfBirth { get; init; }
    public required int? GenderId { get; init; }
    public required int? ReligionId { get; init; }
    public required int? BloodGroupId { get; init; }
    public required string? Nid { get; init; }
    public required string? PresentAddress { get; init; }
    public required string? PermanentAddress { get; init; }
    public required string? SignatureImage { get; init; }
    public required Guid HomeBusinessUnitId { get; init; }
    public required Guid? OfficeId { get; init; }
    public required Guid? WarehouseId { get; init; }
    public required string? Location { get; init; }
    public required DateOnly? JoiningDate { get; init; }
    public required DateOnly? ConfirmationDate { get; init; }
    public required DateOnly? TerminationDate { get; init; }
    public required int? SupervisorId { get; init; }
    public required int? LineManagerId { get; init; }
    public required string? AttendanceId { get; init; }
    public required string? CardNumber { get; init; }
    public required bool IsSalesRepresentative { get; init; }
    public required bool IsServiceStaff { get; init; }
    public required bool IsActive { get; init; }
    public Dictionary<string, JsonElement>? CustomData { get; init; }
    public EmployeeUserInput? User { get; init; }
}

public sealed record EmployeeDetail
{
    public required int Id { get; init; }
    public required string Code { get; init; }
    public required string Name { get; init; }
    public required string? NameBn { get; init; }
    public required string? ImageSource { get; init; }
    public required int? DesignationId { get; init; }
    public required string? DesignationName { get; init; }
    public required int? DepartmentId { get; init; }
    public required string? DepartmentName { get; init; }
    public required int? DepartmentSectionId { get; init; }
    public required string? DepartmentSectionName { get; init; }
    public required int? EmployeeTypeId { get; init; }
    public required string? EmployeeTypeName { get; init; }
    public required string? Mobile { get; init; }
    public required string? Email { get; init; }
    public required string? OfficialContact { get; init; }
    public required string? EmergencyMobile { get; init; }
    public required string? FatherName { get; init; }
    public required string? MotherName { get; init; }
    public required DateOnly? DateOfBirth { get; init; }
    public required int? GenderId { get; init; }
    public required string? GenderName { get; init; }
    public required int? ReligionId { get; init; }
    public required string? ReligionName { get; init; }
    public required int? BloodGroupId { get; init; }
    public required string? BloodGroupName { get; init; }
    public required string? Nid { get; init; }
    public required string? PresentAddress { get; init; }
    public required string? PermanentAddress { get; init; }
    public required string? SignatureImage { get; init; }
    public required Guid HomeBusinessUnitId { get; init; }
    public required string? HomeBusinessUnitName { get; init; }
    public required Guid? OfficeId { get; init; }
    public required string? OfficeName { get; init; }
    public required Guid? WarehouseId { get; init; }
    public required string? WarehouseName { get; init; }
    public required string? Location { get; init; }
    public required DateOnly? JoiningDate { get; init; }
    public required DateOnly? ConfirmationDate { get; init; }
    public required DateOnly? TerminationDate { get; init; }
    public required int? SupervisorId { get; init; }
    public required string? SupervisorName { get; init; }
    public required int? LineManagerId { get; init; }
    public required string? LineManagerName { get; init; }
    public required string? AttendanceId { get; init; }
    public required string? CardNumber { get; init; }
    public required bool IsSalesRepresentative { get; init; }
    public required bool IsServiceStaff { get; init; }
    public required bool IsActive { get; init; }
    public required Dictionary<string, JsonElement>? CustomData { get; init; }
    public required Guid? UserId { get; init; }
    public required string? Username { get; init; }
    public required DateTime? CreatedAt { get; init; }
    public required DateTime? ModifiedAt { get; init; }
}

public sealed record EmployeeStatusRequest(IReadOnlyList<int> Ids, bool IsActive);

internal sealed class SaveEmployeeRequestValidator : AbstractValidator<SaveEmployeeRequest>
{
    public SaveEmployeeRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty();
        RuleFor(x => x.HomeBusinessUnitId).NotEmpty();
        RuleFor(x => x.Email).EmailAddress().When(x => !string.IsNullOrWhiteSpace(x.Email));
        RuleFor(x => x.Email).NotEmpty().WithMessage("An email is needed to make this employee a user.").When(x => x.User is not null);
        RuleFor(x => x.User!.Username).ValidUsername().OverridePropertyName("user.username").When(x => x.User is not null);
        RuleFor(x => x.User!.Password).NotEmpty().StrongPassword().OverridePropertyName("user.password").When(x => x.User is not null);
    }
}

internal sealed class EmployeeStatusRequestValidator : AbstractValidator<EmployeeStatusRequest>
{
    public EmployeeStatusRequestValidator()
    {
        RuleFor(x => x.Ids).NotEmpty();
    }
}
