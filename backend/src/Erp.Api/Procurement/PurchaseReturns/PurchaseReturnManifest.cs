using Erp.Api.UserManagement.Employees;
using Erp.Metadata;
using Erp.Modules.Inventory.ItemManagement.Items;

namespace Erp.Api.Procurement.PurchaseReturns;

public sealed class PurchaseReturnManifest : IFeatureManifestProvider
{
    public const string Receive = "purchase-receive";

    public IEnumerable<FeatureManifest> GetManifests() =>
    [
        new(ProcurementMenu.PurchaseReturn,
        [
            new("general", new("General", "সাধারণ"), 10),
            new("basic", new("Return", "ফেরত"), 10, "general"),
            new("notes", new("Notes", "মন্তব্য"), 20, "general"),
        ],
        [
            new("code", FieldTypes.Text, new("Return no.", "ফেরত নম্বর"), "basic", 10)
            {
                MaxLength = 100,
                Help = new("Leave empty to generate one.", "ফাঁকা রাখলে একটি নম্বর তৈরি হবে।"),
                Placeholder = new("Generated on save", "সংরক্ষণের সময় তৈরি হবে"),
                ListOrder = 10,
                Sort = true,
            },
            new("returnDate", FieldTypes.Date, new("Return date", "ফেরতের তারিখ"), "basic", 20)
            {
                Locked = true,
                ListOrder = 20,
                Sort = true,
            },
            new("purchaseReceiveId", FieldTypes.Lookup, new("Goods receive (GRN)", "মাল গ্রহণ (জিআরএন)"), "basic", 30)
            {
                Locked = true,
                Lookup = Receive,
                Help = new("Pick an approved GRN to return its items.", "অনুমোদিত জিআরএন বেছে নিলে তার আইটেম চলে আসবে।"),
                ListOrder = 40,
            },
            new("supplierId", FieldTypes.Lookup, new("Supplier", "সরবরাহকারী"), "basic", 40)
            {
                Lookup = ItemLookups.Supplier,
                Help = new("Comes from the GRN.", "জিআরএন থেকে আসে।"),
                ListOrder = 30,
                Filter = true,
            },
            new("warehouseId", FieldTypes.Lookup, new("Warehouse", "গুদাম"), "basic", 50)
            {
                Locked = true,
                Lookup = EmployeeLookups.Warehouse,
                Help = new("Where the goods go out from.", "যেখান থেকে মাল ফেরত যাচ্ছে।"),
                ListOrder = 50,
                Filter = true,
            },
            new("reasonId", FieldTypes.Lookup, new("Reason", "কারণ"), "basic", 60)
            {
                Lookup = ProcurementLookups.ReturnReason,
                ListOrder = 60,
                Filter = true,
            },
            new("isVatInclusive", FieldTypes.Boolean, new("Prices include VAT", "দামে ভ্যাট ধরা আছে"), "basic", 70)
            {
                Help = new("Comes from the GRN.", "জিআরএন থেকে আসে।"),
            },

            new("remarks", FieldTypes.TextArea, new("Remarks", "মন্তব্য"), "notes", 10) { MaxLength = 1000 },
        ]),
    ];
}
