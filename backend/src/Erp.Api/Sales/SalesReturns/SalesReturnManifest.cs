using Erp.Api.UserManagement.Employees;
using Erp.Metadata;

namespace Erp.Api.Sales.SalesReturns;

public sealed class SalesReturnManifest : IFeatureManifestProvider
{
    public IEnumerable<FeatureManifest> GetManifests() =>
    [
        new(SalesMenu.SalesReturn,
        [
            new("general", new("General", "সাধারণ"), 10),
            new("basic", new("Return", "ফেরত"), 10, "general", Columns: 6),
            new("reference", new("Reference & credit note", "রেফারেন্স ও ক্রেডিট নোট"), 20, "general", Columns: 6),
            new("settlement", new("Deduction & refund", "কর্তন ও টাকা ফেরত"), 30, "general", Columns: 6),
            new("notes", new("Notes", "মন্তব্য"), 40, "general"),
        ],
        [
            new("source", FieldTypes.Lookup, new("Return from", "যেখান থেকে ফেরত"), "basic", 10)
            {
                Locked = true,
                Lookup = SalesLookups.ReturnSource,
                ListOrder = 35,
                Filter = true,
            },
            new("returnDate", FieldTypes.Date, new("Return date", "ফেরতের তারিখ"), "basic", 20)
            {
                Locked = true,
                ListOrder = 20,
                Sort = true,
            },
            new("deliveryId", FieldTypes.Lookup, new("Sales delivery", "বিক্রয় ডেলিভারি"), "basic", 30)
            {
                ColSpan = 2,
                Lookup = SalesLookups.Delivery,
                ListOrder = 40,
            },
            new("customerId", FieldTypes.Lookup, new("Customer", "গ্রাহক"), "basic", 40)
            {
                Locked = true,
                ColSpan = 2,
                Lookup = SalesLookups.Customer,
                ListOrder = 30,
                Filter = true,
            },
            SalesFields.Code(45, new("Return no.", "ফেরত নম্বর")),
            new("returnTypeId", FieldTypes.Lookup, new("Return type", "ফেরতের ধরন"), "basic", 50) { Lookup = SalesLookups.ReturnType },
            new("returnReasonId", FieldTypes.Lookup, new("Return reason", "ফেরতের কারণ"), "basic", 60)
            {
                Lookup = SalesLookups.ReturnReason,
                ListOrder = 60,
                Filter = true,
            },
            new("warehouseId", FieldTypes.Lookup, new("Warehouse", "গুদাম"), "basic", 70)
            {
                Locked = true,
                Lookup = EmployeeLookups.Warehouse,
                ListOrder = 50,
                Filter = true,
            },
            new("officeId", FieldTypes.Lookup, new("Office", "অফিস"), "basic", 80) { Lookup = EmployeeLookups.Office },
            new("salesForceId", FieldTypes.Lookup, new("Sales force", "বিক্রয় প্রতিনিধি"), "basic", 90) { Lookup = EmployeeLookups.Employee },

            new("returnRequestNo", FieldTypes.Text, new("Return request no.", "ফেরত আবেদন নম্বর"), "reference", 10) { MaxLength = 200 },
            new("returnRefNo", FieldTypes.Text, new("Customer reference no.", "গ্রাহকের রেফারেন্স নম্বর"), "reference", 20) { MaxLength = 200 },
            new("returnRefDate", FieldTypes.Date, new("Reference date", "রেফারেন্সের তারিখ"), "reference", 30),
            new("returnRefAmount", FieldTypes.Money, new("Reference amount", "রেফারেন্স মূল্য"), "reference", 40) { Min = 0 },
            new("creditNoteNo", FieldTypes.Text, new("Credit note no.", "ক্রেডিট নোট নম্বর"), "reference", 50) { MaxLength = 200 },
            new("creditNoteDate", FieldTypes.Date, new("Credit note date", "ক্রেডিট নোটের তারিখ"), "reference", 60),
            new("challanNo", FieldTypes.Text, new("Challan no.", "চালান নম্বর"), "reference", 70) { MaxLength = 250 },
            new("vatChallanNo", FieldTypes.Text, new("VAT challan no.", "ভ্যাট চালান নম্বর"), "reference", 80) { MaxLength = 200 },
            new("phone", FieldTypes.Text, new("Phone", "ফোন"), "reference", 90) { MaxLength = 100 },
            new("deliveryMethodId", FieldTypes.Lookup, new("Brought by", "যেভাবে এসেছে"), "reference", 100) { Lookup = SalesLookups.DeliveryMethod },
            new("pickupAddress", FieldTypes.Text, new("Pickup address", "সংগ্রহের ঠিকানা"), "reference", 110) { MaxLength = 300 },
            new("receivedById", FieldTypes.Lookup, new("Goods received by", "মাল গ্রহণকারী"), "reference", 120) { Lookup = EmployeeLookups.Employee },

            new("chargeRefundAmount", FieldTypes.Money, new("Charge refund", "চার্জ ফেরত"), "settlement", 10) { Min = 0 },
            new("deductionLabel", FieldTypes.Text, new("Deduction for", "কর্তনের কারণ"), "settlement", 20) { MaxLength = 500 },
            new("deductionValue", FieldTypes.Money, new("Deduction amount", "কর্তনের পরিমাণ"), "settlement", 30) { Min = 0 },
            new("refundAmount", FieldTypes.Money, new("Refund now", "এখন টাকা ফেরত"), "settlement", 40) { Min = 0 },
            new("paymentMethodId", FieldTypes.Lookup, new("Refund by", "যেভাবে ফেরত"), "settlement", 50)
            {
                Lookup = Procurement.ProcurementLookups.PaymentMethod,
            },
            new("bankAccountId", FieldTypes.Lookup, new("Refund from", "যে হিসাব থেকে"), "settlement", 60) { Lookup = SalesLookups.BankAccount },
            new("instrumentNo", FieldTypes.Text, new("Cheque / reference no.", "চেক / রেফারেন্স নম্বর"), "settlement", 70)
            {
                MaxLength = 100,
                ColSpan = 2,
            },
            new("instrumentDate", FieldTypes.Date, new("Cheque date", "চেকের তারিখ"), "settlement", 80) { ColSpan = 2 },
            new("isExchange", FieldTypes.Boolean, new("For exchange", "বদলের জন্য"), "settlement", 90),
            new("isVatInclusive", FieldTypes.Boolean, new("Prices include VAT", "দামে ভ্যাট ধরা আছে"), "settlement", 100),

            SalesFields.Remarks(10),
            new("narration", FieldTypes.TextArea, new("Narration", "বিবরণ"), "notes", 20) { MaxLength = 4000, ColSpan = 2 },
        ]),
    ];
}
