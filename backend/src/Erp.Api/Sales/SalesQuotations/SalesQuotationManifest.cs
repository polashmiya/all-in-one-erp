using Erp.Api.UserManagement.Employees;
using Erp.Metadata;

namespace Erp.Api.Sales.SalesQuotations;

public sealed class SalesQuotationManifest : IFeatureManifestProvider
{
    public IEnumerable<FeatureManifest> GetManifests() =>
    [
        new(SalesMenu.SalesQuotation,
        [
            new("general", new("General", "সাধারণ"), 10),
            new("basic", new("Quotation", "কোটেশন"), 10, "general", Columns: 6),
            new("contact", new("Billing & shipping", "বিলিং ও শিপিং"), 20, "general"),
            new("pricing", new("Discount & charges", "ছাড় ও চার্জ"), 30, "general", Columns: 6),
            new("letter", new("Quotation letter", "কোটেশন পত্র"), 40, "general", Columns: 6),
        ],
        [
            new("code", FieldTypes.Text, new("Quotation no.", "কোটেশন নম্বর"), "basic", 10)
            {
                MaxLength = 100,
                Placeholder = new("Generated on save", "সংরক্ষণের সময় তৈরি হবে"),
                ListOrder = 10,
                Sort = true,
            },
            new("quotationTypeId", FieldTypes.Lookup, new("Quotation type", "কোটেশনের ধরন"), "basic", 20)
            {
                Lookup = SalesLookups.QuotationType,
                ListOrder = 50,
                Filter = true,
            },
            new("quotationDate", FieldTypes.Date, new("Quotation date", "কোটেশনের তারিখ"), "basic", 30)
            {
                Locked = true,
                ListOrder = 20,
                Sort = true,
            },
            new("validityDate", FieldTypes.Date, new("Valid until", "মেয়াদ শেষ"), "basic", 40)
            {
                Locked = true,
                ListOrder = 40,
                Sort = true,
            },
            new("version", FieldTypes.Text, new("Version", "সংস্করণ"), "basic", 50) { MaxLength = 100 },
            new("versionDate", FieldTypes.Date, new("Version date", "সংস্করণের তারিখ"), "basic", 60),
            new("customerId", FieldTypes.Lookup, new("Customer", "গ্রাহক"), "basic", 70)
            {
                Locked = true,
                ColSpan = 2,
                Lookup = SalesLookups.Customer,
                ListOrder = 30,
                Filter = true,
            },
            new("officeId", FieldTypes.Lookup, new("Office", "অফিস"), "basic", 80)
            {
                Lookup = EmployeeLookups.Office,
                Filter = true,
            },
            new("salesForceId", FieldTypes.Lookup, new("Sales force", "বিক্রয় প্রতিনিধি"), "basic", 90)
            {
                Lookup = EmployeeLookups.Employee,
                ListOrder = 60,
                Filter = true,
            },
            new("reference", FieldTypes.Text, new("Reference", "রেফারেন্স"), "basic", 100)
            {
                MaxLength = 1250,
                ColSpan = 2,
                Placeholder = new("Enquiry, tender or work order no.", "চাহিদা, টেন্ডার বা কার্যাদেশ নম্বর"),
            },

            new("billingContactPerson", FieldTypes.Text, new("Billing contact person", "বিলিং যোগাযোগ ব্যক্তি"), "contact", 10) { MaxLength = 300 },
            new("billingPhoneNo", FieldTypes.Text, new("Billing phone no.", "বিলিং ফোন নম্বর"), "contact", 20) { MaxLength = 300 },
            new("billingAddress", FieldTypes.Text, new("Billing address", "বিলিং ঠিকানা"), "contact", 30) { MaxLength = 300, ColSpan = 2 },
            new("shippingContactPerson", FieldTypes.Text, new("Shipping contact person", "শিপিং যোগাযোগ ব্যক্তি"), "contact", 40) { MaxLength = 300 },
            new("shippingAddressId", FieldTypes.Lookup, new("Ship to", "শিপিং গন্তব্য"), "contact", 50) { Lookup = SalesLookups.ShippingAddress },
            new("shippingAddressName", FieldTypes.Text, new("Shipping address", "শিপিং ঠিকানা"), "contact", 60) { MaxLength = 300, ColSpan = 2 },

            new("isVatInclusive", FieldTypes.Boolean, new("Prices include VAT", "দামে ভ্যাট ধরা আছে"), "pricing", 10),
            new("isGrossDiscount", FieldTypes.Boolean, new("Discount on total", "মোটের উপর ছাড়"), "pricing", 20),
            new("discountPercentage", FieldTypes.Percent, new("Discount %", "ছাড় %"), "pricing", 30) { Min = 0, Max = 100 },
            new("discountAmount", FieldTypes.Money, new("Discount amount", "ছাড়ের পরিমাণ"), "pricing", 40) { Min = 0 },
            new("deliveryCharge", FieldTypes.Money, new("Delivery charge", "পরিবহন খরচ"), "pricing", 50) { Min = 0 },
            new("extraCharge", FieldTypes.Money, new("Extra charge", "অতিরিক্ত চার্জ"), "pricing", 60) { Min = 0 },

            new("subjects", FieldTypes.Text, new("Subject", "বিষয়"), "letter", 10) { MaxLength = 1250, ColSpan = 4 },
            new("bodyNarration", FieldTypes.TextArea, new("Letter body", "পত্রের মূল অংশ"), "letter", 20) { ColSpan = 2 },
            new("footerNarration", FieldTypes.TextArea, new("Letter footer", "পত্রের শেষাংশ"), "letter", 30) { ColSpan = 2 },
            new("narration", FieldTypes.TextArea, new("Narration", "মন্তব্য"), "letter", 40) { ColSpan = 2 },
        ]),
    ];
}
