using Erp.Api.Accounting;
using Erp.Api.Accounting.BankAccounts;
using Erp.Api.Documents;
using Erp.Api.PartnerManagement;
using Erp.Api.Procurement;
using Erp.Core.Domain.Sales;
using Erp.Infrastructure.Persistence;
using Erp.Metadata;
using Microsoft.EntityFrameworkCore;

namespace Erp.Api.Sales;

internal static class SalesData
{
    public static async Task CheckAsync(
        ErpDbContext db, SaveSalesDocumentRequest request, SaveSalesDocumentRequest? existing, Dictionary<string, string[]> errors,
        CancellationToken cancellationToken)
    {
        await PartnerData.CheckLookupsAsync(db,
        [
            ("priceTypeId", request.PriceTypeId, existing?.PriceTypeId, PartnerLookups.PriceType),
            ("paymentTypeId", request.PaymentTypeId, existing?.PaymentTypeId, ProcurementLookups.PaymentType),
            ("paymentMethodId", request.PaymentMethodId, existing?.PaymentMethodId, ProcurementLookups.PaymentMethod),
            ("shippingAddressId", request.ShippingAddressId, existing?.ShippingAddressId, SalesLookups.ShippingAddress),
            ("freeTypeId", request.FreeTypeId, existing?.FreeTypeId, SalesLookups.FreeType),
            ("currencyId", request.CurrencyId, existing?.CurrencyId, SalesLookups.Currency),
        ], errors, cancellationToken);
        await CheckPeopleAsync(
            db, request.CustomerId, existing?.CustomerId, request.OfficeId, existing?.OfficeId, request.SalesForceId,
            existing?.SalesForceId, errors, cancellationToken);
        await DocumentData.CheckWarehouseAsync(db, request.WarehouseId, existing?.WarehouseId, errors, cancellationToken);
        await PaymentSources.CheckAsync(
            db, request.PaymentMethodId, request.BankAccountId, existing?.BankAccountId, errors, cancellationToken);

        if (request.ReferredByPartnerId is { } partner && partner != existing?.ReferredByPartnerId &&
            !await db.Customers.AnyAsync(c => c.Id == partner && c.IsActive, cancellationToken))
        {
            errors["referredByPartnerId"] = [DocumentData.ChooseFromList];
        }

        foreach (var (field, id, current) in new[]
                 {
                     ("otherChargeAccountId", request.OtherChargeAccountId, existing?.OtherChargeAccountId),
                     ("extraChargeAccountId", request.ExtraChargeAccountId, existing?.ExtraChargeAccountId),
                 })
        {
            if (id is not null && id != current && !await ChargeAccounts.Choices(db).AnyAsync(a => a.Id == id, cancellationToken))
            {
                errors[field] = [DocumentData.ChooseFromList];
            }
        }
    }

    public static async Task CheckPeopleAsync(
        ErpDbContext db, int customerId, int? currentCustomer, Guid? officeId, Guid? currentOffice, int? salesForceId, int? currentSalesForce,
        Dictionary<string, string[]> errors, CancellationToken cancellationToken)
    {
        if (customerId != currentCustomer && !await db.Customers.AnyAsync(c => c.Id == customerId && c.IsActive, cancellationToken))
        {
            errors["customerId"] = ["Choose an active customer of this business unit."];
        }

        if (officeId is not null && officeId != currentOffice && !await db.Offices.AnyAsync(o => o.Id == officeId && o.IsActive, cancellationToken))
        {
            errors["officeId"] = ["Choose an active office of this business unit."];
        }

        await CheckEmployeeAsync(db, "salesForceId", salesForceId, currentSalesForce, errors, cancellationToken);
    }

    public static async Task CheckEmployeeAsync(
        ErpDbContext db, string field, int? id, int? current, Dictionary<string, string[]> errors, CancellationToken cancellationToken)
    {
        if (id is not null && id != current && !await db.Employees.AnyAsync(e => e.Id == id && e.IsActive, cancellationToken))
        {
            errors[field] = [DocumentData.ChooseFromList];
        }
    }

    public static async Task<Dictionary<int, DocumentItemOption>> CheckItemsAsync(
        ErpDbContext db, IReadOnlyCollection<SaveSalesLine> lines, IEnumerable<int> saved, Dictionary<string, string[]> errors,
        CancellationToken cancellationToken)
    {
        var items = await DocumentData.CheckItemsAsync(db, [.. lines.Select(l => l.ItemId).Distinct()], saved, errors, cancellationToken);
        if (lines.DistinctBy(l => (l.ItemId, l.IsOfferItem)).Count() != lines.Count)
        {
            errors["lines"] = ["Each item can be added once, and once more as a free item."];
        }

        return items;
    }

    public static T Line<T>(T line, int index, SaveSalesLine request, DocumentItemOption item) where T : SalesDocumentLine
    {
        line.Sequence = index + 1;
        line.ItemId = item.Id;
        line.ItemName = item.Name;
        line.ItemDescription = DocumentData.Trimmed(request.ItemDescription);
        line.UomId = item.UomId;
        line.UomName = item.UomName;
        line.Quantity = request.Quantity;
        line.SellingPrice = item.SalesPrice;
        line.IsOfferItem = request.IsOfferItem;
        line.Price = request.IsOfferItem ? 0 : request.Price ?? item.SalesPrice;
        line.DiscountPercentage = request.IsOfferItem ? 0 : request.DiscountPercentage ?? 0;
        line.DiscountAmount = request.IsOfferItem ? 0 : request.DiscountAmount ?? 0;
        line.IsAmountWiseDiscount = request.IsAmountWiseDiscount || request.IsDiscountPerQuantity;
        line.IsDiscountPerQuantity = request.IsDiscountPerQuantity;
        line.SdPercentage = request.SdPercentage ?? item.SdPercentage;
        line.VatPercentage = request.VatPercentage ?? item.SalesVatPercentage;
        line.AitPercentage = request.AitPercentage ?? 0;
        line.WarrantyInMonth = request.WarrantyInMonth ?? 0;
        line.ShelfLife = DocumentData.Trimmed(request.ShelfLife);
        return line;
    }

    public static async Task ApplyAsync(
        ErpDbContext db, SalesDocument d, SaveSalesDocumentRequest r, CancellationToken cancellationToken)
    {
        var names = await PartnerData.LookupNamesAsync(
            db, [r.PriceTypeId, r.PaymentTypeId, r.FreeTypeId, r.CurrencyId], cancellationToken);
        string? Name(int? id) => id is { } key ? names.GetValueOrDefault(key) : null;

        if (d.CustomerId != r.CustomerId)
        {
            d.CustomerName = await db.Customers.Where(c => c.Id == r.CustomerId).Select(c => c.Name).SingleAsync(cancellationToken);
        }

        if (d.SalesForceId != r.SalesForceId) d.SalesForceName = await EmployeeNameAsync(db, r.SalesForceId, cancellationToken);
        d.OrderDate = r.OrderDate;
        d.DeliveryDate = r.DeliveryDate;
        d.CustomerId = r.CustomerId;
        d.OfficeId = r.OfficeId;
        d.WarehouseId = r.WarehouseId;
        d.SalesForceId = r.SalesForceId;
        d.PriceTypeId = r.PriceTypeId;
        d.PriceTypeName = Name(r.PriceTypeId);
        d.ProjectName = DocumentData.Trimmed(r.ProjectName);
        d.ReferredByPartnerId = r.ReferredByPartnerId;
        d.ReferredByPartnerName = await db.Customers.Where(c => c.Id == r.ReferredByPartnerId).Select(c => c.Name)
            .FirstOrDefaultAsync(cancellationToken);
        d.CustomerPo = DocumentData.Trimmed(r.CustomerPo);
        d.CustomerPoDate = r.CustomerPoDate;
        d.PaymentTypeId = r.PaymentTypeId;
        d.PaymentTypeName = Name(r.PaymentTypeId);
        d.PaymentTermDays = r.PaymentTermDays ?? 0;
        d.PaymentMethodId = r.PaymentMethodId;
        d.BankAccountId = r.BankAccountId;
        d.BankAccountName = await db.BankAccounts.Where(b => b.Id == r.BankAccountId).Select(b => b.Name).FirstOrDefaultAsync(cancellationToken);
        d.InstrumentNo = DocumentData.Trimmed(r.InstrumentNo);
        d.InstrumentDate = r.InstrumentDate;
        d.ShippingContactPerson = DocumentData.Trimmed(r.ShippingContactPerson);
        d.ShippingAddressId = r.ShippingAddressId;
        d.ShippingAddressName = DocumentData.Trimmed(r.ShippingAddressName);
        d.OtherChargeAccountId = r.OtherChargeAccountId;
        d.ExtraChargeAccountId = r.ExtraChargeAccountId;
        d.DeductionLabel = DocumentData.Trimmed(r.DeductionLabel);
        d.FreeTypeId = r.FreeTypeId;
        d.FreeTypeName = Name(r.FreeTypeId);
        d.CurrencyId = r.CurrencyId;
        d.CurrencyName = Name(r.CurrencyId);
        d.ExchangeRate = r.ExchangeRate ?? 0;
        d.IsAdvanceAdjust = r.IsAdvanceAdjust;
        d.Remarks = DocumentData.Trimmed(r.Remarks);
        d.CustomData = CustomFieldData.ToJson(r.CustomData);
    }

    public static void Total(
        SalesDocument d, IReadOnlyList<SalesDocumentLine> lines, SaveSalesDocumentRequest r, Dictionary<string, string[]> errors)
    {
        d.IsVatInclusive = r.IsVatInclusive;
        d.IsGrossDiscount = r.IsGrossDiscount;
        d.HeaderDiscountPercentage = r.IsGrossDiscount ? r.HeaderDiscountPercentage ?? 0 : 0;
        d.OthersCost = r.OthersCost ?? 0;
        d.ExtraCharge = r.ExtraCharge ?? 0;
        d.DeductionValue = r.DeductionValue ?? 0;
        d.SpecialDiscountAmount = r.SpecialDiscountAmount ?? 0;

        var discount = lines.Sum(l => l.DiscountAmount);
        var itemValue = lines.Sum(l => l.TotalValue);
        d.HeaderDiscount = r.IsGrossDiscount ? discount : 0;
        d.TotalLineDiscount = r.IsGrossDiscount ? 0 : discount;
        d.NetDiscount = discount + d.SpecialDiscountAmount;
        d.TotalQuantity = lines.Sum(l => l.Quantity);
        d.ItemTotalAmount = lines.Sum(l => l.TotalAmount);
        d.DiscountItemTotal = d.ItemTotalAmount - discount;
        d.OfferItemTotal = lines.Where(l => l.IsOfferItem).Sum(l => SalesPricing.Round(l.SellingPrice * l.Quantity));
        d.TotalSd = lines.Sum(l => l.SdAmount);
        d.TotalVat = lines.Sum(l => l.VatAmount);
        d.TotalAit = lines.Sum(l => l.AitAmount);
        d.NetAmount = itemValue + d.OthersCost + d.ExtraCharge - d.DeductionValue - d.SpecialDiscountAmount;
        if (d.NetAmount < 0) errors["specialDiscountAmount"] = ["Discount and deduction cannot be more than the total."];

        var costs = SalesPricing.Shares(d.OthersCost + d.ExtraCharge, [.. lines.Select(l => l.TotalValue)]);
        for (var i = 0; i < lines.Count; i++)
        {
            lines[i].HeaderDiscountProportion = r.IsGrossDiscount ? lines[i].DiscountAmount : 0;
            lines[i].HeaderCostProportion = costs[i];
            lines[i].ItemBasePriceInclusive = SalesPricing.Round(lines[i].TotalValue / lines[i].Quantity);
        }
    }

    public static Task<string?> EmployeeNameAsync(ErpDbContext db, int? id, CancellationToken cancellationToken) =>
        db.Employees.Where(e => e.Id == id).Select(e => e.Name).FirstOrDefaultAsync(cancellationToken);

    public static async Task<SalesNames> NamesAsync(ErpDbContext db, SalesDocument d, CancellationToken cancellationToken)
    {
        var lookups = await PartnerData.LookupNamesAsync(db, [d.PaymentMethodId, d.ShippingAddressId], cancellationToken);
        var accounts = await db.ChartOfAccounts.AsNoTracking()
            .Where(a => a.Id == d.OtherChargeAccountId || a.Id == d.ExtraChargeAccountId)
            .ToDictionaryAsync(a => a.Id, a => a.Name, cancellationToken);
        static string? Find(Dictionary<int, string> names, int? id) => id is { } key ? names.GetValueOrDefault(key) : null;

        return new SalesNames(
            await db.Offices.Where(o => o.Id == d.OfficeId).Select(o => o.Name).FirstOrDefaultAsync(cancellationToken),
            await db.Warehouses.Where(w => w.Id == d.WarehouseId).Select(w => w.Name).FirstOrDefaultAsync(cancellationToken),
            Find(lookups, d.PaymentMethodId),
            Find(lookups, d.ShippingAddressId),
            Find(accounts, d.OtherChargeAccountId),
            Find(accounts, d.ExtraChargeAccountId),
            await DocumentData.ReviewerNameAsync(db, d.ReviewedById, cancellationToken));
    }
}
