using Erp.Api.UserManagement.Employees;
using Erp.Metadata;
using Erp.Modules.Inventory.ItemManagement.Items;

namespace Erp.Api.Procurement.PurchaseRequisitions;

public sealed class PurchaseRequisitionManifest : IFeatureManifestProvider
{
    public IEnumerable<FeatureManifest> GetManifests() =>
    [
        new(ProcurementMenu.PurchaseRequisition,
        [
            new("general", new("General", "সাধারণ"), 10),
            new("basic", new("Requisition", "চাহিদাপত্র"), 10, "general"),
            new("requester", new("Requested by", "চাহিদাকারী"), 20, "general"),
            new("notes", new("Notes", "মন্তব্য"), 30, "general"),
        ],
        [
            new("code", FieldTypes.Text, new("Requisition no.", "চাহিদাপত্র নম্বর"), "basic", 10)
            {
                MaxLength = 100,
                Help = new("Leave empty to generate one.", "ফাঁকা রাখলে একটি নম্বর তৈরি হবে।"),
                Placeholder = new("Generated on save", "সংরক্ষণের সময় তৈরি হবে"),
                ListOrder = 10,
                Sort = true,
            },
            new("requestDate", FieldTypes.Date, new("Request date", "চাহিদার তারিখ"), "basic", 20)
            {
                Locked = true,
                ListOrder = 20,
                Sort = true,
            },
            new("dueDate", FieldTypes.Date, new("Required by", "প্রয়োজনের তারিখ"), "basic", 30)
            {
                Help = new("When the goods are needed.", "কবে মাল প্রয়োজন।"),
                ListOrder = 70,
                Sort = true,
            },
            new("warehouseId", FieldTypes.Lookup, new("Warehouse", "গুদাম"), "basic", 40)
            {
                Locked = true,
                Lookup = EmployeeLookups.Warehouse,
                Help = new("Where the goods will be received.", "যেখানে মাল গ্রহণ করা হবে।"),
                ListOrder = 30,
                Filter = true,
            },
            new("requestTypeId", FieldTypes.Lookup, new("Requisition type", "চাহিদাপত্রের ধরন"), "basic", 50)
            {
                Lookup = ProcurementLookups.RequestType,
                Filter = true,
            },
            new("purchaseMethodId", FieldTypes.Lookup, new("Purchase method", "ক্রয় পদ্ধতি"), "basic", 60)
            {
                Lookup = ProcurementLookups.PurchaseMethod,
            },
            new("itemTypeId", FieldTypes.Lookup, new("Item type", "আইটেমের ধরন"), "basic", 70)
            {
                Lookup = ItemLookups.ItemType,
                Hidden = true,
            },

            new("departmentId", FieldTypes.Lookup, new("Department", "বিভাগ"), "requester", 10)
            {
                Lookup = EmployeeLookups.Department,
                ListOrder = 50,
                Filter = true,
            },
            new("sectionId", FieldTypes.Lookup, new("Section", "শাখা"), "requester", 20)
            {
                Lookup = EmployeeLookups.DepartmentSection,
                Hidden = true,
            },
            new("employeeId", FieldTypes.Lookup, new("Requested by", "চাহিদাকারী"), "requester", 30)
            {
                Lookup = EmployeeLookups.Employee,
                ListOrder = 60,
            },
            new("buyerId", FieldTypes.Lookup, new("Buyer", "ক্রেতা"), "requester", 40)
            {
                Lookup = EmployeeLookups.Employee,
                Help = new("The person who will buy the goods.", "যিনি মাল কিনবেন।"),
                Hidden = true,
            },
            new("projectName", FieldTypes.Text, new("Project", "প্রকল্প"), "requester", 50)
            {
                MaxLength = 200,
                Hidden = true,
            },

            new("remarks", FieldTypes.TextArea, new("Remarks", "মন্তব্য"), "notes", 10) { MaxLength = 300 },
        ]),
    ];
}
