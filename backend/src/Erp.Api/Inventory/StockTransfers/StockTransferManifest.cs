using Erp.Api.UserManagement.Employees;
using Erp.Metadata;

namespace Erp.Api.Inventory.StockTransfers;

public sealed class StockTransferManifest : IFeatureManifestProvider
{
    public IEnumerable<FeatureManifest> GetManifests() =>
    [
        new(InventoryMenu.StockTransfer,
        [
            new("general", new("General", "সাধারণ"), 10),
            new("basic", new("Transfer", "স্থানান্তর"), 10, "general"),
            new("notes", new("Notes", "মন্তব্য"), 20, "general"),
        ],
        [
            new("code", FieldTypes.Text, new("Transfer no.", "স্থানান্তর নম্বর"), "basic", 10)
            {
                MaxLength = 100,
                Help = new("Leave empty to generate one.", "ফাঁকা রাখলে একটি নম্বর তৈরি হবে।"),
                Placeholder = new("Generated on save", "সংরক্ষণের সময় তৈরি হবে"),
                ListOrder = 10,
                Sort = true,
            },
            new("transferDate", FieldTypes.Date, new("Date", "তারিখ"), "basic", 20)
            {
                Locked = true,
                ListOrder = 20,
                Sort = true,
            },
            new("fromWarehouseId", FieldTypes.Lookup, new("From warehouse", "যে গুদাম থেকে"), "basic", 30)
            {
                Locked = true,
                Lookup = EmployeeLookups.Warehouse,
                ListOrder = 30,
                Filter = true,
            },
            new("toWarehouseId", FieldTypes.Lookup, new("To warehouse", "যে গুদামে"), "basic", 40)
            {
                Locked = true,
                Lookup = EmployeeLookups.Warehouse,
                ListOrder = 40,
                Filter = true,
            },

            new("remarks", FieldTypes.TextArea, new("Remarks", "মন্তব্য"), "notes", 10) { MaxLength = 1000 },
        ]),
    ];
}
