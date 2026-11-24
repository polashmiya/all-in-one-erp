using Erp.Api.Procurement;
using Erp.Api.Procurement.PurchasePayments;
using Erp.Metadata;

namespace Erp.Api.Accounting.TaxDeposits;

public sealed class TaxDepositManifest : IFeatureManifestProvider
{
    public const string Type = "tax-type";

    public IEnumerable<FeatureManifest> GetManifests() =>
    [
        new(AccountingMenu.TaxDeposit,
        [
            new("general", new("General", "সাধারণ"), 10),
            new("basic", new("Deposit", "জমা"), 10, "general"),
            new("notes", new("Notes", "মন্তব্য"), 20, "general"),
        ],
        [
            new("code", FieldTypes.Text, new("Deposit no.", "জমা নম্বর"), "basic", 10)
            {
                MaxLength = 100,
                Help = new("Leave empty to generate one.", "ফাঁকা রাখলে একটি নম্বর তৈরি হবে।"),
                Placeholder = new("Generated on save", "সংরক্ষণের সময় তৈরি হবে"),
                ListOrder = 10,
                Sort = true,
            },
            new("depositDate", FieldTypes.Date, new("Deposit date", "জমার তারিখ"), "basic", 20)
            {
                Locked = true,
                ListOrder = 20,
                Sort = true,
            },
            new("type", FieldTypes.Lookup, new("Type", "ধরন"), "basic", 30)
            {
                Locked = true,
                Lookup = Type,
                Help = new("Pick VDS or TDS to load what is not deposited yet.", "ভিডিএস বা টিডিএস বেছে নিলে যা জমা হয়নি তা চলে আসবে।"),
                ListOrder = 30,
                Filter = true,
            },
            new("paymentMethodId", FieldTypes.Lookup, new("Payment method", "পরিশোধ পদ্ধতি"), "basic", 40)
            {
                Lookup = ProcurementLookups.PaymentMethod,
                ListOrder = 40,
            },
            new("bankAccountId", FieldTypes.Lookup, new("Pay from", "যে হিসাব থেকে"), "basic", 50)
            {
                Lookup = PurchasePaymentManifest.BankAccount,
            },
            new("challanNo", FieldTypes.Text, new("Treasury challan no.", "ট্রেজারি চালান নম্বর"), "basic", 60) { MaxLength = 100, ListOrder = 50 },
            new("challanDate", FieldTypes.Date, new("Challan date", "চালানের তারিখ"), "basic", 70),

            new("remarks", FieldTypes.TextArea, new("Remarks", "মন্তব্য"), "notes", 10) { MaxLength = 1000 },
        ]),
    ];
}
