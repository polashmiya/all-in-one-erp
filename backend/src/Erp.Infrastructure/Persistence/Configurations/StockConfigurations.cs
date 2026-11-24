using Erp.Core.Domain.Inventory;
using Erp.Core.Domain.Metadata;
using Erp.Core.Domain.Partners;
using Erp.Core.Domain.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Erp.Infrastructure.Persistence.Configurations;

internal sealed class InventoryTransactionConfiguration : IEntityTypeConfiguration<InventoryTransaction>
{
    public void Configure(EntityTypeBuilder<InventoryTransaction> builder)
    {
        builder.MapRecord("InventoryHeader", "InventoryTransID");
        builder.Property(x => x.BusinessUnitId).HasColumnName("BusinessUnitID");
        builder.Property(x => x.Feature).HasColumnName("InventoryTransType").HasMaxLength(150);
        builder.Property(x => x.DocumentId).HasColumnName("TransactionRefID");
        builder.Property(x => x.DocumentCode).HasColumnName("TransactionRefCode").HasMaxLength(100);
        builder.Property(x => x.SupplierId).HasColumnName("PartnerID");
        builder.Property(x => x.Remarks).HasMaxLength(1500);
        builder.Property(x => x.JournalId).HasColumnName("SubLedgerHeaderID");

        builder.HasIndex(x => new { x.BusinessUnitId, x.Feature, x.DocumentId, x.IsReversal }).IsUnique();
        builder.HasIndex(x => new { x.BusinessUnitId, x.PostingDate });

        builder.HasOne<BusinessUnit>().WithMany().HasForeignKey(x => x.BusinessUnitId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Supplier>().WithMany().HasForeignKey(x => x.SupplierId).OnDelete(DeleteBehavior.NoAction);
        builder.HasOne(x => x.Journal).WithMany().HasForeignKey(x => x.JournalId).OnDelete(DeleteBehavior.NoAction);
        builder.HasMany(x => x.Lines).WithOne(x => x.Transaction).HasForeignKey(x => x.InventoryTransactionId).OnDelete(DeleteBehavior.Cascade);

        builder.ColumnOrder(
        [
            nameof(InventoryTransaction.Id), nameof(InventoryTransaction.Feature), nameof(InventoryTransaction.DocumentId),
            nameof(InventoryTransaction.DocumentCode), nameof(InventoryTransaction.PostingDate), nameof(InventoryTransaction.SupplierId),
            nameof(InventoryTransaction.Remarks), nameof(InventoryTransaction.JournalId), nameof(InventoryTransaction.AccountId),
            nameof(InventoryTransaction.BusinessUnitId),
            .. MasterRecordMapping.AuditColumns,
            nameof(InventoryTransaction.IsReversal),
        ]);
    }
}

internal sealed class InventoryLineConfiguration : IEntityTypeConfiguration<InventoryLine>
{
    public void Configure(EntityTypeBuilder<InventoryLine> builder)
    {
        builder.ToTable("InventoryLine");
        builder.Property(x => x.Id).HasColumnName("InventoryLineID").UseIdentityColumn();
        builder.Property(x => x.InventoryTransactionId).HasColumnName("InventoryTransID");
        builder.Property(x => x.ItemId).HasColumnName("ItemID");
        builder.Property(x => x.UomId).HasColumnName("UomID");
        builder.Property(x => x.WarehouseId).HasColumnName("WarehouseID");
        builder.Property(x => x.Quantity).HasColumnName("TransQuantity").HasPrecision(18, 4);
        builder.Property(x => x.Value).HasColumnName("TransValue").HasPrecision(18, 2);

        builder.HasIndex(x => new { x.WarehouseId, x.ItemId });

        builder.HasOne(x => x.Item).WithMany().HasForeignKey(x => x.ItemId).OnDelete(DeleteBehavior.NoAction);
        builder.HasOne(x => x.Uom).WithMany().HasForeignKey(x => x.UomId).OnDelete(DeleteBehavior.NoAction);
        builder.HasOne<Warehouse>().WithMany().HasForeignKey(x => x.WarehouseId).OnDelete(DeleteBehavior.NoAction);
    }
}

internal sealed class StockBalanceConfiguration : IEntityTypeConfiguration<StockBalance>
{
    public void Configure(EntityTypeBuilder<StockBalance> builder)
    {
        builder.ToTable("StockBalance");
        builder.Property(x => x.Quantity).HasPrecision(18, 4);
        builder.Property(x => x.Value).HasPrecision(18, 2);

        builder.HasIndex(x => new { x.BusinessUnitId, x.WarehouseId, x.ItemId }).IsUnique();

        builder.HasOne<BusinessUnit>().WithMany().HasForeignKey(x => x.BusinessUnitId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Warehouse>().WithMany().HasForeignKey(x => x.WarehouseId).OnDelete(DeleteBehavior.NoAction);
        builder.HasOne(x => x.Item).WithMany().HasForeignKey(x => x.ItemId).OnDelete(DeleteBehavior.NoAction);
    }
}

internal sealed class StockAdjustmentConfiguration : IEntityTypeConfiguration<StockAdjustment>
{
    public void Configure(EntityTypeBuilder<StockAdjustment> builder)
    {
        builder.MapRecord("StockAdjustment", "StockAdjustmentID");
        builder.Property(x => x.BusinessUnitId).HasColumnName("BusinessUnitID");
        builder.Property(x => x.Code).HasColumnName("StockAdjustmentCode").HasMaxLength(100);
        builder.Property(x => x.WarehouseId).HasColumnName("WarehouseID");
        builder.Property(x => x.ReasonId).HasColumnName("ReasonID");
        builder.Property(x => x.Remarks).HasMaxLength(1000);
        builder.Property(x => x.TotalQuantity).HasPrecision(18, 4);
        builder.Property(x => x.TotalAmount).HasPrecision(18, 2);
        builder.Property(x => x.ReviewedAt).HasColumnType("datetime").HasConversion(MasterRecordMapping.Utc);
        builder.Property(x => x.ReviewedById).HasColumnName("ReviewedByID");
        builder.Property(x => x.ReviewComment).HasMaxLength(500);

        builder.HasIndex(x => new { x.BusinessUnitId, x.Code }).IsUnique();
        builder.HasIndex(x => new { x.BusinessUnitId, x.IsActive, x.Status, x.AdjustmentDate });

        builder.HasOne<BusinessUnit>().WithMany().HasForeignKey(x => x.BusinessUnitId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Warehouse>().WithMany().HasForeignKey(x => x.WarehouseId).OnDelete(DeleteBehavior.NoAction);
        builder.HasOne<LookupValue>().WithMany().HasForeignKey(x => x.ReasonId).OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(x => x.Lines).WithOne().HasForeignKey(x => x.StockAdjustmentId).OnDelete(DeleteBehavior.Cascade);

        builder.ColumnOrder(
        [
            nameof(StockAdjustment.Id), nameof(StockAdjustment.Code), nameof(StockAdjustment.AdjustmentDate), nameof(StockAdjustment.Type),
            nameof(StockAdjustment.WarehouseId), nameof(StockAdjustment.ReasonId), nameof(StockAdjustment.Remarks),
            nameof(StockAdjustment.TotalQuantity), nameof(StockAdjustment.TotalAmount), nameof(StockAdjustment.Status),
            nameof(StockAdjustment.ReviewedAt), nameof(StockAdjustment.ReviewedById), nameof(StockAdjustment.ReviewComment),
            nameof(StockAdjustment.AccountId), nameof(StockAdjustment.BusinessUnitId),
            .. MasterRecordMapping.AuditColumns,
            nameof(StockAdjustment.CustomData),
        ]);
    }
}

internal sealed class StockAdjustmentLineConfiguration : IEntityTypeConfiguration<StockAdjustmentLine>
{
    public void Configure(EntityTypeBuilder<StockAdjustmentLine> builder)
    {
        builder.ToTable("StockAdjustmentLine");
        builder.Property(x => x.Id).HasColumnName("StockAdjustmentLineID").UseIdentityColumn();
        builder.Property(x => x.StockAdjustmentId).HasColumnName("StockAdjustmentID");
        builder.Property(x => x.ItemId).HasColumnName("ItemID");
        builder.Property(x => x.UomId).HasColumnName("UomID");
        builder.Property(x => x.Quantity).HasPrecision(18, 4);
        builder.Property(x => x.Rate).HasPrecision(18, 4);
        builder.Property(x => x.Amount).HasPrecision(18, 2);
        builder.Property(x => x.Remarks).HasMaxLength(300);

        builder.HasOne(x => x.Item).WithMany().HasForeignKey(x => x.ItemId).OnDelete(DeleteBehavior.NoAction);
        builder.HasOne(x => x.Uom).WithMany().HasForeignKey(x => x.UomId).OnDelete(DeleteBehavior.NoAction);
    }
}

internal sealed class StockTransferConfiguration : IEntityTypeConfiguration<StockTransfer>
{
    public void Configure(EntityTypeBuilder<StockTransfer> builder)
    {
        builder.MapRecord("StockTransfer", "StockTransferID");
        builder.Property(x => x.BusinessUnitId).HasColumnName("BusinessUnitID");
        builder.Property(x => x.Code).HasColumnName("StockTransferCode").HasMaxLength(100);
        builder.Property(x => x.FromWarehouseId).HasColumnName("FromWarehouseID");
        builder.Property(x => x.FromOfficeId).HasColumnName("FromOfficeID");
        builder.Property(x => x.ToOfficeId).HasColumnName("ToOfficeID");
        builder.Property(x => x.ToWarehouseId).HasColumnName("ToWarehouseID");
        builder.Property(x => x.Remarks).HasMaxLength(1000);
        builder.Property(x => x.TotalQuantity).HasPrecision(18, 4);
        builder.Property(x => x.ReviewedAt).HasColumnType("datetime").HasConversion(MasterRecordMapping.Utc);
        builder.Property(x => x.ReviewedById).HasColumnName("ReviewedByID");
        builder.Property(x => x.ReviewComment).HasMaxLength(500);

        builder.HasIndex(x => new { x.BusinessUnitId, x.Code }).IsUnique();
        builder.HasIndex(x => new { x.BusinessUnitId, x.IsActive, x.Status, x.TransferDate });

        builder.HasOne<BusinessUnit>().WithMany().HasForeignKey(x => x.BusinessUnitId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Warehouse>().WithMany().HasForeignKey(x => x.FromWarehouseId).OnDelete(DeleteBehavior.NoAction);
        builder.HasOne<Office>().WithMany().HasForeignKey(x => x.FromOfficeId).OnDelete(DeleteBehavior.NoAction);
        builder.HasOne<Office>().WithMany().HasForeignKey(x => x.ToOfficeId).OnDelete(DeleteBehavior.NoAction);
        builder.HasOne<Warehouse>().WithMany().HasForeignKey(x => x.ToWarehouseId).OnDelete(DeleteBehavior.NoAction);
        builder.HasMany(x => x.Lines).WithOne().HasForeignKey(x => x.StockTransferId).OnDelete(DeleteBehavior.Cascade);

        builder.ColumnOrder(
        [
            nameof(StockTransfer.Id), nameof(StockTransfer.Code), nameof(StockTransfer.TransferDate), nameof(StockTransfer.FromWarehouseId),
            nameof(StockTransfer.ToWarehouseId), nameof(StockTransfer.Remarks), nameof(StockTransfer.TotalQuantity),
            nameof(StockTransfer.Status), nameof(StockTransfer.ReviewedAt), nameof(StockTransfer.ReviewedById),
            nameof(StockTransfer.ReviewComment), nameof(StockTransfer.AccountId), nameof(StockTransfer.BusinessUnitId),
            .. MasterRecordMapping.AuditColumns,
            nameof(StockTransfer.CustomData),
            nameof(StockTransfer.FromOfficeId), nameof(StockTransfer.ToOfficeId),
        ]);
    }
}

internal sealed class StockTransferLineConfiguration : IEntityTypeConfiguration<StockTransferLine>
{
    public void Configure(EntityTypeBuilder<StockTransferLine> builder)
    {
        builder.ToTable("StockTransferLine");
        builder.Property(x => x.Id).HasColumnName("StockTransferLineID").UseIdentityColumn();
        builder.Property(x => x.StockTransferId).HasColumnName("StockTransferID");
        builder.Property(x => x.ItemId).HasColumnName("ItemID");
        builder.Property(x => x.UomId).HasColumnName("UomID");
        builder.Property(x => x.Quantity).HasPrecision(18, 4);
        builder.Property(x => x.Remarks).HasMaxLength(300);

        builder.HasOne(x => x.Item).WithMany().HasForeignKey(x => x.ItemId).OnDelete(DeleteBehavior.NoAction);
        builder.HasOne(x => x.Uom).WithMany().HasForeignKey(x => x.UomId).OnDelete(DeleteBehavior.NoAction);
    }
}
