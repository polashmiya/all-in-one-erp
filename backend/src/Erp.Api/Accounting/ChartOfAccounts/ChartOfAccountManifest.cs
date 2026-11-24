using Erp.Metadata;

namespace Erp.Api.Accounting.ChartOfAccounts;

public sealed class ChartOfAccountManifest : IFeatureManifestProvider
{
    public const string Type = "account-type";
    public const string Group = "account-group";

    public IEnumerable<FeatureManifest> GetManifests() =>
    [
        new(AccountingMenu.ChartOfAccounts,
        [
            new("general", new("General", "সাধারণ"), 10),
            new("basic", new("Account", "হিসাব"), 10, "general", 2),
        ],
        [
            new("code", FieldTypes.Text, new("Account code", "হিসাব কোড"), "basic", 10)
            {
                Locked = true,
                MaxLength = 50,
                Placeholder = new("e.g. 1110", "যেমন: 1110"),
                ListOrder = 20,
                Sort = true,
            },
            new("name", FieldTypes.Text, new("Account name", "হিসাবের নাম"), "basic", 20)
            {
                Locked = true,
                MaxLength = 300,
                ListOrder = 10,
                Sort = true,
            },
            new("type", FieldTypes.Lookup, new("Type", "ধরন"), "basic", 30)
            {
                Locked = true,
                Lookup = Type,
                ListOrder = 30,
                Filter = true,
            },
            new("parentId", FieldTypes.Lookup, new("Under", "যার অধীনে"), "basic", 40)
            {
                Lookup = Group,
                ListOrder = 40,
                Filter = true,
            },
            new("isGroup", FieldTypes.Boolean, new("Group account", "গ্রুপ হিসাব"), "basic", 50)
            {
                Help = new("Groups hold other accounts. Nothing is posted to a group.", "গ্রুপের অধীনে অন্য হিসাব থাকে। গ্রুপে কিছু পোস্ট হয় না।"),
                ListOrder = 50,
            },
            new("isActive", FieldTypes.Boolean, new("Active", "সক্রিয়"), "basic", 60) { Default = "true" },
            new("description", FieldTypes.TextArea, new("Description", "বিবরণ"), "basic", 70) { MaxLength = 500, ColSpan = 2 },
        ]),
    ];
}
