using Erp.Core.Domain.Approvals;

namespace Erp.Core.Domain.Inventory;

public enum AdjustmentType
{
    In = 1,
    Out = 2,
    Opening = 3,
}

public class StockAdjustment : MasterRecord, IBusinessUnitOwned, IApprovalDocument
{
    public Guid BusinessUnitId { get; set; }

    public required string Code { get; set; }

    public DateOnly AdjustmentDate { get; set; }

    public AdjustmentType Type { get; set; }

    public Guid WarehouseId { get; set; }

    public int? ReasonId { get; set; }

    public string? Remarks { get; set; }

    public decimal TotalQuantity { get; set; }

    public decimal TotalAmount { get; set; }

    public ApprovalStatus Status { get; set; } = ApprovalStatus.Pending;

    public DateTime? ReviewedAt { get; set; }

    public Guid? ReviewedById { get; set; }

    public string? ReviewComment { get; set; }

    public string? CustomData { get; set; }

    public List<StockAdjustmentLine> Lines { get; } = [];
}

public class StockAdjustmentLine
{
    public int Id { get; set; }

    public int StockAdjustmentId { get; set; }

    public int Sequence { get; set; }

    public int ItemId { get; set; }

    public Item? Item { get; set; }

    public int UomId { get; set; }

    public UnitOfMeasure? Uom { get; set; }

    public decimal Quantity { get; set; }

    public decimal Rate { get; set; }

    public decimal Amount { get; set; }

    public string? Remarks { get; set; }
}

public class StockTransfer : MasterRecord, IBusinessUnitOwned, IApprovalDocument
{
    public Guid BusinessUnitId { get; set; }

    public required string Code { get; set; }

    public DateOnly TransferDate { get; set; }

    public Guid? FromOfficeId { get; set; }

    public Guid FromWarehouseId { get; set; }

    public Guid? ToOfficeId { get; set; }

    public Guid ToWarehouseId { get; set; }

    public string? Remarks { get; set; }

    public decimal TotalQuantity { get; set; }

    public ApprovalStatus Status { get; set; } = ApprovalStatus.Pending;

    public DateTime? ReviewedAt { get; set; }

    public Guid? ReviewedById { get; set; }

    public string? ReviewComment { get; set; }

    public string? CustomData { get; set; }

    public List<StockTransferLine> Lines { get; } = [];
}

public class StockTransferLine
{
    public int Id { get; set; }

    public int StockTransferId { get; set; }

    public int Sequence { get; set; }

    public int ItemId { get; set; }

    public Item? Item { get; set; }

    public int UomId { get; set; }

    public UnitOfMeasure? Uom { get; set; }

    public decimal Quantity { get; set; }

    public string? Remarks { get; set; }
}
