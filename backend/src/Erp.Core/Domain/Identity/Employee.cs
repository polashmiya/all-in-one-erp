namespace Erp.Core.Domain.Identity;

/// <summary>A person who works for the account. Employees have no permissions; one may be linked to an ERP user.</summary>
public class Employee : MasterRecord
{
    public required string Code { get; set; }

    public required string Name { get; set; }

    public string? NameBn { get; set; }

    public string? ImageSource { get; set; }

    public int? DesignationId { get; set; }

    public int? DepartmentId { get; set; }

    public int? DepartmentSectionId { get; set; }

    public int? EmployeeTypeId { get; set; }

    public string? Mobile { get; set; }

    public string? Email { get; set; }

    public string? OfficialContact { get; set; }

    public string? EmergencyMobile { get; set; }

    public string? FatherName { get; set; }

    public string? MotherName { get; set; }

    public DateOnly? DateOfBirth { get; set; }

    public int? GenderId { get; set; }

    public int? ReligionId { get; set; }

    public int? BloodGroupId { get; set; }

    public string? Nid { get; set; }

    public string? PresentAddress { get; set; }

    public string? PermanentAddress { get; set; }

    public string? SignatureImage { get; set; }

    public Guid HomeBusinessUnitId { get; set; }

    public Guid? OfficeId { get; set; }

    public Guid? WarehouseId { get; set; }

    public string? Location { get; set; }

    public DateOnly? JoiningDate { get; set; }

    public DateOnly? ConfirmationDate { get; set; }

    public DateOnly? TerminationDate { get; set; }

    public int? SupervisorId { get; set; }

    public int? LineManagerId { get; set; }

    public string? AttendanceId { get; set; }

    public string? CardNumber { get; set; }

    public bool IsSalesRepresentative { get; set; }

    public bool IsServiceStaff { get; set; }

    public string? CustomData { get; set; }
}
