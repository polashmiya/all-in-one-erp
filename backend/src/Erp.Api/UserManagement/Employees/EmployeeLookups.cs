using Erp.Metadata;

namespace Erp.Api.UserManagement.Employees;

public sealed class EmployeeLookups : ILookupProvider
{
    public const string Designation = "designation";
    public const string Department = "employee-department";
    public const string DepartmentSection = "department-section";
    public const string EmployeeType = "employee-type";
    public const string Gender = "gender";
    public const string Religion = "religion";
    public const string BloodGroup = "blood-group";

    public const string BusinessUnit = "business-unit";
    public const string Office = "office";
    public const string Warehouse = "warehouse";
    public const string Employee = "employee";

    public IEnumerable<LookupDef> GetLookups() =>
    [
        new(Designation, new("Designation", "পদবি"),
        [
            new("MGR", new("Manager", "ম্যানেজার")),
            new("EXEC", new("Executive", "এক্সিকিউটিভ")),
            new("OFFICER", new("Officer", "অফিসার")),
            new("ASSIST", new("Assistant", "সহকারী")),
        ]),
        new(Department, new("Employee department", "কর্মী বিভাগ"),
        [
            new("ADMIN", new("Administration", "প্রশাসন")),
            new("ACC", new("Accounts", "হিসাব")),
            new("SALES", new("Sales", "বিক্রয়")),
            new("STORE", new("Store", "স্টোর")),
        ]),
        new(DepartmentSection, new("Department section", "বিভাগের শাখা"), []),
        new(EmployeeType, new("Employee type", "কর্মীর ধরন"),
        [
            new("PERM", new("Permanent", "স্থায়ী"), IsDefault: true),
            new("CONT", new("Contract", "চুক্তিভিত্তিক")),
            new("PROB", new("Probation", "শিক্ষানবিশ")),
            new("PART", new("Part time", "খণ্ডকালীন")),
        ]),
        new(Gender, new("Gender", "লিঙ্গ"),
        [
            new("M", new("Male", "পুরুষ")),
            new("F", new("Female", "নারী")),
            new("O", new("Other", "অন্যান্য")),
        ]),
        new(Religion, new("Religion", "ধর্ম"),
        [
            new("ISL", new("Islam", "ইসলাম")),
            new("HIN", new("Hinduism", "হিন্দু")),
            new("BUD", new("Buddhism", "বৌদ্ধ")),
            new("CHR", new("Christianity", "খ্রিস্টান")),
            new("OTH", new("Other", "অন্যান্য")),
        ]),
        new(BloodGroup, new("Blood group", "রক্তের গ্রুপ"),
        [
            new("A-POS", new("A+", "এ+")),
            new("A-NEG", new("A-", "এ-")),
            new("B-POS", new("B+", "বি+")),
            new("B-NEG", new("B-", "বি-")),
            new("AB-POS", new("AB+", "এবি+")),
            new("AB-NEG", new("AB-", "এবি-")),
            new("O-POS", new("O+", "ও+")),
            new("O-NEG", new("O-", "ও-")),
        ]),
    ];
}
