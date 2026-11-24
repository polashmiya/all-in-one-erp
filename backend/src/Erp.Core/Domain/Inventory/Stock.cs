using Erp.Core.Domain.Accounting;

namespace Erp.Core.Domain.Inventory;

public class InventoryTransaction : MasterRecord, IBusinessUnitOwned
{
    public Guid BusinessUnitId { get; set; }

    public required string Feature { get; set; }

    public int DocumentId { get; set; }

    public required string DocumentCode { get; set; }

    public DateOnly PostingDate { get; set; }

    public int? SupplierId { get; set; }

    public string? Remarks { get; set; }

    public int? JournalId { get; set; }

    public Journal? Journal { get; set; }

    public List<InventoryLine> Lines { get; } = [];
}

public class InventoryLine
{
    public int Id { get; set; }

    public int InventoryTransactionId { get; set; }

    public InventoryTransaction? Transaction { get; set; }

    public int Sequence { get; set; }

    public int ItemId { get; set; }

    public Item? Item { get; set; }

    public int UomId { get; set; }

    public UnitOfMeasure? Uom { get; set; }

    public Guid WarehouseId { get; set; }

    public decimal Quantity { get; set; }

    public decimal Value { get; set; }
}

public class StockBalance : BaseEntity, IBusinessUnitOwned
{
    public Guid AccountId { get; set; }

    public Guid BusinessUnitId { get; set; }

    public Guid WarehouseId { get; set; }

    public int ItemId { get; set; }

    public Item? Item { get; set; }

    public decimal Quantity { get; set; }

    public decimal Value { get; set; }
}
