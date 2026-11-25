using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Erp.Api.Approvals;
using Erp.Api.Documents;
using Erp.Core.Domain.Sales;
using Erp.Metadata;
using FluentValidation;

namespace Erp.Api.Sales;

public abstract record SaveSalesDocumentRequest
{
    protected SaveSalesDocumentRequest()
    {
    }

    [SetsRequiredMembers]
    protected SaveSalesDocumentRequest(SalesDocument d)
    {
        Code = d.Code;
        OrderDate = d.OrderDate;
        DeliveryDate = d.DeliveryDate;
        CustomerId = d.CustomerId;
        OfficeId = d.OfficeId;
        WarehouseId = d.WarehouseId;
        SalesForceId = d.SalesForceId;
        PriceTypeId = d.PriceTypeId;
        ProjectName = d.ProjectName;
        ReferredByPartnerId = d.ReferredByPartnerId;
        CustomerPo = d.CustomerPo;
        CustomerPoDate = d.CustomerPoDate;
        PaymentTypeId = d.PaymentTypeId;
        PaymentTermDays = d.PaymentTermDays;
        PaymentMethodId = d.PaymentMethodId;
        BankAccountId = d.BankAccountId;
        InstrumentNo = d.InstrumentNo;
        InstrumentDate = d.InstrumentDate;
        ShippingContactPerson = d.ShippingContactPerson;
        ShippingAddressId = d.ShippingAddressId;
        ShippingAddressName = d.ShippingAddressName;
        IsVatInclusive = d.IsVatInclusive;
        IsGrossDiscount = d.IsGrossDiscount;
        HeaderDiscountPercentage = d.HeaderDiscountPercentage;
        HeaderDiscount = d.HeaderDiscount;
        SpecialDiscountAmount = d.SpecialDiscountAmount;
        OthersCost = d.OthersCost;
        OtherChargeAccountId = d.OtherChargeAccountId;
        ExtraCharge = d.ExtraCharge;
        ExtraChargeAccountId = d.ExtraChargeAccountId;
        DeductionLabel = d.DeductionLabel;
        DeductionValue = d.DeductionValue;
        FreeTypeId = d.FreeTypeId;
        CurrencyId = d.CurrencyId;
        ExchangeRate = d.ExchangeRate;
        IsAdvanceAdjust = d.IsAdvanceAdjust;
        Remarks = d.Remarks;
        CustomData = CustomFieldData.Parse(d.CustomData);
    }

    public required string? Code { get; init; }
    public required DateOnly OrderDate { get; init; }
    public required DateOnly DeliveryDate { get; init; }
    public required int CustomerId { get; init; }
    public required Guid? OfficeId { get; init; }
    public required Guid WarehouseId { get; init; }
    public required int? SalesForceId { get; init; }
    public required int? PriceTypeId { get; init; }
    public required string? ProjectName { get; init; }
    public required int? ReferredByPartnerId { get; init; }
    public required string? CustomerPo { get; init; }
    public required DateOnly? CustomerPoDate { get; init; }
    public required int? PaymentTypeId { get; init; }
    public required int? PaymentTermDays { get; init; }
    public required int? PaymentMethodId { get; init; }
    public required int? BankAccountId { get; init; }
    public required string? InstrumentNo { get; init; }
    public required DateOnly? InstrumentDate { get; init; }
    public required string? ShippingContactPerson { get; init; }
    public required int? ShippingAddressId { get; init; }
    public required string? ShippingAddressName { get; init; }
    public required bool IsVatInclusive { get; init; }
    public required bool IsGrossDiscount { get; init; }
    public required decimal? HeaderDiscountPercentage { get; init; }
    public required decimal? HeaderDiscount { get; init; }
    public required decimal? SpecialDiscountAmount { get; init; }
    public required decimal? OthersCost { get; init; }
    public required int? OtherChargeAccountId { get; init; }
    public required decimal? ExtraCharge { get; init; }
    public required int? ExtraChargeAccountId { get; init; }
    public required string? DeductionLabel { get; init; }
    public required decimal? DeductionValue { get; init; }
    public required int? FreeTypeId { get; init; }
    public required int? CurrencyId { get; init; }
    public required decimal? ExchangeRate { get; init; }
    public required bool IsAdvanceAdjust { get; init; }
    public required string? Remarks { get; init; }
    public Dictionary<string, JsonElement>? CustomData { get; init; }
}

public abstract record SaveSalesLine
{
    public required int ItemId { get; init; }
    public required string? ItemDescription { get; init; }
    public required decimal Quantity { get; init; }
    public required decimal? Price { get; init; }
    public required decimal? DiscountPercentage { get; init; }
    public required decimal? DiscountAmount { get; init; }
    public required bool IsAmountWiseDiscount { get; init; }
    public required bool IsDiscountPerQuantity { get; init; }
    public required decimal? SdPercentage { get; init; }
    public required decimal? VatPercentage { get; init; }
    public required decimal? AitPercentage { get; init; }
    public required int? WarrantyInMonth { get; init; }
    public required bool IsOfferItem { get; init; }
    public required string? ShelfLife { get; init; }
}

public sealed record SalesNames(
    string? Office, string? Warehouse, string? PaymentMethod, string? ShipTo, string? OtherChargeAccount, string? ExtraChargeAccount,
    string? ReviewedBy);

public abstract record SalesDocumentDetail
{
    protected SalesDocumentDetail()
    {
    }

    [SetsRequiredMembers]
    protected SalesDocumentDetail(SalesDocument d, SalesNames names)
    {
        Id = d.Id;
        Code = d.Code;
        OrderDate = d.OrderDate;
        DeliveryDate = d.DeliveryDate;
        CustomerId = d.CustomerId;
        CustomerName = d.CustomerName;
        OfficeId = d.OfficeId;
        OfficeName = names.Office;
        WarehouseId = d.WarehouseId;
        WarehouseName = names.Warehouse;
        SalesForceId = d.SalesForceId;
        SalesForceName = d.SalesForceName;
        PriceTypeId = d.PriceTypeId;
        PriceTypeName = d.PriceTypeName;
        ProjectName = d.ProjectName;
        ReferredByPartnerId = d.ReferredByPartnerId;
        ReferredByPartnerName = d.ReferredByPartnerName;
        CustomerPo = d.CustomerPo;
        CustomerPoDate = d.CustomerPoDate;
        PaymentTypeId = d.PaymentTypeId;
        PaymentTypeName = d.PaymentTypeName;
        PaymentTermDays = d.PaymentTermDays;
        PaymentMethodId = d.PaymentMethodId;
        PaymentMethodName = names.PaymentMethod;
        BankAccountId = d.BankAccountId;
        BankAccountName = d.BankAccountName;
        InstrumentNo = d.InstrumentNo;
        InstrumentDate = d.InstrumentDate;
        ShippingContactPerson = d.ShippingContactPerson;
        ShippingAddressId = d.ShippingAddressId;
        ShippingAddressName = d.ShippingAddressName;
        ShipToName = names.ShipTo;
        IsVatInclusive = d.IsVatInclusive;
        IsGrossDiscount = d.IsGrossDiscount;
        HeaderDiscountPercentage = d.HeaderDiscountPercentage;
        HeaderDiscount = d.HeaderDiscount;
        TotalLineDiscount = d.TotalLineDiscount;
        NetDiscount = d.NetDiscount;
        SpecialDiscountAmount = d.SpecialDiscountAmount;
        OthersCost = d.OthersCost;
        OtherChargeAccountId = d.OtherChargeAccountId;
        OtherChargeAccountName = names.OtherChargeAccount;
        ExtraCharge = d.ExtraCharge;
        ExtraChargeAccountId = d.ExtraChargeAccountId;
        ExtraChargeAccountName = names.ExtraChargeAccount;
        DeductionLabel = d.DeductionLabel;
        DeductionValue = d.DeductionValue;
        FreeTypeId = d.FreeTypeId;
        FreeTypeName = d.FreeTypeName;
        CurrencyId = d.CurrencyId;
        CurrencyName = d.CurrencyName;
        ExchangeRate = d.ExchangeRate;
        IsAdvanceAdjust = d.IsAdvanceAdjust;
        Remarks = d.Remarks;
        TotalQuantity = d.TotalQuantity;
        ItemTotalAmount = d.ItemTotalAmount;
        OfferItemTotal = d.OfferItemTotal;
        TotalSd = d.TotalSd;
        TotalVat = d.TotalVat;
        TotalAit = d.TotalAit;
        NetAmount = d.NetAmount;
        ReceiveAmount = d.ReceiveAmount;
        PendingAmount = d.PendingAmount;
        Status = ApprovalFlow.StatusKey(d.Status);
        ReviewedAt = d.ReviewedAt;
        ReviewedByName = names.ReviewedBy;
        ReviewComment = d.ReviewComment;
        CustomData = CustomFieldData.Parse(d.CustomData);
        CreatedAt = d.CreatedAt;
        ModifiedAt = d.ModifiedAt;
    }

    public required int Id { get; init; }
    public required string Code { get; init; }
    public required DateOnly OrderDate { get; init; }
    public required DateOnly DeliveryDate { get; init; }
    public required int CustomerId { get; init; }
    public required string CustomerName { get; init; }
    public required Guid? OfficeId { get; init; }
    public required string? OfficeName { get; init; }
    public required Guid WarehouseId { get; init; }
    public required string? WarehouseName { get; init; }
    public required int? SalesForceId { get; init; }
    public required string? SalesForceName { get; init; }
    public required int? PriceTypeId { get; init; }
    public required string? PriceTypeName { get; init; }
    public required string? ProjectName { get; init; }
    public required int? ReferredByPartnerId { get; init; }
    public required string? ReferredByPartnerName { get; init; }
    public required string? CustomerPo { get; init; }
    public required DateOnly? CustomerPoDate { get; init; }
    public required int? PaymentTypeId { get; init; }
    public required string? PaymentTypeName { get; init; }
    public required int PaymentTermDays { get; init; }
    public required int? PaymentMethodId { get; init; }
    public required string? PaymentMethodName { get; init; }
    public required int? BankAccountId { get; init; }
    public required string? BankAccountName { get; init; }
    public required string? InstrumentNo { get; init; }
    public required DateOnly? InstrumentDate { get; init; }
    public required string? ShippingContactPerson { get; init; }
    public required int? ShippingAddressId { get; init; }
    public required string? ShippingAddressName { get; init; }
    public required string? ShipToName { get; init; }
    public required bool IsVatInclusive { get; init; }
    public required bool IsGrossDiscount { get; init; }
    public required decimal HeaderDiscountPercentage { get; init; }
    public required decimal HeaderDiscount { get; init; }
    public required decimal TotalLineDiscount { get; init; }
    public required decimal NetDiscount { get; init; }
    public required decimal SpecialDiscountAmount { get; init; }
    public required decimal OthersCost { get; init; }
    public required int? OtherChargeAccountId { get; init; }
    public required string? OtherChargeAccountName { get; init; }
    public required decimal ExtraCharge { get; init; }
    public required int? ExtraChargeAccountId { get; init; }
    public required string? ExtraChargeAccountName { get; init; }
    public required string? DeductionLabel { get; init; }
    public required decimal DeductionValue { get; init; }
    public required int? FreeTypeId { get; init; }
    public required string? FreeTypeName { get; init; }
    public required int? CurrencyId { get; init; }
    public required string? CurrencyName { get; init; }
    public required decimal ExchangeRate { get; init; }
    public required bool IsAdvanceAdjust { get; init; }
    public required string? Remarks { get; init; }
    public required decimal TotalQuantity { get; init; }
    public required decimal ItemTotalAmount { get; init; }
    public required decimal OfferItemTotal { get; init; }
    public required decimal TotalSd { get; init; }
    public required decimal TotalVat { get; init; }
    public required decimal TotalAit { get; init; }
    public required decimal NetAmount { get; init; }
    public required decimal ReceiveAmount { get; init; }
    public required decimal PendingAmount { get; init; }
    public required string Status { get; init; }
    public required DateTime? ReviewedAt { get; init; }
    public required string? ReviewedByName { get; init; }
    public required string? ReviewComment { get; init; }
    public required Dictionary<string, JsonElement>? CustomData { get; init; }
    public required DateTime? CreatedAt { get; init; }
    public required DateTime? ModifiedAt { get; init; }
}

public abstract record SalesLineDetail
{
    protected SalesLineDetail()
    {
    }

    [SetsRequiredMembers]
    protected SalesLineDetail(SalesDocumentLine l)
    {
        Id = l.Id;
        ItemId = l.ItemId;
        ItemCode = l.Item!.Code;
        ItemName = l.ItemName;
        ItemDescription = l.ItemDescription;
        UomName = l.UomName;
        Quantity = l.Quantity;
        Price = l.Price;
        TotalAmount = l.TotalAmount;
        DiscountPercentage = l.DiscountPercentage;
        DiscountAmount = l.DiscountAmount;
        IsAmountWiseDiscount = l.IsAmountWiseDiscount;
        IsDiscountPerQuantity = l.IsDiscountPerQuantity;
        SdPercentage = l.SdPercentage;
        SdAmount = l.SdAmount;
        VatPercentage = l.VatPercentage;
        VatAmount = l.VatAmount;
        AitPercentage = l.AitPercentage;
        AitAmount = l.AitAmount;
        TotalValue = l.TotalValue;
        WarrantyInMonth = l.WarrantyInMonth;
        IsOfferItem = l.IsOfferItem;
        ShelfLife = l.ShelfLife;
    }

    public required int Id { get; init; }
    public required int ItemId { get; init; }
    public required string ItemCode { get; init; }
    public required string ItemName { get; init; }
    public required string? ItemDescription { get; init; }
    public required string UomName { get; init; }
    public required decimal Quantity { get; init; }
    public required decimal Price { get; init; }
    public required decimal TotalAmount { get; init; }
    public required decimal DiscountPercentage { get; init; }
    public required decimal DiscountAmount { get; init; }
    public required bool IsAmountWiseDiscount { get; init; }
    public required bool IsDiscountPerQuantity { get; init; }
    public required decimal SdPercentage { get; init; }
    public required decimal SdAmount { get; init; }
    public required decimal VatPercentage { get; init; }
    public required decimal VatAmount { get; init; }
    public required decimal AitPercentage { get; init; }
    public required decimal AitAmount { get; init; }
    public required decimal TotalValue { get; init; }
    public required int WarrantyInMonth { get; init; }
    public required bool IsOfferItem { get; init; }
    public required string? ShelfLife { get; init; }
}

internal static class SalesValidation
{
    public static void AddRules<T>(AbstractValidator<T> validator, Func<T, int> lineCount) where T : SaveSalesDocumentRequest
    {
        validator.RuleFor(x => x.CustomerId).GreaterThan(0).WithMessage("Customer is required.");
        validator.RuleFor(x => x.WarehouseId).NotEmpty();
        validator.RuleFor(x => x.DeliveryDate).GreaterThanOrEqualTo(x => x.OrderDate).WithMessage("Use a date on or after the order date.");
        validator.RuleFor(x => lineCount(x)).InclusiveBetween(1, DocumentData.MaxLines).OverridePropertyName("lines")
            .WithMessage("Add at least one item.");
    }

    public static void AddLineRules<T>(InlineValidator<T> line) where T : SaveSalesLine
    {
        line.RuleFor(l => l.ItemId).GreaterThan(0);
        line.RuleFor(l => l.ItemDescription).MaximumLength(2000);
        line.RuleFor(l => l.Quantity).GreaterThan(0);
        line.RuleFor(l => l.Price).GreaterThanOrEqualTo(0).When(l => l.Price is not null);
        line.RuleFor(l => l.DiscountPercentage).InclusiveBetween(0, 100).When(l => l.DiscountPercentage is not null);
        line.RuleFor(l => l.DiscountAmount).GreaterThanOrEqualTo(0).When(l => l.DiscountAmount is not null);
        line.RuleFor(l => l.SdPercentage).InclusiveBetween(0, 1000).When(l => l.SdPercentage is not null);
        line.RuleFor(l => l.VatPercentage).InclusiveBetween(0, 100).When(l => l.VatPercentage is not null);
        line.RuleFor(l => l.AitPercentage).InclusiveBetween(0, 100).When(l => l.AitPercentage is not null);
        line.RuleFor(l => l.WarrantyInMonth).InclusiveBetween(0, 1200).When(l => l.WarrantyInMonth is not null);
        line.RuleFor(l => l.ShelfLife).MaximumLength(150);
    }
}
