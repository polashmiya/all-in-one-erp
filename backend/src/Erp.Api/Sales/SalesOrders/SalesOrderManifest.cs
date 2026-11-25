using Erp.Metadata;

namespace Erp.Api.Sales.SalesOrders;

public sealed class SalesOrderManifest : IFeatureManifestProvider
{
    public IEnumerable<FeatureManifest> GetManifests() =>
    [
        new(SalesMenu.SalesOrder,
        [
            new("general", new("General", "সাধারণ"), 10),
            new("basic", new("Order", "আদেশ"), 10, "general", Columns: 6),
            new("terms", new("Customer PO & payment terms", "গ্রাহকের পিও ও পরিশোধের শর্ত"), 20, "general", Columns: 6),
            new("shipping", new("Shipping", "শিপিং"), 30, "general"),
            new("advance", new("Advance", "অগ্রিম"), 40, "general", Columns: 6),
            new("charges", new("Discount & charges", "ছাড় ও চার্জ"), 50, "general", Columns: 6),
            new("export", new("Export & LC", "রপ্তানি ও এলসি"), 60, "general"),
            new("notes", new("Notes", "মন্তব্য"), 70, "general", Columns: 2),
        ],
        [
            SalesFields.Code(10, new("Order no.", "আদেশ নম্বর")),
            new("orderDate", FieldTypes.Date, new("Order date", "আদেশের তারিখ"), "basic", 20)
            {
                Locked = true,
                ListOrder = 20,
                Sort = true,
            },
            new("deliveryDate", FieldTypes.Date, new("Delivery date", "ডেলিভারির তারিখ"), "basic", 30)
            {
                Locked = true,
                ListOrder = 40,
                Sort = true,
            },
            new("salesQuotationId", FieldTypes.Lookup, new("Quotation", "কোটেশন"), "basic", 40)
            {
                Lookup = SalesLookups.Quotation,
                ListOrder = 35,
            },
            SalesFields.Customer(50),
            .. SalesFields.Organization(60),
            new("salesTermId", FieldTypes.Lookup, new("Sales term", "বিক্রয়ের শর্ত"), "basic", 70) { Lookup = SalesLookups.SalesTerm },
            new("dueLimit", FieldTypes.Money, new("Due limit", "বাকির সীমা"), "basic", 80) { Min = 0 },
            new("dmsInvoice", FieldTypes.Text, new("DMS invoice", "ডিএমএস ইনভয়েস"), "basic", 90) { MaxLength = 500 },
            new("review", FieldTypes.Text, new("Review", "পর্যালোচনা"), "basic", 100) { MaxLength = 300, ColSpan = 3 },

            .. SalesFields.CustomerOrder("terms", 10),
            new("customerPoAmount", FieldTypes.Money, new("Customer PO amount", "গ্রাহকের পিও মূল্য"), "terms", 20) { Min = 0 },
            new("customerPoRefAmount", FieldTypes.Money, new("PO reference amount", "পিও রেফারেন্স মূল্য"), "terms", 30) { Min = 0 },
            .. SalesFields.PaymentTerms("terms", 40),

            .. SalesFields.Shipping(),

            new("advanceAmount", FieldTypes.Money, new("Advance amount", "অগ্রিমের পরিমাণ"), "advance", 10) { Min = 0 },
            new("isAdvanceAdjust", FieldTypes.Boolean, new("Adjust on delivery", "ডেলিভারিতে সমন্বয়"), "advance", 20) { Default = "true" },
            .. SalesFields.Receipt("advance", 30),

            .. SalesFields.Charges(),

            new("isForeign", FieldTypes.Boolean, new("Export order", "রপ্তানি আদেশ"), "export", 10),
            .. SalesFields.Currency("export", 20),
            new("lcTypeId", FieldTypes.Lookup, new("LC type", "এলসির ধরন"), "export", 30) { Lookup = SalesLookups.LcType },
            new("lcNo", FieldTypes.Text, new("LC no.", "এলসি নম্বর"), "export", 40) { MaxLength = 150 },
            new("paymentTermsId", FieldTypes.Lookup, new("Payment terms", "পরিশোধের শর্ত"), "export", 50) { Lookup = SalesLookups.PaymentTerms },
            new("isAllowPartialShipment", FieldTypes.Boolean, new("Partial shipment", "আংশিক শিপমেন্ট"), "export", 60),
            new("isAllowTransitShipment", FieldTypes.Boolean, new("Transit shipment", "ট্রানজিট শিপমেন্ট"), "export", 70),

            SalesFields.Remarks(10),
        ]),
    ];
}
