using Erp.Api.UserManagement.Employees;
using Erp.Metadata;
using Erp.Modules.Inventory.ItemManagement.Items;

namespace Erp.Api.Procurement.PurchaseOrders;

public sealed class PurchaseOrderManifest : IFeatureManifestProvider
{
    public const string Requisition = "purchase-requisition";

    public IEnumerable<FeatureManifest> GetManifests() =>
    [
        new(ProcurementMenu.PurchaseOrder,
        [
            new("general", new("General", "সাধারণ"), 10),
            new("basic", new("Order", "আদেশ"), 10, "general"),
            new("terms", new("Payment & delivery", "পরিশোধ ও সরবরাহ"), 20, "general"),
            new("charges", new("Discount & charges", "ছাড় ও চার্জ"), 30, "general"),
            new("notes", new("Notes", "মন্তব্য"), 40, "general"),
        ],
        [
            new("code", FieldTypes.Text, new("PO no.", "ক্রয় আদেশ নম্বর"), "basic", 10)
            {
                MaxLength = 50,
                Help = new("Leave empty to generate one.", "ফাঁকা রাখলে একটি নম্বর তৈরি হবে।"),
                Placeholder = new("Generated on save", "সংরক্ষণের সময় তৈরি হবে"),
                ListOrder = 10,
                Sort = true,
            },
            new("orderDate", FieldTypes.Date, new("Order date", "আদেশের তারিখ"), "basic", 20)
            {
                Locked = true,
                ListOrder = 20,
                Sort = true,
            },
            new("supplierId", FieldTypes.Lookup, new("Supplier", "সরবরাহকারী"), "basic", 30)
            {
                Locked = true,
                Lookup = ItemLookups.Supplier,
                ListOrder = 30,
                Filter = true,
            },
            new("warehouseId", FieldTypes.Lookup, new("Warehouse", "গুদাম"), "basic", 40)
            {
                Locked = true,
                Lookup = EmployeeLookups.Warehouse,
                Help = new("Where the goods will be received.", "যেখানে মাল গ্রহণ করা হবে।"),
                ListOrder = 40,
                Filter = true,
            },
            new("requisitionId", FieldTypes.Lookup, new("Requisition", "চাহিদাপত্র"), "basic", 50)
            {
                Lookup = Requisition,
                Help = new("Pick an approved requisition to copy its items.", "অনুমোদিত চাহিদাপত্র বেছে নিলে তার আইটেম চলে আসবে।"),
            },
            new("referenceNo", FieldTypes.Text, new("Reference no.", "রেফারেন্স নম্বর"), "basic", 60)
            {
                MaxLength = 100,
                Help = new("The supplier's quotation or offer number.", "সরবরাহকারীর কোটেশন বা অফার নম্বর।"),
            },
            new("expectedReceiveDate", FieldTypes.Date, new("Expected delivery", "সম্ভাব্য সরবরাহের তারিখ"), "basic", 70)
            {
                ListOrder = 50,
                Sort = true,
            },

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
            new("isVatInclusive", FieldTypes.Boolean, new("Prices include VAT", "দামে ভ্যাট ধরা আছে"), "terms", 30)
            {
                Help = new("Turn on when the item prices already include VAT.", "আইটেমের দামে ভ্যাট ধরা থাকলে চালু করুন।"),
            },
            new("itemTypeId", FieldTypes.Lookup, new("Item type", "আইটেমের ধরন"), "terms", 40)
            {
                Lookup = ItemLookups.ItemType,
                Hidden = true,
            },
            new("shippingAddress", FieldTypes.TextArea, new("Delivery address", "সরবরাহের ঠিকানা"), "terms", 50)
            {
                MaxLength = 300,
                Hidden = true,
            },

            new("headerDiscount", FieldTypes.Money, new("Discount", "ছাড়"), "charges", 10) { Min = 0 },
            new("deliveryCharge", FieldTypes.Money, new("Delivery charge", "পরিবহন খরচ"), "charges", 20) { Min = 0 },
            new("laborCharge", FieldTypes.Money, new("Labor charge", "শ্রমিক খরচ"), "charges", 30) { Min = 0 },
            new("otherCharge", FieldTypes.Money, new("Other charge", "অন্যান্য খরচ"), "charges", 40) { Min = 0 },

            new("remarks", FieldTypes.TextArea, new("Remarks", "মন্তব্য"), "notes", 10) { MaxLength = 1000 },
        ]),
    ];
}
