using Erp.Metadata;

namespace Erp.Modules.Inventory.ItemManagement.Items;

public sealed class ItemProfileManifest : IFeatureManifestProvider
{
    public IEnumerable<FeatureManifest> GetManifests() =>
    [
        new(ItemManagementMenu.ItemProfile,
        [
            new("general", new("General", "সাধারণ"), 10),
            new("pricing", new("Units & pricing", "একক ও মূল্য"), 20),
            new("tax", new("VAT & tax", "ভ্যাট ও কর"), 30),
            new("status", new("Status", "অবস্থা"), 40),
            new("basic", new("Basic information", "মৌলিক তথ্য"), 10, "general"),
            new("classification", new("Classification", "শ্রেণিবিন্যাস"), 20, "general"),
            new("barcode", new("Barcode", "বারকোড"), 30, "general"),
            new("units", new("Units of measure", "পরিমাপের একক"), 10, "pricing"),
            new("prices", new("Prices", "মূল্য"), 20, "pricing"),
            new("vat", new("VAT & tax", "ভ্যাট ও কর"), 10, "tax"),
            new("usage", new("Status & usage", "অবস্থা ও ব্যবহার"), 10, "status"),
        ],
        [
            new("name", FieldTypes.Text, new("Item name", "আইটেমের নাম"), "basic", 10)
            {
                Locked = true,
                MaxLength = 200,
                Placeholder = new("e.g. Napa 500 mg tablet", "যেমন: নাপা ৫০০ মি.গ্রা. ট্যাবলেট"),
                ListOrder = 10,
                Sort = true,
            },
            new("itemTypeId", FieldTypes.Lookup, new("Item type", "আইটেমের ধরন"), "basic", 20)
            {
                Required = true,
                Lookup = "item-type",
                ListOrder = 40,
                Filter = true,
            },
            new("hsCode", FieldTypes.Text, new("HS code", "এইচএস কোড"), "basic", 30)
            {
                Required = true,
                MaxLength = 50,
                Placeholder = new("e.g. 3004.90.99", "যেমন: 3004.90.99"),
            },
            new("code", FieldTypes.Text, new("Item code", "আইটেম কোড"), "basic", 40)
            {
                MaxLength = 50,
                Help = new("Leave empty to generate one from the category code.", "ফাঁকা রাখলে ক্যাটাগরি কোড থেকে একটি কোড তৈরি হবে।"),
                Placeholder = new("Generated on save", "সংরক্ষণের সময় তৈরি হবে"),
                ListOrder = 20,
                Sort = true,
            },
            new("description", FieldTypes.TextArea, new("Description", "বিবরণ"), "basic", 50) { MaxLength = 2000, ColSpan = 3 },
            new("imageSource", FieldTypes.Image, new("Image", "ছবি"), "basic", 60)
            {
                Help = new("PNG, JPG or WebP · resized to 320 px", "PNG, JPG বা WebP · ৩২০ পিক্সেলে ছোট করা হবে"),
            },

            new("departmentId", FieldTypes.Lookup, new("Department", "ডিপার্টমেন্ট"), "classification", 10)
            {
                Lookup = "item-department",
                Placeholder = new("All departments", "সব ডিপার্টমেন্ট"),
                Filter = true,
            },
            new("categoryId", FieldTypes.Lookup, new("Category", "ক্যাটাগরি"), "classification", 20)
            {
                Lookup = "item-category",
                ParentField = "departmentId",
                Placeholder = new("All categories", "সব ক্যাটাগরি"),
                ListOrder = 30,
                Filter = true,
            },
            new("subCategoryId", FieldTypes.Lookup, new("Sub category", "সাব ক্যাটাগরি"), "classification", 30)
            {
                Lookup = "item-sub-category",
                ParentField = "categoryId",
                Placeholder = new("All sub categories", "সব সাব ক্যাটাগরি"),
            },
            new("minorCategoryId", FieldTypes.Lookup, new("Minor category", "মাইনর ক্যাটাগরি"), "classification", 40)
            {
                Locked = true,
                Lookup = "item-minor-category",
                ParentField = "subCategoryId",
                Help = new(
                    "Choose the minor category; department, category and sub category follow from it.",
                    "মাইনর ক্যাটাগরি বেছে নিন; ডিপার্টমেন্ট, ক্যাটাগরি ও সাব ক্যাটাগরি সেখান থেকেই আসবে।"),
                Placeholder = new("Choose a minor category", "মাইনর ক্যাটাগরি বেছে নিন"),
            },

            new("isAutoBarcode", FieldTypes.Boolean, new("Generate barcode", "বারকোড তৈরি করুন"), "barcode", 10)
            {
                Help = new("Creates an EAN-13 barcode for in-store use.", "স্টোরে ব্যবহারের জন্য একটি EAN-13 বারকোড তৈরি করে।"),
                Default = "true",
            },
            new("barCode", FieldTypes.Text, new("Barcode", "বারকোড"), "barcode", 20) { MaxLength = 50 },
            new("isChildBarcode", FieldTypes.Boolean, new("Child item", "চাইল্ড আইটেম"), "barcode", 30)
            {
                Help = new("A pack or variant of another item, with its own barcode.", "অন্য আইটেমের একটি প্যাক বা ভ্যারিয়েন্ট, নিজস্ব বারকোডসহ।"),
            },
            new("masterItemId", FieldTypes.Lookup, new("Master item", "মাস্টার আইটেম"), "barcode", 40)
            {
                Lookup = "item",
                Placeholder = new("Search code, name or barcode", "কোড, নাম বা বারকোড খুঁজুন"),
            },

            new("uomId", FieldTypes.Lookup, new("Unit of measure", "পরিমাপের একক"), "units", 10)
            {
                Locked = true,
                Lookup = "uom",
                ListOrder = 50,
            },
            new("isNoDecimalQty", FieldTypes.Boolean, new("Whole quantities only", "শুধু পূর্ণ সংখ্যা"), "units", 20)
            {
                Help = new("Quantities of this item cannot have decimals.", "এই আইটেমের পরিমাণে দশমিক থাকবে না।"),
            },
            new("altUomId", FieldTypes.Lookup, new("Alternate unit", "বিকল্প একক"), "units", 30)
            {
                Lookup = "uom",
                Placeholder = new("None", "নেই"),
            },
            new("altUomConversionQty", FieldTypes.Decimal, new("Quantity in one alternate unit", "এক বিকল্প এককে পরিমাণ"), "units", 40)
            {
                Help = new("e.g. one Box holds 10 Strips", "যেমন: এক বক্সে ১০টি স্ট্রিপ"),
                Min = 0.0001m,
                Precision = 4,
            },

            new("lastCostPrice", FieldTypes.Money, new("Last cost price", "সর্বশেষ ক্রয় মূল্য"), "prices", 10) { Min = 0m },
            new("averageCostPrice", FieldTypes.Money, new("Average cost price", "গড় ক্রয় মূল্য"), "prices", 20) { Min = 0m },
            new("salesPrice", FieldTypes.Money, new("Sales price", "বিক্রয় মূল্য"), "prices", 30) { Min = 0m, ListOrder = 60, Sort = true },
            new("supplierName", FieldTypes.Text, new("Supplier", "সরবরাহকারী"), "prices", 40)
            {
                MaxLength = 100,
                Help = new("Free text until supplier records are available.", "সরবরাহকারীর রেকর্ড চালু না হওয়া পর্যন্ত মুক্ত লেখা।"),
            },

            new("purchaseSupplyTypeId", FieldTypes.Lookup, new("Purchase supply type", "ক্রয় সাপ্লাই টাইপ"), "vat", 10)
            {
                Required = true,
                Lookup = "vat-supply-type",
                Default = "1",
            },
            new("purchaseVatPercentage", FieldTypes.Percent, new("Purchase VAT", "ক্রয় ভ্যাট"), "vat", 20) { Min = 0m, Max = 100m, Default = "15" },
            new("salesSupplyTypeId", FieldTypes.Lookup, new("Sales supply type", "বিক্রয় সাপ্লাই টাইপ"), "vat", 30)
            {
                Required = true,
                Lookup = "vat-supply-type",
                Default = "1",
            },
            new("salesVatPercentage", FieldTypes.Percent, new("Sales VAT", "বিক্রয় ভ্যাট"), "vat", 40) { Min = 0m, Max = 100m, Default = "15" },
            new("sdPercentage", FieldTypes.Percent, new("Supplementary duty (SD)", "সম্পূরক শুল্ক (এসডি)"), "vat", 50) { Min = 0m, Max = 100m },
            new("isVatInclusive", FieldTypes.Boolean, new("Prices include VAT", "মূল্যে ভ্যাট অন্তর্ভুক্ত"), "vat", 60),
            new("isVatRebatable", FieldTypes.Boolean, new("VAT rebate allowed", "ভ্যাট রেয়াত প্রযোজ্য"), "vat", 70) { Default = "true" },

            new("isActive", FieldTypes.Boolean, new("Active", "সক্রিয়"), "usage", 10) { Default = "true" },
            new("isSaleable", FieldTypes.Boolean, new("Can be sold", "বিক্রয়যোগ্য"), "usage", 20) { Default = "true" },
            new("isPurchase", FieldTypes.Boolean, new("Can be purchased", "ক্রয়যোগ্য"), "usage", 30) { Default = "true" },
            new("isNegativeSales", FieldTypes.Boolean, new("Sell below zero stock", "শূন্য মজুতের নিচে বিক্রয়"), "usage", 40),
            new("isMultipleSalesPrice", FieldTypes.Boolean, new("Multiple sales prices", "একাধিক বিক্রয় মূল্য"), "usage", 50),
        ]),
    ];
}
