using Erp.Api.UserManagement.Employees;
using Erp.Metadata;

namespace Erp.Api.Sales.SalesDeliveries;

public sealed class SalesDeliveryManifest : IFeatureManifestProvider
{
    public IEnumerable<FeatureManifest> GetManifests() =>
    [
        new(SalesMenu.SalesDelivery,
        [
            new("general", new("General", "সাধারণ"), 10),
            new("basic", new("Delivery", "ডেলিভারি"), 10, "general"),
            new("challan", new("Challan & transport", "চালান ও পরিবহন"), 20, "general"),
            new("shipping", new("Shipping", "শিপিং"), 30, "general"),
            new("payment", new("Payment & installment", "পরিশোধ ও কিস্তি"), 40, "general"),
            new("charges", new("Discount & charges", "ছাড় ও চার্জ"), 50, "general"),
            new("export", new("Commission & currency", "কমিশন ও মুদ্রা"), 60, "general"),
            new("notes", new("Notes", "মন্তব্য"), 70, "general"),
        ],
        [
            new("type", FieldTypes.Lookup, new("Delivery type", "ডেলিভারির ধরন"), "basic", 10)
            {
                Locked = true,
                Lookup = SalesLookups.DeliveryType,
                ListOrder = 20,
                Filter = true,
            },
            new("orderId", FieldTypes.Lookup, new("Sales order", "বিক্রয় আদেশ"), "basic", 20)
            {
                Lookup = SalesLookups.Order,
                ListOrder = 40,
            },
            SalesFields.Code(30, new("Delivery no.", "ডেলিভারি নম্বর")),
            new("deliveryDate", FieldTypes.Date, new("Delivery date", "ডেলিভারির তারিখ"), "basic", 40)
            {
                Locked = true,
                ListOrder = 25,
                Sort = true,
            },
            SalesFields.Customer(50),
            new("orderDate", FieldTypes.Date, new("Order date", "আদেশের তারিখ"), "basic", 55) { Locked = true },
            new("phone", FieldTypes.Text, new("Phone", "ফোন"), "basic", 56) { MaxLength = 100 },
            .. SalesFields.Organization(60, wide: 2),
            new("anonymousAddress", FieldTypes.Text, new("Walk-in customer address", "খুচরা গ্রাহকের ঠিকানা"), "basic", 70)
            {
                MaxLength = 500,
                ColSpan = 2,
            },

            new("challanNo", FieldTypes.Text, new("Challan no.", "চালান নম্বর"), "challan", 10) { MaxLength = 250 },
            new("vatChallanNo", FieldTypes.Text, new("VAT challan no.", "ভ্যাট চালান নম্বর"), "challan", 20) { MaxLength = 200 },
            new("billNo", FieldTypes.Text, new("Bill no.", "বিল নম্বর"), "challan", 30) { MaxLength = 100 },
            new("refInvoice", FieldTypes.Text, new("Reference invoice", "রেফারেন্স ইনভয়েস"), "challan", 40) { MaxLength = 250 },
            .. SalesFields.CustomerOrder("challan", 50),
            new("deliveryMethodId", FieldTypes.Lookup, new("Delivery method", "ডেলিভারি পদ্ধতি"), "challan", 60)
            {
                Lookup = SalesLookups.DeliveryMethod,
                ColSpan = 2,
            },
            new("courierOrEmployeeId", FieldTypes.Lookup, new("Delivered by", "ডেলিভারি দাতা"), "challan", 70)
            {
                Lookup = EmployeeLookups.Employee,
                ColSpan = 2,
            },
            new("submissionDate", FieldTypes.Date, new("Bill submission date", "বিল জমার তারিখ"), "challan", 80),
            new("alternativeDeliveryDate", FieldTypes.Date, new("Alternative date", "বিকল্প তারিখ"), "challan", 90),

            .. SalesFields.Shipping(),

            .. SalesFields.PaymentTerms("payment", 10),
            new("advanceBalanceAdjust", FieldTypes.Money, new("Adjust from advance", "অগ্রিম থেকে সমন্বয়"), "payment", 20) { Min = 0 },
            new("receiveAmount", FieldTypes.Money, new("Received now", "এখন প্রাপ্ত"), "payment", 30) { Min = 0 },
            .. SalesFields.Receipt("payment", 40),

            new("interestRate", FieldTypes.Percent, new("Interest rate", "সুদের হার"), "payment", 60) { Min = 0, Max = 100 },
            new("totalNoOfInstallment", FieldTypes.Number, new("No. of installments", "কিস্তির সংখ্যা"), "payment", 70) { Min = 0, Max = 360 },
            new("installmentTypeId", FieldTypes.Lookup, new("Installment type", "কিস্তির ধরন"), "payment", 80) { Lookup = SalesLookups.InstallmentType },
            new("installmentStartDate", FieldTypes.Date, new("Installment start", "কিস্তি শুরু"), "payment", 90),

            .. SalesFields.Charges(),

            new("isExportSales", FieldTypes.Boolean, new("Export sale", "রপ্তানি বিক্রয়"), "export", 10),
            .. SalesFields.Currency("export", 20),
            new("commissionPercentage", FieldTypes.Percent, new("Commission %", "কমিশন %"), "export", 30) { Min = 0, Max = 100 },

            SalesFields.Remarks(10),
            new("narration", FieldTypes.TextArea, new("Narration", "বিবরণ"), "notes", 20) { MaxLength = 4000, ColSpan = 2 },
        ]),
    ];
}
