using Erp.Core.Modules;
using Erp.Metadata;

namespace Erp.Modules.Inventory.ItemManagement;

public sealed class ItemMasterDataManifest : IFeatureManifestProvider
{
    private const string Tab = "general";
    private const string Section = "details";
    private const int NameLength = 100;

    private static readonly MenuText ImageHelp = new("PNG, JPG or WebP · resized to 320 px", "PNG, JPG বা WebP · ৩২০ পিক্সেলে ছোট করা হবে");

    public IEnumerable<FeatureManifest> GetManifests() =>
    [
        Screen(ItemManagementMenu.ItemDepartment,
        [
            new("code", FieldTypes.Text, new("Department code", "ডিপার্টমেন্ট কোড"), Section, 10)
            {
                Locked = true,
                MaxLength = 20,
                Placeholder = new("e.g. PHARMA", "যেমন: PHARMA"),
                ListOrder = 20,
                Sort = true,
            },
            new("serial", FieldTypes.Number, new("Serial", "ক্রমিক"), Section, 20)
            {
                Min = 0,
                Max = 999_999,
                ListOrder = 30,
                Sort = true,
                Help = new("Departments are listed in this order. Leave empty to add it last.", "ডিপার্টমেন্ট এই ক্রমে দেখানো হয়। ফাঁকা রাখলে শেষে যোগ হবে।"),
            },
            Name(new("Department name", "ডিপার্টমেন্টের নাম"), 30, new("e.g. Pharmaceuticals", "যেমন: ফার্মাসিউটিক্যালস")),
            Image(40),
            Active(50),
        ]),
        Screen(ItemManagementMenu.ItemCategory,
        [
            Parent("departmentId", new("Department", "ডিপার্টমেন্ট"), 10, "item-department", null, new("Choose a department", "ডিপার্টমেন্ট বেছে নিন")) with
            {
                ListOrder = 30,
                Sort = true,
                Help = new("Moving the category also moves its sub categories.", "ক্যাটাগরি সরালে এর সাব-ক্যাটাগরিগুলোও সরে যাবে।"),
            },
            new("code", FieldTypes.Text, new("Category code", "ক্যাটাগরি কোড"), Section, 20)
            {
                Locked = true,
                MaxLength = 3,
                Help = new("Up to 3 characters; item codes start with it.", "সর্বোচ্চ ৩ অক্ষর; আইটেম কোড এটি দিয়ে শুরু হয়।"),
                Placeholder = new("e.g. INJ", "যেমন: INJ"),
                ListOrder = 20,
                Sort = true,
            },
            Name(new("Category name", "ক্যাটাগরির নাম"), 30, new("e.g. Injection", "যেমন: ইনজেকশন")),
            Image(40),
            Active(50),
        ]),
        Screen(ItemManagementMenu.ItemSubCategory,
        [
            Filter("departmentId", new("Department", "ডিপার্টমেন্ট"), 10, "item-department", null, new("All departments", "সব ডিপার্টমেন্ট")) with
            {
                ListOrder = 30,
                Sort = true,
                Help = new("Narrows the category list; saved from the category.", "ক্যাটাগরির তালিকা ছোট করে; ক্যাটাগরি থেকেই সংরক্ষিত হয়।"),
            },
            Parent("categoryId", new("Category", "ক্যাটাগরি"), 20, "item-category", "departmentId", new("Choose a category", "ক্যাটাগরি বেছে নিন")) with
            {
                ListOrder = 20,
                Sort = true,
                Help = new("Moving the sub category also moves its minor categories.", "সাব-ক্যাটাগরি সরালে এর মাইনর ক্যাটাগরিগুলোও সরে যাবে।"),
            },
            Name(new("Sub category name", "সাব ক্যাটাগরির নাম"), 30, new("e.g. Antibiotic injection", "যেমন: অ্যান্টিবায়োটিক ইনজেকশন")),
            Image(40),
            Active(50),
        ]),
        Screen(ItemManagementMenu.ItemMinorCategory,
        [
            Filter("departmentId", new("Department", "ডিপার্টমেন্ট"), 10, "item-department", null, new("All departments", "সব ডিপার্টমেন্ট")) with { ListOrder = 40 },
            Filter("categoryId", new("Category", "ক্যাটাগরি"), 20, "item-category", "departmentId", new("All categories", "সব ক্যাটাগরি")) with { ListOrder = 30, Sort = true },
            Parent("subCategoryId", new("Sub category", "সাব ক্যাটাগরি"), 30, "item-sub-category", "categoryId", new("Choose a sub category", "সাব ক্যাটাগরি বেছে নিন")) with
            {
                ListOrder = 20,
                Sort = true,
                Help = new("Department and category only narrow this list; they are saved from the sub category.", "ডিপার্টমেন্ট ও ক্যাটাগরি শুধু এই তালিকা ছোট করে; সাব-ক্যাটাগরি থেকেই সংরক্ষিত হয়।"),
            },
            Name(new("Minor category name", "মাইনর ক্যাটাগরির নাম"), 40, new("e.g. Cephalosporin", "যেমন: সেফালোস্পোরিন")),
            Image(50),
            Active(60),
        ]),
        Screen(ItemManagementMenu.Uom,
        [
            new("code", FieldTypes.Text, new("UOM code", "একক কোড"), Section, 10)
            {
                Locked = true,
                MaxLength = 10,
                Placeholder = new("e.g. PCS", "যেমন: PCS"),
                ListOrder = 20,
                Sort = true,
            },
            new("name", FieldTypes.Text, new("UOM name", "এককের নাম"), Section, 20)
            {
                Locked = true,
                MaxLength = 50,
                Placeholder = new("e.g. Piece", "যেমন: পিস"),
                ListOrder = 10,
                Sort = true,
            },
            new("description", FieldTypes.TextArea, new("Description", "বিবরণ"), Section, 30) { MaxLength = 255, ColSpan = 2, ListOrder = 40 },
            new("isDecimalAllowed", FieldTypes.Boolean, new("Allow decimal quantity", "দশমিক পরিমাণ অনুমোদন"), Section, 40)
            {
                ListOrder = 30,
                Help = new("Turn on for units such as kg or litre.", "কেজি বা লিটারের মতো এককের জন্য চালু করুন।"),
            },
            new("roundingLength", FieldTypes.Number, new("Decimal places", "দশমিক ঘর"), Section, 50)
            {
                Min = 1,
                Max = 6,
                Default = "2",
                Help = new("Quantities are rounded to this many places.", "পরিমাণ এত ঘর পর্যন্ত রাউন্ড করা হয়।"),
            },
            Active(60),
        ]),
    ];

    private static FeatureManifest Screen(string feature, IReadOnlyList<FieldDef> fields) => new(
        feature,
        [
            new(Tab, new("General", "সাধারণ"), 10),
            new(Section, new("Details", "বিবরণ"), 10, Tab, 2),
        ],
        fields);

    private static FieldDef Name(MenuText label, int order, MenuText placeholder) =>
        new("name", FieldTypes.Text, label, Section, order)
        {
            Locked = true,
            MaxLength = NameLength,
            Placeholder = placeholder,
            ListOrder = 10,
            Sort = true,
        };

    private static FieldDef Parent(string key, MenuText label, int order, string lookup, string? parent, MenuText placeholder) =>
        new(key, FieldTypes.Lookup, label, Section, order) { Locked = true, Lookup = lookup, ParentField = parent, Placeholder = placeholder };

    private static FieldDef Filter(string key, MenuText label, int order, string lookup, string? parent, MenuText placeholder) =>
        new(key, FieldTypes.Lookup, label, Section, order) { Lookup = lookup, ParentField = parent, Placeholder = placeholder };

    private static FieldDef Image(int order) =>
        new("imageSource", FieldTypes.Image, new("Image", "ছবি"), Section, order) { Help = ImageHelp, ColSpan = 2 };

    private static FieldDef Active(int order) =>
        new("isActive", FieldTypes.Boolean, new("Active", "সক্রিয়"), Section, order)
        {
            Default = "true",
            Help = new("Inactive records stay on old documents but cannot be chosen for new ones.", "নিষ্ক্রিয় রেকর্ড পুরোনো নথিতে থাকে, কিন্তু নতুন নথিতে বেছে নেওয়া যায় না।"),
        };
}
