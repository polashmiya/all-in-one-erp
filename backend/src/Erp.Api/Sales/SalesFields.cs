using Erp.Api.PartnerManagement;
using Erp.Api.Procurement;
using Erp.Api.UserManagement.Employees;
using Erp.Metadata;

namespace Erp.Api.Sales;

internal static class SalesFields
{
    public static FieldDef Code(int order, Core.Modules.MenuText label) => new("code", FieldTypes.Text, label, "basic", order)
    {
        MaxLength = 50,
        Placeholder = new("Generated on save", "সংরক্ষণের সময় তৈরি হবে"),
        ListOrder = 10,
        Sort = true,
    };

    public static FieldDef Customer(int order) => new("customerId", FieldTypes.Lookup, new("Customer", "গ্রাহক"), "basic", order)
    {
        Locked = true,
        ColSpan = 2,
        Lookup = SalesLookups.Customer,
        ListOrder = 30,
        Filter = true,
    };

    public static IEnumerable<FieldDef> Organization(int order, int wide = 1) =>
    [
        new("officeId", FieldTypes.Lookup, new("Office", "অফিস"), "basic", order) { Lookup = EmployeeLookups.Office },
        new("warehouseId", FieldTypes.Lookup, new("Warehouse", "গুদাম"), "basic", order + 1)
        {
            Locked = true,
            Lookup = EmployeeLookups.Warehouse,
            ListOrder = 50,
            Filter = true,
        },
        new("salesForceId", FieldTypes.Lookup, new("Sales force", "বিক্রয় প্রতিনিধি"), "basic", order + 2)
        {
            Lookup = EmployeeLookups.Employee,
            Filter = true,
        },
        new("priceTypeId", FieldTypes.Lookup, new("Price type", "মূল্যের ধরন"), "basic", order + 3) { Lookup = PartnerLookups.PriceType },
        new("projectName", FieldTypes.Text, new("Project", "প্রকল্প"), "basic", order + 4) { MaxLength = 300, ColSpan = wide },
        new("referredByPartnerId", FieldTypes.Lookup, new("Referred by", "সুপারিশকারী"), "basic", order + 5)
        {
            Lookup = SalesLookups.Customer,
            ColSpan = wide,
        },
    ];

    public static IEnumerable<FieldDef> CustomerOrder(string section, int order) =>
    [
        new("customerPo", FieldTypes.Text, new("Customer PO no.", "গ্রাহকের পিও নম্বর"), section, order) { MaxLength = 100 },
        new("customerPoDate", FieldTypes.Date, new("Customer PO date", "গ্রাহকের পিও তারিখ"), section, order + 1),
    ];

    public static IEnumerable<FieldDef> PaymentTerms(string section, int order) =>
    [
        new("paymentTypeId", FieldTypes.Lookup, new("Payment type", "পরিশোধের ধরন"), section, order)
        {
            Lookup = ProcurementLookups.PaymentType,
            Filter = true,
        },
        new("paymentTermDays", FieldTypes.Number, new("Payment days", "পরিশোধের দিন"), section, order + 1) { Min = 0, Max = 3650 },
    ];

    public static IEnumerable<FieldDef> Shipping() =>
    [
        new("shippingContactPerson", FieldTypes.Text, new("Shipping contact person", "শিপিং যোগাযোগ ব্যক্তি"), "shipping", 10) { MaxLength = 200 },
        new("shippingAddressId", FieldTypes.Lookup, new("Ship to", "শিপিং গন্তব্য"), "shipping", 20) { Lookup = SalesLookups.ShippingAddress },
        new("shippingAddressName", FieldTypes.Text, new("Shipping address", "শিপিং ঠিকানা"), "shipping", 30) { MaxLength = 300, ColSpan = 2 },
    ];

    public static IEnumerable<FieldDef> Receipt(string section, int order) =>
    [
        new("paymentMethodId", FieldTypes.Lookup, new("Received by", "যেভাবে পাওয়া"), section, order) { Lookup = ProcurementLookups.PaymentMethod },
        new("bankAccountId", FieldTypes.Lookup, new("Received in", "যে হিসাবে জমা"), section, order + 1) { Lookup = SalesLookups.BankAccount },
        new("instrumentNo", FieldTypes.Text, new("Cheque / reference no.", "চেক / রেফারেন্স নম্বর"), section, order + 2) { MaxLength = 100 },
        new("instrumentDate", FieldTypes.Date, new("Cheque date", "চেকের তারিখ"), section, order + 3),
    ];

    public static IEnumerable<FieldDef> Charges() =>
    [
        new("isVatInclusive", FieldTypes.Boolean, new("Prices include VAT", "দামে ভ্যাট ধরা আছে"), "charges", 10),
        new("isGrossDiscount", FieldTypes.Boolean, new("Discount on total", "মোটের উপর ছাড়"), "charges", 20),
        new("headerDiscountPercentage", FieldTypes.Percent, new("Discount %", "ছাড় %"), "charges", 30) { Min = 0, Max = 100 },
        new("headerDiscount", FieldTypes.Money, new("Discount amount", "ছাড়ের পরিমাণ"), "charges", 40) { Min = 0 },
        new("specialDiscountAmount", FieldTypes.Money, new("Special discount", "বিশেষ ছাড়"), "charges", 50) { Min = 0 },
        new("freeTypeId", FieldTypes.Lookup, new("Free type", "ফ্রির ধরন"), "charges", 60) { Lookup = SalesLookups.FreeType },
        new("othersCost", FieldTypes.Money, new("Delivery charge", "পরিবহন খরচ"), "charges", 70) { Min = 0 },
        new("otherChargeAccountId", FieldTypes.Lookup, new("Delivery charge account", "পরিবহন খরচের হিসাব"), "charges", 80)
        {
            Lookup = SalesLookups.ChargeAccount,
        },
        new("extraCharge", FieldTypes.Money, new("Extra charge", "অতিরিক্ত চার্জ"), "charges", 90) { Min = 0 },
        new("extraChargeAccountId", FieldTypes.Lookup, new("Extra charge account", "অতিরিক্ত চার্জের হিসাব"), "charges", 100)
        {
            Lookup = SalesLookups.ChargeAccount,
        },
        new("deductionLabel", FieldTypes.Text, new("Deduction for", "কর্তনের কারণ"), "charges", 110) { MaxLength = 500 },
        new("deductionValue", FieldTypes.Money, new("Deduction amount", "কর্তনের পরিমাণ"), "charges", 120) { Min = 0 },
    ];

    public static IEnumerable<FieldDef> Currency(string section, int order) =>
    [
        new("currencyId", FieldTypes.Lookup, new("Currency", "মুদ্রা"), section, order) { Lookup = SalesLookups.Currency },
        new("exchangeRate", FieldTypes.Decimal, new("Exchange rate", "বিনিময় হার"), section, order + 1) { Min = 0 },
    ];

    public static FieldDef Remarks(int order) => new("remarks", FieldTypes.TextArea, new("Remarks", "মন্তব্য"), "notes", order)
    {
        MaxLength = 1500,
        ColSpan = 2,
    };
}
