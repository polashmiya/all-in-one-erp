using System.Net.Http.Json;
using Erp.Api.Accounting.ChartOfAccounts;
using Erp.Api.Accounting.Journals;
using Erp.Api.Approvals;
using Erp.Api.Sales.SalesDeliveries;
using Erp.Api.Sales.SalesOrders;
using Erp.Api.Sales.SalesQuotations;
using Erp.Core.Domain.Partners;
using Erp.Infrastructure.Persistence;
using Erp.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using static Erp.Tests.Api.ProcurementTestData;

namespace Erp.Tests.Api;

internal static class SalesTestData
{
    public const string QuotationsUrl = "/api/erp/sales/sales-quotations";
    public const string OrdersUrl = "/api/erp/sales/sales-orders";
    public const string DeliveriesUrl = "/api/erp/sales/sales-deliveries";

    public static async Task<HttpClient> SalesAdminAsync(ApiFactory factory, Guid unitId)
    {
        var login = await factory.ErpAdminLoginAsync();
        return factory.CreateClient(login.AccessToken, unitId);
    }

    public static async Task<int> CustomerAsync(Guid unitId)
    {
        await using var db = ApiFactory.CreateDbContext();
        var accountId = await db.BusinessUnits.AcrossTenants().Where(u => u.Id == unitId).Select(u => u.AccountId).FirstAsync();
        var code = Unique();
        var customer = new Customer { AccountId = accountId, BusinessUnitId = unitId, Code = code, Name = $"Customer {code}" };
        db.Customers.Add(customer);
        await db.SaveChangesAsync();
        return customer.Id;
    }

    public static async Task<int> StockedItemAsync(ApiFactory factory, HttpClient admin, decimal quantity, decimal cost)
    {
        var item = await ItemAsync(factory.DhakaId, cost);
        await ApprovedReceiveAsync(factory, admin, item, quantity);
        return item;
    }

    public static async Task<T> ApproveAsync<T>(HttpClient admin, string url, int id) =>
        await ReadAsync<T>(await admin.PostAsJsonAsync($"{url}/{id}/approve", new ReviewRequest(null)));

    public static async Task<Dictionary<string, decimal>> EntriesAsync(HttpClient admin, string documentCode)
    {
        var accounts = (await admin.GetFromJsonAsync<List<ChartOfAccountOption>>("/api/erp/accounting/chart-of-accounts/options"))!;
        var rows = (await admin.GetFromJsonAsync<JournalList>($"/api/erp/accounting/journals?search={documentCode}"))!.Items;
        var entries = new Dictionary<string, decimal>();
        foreach (var row in rows)
        {
            var journal = (await admin.GetFromJsonAsync<JournalDetail>($"/api/erp/accounting/journals/{row.Id}"))!;
            foreach (var line in journal.Lines)
            {
                var key = accounts.Single(a => a.Id == line.ChartOfAccountId).PostingKeys.FirstOrDefault() ?? line.AccountName;
                entries[key] = entries.GetValueOrDefault(key) + line.Debit - line.Credit;
            }
        }

        return entries;
    }

    public static async Task<SaveSalesQuotationRequest> QuotationAsync(Guid unitId) => new()
    {
        Code = null,
        QuotationTypeId = null,
        Version = null,
        VersionDate = null,
        QuotationDate = Today,
        ValidityDate = Today.AddDays(15),
        CustomerId = await CustomerAsync(unitId),
        OfficeId = null,
        SalesForceId = null,
        Reference = null,
        BillingContactPerson = null,
        BillingPhoneNo = null,
        BillingAddress = null,
        ShippingContactPerson = null,
        ShippingAddressId = null,
        ShippingAddressName = null,
        IsVatInclusive = false,
        IsGrossDiscount = false,
        DiscountPercentage = null,
        DiscountAmount = null,
        DeliveryCharge = null,
        ExtraCharge = null,
        Subjects = null,
        BodyNarration = null,
        FooterNarration = null,
        Narration = null,
        Lines = [],
    };

    public static SaveSalesQuotationLine Line(int itemId, decimal quantity, decimal price) => new()
    {
        ItemId = itemId,
        ItemDescription = null,
        Quantity = quantity,
        Price = price,
        DiscountPercentage = null,
        DiscountAmount = null,
        IsAmountWiseDiscount = false,
        IsDiscountPerQuantity = false,
        SdPercentage = null,
        VatPercentage = null,
        AitPercentage = null,
        WarrantyInMonth = null,
        DeliveryDate = null,
    };

    public static async Task<SaveSalesOrderRequest> OrderAsync(Guid unitId, params SaveSalesOrderLine[] lines) => new()
    {
        Code = null,
        OrderDate = Today,
        DeliveryDate = Today,
        CustomerId = await CustomerAsync(unitId),
        OfficeId = null,
        WarehouseId = await WarehouseAsync(unitId),
        SalesForceId = null,
        PriceTypeId = null,
        ProjectName = null,
        ReferredByPartnerId = null,
        CustomerPo = null,
        CustomerPoDate = null,
        PaymentTypeId = null,
        PaymentTermDays = null,
        PaymentMethodId = null,
        BankAccountId = null,
        InstrumentNo = null,
        InstrumentDate = null,
        ShippingContactPerson = null,
        ShippingAddressId = null,
        ShippingAddressName = null,
        IsVatInclusive = false,
        IsGrossDiscount = false,
        HeaderDiscountPercentage = null,
        HeaderDiscount = null,
        SpecialDiscountAmount = null,
        OthersCost = null,
        OtherChargeAccountId = null,
        ExtraCharge = null,
        ExtraChargeAccountId = null,
        DeductionLabel = null,
        DeductionValue = null,
        FreeTypeId = null,
        CurrencyId = null,
        ExchangeRate = null,
        IsAdvanceAdjust = true,
        Remarks = null,
        SalesQuotationId = null,
        AdvanceAmount = null,
        DueLimit = null,
        CustomerPoAmount = null,
        CustomerPoRefAmount = null,
        SalesTermId = null,
        DmsInvoice = null,
        IsForeign = false,
        LcTypeId = null,
        LcNo = null,
        PaymentTermsId = null,
        IsAllowPartialShipment = false,
        IsAllowTransitShipment = false,
        Review = null,
        Lines = lines,
    };

    public static SaveSalesOrderLine OrderLine(int itemId, decimal quantity, decimal price) => new()
    {
        ItemId = itemId,
        ItemDescription = null,
        Quantity = quantity,
        Price = price,
        DiscountPercentage = null,
        DiscountAmount = null,
        IsAmountWiseDiscount = false,
        IsDiscountPerQuantity = false,
        SdPercentage = null,
        VatPercentage = null,
        AitPercentage = null,
        WarrantyInMonth = null,
        IsOfferItem = false,
        ShelfLife = null,
        DemandQuantity = null,
        FobRate = null,
        FreightRate = null,
    };

    public static async Task<SaveSalesDeliveryRequest> DeliveryAsync(Guid unitId, params SaveSalesDeliveryLine[] lines) => new()
    {
        Code = null,
        Type = "direct",
        OrderId = null,
        OrderDate = Today,
        DeliveryDate = Today,
        CustomerId = await CustomerAsync(unitId),
        OfficeId = null,
        WarehouseId = await WarehouseAsync(unitId),
        SalesForceId = null,
        PriceTypeId = null,
        ProjectName = null,
        ReferredByPartnerId = null,
        CustomerPo = null,
        CustomerPoDate = null,
        PaymentTypeId = null,
        PaymentTermDays = null,
        PaymentMethodId = null,
        BankAccountId = null,
        InstrumentNo = null,
        InstrumentDate = null,
        ShippingContactPerson = null,
        ShippingAddressId = null,
        ShippingAddressName = null,
        IsVatInclusive = false,
        IsGrossDiscount = false,
        HeaderDiscountPercentage = null,
        HeaderDiscount = null,
        SpecialDiscountAmount = null,
        OthersCost = null,
        OtherChargeAccountId = null,
        ExtraCharge = null,
        ExtraChargeAccountId = null,
        DeductionLabel = null,
        DeductionValue = null,
        FreeTypeId = null,
        CurrencyId = null,
        ExchangeRate = null,
        IsAdvanceAdjust = false,
        Remarks = null,
        Phone = null,
        ChallanNo = null,
        VatChallanNo = null,
        BillNo = null,
        RefInvoice = null,
        AnonymousAddress = null,
        Narration = null,
        DeliveryMethodId = null,
        CourierOrEmployeeId = null,
        SubmissionDate = null,
        AlternativeDeliveryDate = null,
        AdvanceBalanceAdjust = null,
        ReceiveAmount = null,
        InterestRate = null,
        TotalNoOfInstallment = null,
        InstallmentStartDate = null,
        InstallmentTypeId = null,
        CommissionPercentage = null,
        IsExportSales = false,
        Lines = lines,
    };

    public static async Task<SaveSalesDeliveryRequest> DeliveryOfAsync(Guid unitId, SalesOrderDetail order, decimal quantity) =>
        await DeliveryAsync(unitId, [.. order.Lines.Select(l => DeliveryLine(l.ItemId, quantity, 0) with { OrderLineId = l.Id })]) with
        {
            Type = "order",
            OrderId = order.Id,
        };

    public static SaveSalesDeliveryLine DeliveryLine(int itemId, decimal quantity, decimal price) => new()
    {
        ItemId = itemId,
        ItemDescription = null,
        Quantity = quantity,
        Price = price,
        DiscountPercentage = null,
        DiscountAmount = null,
        IsAmountWiseDiscount = false,
        IsDiscountPerQuantity = false,
        SdPercentage = null,
        VatPercentage = null,
        AitPercentage = null,
        WarrantyInMonth = null,
        IsOfferItem = false,
        ShelfLife = null,
        OrderLineId = null,
        Batch = null,
        ItemSerial = null,
        PackQuantity = null,
        WarrantyDescription = null,
        WarrantyExpiredDate = null,
    };
}
