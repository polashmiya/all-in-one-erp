using Erp.Core.Domain.Inventory;

namespace Erp.Core.Domain.Procurement;

public class PurchaseRequisition : MasterRecord, IBusinessUnitOwned, IApprovalDocument
{
    public Guid BusinessUnitId { get; set; }

    public required string Code { get; set; }

    public DateOnly RequestDate { get; set; }

    public DateOnly? DueDate { get; set; }

    public Guid WarehouseId { get; set; }

    public int? RequestTypeId { get; set; }

    public int? PurchaseMethodId { get; set; }

    public int? ItemTypeId { get; set; }

    public int? DepartmentId { get; set; }

    public int? SectionId { get; set; }

    public int? EmployeeId { get; set; }

    public int? BuyerId { get; set; }

    public string? ProjectName { get; set; }

    public string? Remarks { get; set; }

    public ApprovalStatus Status { get; set; } = ApprovalStatus.Pending;

    public DateTime? ReviewedAt { get; set; }

    public Guid? ReviewedById { get; set; }

    public string? ReviewComment { get; set; }

    public decimal TotalQuantity { get; set; }

    public decimal TotalAmount { get; set; }

    public string? CustomData { get; set; }

    public List<PurchaseRequisitionRow> Rows { get; } = [];
}

public class PurchaseRequisitionRow
{
    public int Id { get; set; }

    public int RequisitionId { get; set; }

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
