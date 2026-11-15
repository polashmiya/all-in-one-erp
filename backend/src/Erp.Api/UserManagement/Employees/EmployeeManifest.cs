using Erp.Metadata;

namespace Erp.Api.UserManagement.Employees;

public sealed class EmployeeManifest : IFeatureManifestProvider
{
    public IEnumerable<FeatureManifest> GetManifests() =>
    [
        new(UserManagementMenu.Employee,
        [
            new("general", new("General", "সাধারণ"), 10),
            new("personal", new("Personal", "ব্যক্তিগত"), 20),
            new("job", new("Job", "চাকরি"), 30),
            new("basic", new("Basic", "মৌলিক তথ্য"), 10, "general"),
            new("contact", new("Contact", "যোগাযোগ"), 20, "general"),
            new("photo", new("Photo", "ছবি"), 30, "general"),
            new("status", new("Status", "অবস্থা"), 40, "general"),
            new("details", new("Details", "বিস্তারিত"), 10, "personal"),
            new("address", new("Address", "ঠিকানা"), 20, "personal"),
            new("posting", new("Posting", "পদায়ন"), 10, "job"),
            new("service", new("Service", "চাকরির তথ্য"), 20, "job"),
            new("other", new("Other", "অন্যান্য"), 30, "job"),
        ],
        [
            new("code", FieldTypes.Text, new("Code", "কোড"), "basic", 10)
            {
                MaxLength = 50,
                Help = new("Leave empty to generate one.", "ফাঁকা রাখলে একটি কোড তৈরি হবে।"),
                Placeholder = new("Generated on save", "সংরক্ষণের সময় তৈরি হবে"),
                ListOrder = 20,
                Sort = true,
            },
            new("name", FieldTypes.Text, new("Name (English)", "নাম (ইংরেজি)"), "basic", 20)
            {
                Locked = true,
                MaxLength = 200,
                ListOrder = 10,
                Sort = true,
            },
            new("nameBn", FieldTypes.Text, new("Name (Bangla)", "নাম (বাংলা)"), "basic", 30) { MaxLength = 200 },
            new("designationId", FieldTypes.Lookup, new("Designation", "পদবি"), "basic", 40)
            {
                Lookup = EmployeeLookups.Designation,
                ListOrder = 30,
                Filter = true,
            },
            new("departmentId", FieldTypes.Lookup, new("Department", "বিভাগ"), "basic", 50)
            {
                Lookup = EmployeeLookups.Department,
                ListOrder = 40,
                Filter = true,
            },
            new("departmentSectionId", FieldTypes.Lookup, new("Department section", "বিভাগের শাখা"), "basic", 60)
            {
                Lookup = EmployeeLookups.DepartmentSection,
                Hidden = true,
            },
            new("employeeTypeId", FieldTypes.Lookup, new("Employee type", "কর্মীর ধরন"), "basic", 70)
            {
                Lookup = EmployeeLookups.EmployeeType,
            },

            new("mobile", FieldTypes.Text, new("Mobile", "মোবাইল"), "contact", 10) { MaxLength = 32, ListOrder = 60 },
            new("email", FieldTypes.Text, new("Email", "ইমেইল"), "contact", 20)
            {
                MaxLength = 256,
                Help = new("Needed if this employee becomes a user.", "এই কর্মী ইউজার হলে লাগবে।"),
            },
            new("officialContact", FieldTypes.Text, new("Official contact", "অফিসিয়াল যোগাযোগ"), "contact", 30)
            {
                MaxLength = 100,
                Hidden = true,
            },
            new("emergencyMobile", FieldTypes.Text, new("Emergency mobile", "জরুরি মোবাইল"), "contact", 40) { MaxLength = 32 },

            new("imageSource", FieldTypes.Image, new("Photo", "ছবি"), "photo", 10),
            new("isActive", FieldTypes.Boolean, new("Active", "সক্রিয়"), "status", 10) { Default = "true" },

            new("fatherName", FieldTypes.Text, new("Father's name", "পিতার নাম"), "details", 10) { MaxLength = 200 },
            new("motherName", FieldTypes.Text, new("Mother's name", "মাতার নাম"), "details", 20) { MaxLength = 200 },
            new("dateOfBirth", FieldTypes.Date, new("Date of birth", "জন্ম তারিখ"), "details", 30),
            new("genderId", FieldTypes.Lookup, new("Gender", "লিঙ্গ"), "details", 40) { Lookup = EmployeeLookups.Gender },
            new("religionId", FieldTypes.Lookup, new("Religion", "ধর্ম"), "details", 50)
            {
                Lookup = EmployeeLookups.Religion,
                Hidden = true,
            },
            new("bloodGroupId", FieldTypes.Lookup, new("Blood group", "রক্তের গ্রুপ"), "details", 60) { Lookup = EmployeeLookups.BloodGroup },
            new("nid", FieldTypes.Text, new("NID", "জাতীয় পরিচয়পত্র"), "details", 70) { MaxLength = 50 },

            new("presentAddress", FieldTypes.TextArea, new("Present address", "বর্তমান ঠিকানা"), "address", 10) { MaxLength = 500 },
            new("permanentAddress", FieldTypes.TextArea, new("Permanent address", "স্থায়ী ঠিকানা"), "address", 20) { MaxLength = 500 },
            new("signatureImage", FieldTypes.Image, new("Signature", "স্বাক্ষর"), "address", 30) { Hidden = true },

            new("homeBusinessUnitId", FieldTypes.Lookup, new("Home business unit", "নিজ বিজনেস ইউনিট"), "posting", 10)
            {
                Locked = true,
                Lookup = EmployeeLookups.BusinessUnit,
                Help = new("The employee shows in this unit's list.", "কর্মী এই ইউনিটের তালিকায় দেখাবে।"),
            },
            new("officeId", FieldTypes.Lookup, new("Office", "অফিস"), "posting", 20)
            {
                Lookup = EmployeeLookups.Office,
                ListOrder = 50,
                Filter = true,
            },
            new("warehouseId", FieldTypes.Lookup, new("Warehouse", "গুদাম"), "posting", 30)
            {
                Lookup = EmployeeLookups.Warehouse,
                Hidden = true,
            },
            new("location", FieldTypes.Text, new("Location", "অবস্থান"), "posting", 40) { MaxLength = 200, Hidden = true },

            new("joiningDate", FieldTypes.Date, new("Joining date", "যোগদানের তারিখ"), "service", 10) { ListOrder = 70, Sort = true },
            new("confirmationDate", FieldTypes.Date, new("Confirmation date", "স্থায়ীকরণের তারিখ"), "service", 20),
            new("terminationDate", FieldTypes.Date, new("Termination date", "অব্যাহতির তারিখ"), "service", 30) { Hidden = true },
            new("supervisorId", FieldTypes.Lookup, new("Supervisor", "সুপারভাইজার"), "service", 40) { Lookup = EmployeeLookups.Employee },
            new("lineManagerId", FieldTypes.Lookup, new("Line manager", "লাইন ম্যানেজার"), "service", 50) { Lookup = EmployeeLookups.Employee },

            new("attendanceId", FieldTypes.Text, new("Attendance ID", "হাজিরা আইডি"), "other", 10) { MaxLength = 50, Hidden = true },
            new("cardNumber", FieldTypes.Text, new("Card number", "কার্ড নম্বর"), "other", 20) { MaxLength = 50, Hidden = true },
            new("isSalesRepresentative", FieldTypes.Boolean, new("Sales representative", "সেলস রিপ্রেজেন্টেটিভ"), "other", 30) { Hidden = true },
            new("isServiceStaff", FieldTypes.Boolean, new("Service staff", "সার্ভিস স্টাফ"), "other", 40) { Hidden = true },
        ]),
    ];
}
