using Erp.Metadata;

namespace Erp.Modules.Inventory.ItemManagement.Items;

public sealed class ItemLookups : ILookupProvider
{
    public const string ItemType = "item-type";
    public const string SupplyType = "vat-supply-type";

    public IEnumerable<LookupDef> GetLookups() =>
    [
        new(ItemType, new("Item type", "আইটেমের ধরন"),
        [
            new("FG", new("Finished Goods", "তৈরি পণ্য")),
            new("RM", new("Raw Material", "কাঁচামাল")),
            new("SFG", new("Semi-Finished Goods", "আধা-তৈরি পণ্য")),
            new("PM", new("Packaging Material", "প্যাকেজিং উপকরণ")),
            new("CON", new("Consumable", "ভোগ্য সামগ্রী")),
            new("SP", new("Spare Parts", "খুচরা যন্ত্রাংশ")),
            new("SRV", new("Service", "সেবা")),
            new("FA", new("Fixed Asset", "স্থায়ী সম্পদ")),
        ]),
        new(SupplyType, new("VAT supply type", "ভ্যাট সাপ্লাই টাইপ"),
        [
            new("STD", new("Standard VAT", "স্ট্যান্ডার্ড ভ্যাট"), IsDefault: true),
            new("RED", new("Reduced VAT", "হ্রাসকৃত ভ্যাট")),
            new("ZERO", new("Zero Rated", "শূন্য হার")),
            new("EXEMPT", new("Exempted", "অব্যাহতিপ্রাপ্ত")),
            new("SPECIFIC", new("Specific VAT", "সুনির্দিষ্ট ভ্যাট")),
            new("MRP", new("MRP Based", "এমআরপি ভিত্তিক")),
        ]),
    ];
}
