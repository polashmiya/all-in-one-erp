using Erp.Metadata;
using Erp.Modules.Inventory.ItemManagement.Items;

namespace Erp.Api.Procurement.PurchasePayments;

public sealed class PurchasePaymentManifest : IFeatureManifestProvider
{
    public IEnumerable<FeatureManifest> GetManifests() =>
    [
        new(ProcurementMenu.PurchasePayment,
        [
            new("general", new("General", "সাধারণ"), 10),
            new("basic", new("Payment", "পরিশোধ"), 10, "general"),
            new("notes", new("Notes", "মন্তব্য"), 20, "general"),
        ],
        [
            new("code", FieldTypes.Text, new("Payment no.", "পরিশোধ নম্বর"), "basic", 10)
            {
                MaxLength = 100,
                Help = new("Leave empty to generate one.", "ফাঁকা রাখলে একটি নম্বর তৈরি হবে।"),
                Placeholder = new("Generated on save", "সংরক্ষণের সময় তৈরি হবে"),
                ListOrder = 10,
                Sort = true,
            },
            new("paymentDate", FieldTypes.Date, new("Payment date", "পরিশোধের তারিখ"), "basic", 20)
            {
                Locked = true,
                ListOrder = 20,
                Sort = true,
            },
            new("supplierId", FieldTypes.Lookup, new("Supplier", "সরবরাহকারী"), "basic", 30)
            {
                Locked = true,
                Lookup = ItemLookups.Supplier,
                Help = new("Pick a supplier to load the bills still due.", "সরবরাহকারী বেছে নিলে বাকি থাকা বিল চলে আসবে।"),
                ListOrder = 30,
                Filter = true,
            },
            new("paymentMethodId", FieldTypes.Lookup, new("Payment method", "পরিশোধ পদ্ধতি"), "basic", 40)
            {
                Lookup = ProcurementLookups.PaymentMethod,
                ListOrder = 40,
                Filter = true,
            },
            new("payeeName", FieldTypes.Text, new("Pay to", "প্রাপক"), "basic", 50)
            {
                MaxLength = 100,
                Help = new("Name on the cheque or receipt, if not the supplier.", "চেক বা রসিদে নাম, সরবরাহকারী না হলে।"),
            },

            new("bankAccountNo", FieldTypes.Text, new("Bank account no.", "ব্যাংক হিসাব নম্বর"), "basic", 60) { MaxLength = 100 },
            new("instrumentNo", FieldTypes.Text, new("Cheque or reference no.", "চেক বা রেফারেন্স নম্বর"), "basic", 70) { MaxLength = 200 },
            new("instrumentDate", FieldTypes.Date, new("Cheque date", "চেকের তারিখ"), "basic", 80),

            new("remarks", FieldTypes.TextArea, new("Remarks", "মন্তব্য"), "notes", 10) { MaxLength = 1500 },
        ]),
    ];
}
