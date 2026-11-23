using Erp.Core.Domain.Approvals;
using Erp.Core.Domain.Inventory;

namespace Erp.Core.Domain.Procurement;

public class PurchaseReceive : MasterRecord, IBusinessUnitOwned, IApprovalDocument
{
    public Guid BusinessUnitId { get; set; }

    public required string Code { get; set; }

    public DateOnly ReceiveDate { get; set; }

    public int PurchaseOrderId { get; set; }

    public int SupplierId { get; set; }

    public Guid WarehouseId { get; set; }

    public string? ChallanNo { get; set; }

    public DateOnly? ChallanDate { get; set; }

    public string? VatChallanNo { get; set; }

    public int? PaymentTypeId { get; set; }

    public int? PaymentDays { get; set; }

    public bool IsVatInclusive { get; set; }

    public string? Remarks { get; set; }

    public decimal HeaderDiscount { get; set; }

    public decimal DeliveryCharge { get; set; }

    public decimal LaborCharge { get; set; }

    public decimal OtherCharge { get; set; }

    public decimal TotalQuantity { get; set; }

    public decimal TotalLineDiscount { get; set; }

    public decimal ItemTotalAmount { get; set; }

    public decimal TotalVatAmount { get; set; }

    public decimal NetAmount { get; set; }

    public decimal ReturnAmount { get; set; }

    public decimal PaidAmount { get; set; }

    public ApprovalStatus Status { get; set; } = ApprovalStatus.Pending;

    public DateTime? ReviewedAt { get; set; }

    public Guid? ReviewedById { get; set; }

    public string? ReviewComment { get; set; }

    public string? CustomData { get; set; }

    public List<PurchaseReceiveLine> Lines { get; } = [];
}

public class PurchaseReceiveLine
{
    public int Id { get; set; }

    public int PurchaseReceiveId { get; set; }

    public int Sequence { get; set; }

    public int ItemId { get; set; }

    public Item? Item { get; set; }

    public int UomId { get; set; }

    public UnitOfMeasure? Uom { get; set; }

    public decimal Quantity { get; set; }

    public decimal ReturnedQuantity { get; set; }

    public decimal Price { get; set; }

    public decimal TotalAmount { get; set; }

    public decimal Discount { get; set; }

    public decimal VatPercentage { get; set; }

    public decimal VatAmount { get; set; }

    public decimal NetAmount { get; set; }

    public string? Batch { get; set; }
}
