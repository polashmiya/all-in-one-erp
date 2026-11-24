using Erp.Metadata;

namespace Erp.Api.Accounting.BankAccounts;

public sealed class BankAccountManifest : IFeatureManifestProvider
{
    public const string Type = "bank-account-type";
    public const string ChartOfAccount = "chart-of-account";

    public IEnumerable<FeatureManifest> GetManifests() =>
    [
        new(AccountingMenu.BankAccounts,
        [
            new("general", new("General", "সাধারণ"), 10),
            new("basic", new("Bank account", "ব্যাংক হিসাব"), 10, "general", 2),
        ],
        [
            new("name", FieldTypes.Text, new("Title", "নাম"), "basic", 10)
            {
                Locked = true,
                MaxLength = 100,
                Placeholder = new("e.g. City Bank Gulshan", "যেমন: সিটি ব্যাংক গুলশান"),
                ListOrder = 10,
                Sort = true,
            },
            new("type", FieldTypes.Lookup, new("Type", "ধরন"), "basic", 20)
            {
                Locked = true,
                Lookup = Type,
                ListOrder = 20,
                Filter = true,
            },
            new("accountNo", FieldTypes.Text, new("Account no.", "হিসাব নম্বর"), "basic", 30)
            {
                Locked = true,
                MaxLength = 100,
                ListOrder = 30,
            },
            new("bankName", FieldTypes.Text, new("Bank", "ব্যাংক"), "basic", 40) { MaxLength = 100, ListOrder = 40 },
            new("branchName", FieldTypes.Text, new("Bank branch", "ব্যাংকের শাখা"), "basic", 50) { MaxLength = 100, ListOrder = 50 },
            new("routingNo", FieldTypes.Text, new("Routing no.", "রাউটিং নম্বর"), "basic", 60) { MaxLength = 50 },
            new("chartOfAccountId", FieldTypes.Lookup, new("Account in the books", "খাতার হিসাব"), "basic", 70)
            {
                Lookup = ChartOfAccount,
                Help = new("Leave empty to create one under Cash and bank.", "ফাঁকা রাখলে নগদ ও ব্যাংকের অধীনে একটি হিসাব তৈরি হবে।"),
                ListOrder = 60,
            },
            new("isActive", FieldTypes.Boolean, new("Active", "সক্রিয়"), "basic", 80) { Default = "true" },
        ]),
    ];
}
