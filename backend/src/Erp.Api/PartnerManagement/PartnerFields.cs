using Erp.Api.Locations;
using Erp.Core.Modules;
using Erp.Metadata;

namespace Erp.Api.PartnerManagement;

internal static class PartnerFields
{
    public static SectionDef[] GeneralSections(string tab) =>
    [
        new("basic", new("Basic", "মৌলিক তথ্য"), 10, tab),
        new("contact", new("Contact", "যোগাযোগ"), 20, tab),
        new("address", new("Address", "ঠিকানা"), 30, tab),
        new("status", new("Status", "অবস্থা"), 40, tab),
        new("notes", new("Notes", "মন্তব্য"), 50, tab),
    ];

    public static SectionDef[] TaxSections(string tab) =>
    [
        new("registration", new("Registration", "নিবন্ধন"), 10, tab),
        new("vat", new("VAT", "ভ্যাট"), 20, tab),
    ];

    public static FieldDef[] Basic(string groupLookup, MenuText groupLabel) =>
    [
        new("code", FieldTypes.Text, new("Code", "কোড"), "basic", 10)
        {
            MaxLength = 50,
            Help = new("Leave empty to generate one.", "ফাঁকা রাখলে একটি কোড তৈরি হবে।"),
            Placeholder = new("Generated on save", "সংরক্ষণের সময় তৈরি হবে"),
            ListOrder = 20,
            Sort = true,
        },
        new("name", FieldTypes.Text, new("Name", "নাম"), "basic", 20)
        {
            Locked = true,
            MaxLength = 500,
            ListOrder = 10,
            Sort = true,
        },
        new("billingName", FieldTypes.Text, new("Billing name", "বিলের নাম"), "basic", 30)
        {
            MaxLength = 500,
            Help = new("Printed on invoices. Leave empty to use the name.", "ইনভয়েসে ছাপা হবে। ফাঁকা রাখলে নামটিই ব্যবহার হবে।"),
        },
        new("groupId", FieldTypes.Lookup, groupLabel, "basic", 40)
        {
            Lookup = groupLookup,
            ListOrder = 30,
            Filter = true,
        },
    ];

    public static FieldDef[] Contact(int mobileListOrder, int? contactPersonListOrder = null) =>
    [
        new("mobile", FieldTypes.Text, new("Mobile", "মোবাইল"), "contact", 10) { MaxLength = 50, ListOrder = mobileListOrder },
        new("email", FieldTypes.Text, new("Email", "ইমেইল"), "contact", 20) { MaxLength = 100 },
        new("contactPerson", FieldTypes.Text, new("Contact person", "যোগাযোগের ব্যক্তি"), "contact", 30)
        {
            MaxLength = 200,
            ListOrder = contactPersonListOrder,
        },
        new("contactMobile", FieldTypes.Text, new("Contact person's mobile", "যোগাযোগের ব্যক্তির মোবাইল"), "contact", 40) { MaxLength = 50 },
    ];

    public static FieldDef[] Address() =>
    [
        new("address", FieldTypes.TextArea, new("Address", "ঠিকানা"), "address", 10) { MaxLength = 300, ColSpan = 2 },
        new("city", FieldTypes.Text, new("City", "শহর"), "address", 20) { MaxLength = 100 },
        new("districtId", FieldTypes.Lookup, new("District", "জেলা"), "address", 30)
        {
            Lookup = LocationEndpoints.District,
            ListOrder = 60,
            Filter = true,
        },
        new("thanaId", FieldTypes.Lookup, new("Thana", "থানা"), "address", 40)
        {
            Lookup = LocationEndpoints.Thana,
            ParentField = "districtId",
        },
    ];

    public static FieldDef[] Notes() =>
    [
        new("remarks", FieldTypes.TextArea, new("Remarks", "মন্তব্য"), "notes", 10) { MaxLength = 1000 },
    ];

    public static FieldDef[] Status(MenuText foreignLabel, MenuText foreignHelp) =>
    [
        new("isActive", FieldTypes.Boolean, new("Active", "সক্রিয়"), "status", 10) { Default = "true" },
        new("isForeign", FieldTypes.Boolean, foreignLabel, "status", 20) { Help = foreignHelp },
    ];

    public static FieldDef CreditLimit(string section, int order, MenuText help, int? listOrder = null) =>
        new("creditLimit", FieldTypes.Money, new("Credit limit", "বাকির সীমা"), section, order)
        {
            Min = 0,
            Help = help,
            ListOrder = listOrder,
        };

    public static FieldDef PaymentDays(string section, int order, MenuText help, int? listOrder = null) =>
        new("paymentDays", FieldTypes.Number, new("Credit days", "বাকির দিন"), section, order)
        {
            Min = 0,
            Max = 365,
            Help = help,
            ListOrder = listOrder,
        };

    public static FieldDef[] Tax(MenuText vdsHelp) =>
    [
        new("binNumber", FieldTypes.Text, new("BIN", "বিআইএন"), "registration", 10)
        {
            MaxLength = 250,
            Help = new("VAT business identification number.", "ভ্যাট ব্যবসা শনাক্তকরণ নম্বর।"),
        },
        new("tinNumber", FieldTypes.Text, new("TIN", "টিআইএন"), "registration", 20) { MaxLength = 150 },
        new("tradeLicenseNumber", FieldTypes.Text, new("Trade license number", "ট্রেড লাইসেন্স নম্বর"), "registration", 30) { MaxLength = 150 },
        new("nid", FieldTypes.Text, new("NID", "জাতীয় পরিচয়পত্র"), "registration", 40) { MaxLength = 100 },
        new("centralVatRegistrationNo", FieldTypes.Text, new("Central VAT registration no.", "কেন্দ্রীয় ভ্যাট নিবন্ধন নম্বর"), "registration", 50)
        {
            MaxLength = 200,
            Hidden = true,
        },
        new("isVatNotApplicable", FieldTypes.Boolean, new("VAT not applicable", "ভ্যাট প্রযোজ্য নয়"), "vat", 10),
        new("isVdsApplicable", FieldTypes.Boolean, new("VDS applicable", "ভিডিএস প্রযোজ্য"), "vat", 20) { Help = vdsHelp },
    ];
}
