using Erp.Metadata;

namespace Erp.Api.Sales;

public sealed class SalesLookups : ILookupProvider
{
    public const string QuotationType = "quotation-type";
    public const string ShippingAddress = "shipping-address";

    public const string Customer = "customer";

    public IEnumerable<LookupDef> GetLookups() =>
    [
        new(QuotationType, new("Quotation type", "কোটেশনের ধরন"),
        [
            new("REGULAR", new("Regular", "নিয়মিত"), IsDefault: true),
            new("PROJECT", new("Project", "প্রকল্প")),
            new("TENDER", new("Tender", "টেন্ডার")),
            new("SERVICE", new("Service", "সেবা")),
        ]),
        new(ShippingAddress, new("Ship to", "শিপিং গন্তব্য"),
        [
            new("CUSTOMER", new("Customer address", "গ্রাহকের ঠিকানা"), IsDefault: true),
            new("WAREHOUSE", new("Customer warehouse", "গ্রাহকের গুদাম")),
            new("SITE", new("Project site", "প্রকল্পের স্থান")),
            new("PICKUP", new("Customer pickup", "গ্রাহক নিজে নেবেন")),
        ]),
    ];
}
