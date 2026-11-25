using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using Erp.Api.Documents;
using Erp.Core.Domain.Sales;
using FluentValidation;

namespace Erp.Api.Sales.SalesDeliveries;

public sealed record SalesDeliveryRow(
    int Id,
    string Code,
    string Type,
    DateOnly DeliveryDate,
    int CustomerId,
    string CustomerName,
    int? OrderId,
    string? OrderName,
    Guid WarehouseId,
    string? WarehouseName,
    string? ChallanNo,
    int? SalesForceId,
    string? SalesForceName,
    string Status,
    bool IsConfirmed,
    int ItemCount,
    decimal TotalQuantity,
    decimal NetAmount,
    decimal PendingAmount,
    DateTime? CreatedAt,
    DateTime? ModifiedAt);

public sealed record SalesDeliveryList(IReadOnlyList<SalesDeliveryRow> Items, int Total, ApprovalCounts Counts);

public sealed record DeliveryOrderLine(
    int OrderLineId, int ItemId, string ItemCode, string ItemName, string UomName, string? ItemDescription, decimal Ordered,
    decimal Remaining, decimal Price, decimal DiscountPerUnit, decimal SdPercentage, decimal VatPercentage, decimal AitPercentage,
    int WarrantyInMonth, string? ShelfLife, bool IsOfferItem);

public sealed record DeliveryOrderOption
{
    public required int Id { get; init; }
    public required string Code { get; init; }
    public required string Name { get; init; }
    public required bool IsActive { get; init; }
    public required DateOnly OrderDate { get; init; }
    public required int CustomerId { get; init; }
    public required string CustomerName { get; init; }
    public required string? Phone { get; init; }
    public required Guid? OfficeId { get; init; }
    public required Guid WarehouseId { get; init; }
    public required int? SalesForceId { get; init; }
    public required int? PriceTypeId { get; init; }
    public required string? ProjectName { get; init; }
    public required int? ReferredByPartnerId { get; init; }
    public required string? CustomerPo { get; init; }
    public required DateOnly? CustomerPoDate { get; init; }
    public required int? PaymentTypeId { get; init; }
    public required int PaymentTermDays { get; init; }
    public required string? ShippingContactPerson { get; init; }
    public required int? ShippingAddressId { get; init; }
    public required string? ShippingAddressName { get; init; }
    public required bool IsVatInclusive { get; init; }
    public required decimal SpecialDiscountAmount { get; init; }
    public required decimal OthersCost { get; init; }
    public required int? OtherChargeAccountId { get; init; }
    public required decimal ExtraCharge { get; init; }
    public required int? ExtraChargeAccountId { get; init; }
    public required string? DeductionLabel { get; init; }
    public required decimal DeductionValue { get; init; }
    public required int? FreeTypeId { get; init; }
    public required int? CurrencyId { get; init; }
    public required decimal ExchangeRate { get; init; }
    public required bool IsAdvanceAdjust { get; init; }
    public required decimal AdvanceAvailable { get; init; }
    public required IReadOnlyList<DeliveryOrderLine> Lines { get; init; }
}

public sealed record SaveSalesDeliveryLine : SaveSalesLine
{
    public required int? OrderLineId { get; init; }
    public required string? Batch { get; init; }
    public required string? ItemSerial { get; init; }
    public required string? PackQuantity { get; init; }
    public required string? WarrantyDescription { get; init; }
    public required DateOnly? WarrantyExpiredDate { get; init; }
}

public sealed record SaveSalesDeliveryRequest : SaveSalesDocumentRequest
{
    [JsonConstructor]
    public SaveSalesDeliveryRequest()
    {
    }

    [SetsRequiredMembers]
    public SaveSalesDeliveryRequest(SalesDelivery d)
        : base(d)
    {
        Type = SalesDeliveryEndpoints.TypeKey(d.Type);
        OrderId = d.OrderId;
        Phone = d.Phone;
        ChallanNo = d.ChallanNo;
        VatChallanNo = d.VatChallanNo;
        BillNo = d.BillNo;
        RefInvoice = d.RefInvoice;
        AnonymousAddress = d.AnonymousAddress;
        Narration = d.Narration;
        DeliveryMethodId = d.DeliveryMethodId;
        CourierOrEmployeeId = d.CourierOrEmployeeId;
        SubmissionDate = d.SubmissionDate;
        AlternativeDeliveryDate = d.AlternativeDeliveryDate;
        AdvanceBalanceAdjust = d.AdvanceBalanceAdjust;
        ReceiveAmount = d.ReceiveAmount;
        InterestRate = d.InterestRate;
        TotalNoOfInstallment = d.TotalNoOfInstallment;
        InstallmentStartDate = d.InstallmentStartDate;
        InstallmentTypeId = d.InstallmentTypeId;
        CommissionPercentage = d.CommissionPercentage;
        IsExportSales = d.IsExportSales;
        Lines = [];
    }

    public required string Type { get; init; }
    public required int? OrderId { get; init; }
    public required string? Phone { get; init; }
    public required string? ChallanNo { get; init; }
    public required string? VatChallanNo { get; init; }
    public required string? BillNo { get; init; }
    public required string? RefInvoice { get; init; }
    public required string? AnonymousAddress { get; init; }
    public required string? Narration { get; init; }
    public required int? DeliveryMethodId { get; init; }
    public required int? CourierOrEmployeeId { get; init; }
    public required DateOnly? SubmissionDate { get; init; }
    public required DateOnly? AlternativeDeliveryDate { get; init; }
    public required decimal? AdvanceBalanceAdjust { get; init; }
    public required decimal? ReceiveAmount { get; init; }
    public required decimal? InterestRate { get; init; }
    public required int? TotalNoOfInstallment { get; init; }
    public required DateOnly? InstallmentStartDate { get; init; }
    public required int? InstallmentTypeId { get; init; }
    public required decimal? CommissionPercentage { get; init; }
    public required bool IsExportSales { get; init; }
    public required IReadOnlyList<SaveSalesDeliveryLine> Lines { get; init; }
}

public sealed record ConfirmDeliveryRequest
{
    public required DateOnly ActualDeliveryDate { get; init; }
    public required int? CollectedById { get; init; }
    public required string? GrnNo { get; init; }
    public required DateOnly? GrnDate { get; init; }
    public required decimal? GrnAmount { get; init; }
    public required DateOnly? GrnCollectionDate { get; init; }
    public required string? Remarks { get; init; }
}

public sealed record DeliveryFollowUp(string Feature, int Id, string Code, DateOnly Date, decimal Amount, string Status);

public sealed record SalesDeliveryLineDetail : SalesLineDetail
{
    [JsonConstructor]
    public SalesDeliveryLineDetail()
    {
    }

    [SetsRequiredMembers]
    public SalesDeliveryLineDetail(SalesDeliveryLine l, decimal? remaining)
        : base(l)
    {
        OrderLineId = l.OrderLineId;
        Remaining = remaining;
        ReturnedQuantity = l.ReturnedQuantity;
        Batch = l.Batch;
        ItemSerial = l.ItemSerial;
        PackQuantity = l.PackQuantity;
        WarrantyDescription = l.WarrantyDescription;
        WarrantyExpiredDate = l.WarrantyExpiredDate;
        CostPrice = l.CostPrice;
        CostTotal = l.CostTotal;
    }

    public required int? OrderLineId { get; init; }
    public required decimal? Remaining { get; init; }
    public required decimal ReturnedQuantity { get; init; }
    public required string? Batch { get; init; }
    public required string? ItemSerial { get; init; }
    public required string? PackQuantity { get; init; }
    public required string? WarrantyDescription { get; init; }
    public required DateOnly? WarrantyExpiredDate { get; init; }
    public required decimal CostPrice { get; init; }
    public required decimal CostTotal { get; init; }
}

public sealed record SalesDeliveryDetail : SalesDocumentDetail
{
    [JsonConstructor]
    public SalesDeliveryDetail()
    {
    }

    [SetsRequiredMembers]
    public SalesDeliveryDetail(SalesDelivery d, SalesNames names, decimal advanceAvailable, Dictionary<int, decimal> remaining,
        IReadOnlyList<DeliveryFollowUp> followUps)
        : base(d, names)
    {
        Type = SalesDeliveryEndpoints.TypeKey(d.Type);
        OrderId = d.OrderId;
        OrderName = d.OrderCode;
        Phone = d.Phone;
        ChallanNo = d.ChallanNo;
        VatChallanNo = d.VatChallanNo;
        BillNo = d.BillNo;
        RefInvoice = d.RefInvoice;
        AnonymousAddress = d.AnonymousAddress;
        Narration = d.Narration;
        DeliveryMethodId = d.DeliveryMethodId;
        DeliveryMethodName = d.DeliveryMethodName;
        CourierOrEmployeeId = d.CourierOrEmployeeId;
        CourierOrEmployeeName = d.CourierOrEmployeeName;
        SubmissionDate = d.SubmissionDate;
        AlternativeDeliveryDate = d.AlternativeDeliveryDate;
        AdvanceAvailable = advanceAvailable;
        AdvanceBalanceAdjust = d.AdvanceBalanceAdjust;
        CustomerNetAmount = d.CustomerNetAmount;
        InterestRate = d.InterestRate;
        NetAmountWithInterest = d.NetAmountWithInterest;
        TotalNoOfInstallment = d.TotalNoOfInstallment;
        InstallmentStartDate = d.InstallmentStartDate;
        InstallmentTypeId = d.InstallmentTypeId;
        InstallmentTypeName = d.InstallmentTypeName;
        AmountPerInstallment = d.AmountPerInstallment;
        CommissionPercentage = d.CommissionPercentage;
        IsExportSales = d.IsExportSales;
        IsConfirmed = d.IsConfirmed;
        ConfirmedByName = d.ConfirmedByName;
        ConfirmedDate = d.ConfirmedDate;
        ActualDeliveryDate = d.ActualDeliveryDate;
        CollectedByName = d.CollectedByName;
        GrnNo = d.GrnNo;
        GrnDate = d.GrnDate;
        GrnAmount = d.GrnAmount;
        GrnCollectionDate = d.GrnCollectionDate;
        ConfirmRemarks = d.ConfirmRemarks;
        TotalCost = d.Lines.Sum(l => l.CostTotal);
        ReturnAmount = d.ReturnAmount;
        ReturnAdjusted = d.ReturnAdjusted;
        CollectedAmount = d.CollectedAmount;
        FollowUps = followUps;
        Lines =
        [
            .. d.Lines.OrderBy(l => l.Sequence).Select(l => new SalesDeliveryLineDetail(
                l, l.OrderLineId is { } id && remaining.TryGetValue(id, out var left) ? left : null)),
        ];
    }

    public required string Type { get; init; }
    public required int? OrderId { get; init; }
    public required string? OrderName { get; init; }
    public required string? Phone { get; init; }
    public required string? ChallanNo { get; init; }
    public required string? VatChallanNo { get; init; }
    public required string? BillNo { get; init; }
    public required string? RefInvoice { get; init; }
    public required string? AnonymousAddress { get; init; }
    public required string? Narration { get; init; }
    public required int? DeliveryMethodId { get; init; }
    public required string? DeliveryMethodName { get; init; }
    public required int? CourierOrEmployeeId { get; init; }
    public required string? CourierOrEmployeeName { get; init; }
    public required DateOnly? SubmissionDate { get; init; }
    public required DateOnly? AlternativeDeliveryDate { get; init; }
    public required decimal AdvanceAvailable { get; init; }
    public required decimal AdvanceBalanceAdjust { get; init; }
    public required decimal CustomerNetAmount { get; init; }
    public required decimal InterestRate { get; init; }
    public required decimal NetAmountWithInterest { get; init; }
    public required int TotalNoOfInstallment { get; init; }
    public required DateOnly? InstallmentStartDate { get; init; }
    public required int? InstallmentTypeId { get; init; }
    public required string? InstallmentTypeName { get; init; }
    public required decimal AmountPerInstallment { get; init; }
    public required decimal CommissionPercentage { get; init; }
    public required bool IsExportSales { get; init; }
    public required bool IsConfirmed { get; init; }
    public required string? ConfirmedByName { get; init; }
    public required DateTime? ConfirmedDate { get; init; }
    public required DateOnly? ActualDeliveryDate { get; init; }
    public required string? CollectedByName { get; init; }
    public required string? GrnNo { get; init; }
    public required DateOnly? GrnDate { get; init; }
    public required decimal? GrnAmount { get; init; }
    public required DateOnly? GrnCollectionDate { get; init; }
    public required string? ConfirmRemarks { get; init; }
    public required decimal TotalCost { get; init; }
    public required decimal ReturnAmount { get; init; }
    public required decimal ReturnAdjusted { get; init; }
    public required decimal CollectedAmount { get; init; }
    public required IReadOnlyList<DeliveryFollowUp> FollowUps { get; init; }
    public required IReadOnlyList<SalesDeliveryLineDetail> Lines { get; init; }
}

internal sealed class SaveSalesDeliveryRequestValidator : AbstractValidator<SaveSalesDeliveryRequest>
{
    public SaveSalesDeliveryRequestValidator()
    {
        SalesValidation.AddRules(this, x => x.Lines.Count);
        RuleFor(x => x.Type).Must(t => SalesDeliveryEndpoints.ParseType(t) is not null).WithMessage("Choose a delivery type.");
        RuleFor(x => x.OrderId).NotNull().When(x => SalesDeliveryEndpoints.ParseType(x.Type) == DeliveryType.Order)
            .WithMessage("Choose the sales order.");
        RuleFor(x => x.OrderId).Null().When(x => SalesDeliveryEndpoints.ParseType(x.Type) == DeliveryType.Direct)
            .WithMessage("A direct delivery has no sales order.");
        RuleForEach(x => x.Lines).ChildRules(line =>
        {
            SalesValidation.AddLineRules(line);
            line.RuleFor(l => l.Batch).MaximumLength(500);
            line.RuleFor(l => l.ItemSerial).MaximumLength(2000);
            line.RuleFor(l => l.PackQuantity).MaximumLength(50);
            line.RuleFor(l => l.WarrantyDescription).MaximumLength(500);
        });
    }
}

internal sealed class ConfirmDeliveryRequestValidator : AbstractValidator<ConfirmDeliveryRequest>
{
    public ConfirmDeliveryRequestValidator()
    {
        RuleFor(x => x.ActualDeliveryDate).NotEmpty();
        RuleFor(x => x.GrnNo).MaximumLength(150);
        RuleFor(x => x.GrnAmount).GreaterThanOrEqualTo(0).When(x => x.GrnAmount is not null);
        RuleFor(x => x.Remarks).MaximumLength(500);
    }
}
