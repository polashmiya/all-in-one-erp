using Erp.Metadata;

namespace Erp.Api.PartnerManagement;

public sealed class PartnerLookups : ILookupProvider
{
    public const string CustomerGroup = "customer-group";
    public const string SupplierGroup = "supplier-group";
    public const string PriceType = "price-type";
    public const string Territory = "territory";

    public IEnumerable<LookupDef> GetLookups() =>
    [
        new(CustomerGroup, new("Customer group", "গ্রাহক গ্রুপ"),
        [
            new("RETAIL", new("Retail", "খুচরা"), IsDefault: true),
            new("WHOLESALE", new("Wholesale", "পাইকারি")),
            new("CORPORATE", new("Corporate", "কর্পোরেট")),
            new("DEALER", new("Dealer", "ডিলার")),
        ]),
        new(SupplierGroup, new("Supplier group", "সরবরাহকারী গ্রুপ"),
        [
            new("LOCAL", new("Local supplier", "স্থানীয় সরবরাহকারী"), IsDefault: true),
            new("MANUFACTURER", new("Manufacturer", "উৎপাদনকারী")),
            new("DISTRIBUTOR", new("Distributor", "পরিবেশক")),
            new("IMPORTER", new("Importer", "আমদানিকারক")),
            new("SERVICE", new("Service provider", "সেবা প্রদানকারী")),
        ]),
        new(PriceType, new("Price type", "মূল্যের ধরন"),
        [
            new("RETAIL", new("Retail price", "খুচরা মূল্য"), IsDefault: true),
            new("WHOLESALE", new("Wholesale price", "পাইকারি মূল্য")),
            new("DEALER", new("Dealer price", "ডিলার মূল্য")),
        ]),
        new(Territory, new("Territory", "এলাকা"), []),
    ];
}
