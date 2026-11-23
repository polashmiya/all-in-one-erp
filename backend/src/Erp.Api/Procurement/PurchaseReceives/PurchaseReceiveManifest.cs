using Erp.Api.UserManagement.Employees;
using Erp.Metadata;
using Erp.Modules.Inventory.ItemManagement.Items;

namespace Erp.Api.Procurement.PurchaseReceives;

public sealed class PurchaseReceiveManifest : IFeatureManifestProvider
{
    public const string Order = "purchase-order";

    public IEnumerable<FeatureManifest> GetManifests() =>
    [
        new(ProcurementMenu.PurchaseReceive,
        [
            new("general", new("General", "সাধারণ"), 10),
            new("basic", new("Receive", "গ্রহণ"), 10, "general"),
            new("documents", new("Supplier documents", "সরবরাহকারীর কাগজপত্র"), 20, "general"),
            new("terms", new("Payment", "পরিশোধ"), 30, "general"),
            new("charges", new("Discount & charges", "ছাড় ও চার্জ"), 40, "general"),
            new("notes", new("Notes", "মন্তব্য"), 50, "general"),
        ],
        [
            new("code", FieldTypes.Text, new("GRN no.", "জিআরএন নম্বর"), "basic", 10)
            {
                MaxLength = 100,
                Help = new("Leave empty to generate one.", "ফাঁকা রাখলে একটি নম্বর তৈরি হবে।"),
                Placeholder = new("Generated on save", "সংরক্ষণের সময় তৈরি হবে"),
                ListOrder = 10,
                Sort = true,
            },
            new("receiveDate", FieldTypes.Date, new("Receive date", "গ্রহণের তারিখ"), "basic", 20)
            {
                Locked = true,
                ListOrder = 20,
                Sort = true,
            },
            new("purchaseOrderId", FieldTypes.Lookup, new("Purchase order", "ক্রয় আদেশ"), "basic", 30)
            {
                Locked = true,
                Lookup = Order,
                Help = new("Pick an approved order to receive its items.", "অনুমোদিত আদেশ বেছে নিলে তার আইটেম চলে আসবে।"),
                ListOrder = 40,
            },
            new("supplierId", FieldTypes.Lookup, new("Supplier", "সরবরাহকারী"), "basic", 40)
            {
                Lookup = ItemLookups.Supplier,
                Help = new("Comes from the purchase order.", "ক্রয় আদেশ থেকে আসে।"),
                ListOrder = 30,
                Filter = true,
            },
            new("warehouseId", FieldTypes.Lookup, new("Warehouse", "গুদাম"), "basic", 50)
            {
                Locked = true,
                Lookup = EmployeeLookups.Warehouse,
                Help = new("Where the goods are received.", "যেখানে মাল গ্রহণ করা হচ্ছে।"),
                ListOrder = 50,
                Filter = true,
            },

            new("challanNo", FieldTypes.Text, new("Challan no.", "চালান নম্বর"), "documents", 10)
            {
                MaxLength = 50,
                Help = new("The supplier's delivery challan.", "সরবরাহকারীর ডেলিভারি চালান।"),
                ListOrder = 60,
            },
            new("challanDate", FieldTypes.Date, new("Challan date", "চালানের তারিখ"), "documents", 20),
            new("vatChallanNo", FieldTypes.Text, new("VAT challan no.", "ভ্যাট চালান নম্বর"), "documents", 30) { MaxLength = 250 },

            new("paymentTypeId", FieldTypes.Lookup, new("Payment type", "পরিশোধের ধরন"), "terms", 10)
            {
                Lookup = ProcurementLookups.PaymentType,
                Filter = true,
            },
            new("paymentDays", FieldTypes.Number, new("Payment days after receive", "মাল পাওয়ার পর পরিশোধের দিন"), "terms", 20)
            {
                Min = 0,
                Max = 365,
            },
            new("isVatInclusive", FieldTypes.Boolean, new("Prices include VAT", "দামে ভ্যাট ধরা আছে"), "terms", 30),

            new("headerDiscount", FieldTypes.Money, new("Discount", "ছাড়"), "charges", 10) { Min = 0 },
            new("deliveryCharge", FieldTypes.Money, new("Delivery charge", "পরিবহন খরচ"), "charges", 20) { Min = 0 },
            new("laborCharge", FieldTypes.Money, new("Labor charge", "শ্রমিক খরচ"), "charges", 30) { Min = 0 },
            new("otherCharge", FieldTypes.Money, new("Other charge", "অন্যান্য খরচ"), "charges", 40) { Min = 0 },

            new("remarks", FieldTypes.TextArea, new("Remarks", "মন্তব্য"), "notes", 10) { MaxLength = 1000 },
        ]),
    ];
}
