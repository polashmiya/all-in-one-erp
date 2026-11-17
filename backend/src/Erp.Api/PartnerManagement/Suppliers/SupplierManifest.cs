using Erp.Metadata;

namespace Erp.Api.PartnerManagement.Suppliers;

public sealed class SupplierManifest : IFeatureManifestProvider
{
    public IEnumerable<FeatureManifest> GetManifests() =>
    [
        new(PartnerManagementMenu.Supplier,
        [
            new("general", new("General", "সাধারণ"), 10),
            new("terms", new("Purchase terms", "ক্রয়ের শর্ত"), 20),
            new("tax", new("Tax & legal", "কর ও আইনি"), 30),
            .. PartnerFields.GeneralSections("general"),
            new("payment", new("Payment", "পরিশোধ"), 10, "terms"),
            new("receiving", new("Receiving", "মাল গ্রহণ"), 20, "terms"),
            .. PartnerFields.TaxSections("tax"),
        ],
        [
            .. PartnerFields.Basic(PartnerLookups.SupplierGroup, new("Supplier group", "সরবরাহকারী গ্রুপ")),
            .. PartnerFields.Contact(mobileListOrder: 50, contactPersonListOrder: 40),
            .. PartnerFields.Address(),
            .. PartnerFields.Notes(),
            .. PartnerFields.Status(
                new("Foreign supplier", "বিদেশি সরবরাহকারী"),
                new("Supplies from outside the country.", "দেশের বাইরে থেকে সরবরাহ করেন।")),

            PartnerFields.PaymentDays("payment", 10, new("Days we have to pay this supplier.", "এই সরবরাহকারীকে পরিশোধে আমরা কত দিন পাই।"), listOrder: 70),
            PartnerFields.CreditLimit("payment", 20, new("Most we may owe this supplier. Leave empty for no limit.", "এই সরবরাহকারীর কাছে সর্বোচ্চ যত বাকি রাখা যাবে। সীমা না থাকলে ফাঁকা রাখুন।")),
            new("tolerance", FieldTypes.Percent, new("Receive tolerance", "গ্রহণের সহনসীমা"), "receiving", 10)
            {
                Min = 0,
                Max = 100,
                Help = new("Extra quantity allowed when receiving against a purchase order.", "ক্রয় আদেশের বিপরীতে মাল নেওয়ার সময় অতিরিক্ত যতটুকু পরিমাণ নেওয়া যাবে।"),
            },

            .. PartnerFields.Tax(new("We deduct VAT at source when paying this supplier.", "এই সরবরাহকারীকে পরিশোধের সময় আমরা উৎসে ভ্যাট কেটে রাখি।")),
        ]),
    ];
}
