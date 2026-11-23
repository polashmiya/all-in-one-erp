using Erp.Core.Domain.Approvals;
using Erp.Core.Domain.Inventory;

namespace Erp.Core.Domain.Procurement;

public class PurchaseReturn : MasterRecord, IBusinessUnitOwned, IApprovalDocument
{
    public Guid BusinessUnitId { get; set; }

    public required string Code { get; set; }

    public DateOnly ReturnDate { get; set; }

    public int PurchaseReceiveId { get; set; }

    public int PurchaseOrderId { get; set; }

    public int SupplierId { get; set; }

    public Guid WarehouseId { get; set; }

    public int? ReasonId { get; set; }

    public bool IsVatInclusive { get; set; }

    public string? Remarks { get; set; }

    public decimal TotalQuantity { get; set; }

    public decimal ItemTotalAmount { get; set; }

    public decimal TotalVatAmount { get; set; }

    public decimal NetAmount { get; set; }

    public ApprovalStatus Status { get; set; } = ApprovalStatus.Pending;

    public DateTime? ReviewedAt { get; set; }

    public Guid? ReviewedById { get; set; }

    public string? ReviewComment { get; set; }

    public string? CustomData { get; set; }

    public List<PurchaseReturnLine> Lines { get; } = [];
}

public class PurchaseReturnLine
{
    public int Id { get; set; }

    public int PurchaseReturnId { get; set; }

    public int Sequence { get; set; }

    public int ItemId { get; set; }

    public Item? Item { get; set; }

    public int UomId { get; set; }

    public UnitOfMeasure? Uom { get; set; }

    public decimal Quantity { get; set; }

    public decimal Price { get; set; }

    public decimal TotalAmount { get; set; }

    public decimal Discount { get; set; }

    public decimal VatPercentage { get; set; }

    public decimal VatAmount { get; set; }

    public decimal NetAmount { get; set; }

    public string? Batch { get; set; }
}
