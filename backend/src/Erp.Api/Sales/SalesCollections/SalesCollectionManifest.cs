using Erp.Api.UserManagement.Employees;
using Erp.Metadata;

namespace Erp.Api.Sales.SalesCollections;

public sealed class SalesCollectionManifest : IFeatureManifestProvider
{
    public IEnumerable<FeatureManifest> GetManifests() =>
    [
        new(SalesMenu.SalesCollection,
        [
            new("general", new("General", "সাধারণ"), 10),
            new("basic", new("Collection", "আদায়"), 10, "general"),
            new("payment", new("Payment", "পরিশোধ"), 20, "general"),
            new("certificate", new("VDS & AIT certificates", "ভিডিএস ও এআইটি সনদ"), 30, "general"),
            new("notes", new("Notes", "মন্তব্য"), 40, "general"),
        ],
        [
            SalesFields.Code(10, new("Collection no.", "আদায় নম্বর")),
            new("collectionDate", FieldTypes.Date, new("Collection date", "আদায়ের তারিখ"), "basic", 20)
            {
                Locked = true,
                ListOrder = 20,
                Sort = true,
            },
            SalesFields.Customer(30),
            new("isAdvance", FieldTypes.Boolean, new("Advance", "অগ্রিম"), "basic", 40),
            new("advanceAmount", FieldTypes.Money, new("Advance amount", "অগ্রিমের পরিমাণ"), "basic", 50) { Min = 0 },
            new("officeId", FieldTypes.Lookup, new("Office", "অফিস"), "basic", 60) { Lookup = EmployeeLookups.Office },
            new("collectedById", FieldTypes.Lookup, new("Collected by", "আদায়কারী"), "basic", 70)
            {
                ColSpan = 2,
                Lookup = EmployeeLookups.Employee,
                ListOrder = 60,
                Filter = true,
            },
            new("moneyReceiptNo", FieldTypes.Text, new("Money receipt no.", "মানি রিসিট নম্বর"), "basic", 80) { MaxLength = 100, ListOrder = 40 },
            new("referenceNo", FieldTypes.Text, new("Reference no.", "রেফারেন্স নম্বর"), "basic", 90) { MaxLength = 200, ColSpan = 2 },

            new("paymentMethodId", FieldTypes.Lookup, new("Received by", "যেভাবে পাওয়া"), "payment", 10)
            {
                Lookup = Procurement.ProcurementLookups.PaymentMethod,
                ListOrder = 50,
                Filter = true,
            },
            new("bankAccountId", FieldTypes.Lookup, new("Received in", "যে হিসাবে জমা"), "payment", 20) { Lookup = SalesLookups.BankAccount },
            new("instrumentTypeId", FieldTypes.Lookup, new("Instrument type", "ইনস্ট্রুমেন্টের ধরন"), "payment", 30)
            {
                Lookup = SalesLookups.InstrumentType,
            },
            new("instrumentNo", FieldTypes.Text, new("Cheque / instrument no.", "চেক / ইনস্ট্রুমেন্ট নম্বর"), "payment", 40) { MaxLength = 100 },
            new("instrumentDate", FieldTypes.Date, new("Cheque date", "চেকের তারিখ"), "payment", 50),
            new("depositDate", FieldTypes.Date, new("Deposit date", "জমার তারিখ"), "payment", 60),
            new("customerBankName", FieldTypes.Text, new("Customer's bank", "গ্রাহকের ব্যাংক"), "payment", 70) { MaxLength = 200 },
            new("customerBankBranch", FieldTypes.Text, new("Bank branch", "ব্যাংক শাখা"), "payment", 80) { MaxLength = 200 },
            new("transactionNo", FieldTypes.Text, new("Transaction no.", "লেনদেন নম্বর"), "payment", 90) { MaxLength = 100, ColSpan = 2 },
            .. SalesFields.Currency("payment", 100),

            new("vdsCertificateNo", FieldTypes.Text, new("VDS certificate no.", "ভিডিএস সনদ নম্বর"), "certificate", 10) { MaxLength = 100, ColSpan = 2 },
            new("vdsCertificateDate", FieldTypes.Date, new("VDS date", "ভিডিএস তারিখ"), "certificate", 20),
            new("aitCertificateNo", FieldTypes.Text, new("AIT certificate no.", "এআইটি সনদ নম্বর"), "certificate", 30) { MaxLength = 100, ColSpan = 2 },
            new("aitCertificateDate", FieldTypes.Date, new("AIT date", "এআইটি তারিখ"), "certificate", 40),

            SalesFields.Remarks(10),
            new("narration", FieldTypes.TextArea, new("Narration", "বিবরণ"), "notes", 20) { MaxLength = 4000, ColSpan = 2 },
        ]),
    ];
}
