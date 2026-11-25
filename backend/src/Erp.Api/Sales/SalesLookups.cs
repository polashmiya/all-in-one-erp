using Erp.Metadata;

namespace Erp.Api.Sales;

public sealed class SalesLookups : ILookupProvider
{
    public const string QuotationType = "quotation-type";
    public const string ShippingAddress = "shipping-address";

    public const string SalesTerm = "sales-term";
    public const string FreeType = "free-type";
    public const string Currency = "currency";
    public const string LcType = "lc-type";
    public const string PaymentTerms = "payment-terms";
    public const string DeliveryMethod = "delivery-method";
    public const string InstallmentType = "installment-type";
    public const string ReturnType = "sales-return-type";
    public const string ReturnReason = "sales-return-reason";
    public const string InstrumentType = "instrument-type";

    public const string Customer = "customer";
    public const string Quotation = "sales-quotation";
    public const string Order = "sales-order";
    public const string Delivery = "sales-delivery";
    public const string DeliveryType = "sales-delivery-type";
    public const string BankAccount = "bank-account";
    public const string ChargeAccount = "charge-account";

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
        new(SalesTerm, new("Sales term", "বিক্রয়ের শর্ত"),
        [
            new("REGULAR", new("Regular", "নিয়মিত"), IsDefault: true),
            new("CONTRACT", new("Contract", "চুক্তিভিত্তিক")),
            new("FOB", new("FOB", "FOB")),
            new("CIF", new("CIF", "CIF")),
        ]),
        new(FreeType, new("Free type", "ফ্রির ধরন"),
        [
            new("BONUS", new("Bonus", "বোনাস")),
            new("SAMPLE", new("Sample", "নমুনা")),
            new("GIFT", new("Gift", "উপহার")),
        ]),
        new(Currency, new("Currency", "মুদ্রা"),
        [
            new("BDT", new("BDT", "BDT"), IsDefault: true),
            new("USD", new("USD", "USD")),
            new("EUR", new("EUR", "EUR")),
            new("INR", new("INR", "INR")),
        ]),
        new(LcType, new("LC type", "এলসির ধরন"),
        [
            new("SIGHT", new("At sight", "অ্যাট সাইট")),
            new("DEFERRED", new("Deferred", "ডেফার্ড")),
            new("UPAS", new("UPAS", "UPAS")),
        ]),
        new(PaymentTerms, new("Payment terms", "পরিশোধের শর্ত"),
        [
            new("LC", new("LC", "এলসি")),
            new("TT", new("TT", "TT")),
            new("ADVANCE", new("Advance", "অগ্রিম")),
            new("CREDIT", new("Credit", "বাকি")),
        ]),
        new(DeliveryMethod, new("Delivery method", "ডেলিভারি পদ্ধতি"),
        [
            new("OWN", new("Own transport", "নিজস্ব পরিবহন"), IsDefault: true),
            new("COURIER", new("Courier", "কুরিয়ার")),
            new("HIRED", new("Hired transport", "ভাড়া পরিবহন")),
            new("PICKUP", new("Customer pickup", "গ্রাহক নিজে নেবেন")),
        ]),
        new(InstallmentType, new("Installment type", "কিস্তির ধরন"),
        [
            new("MONTHLY", new("Monthly", "মাসিক")),
            new("QUARTERLY", new("Quarterly", "ত্রৈমাসিক")),
            new("YEARLY", new("Yearly", "বার্ষিক")),
        ]),
        new(ReturnType, new("Sales return type", "বিক্রয় ফেরতের ধরন"),
        [
            new("RETURN", new("Return", "ফেরত"), IsDefault: true),
            new("PARTIAL", new("Partial return", "আংশিক ফেরত")),
            new("DAMAGE", new("Damage return", "ক্ষতিগ্রস্ত ফেরত")),
            new("EXPIRED", new("Expired return", "মেয়াদোত্তীর্ণ ফেরত")),
            new("EXCHANGE", new("Exchange", "বদল")),
        ]),
        new(ReturnReason, new("Sales return reason", "বিক্রয় ফেরতের কারণ"),
        [
            new("DAMAGED", new("Damaged", "ক্ষতিগ্রস্ত")),
            new("WRONG", new("Wrong item", "ভুল আইটেম")),
            new("EXPIRED", new("Expired", "মেয়াদোত্তীর্ণ")),
            new("QUALITY", new("Poor quality", "নিম্নমানের")),
            new("EXCESS", new("Excess quantity", "অতিরিক্ত পরিমাণ")),
            new("NOTNEEDED", new("Not needed", "প্রয়োজন নেই")),
        ]),
        new(InstrumentType, new("Instrument type", "ইনস্ট্রুমেন্টের ধরন"),
        [
            new("CHEQUE", new("Cheque", "চেক")),
            new("PAYORDER", new("Pay order", "পে অর্ডার")),
            new("DD", new("Demand draft", "ডিমান্ড ড্রাফট")),
            new("EFT", new("EFT / RTGS", "EFT / RTGS")),
            new("CARD", new("Card", "কার্ড")),
        ]),
    ];
}
