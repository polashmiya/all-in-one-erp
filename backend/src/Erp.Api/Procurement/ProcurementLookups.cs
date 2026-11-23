using Erp.Metadata;

namespace Erp.Api.Procurement;

public sealed class ProcurementLookups : ILookupProvider
{
    public const string RequestType = "requisition-type";
    public const string PurchaseMethod = "purchase-method";

    public IEnumerable<LookupDef> GetLookups() =>
    [
        new(RequestType, new("Requisition type", "চাহিদাপত্রের ধরন"),
        [
            new("REGULAR", new("Regular", "নিয়মিত"), IsDefault: true),
            new("URGENT", new("Urgent", "জরুরি")),
            new("PROJECT", new("Project", "প্রকল্প")),
        ]),
        new(PurchaseMethod, new("Purchase method", "ক্রয় পদ্ধতি"),
        [
            new("LOCAL", new("Local purchase", "স্থানীয় ক্রয়"), IsDefault: true),
            new("CASH", new("Cash purchase", "নগদ ক্রয়")),
            new("QUOTATION", new("Quotation", "কোটেশন")),
            new("IMPORT", new("Import", "আমদানি")),
        ]),
    ];
}
