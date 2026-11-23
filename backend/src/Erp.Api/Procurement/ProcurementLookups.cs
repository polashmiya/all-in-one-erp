using Erp.Metadata;

namespace Erp.Api.Procurement;

public sealed class ProcurementLookups : ILookupProvider
{
    public const string RequestType = "requisition-type";
    public const string PurchaseMethod = "purchase-method";
    public const string PaymentType = "payment-type";
    public const string ReturnReason = "return-reason";
    public const string PaymentMethod = "payment-method";

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
        new(PaymentType, new("Payment type", "পরিশোধের ধরন"),
        [
            new("CASH", new("Cash", "নগদ"), IsDefault: true),
            new("CREDIT", new("Credit", "বাকি")),
            new("CHEQUE", new("Cheque", "চেক")),
            new("BANK", new("Bank transfer", "ব্যাংক ট্রান্সফার")),
            new("MFS", new("Mobile banking", "মোবাইল ব্যাংকিং")),
        ]),
        new(PaymentMethod, new("Payment method", "পরিশোধ পদ্ধতি"),
        [
            new("CASH", new("Cash", "নগদ"), IsDefault: true),
            new("CHEQUE", new("Cheque", "চেক")),
            new("BANK", new("Bank transfer", "ব্যাংক ট্রান্সফার")),
            new("MFS", new("Mobile banking", "মোবাইল ব্যাংকিং")),
        ]),
        new(ReturnReason, new("Return reason", "ফেরতের কারণ"),
        [
            new("DAMAGED", new("Damaged", "ক্ষতিগ্রস্ত"), IsDefault: true),
            new("WRONG", new("Wrong item", "ভুল আইটেম")),
            new("EXPIRED", new("Expired", "মেয়াদোত্তীর্ণ")),
            new("QUALITY", new("Poor quality", "নিম্নমানের")),
            new("EXCESS", new("Excess quantity", "অতিরিক্ত পরিমাণ")),
        ]),
    ];
}
