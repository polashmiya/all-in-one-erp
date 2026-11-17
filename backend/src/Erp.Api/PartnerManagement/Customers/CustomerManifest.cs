using Erp.Api.UserManagement.Employees;
using Erp.Metadata;

namespace Erp.Api.PartnerManagement.Customers;

public sealed class CustomerManifest : IFeatureManifestProvider
{
    public IEnumerable<FeatureManifest> GetManifests() =>
    [
        new(PartnerManagementMenu.Customer,
        [
            new("general", new("General", "সাধারণ"), 10),
            new("sales", new("Sales & credit", "বিক্রয় ও বাকি"), 20),
            new("tax", new("Tax & legal", "কর ও আইনি"), 30),
            new("personal", new("Personal", "ব্যক্তিগত"), 40),
            .. PartnerFields.GeneralSections("general"),
            new("credit", new("Credit", "বাকি"), 10, "sales"),
            new("salesTeam", new("Sales team", "বিক্রয় দল"), 20, "sales"),
            .. PartnerFields.TaxSections("tax"),
            new("details", new("Personal details", "ব্যক্তিগত তথ্য"), 10, "personal"),
        ],
        [
            .. PartnerFields.Basic(PartnerLookups.CustomerGroup, new("Customer group", "গ্রাহক গ্রুপ")),
            new("priceTypeId", FieldTypes.Lookup, new("Price type", "মূল্যের ধরন"), "basic", 50)
            {
                Lookup = PartnerLookups.PriceType,
                Filter = true,
                Help = new("The price list used when selling to this customer.", "এই গ্রাহকের কাছে বিক্রিতে যে মূল্য তালিকা ব্যবহার হবে।"),
            },
            .. PartnerFields.Contact(mobileListOrder: 40),
            .. PartnerFields.Address(),
            new("territoryId", FieldTypes.Lookup, new("Territory", "এলাকা"), "address", 50)
            {
                Lookup = PartnerLookups.Territory,
                Hidden = true,
            },
            .. PartnerFields.Notes(),
            .. PartnerFields.Status(
                new("Foreign customer", "বিদেশি গ্রাহক"),
                new("Buys from outside the country.", "দেশের বাইরে থেকে কেনেন।")),

            PartnerFields.CreditLimit("credit", 10, new("Leave empty for no limit.", "সীমা না থাকলে ফাঁকা রাখুন।"), listOrder: 70),
            PartnerFields.PaymentDays("credit", 20, new("Days the customer has to pay an invoice.", "ইনভয়েস পরিশোধে গ্রাহক কত দিন পাবেন।")),
            new("salesRepresentativeId", FieldTypes.Lookup, new("Sales representative", "সেলস রিপ্রেজেন্টেটিভ"), "salesTeam", 10)
            {
                Lookup = EmployeeLookups.Employee,
                ListOrder = 80,
            },

            .. PartnerFields.Tax(new("The customer deducts VAT at source when paying.", "পরিশোধের সময় গ্রাহক উৎসে ভ্যাট কেটে রাখেন।")),

            new("dateOfBirth", FieldTypes.Date, new("Date of birth", "জন্ম তারিখ"), "details", 10),
            new("genderId", FieldTypes.Lookup, new("Gender", "লিঙ্গ"), "details", 20) { Lookup = EmployeeLookups.Gender },
            new("profession", FieldTypes.Text, new("Profession", "পেশা"), "details", 30) { MaxLength = 200 },
            new("marriageDate", FieldTypes.Date, new("Marriage anniversary", "বিবাহবার্ষিকী"), "details", 40) { Hidden = true },
        ]),
    ];
}
