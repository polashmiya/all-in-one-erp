using Erp.Api.UserManagement.Employees;
using Erp.Metadata;

namespace Erp.Api.Inventory.StockAdjustments;

public sealed class StockAdjustmentManifest : IFeatureManifestProvider
{
    public const string Type = "stock-adjustment-type";

    public IEnumerable<FeatureManifest> GetManifests() =>
    [
        new(InventoryMenu.StockAdjustment,
        [
            new("general", new("General", "সাধারণ"), 10),
            new("basic", new("Adjustment", "সমন্বয়"), 10, "general"),
            new("notes", new("Notes", "মন্তব্য"), 20, "general"),
        ],
        [
            new("code", FieldTypes.Text, new("Adjustment no.", "সমন্বয় নম্বর"), "basic", 10)
            {
                MaxLength = 100,
                Help = new("Leave empty to generate one.", "ফাঁকা রাখলে একটি নম্বর তৈরি হবে।"),
                Placeholder = new("Generated on save", "সংরক্ষণের সময় তৈরি হবে"),
                ListOrder = 10,
                Sort = true,
            },
            new("adjustmentDate", FieldTypes.Date, new("Date", "তারিখ"), "basic", 20)
            {
                Locked = true,
                ListOrder = 20,
                Sort = true,
            },
            new("type", FieldTypes.Lookup, new("Type", "ধরন"), "basic", 30)
            {
                Locked = true,
                Lookup = Type,
                Help = new("Stock in and opening add to the stock, stock out takes from it.", "মজুত বৃদ্ধি ও প্রারম্ভিক মজুত যোগ করে, মজুত হ্রাস কমায়।"),
                ListOrder = 30,
                Filter = true,
            },
            new("warehouseId", FieldTypes.Lookup, new("Warehouse", "গুদাম"), "basic", 40)
            {
                Locked = true,
                Lookup = EmployeeLookups.Warehouse,
                ListOrder = 40,
                Filter = true,
            },
            new("reasonId", FieldTypes.Lookup, new("Reason", "কারণ"), "basic", 50)
            {
                Lookup = StockLookups.AdjustmentReason,
                ListOrder = 50,
                Filter = true,
            },

            new("remarks", FieldTypes.TextArea, new("Remarks", "মন্তব্য"), "notes", 10) { MaxLength = 1000 },
        ]),
    ];
}
