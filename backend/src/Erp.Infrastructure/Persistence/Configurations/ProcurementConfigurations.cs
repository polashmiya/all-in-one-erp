using Erp.Core.Domain.Identity;
using Erp.Core.Domain.Metadata;
using Erp.Core.Domain.Partners;
using Erp.Core.Domain.Procurement;
using Erp.Core.Domain.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Erp.Infrastructure.Persistence.Configurations;

internal sealed class PurchaseRequisitionConfiguration : IEntityTypeConfiguration<PurchaseRequisition>
{
    public void Configure(EntityTypeBuilder<PurchaseRequisition> builder)
    {
        builder.MapRecord("PurchaseRequisitionHeader", "PurchaseRequisitionID");
        builder.Property(x => x.BusinessUnitId).HasColumnName("BusinessUnitID");
        builder.Property(x => x.Code).HasColumnName("PurchaseRequisitionCode").HasMaxLength(100);
        builder.Property(x => x.WarehouseId).HasColumnName("WarehouseID");
        builder.Property(x => x.RequestTypeId).HasColumnName("RequestTypeID");
        builder.Property(x => x.PurchaseMethodId).HasColumnName("PurchaseMethodID");
        builder.Property(x => x.ItemTypeId).HasColumnName("ItemTypeID");
        builder.Property(x => x.DepartmentId).HasColumnName("DepartmentID");
        builder.Property(x => x.SectionId).HasColumnName("SectionID");
        builder.Property(x => x.EmployeeId).HasColumnName("EmployeeID");
        builder.Property(x => x.BuyerId).HasColumnName("BuyerID");
        builder.Property(x => x.ProjectName).HasMaxLength(200);
        builder.Property(x => x.Remarks).HasMaxLength(300);
        builder.Property(x => x.ReviewedAt).HasColumnType("datetime").HasConversion(MasterRecordMapping.Utc);
        builder.Property(x => x.ReviewedById).HasColumnName("ReviewedByID");
        builder.Property(x => x.ReviewComment).HasMaxLength(500);
        builder.Property(x => x.TotalQuantity).HasPrecision(18, 4);
        builder.Property(x => x.TotalAmount).HasPrecision(18, 2);

        builder.HasIndex(x => new { x.BusinessUnitId, x.Code }).IsUnique();
        builder.HasIndex(x => new { x.BusinessUnitId, x.IsActive, x.Status, x.RequestDate });

        builder.HasOne<BusinessUnit>().WithMany().HasForeignKey(x => x.BusinessUnitId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Warehouse>().WithMany().HasForeignKey(x => x.WarehouseId).OnDelete(DeleteBehavior.NoAction);
        builder.HasOne<Employee>().WithMany().HasForeignKey(x => x.EmployeeId).OnDelete(DeleteBehavior.NoAction);
        builder.HasOne<Employee>().WithMany().HasForeignKey(x => x.BuyerId).OnDelete(DeleteBehavior.NoAction);
        builder.HasOne<LookupValue>().WithMany().HasForeignKey(x => x.RequestTypeId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<LookupValue>().WithMany().HasForeignKey(x => x.PurchaseMethodId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<LookupValue>().WithMany().HasForeignKey(x => x.ItemTypeId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<LookupValue>().WithMany().HasForeignKey(x => x.DepartmentId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<LookupValue>().WithMany().HasForeignKey(x => x.SectionId).OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(x => x.Rows).WithOne().HasForeignKey(x => x.RequisitionId).OnDelete(DeleteBehavior.Cascade);

        builder.ColumnOrder(
        [
            nameof(PurchaseRequisition.Id), nameof(PurchaseRequisition.Code), nameof(PurchaseRequisition.RequestDate),
            nameof(PurchaseRequisition.DueDate), nameof(PurchaseRequisition.WarehouseId), nameof(PurchaseRequisition.RequestTypeId),
            nameof(PurchaseRequisition.PurchaseMethodId), nameof(PurchaseRequisition.ItemTypeId), nameof(PurchaseRequisition.DepartmentId),
            nameof(PurchaseRequisition.SectionId), nameof(PurchaseRequisition.EmployeeId), nameof(PurchaseRequisition.BuyerId),
            nameof(PurchaseRequisition.ProjectName), nameof(PurchaseRequisition.Remarks), nameof(PurchaseRequisition.Status),
            nameof(PurchaseRequisition.ReviewedAt), nameof(PurchaseRequisition.ReviewedById), nameof(PurchaseRequisition.ReviewComment),
            nameof(PurchaseRequisition.TotalQuantity), nameof(PurchaseRequisition.TotalAmount), nameof(PurchaseRequisition.AccountId),
            nameof(PurchaseRequisition.BusinessUnitId),
            .. MasterRecordMapping.AuditColumns,
            nameof(PurchaseRequisition.CustomData),
        ]);
    }
}

internal sealed class PurchaseRequisitionRowConfiguration : IEntityTypeConfiguration<PurchaseRequisitionRow>
{
    public void Configure(EntityTypeBuilder<PurchaseRequisitionRow> builder)
    {
        builder.ToTable("PurchaseRequisitionRow");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("PurchaseRequisitionRowID").UseIdentityColumn();
        builder.Property(x => x.RequisitionId).HasColumnName("PurchaseRequisitionID");
        builder.Property(x => x.ItemId).HasColumnName("ItemID");
        builder.Property(x => x.UomId).HasColumnName("UomID");
        builder.Property(x => x.Quantity).HasPrecision(18, 4);
        builder.Property(x => x.Rate).HasPrecision(18, 4);
        builder.Property(x => x.Amount).HasPrecision(18, 2);
        builder.Property(x => x.Remarks).HasMaxLength(300);

        builder.HasOne(x => x.Item).WithMany().HasForeignKey(x => x.ItemId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Uom).WithMany().HasForeignKey(x => x.UomId).OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class PurchaseOrderConfiguration : IEntityTypeConfiguration<PurchaseOrder>
{
    public void Configure(EntityTypeBuilder<PurchaseOrder> builder)
    {
        builder.MapRecord("PurchaseOrder", "PurchaseOrderID");
        builder.Property(x => x.BusinessUnitId).HasColumnName("BusinessUnitID");
        builder.Property(x => x.Code).HasColumnName("PurchaseOrderCode").HasMaxLength(50);
        builder.Property(x => x.SupplierId).HasColumnName("SupplierID");
        builder.Property(x => x.WarehouseId).HasColumnName("WarehouseID");
        builder.Property(x => x.RequisitionId).HasColumnName("PurchaseRequisitionID");
        builder.Property(x => x.PaymentTypeId).HasColumnName("PaymentTypeID");
        builder.Property(x => x.PaymentDays).HasColumnName("PaymentDaysAfterReceive");
        builder.Property(x => x.ItemTypeId).HasColumnName("ItemTypeID");
        builder.Property(x => x.ReferenceNo).HasMaxLength(100);
        builder.Property(x => x.ShippingAddress).HasMaxLength(300);
        builder.Property(x => x.Remarks).HasMaxLength(1000);
        builder.Property(x => x.IsVatInclusive).HasColumnName("isInclusive");
        builder.Property(x => x.HeaderDiscount).HasPrecision(18, 2);
        builder.Property(x => x.DeliveryCharge).HasPrecision(18, 2);
        builder.Property(x => x.LaborCharge).HasPrecision(18, 2);
        builder.Property(x => x.OtherCharge).HasPrecision(18, 2);
        builder.Property(x => x.TotalQuantity).HasPrecision(18, 6);
        builder.Property(x => x.ReceivedQuantity).HasPrecision(18, 6);
        builder.Property(x => x.ItemTotalAmount).HasPrecision(18, 2);
        builder.Property(x => x.TotalVatAmount).HasPrecision(18, 2);
        builder.Property(x => x.NetAmount).HasPrecision(18, 2);
        builder.Property(x => x.ReviewedAt).HasColumnType("datetime").HasConversion(MasterRecordMapping.Utc);
        builder.Property(x => x.ReviewedById).HasColumnName("ReviewedByID");
        builder.Property(x => x.ReviewComment).HasMaxLength(500);

        builder.HasIndex(x => new { x.BusinessUnitId, x.Code }).IsUnique();
        builder.HasIndex(x => new { x.BusinessUnitId, x.IsActive, x.Status, x.OrderDate });

        builder.HasOne<BusinessUnit>().WithMany().HasForeignKey(x => x.BusinessUnitId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Supplier>().WithMany().HasForeignKey(x => x.SupplierId).OnDelete(DeleteBehavior.NoAction);
        builder.HasOne<Warehouse>().WithMany().HasForeignKey(x => x.WarehouseId).OnDelete(DeleteBehavior.NoAction);
        builder.HasOne<PurchaseRequisition>().WithMany().HasForeignKey(x => x.RequisitionId).OnDelete(DeleteBehavior.NoAction);
        builder.HasOne<LookupValue>().WithMany().HasForeignKey(x => x.PaymentTypeId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<LookupValue>().WithMany().HasForeignKey(x => x.ItemTypeId).OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(x => x.Lines).WithOne().HasForeignKey(x => x.PurchaseOrderId).OnDelete(DeleteBehavior.Cascade);

        builder.ColumnOrder(
        [
            nameof(PurchaseOrder.Id), nameof(PurchaseOrder.Code), nameof(PurchaseOrder.OrderDate), nameof(PurchaseOrder.SupplierId),
            nameof(PurchaseOrder.WarehouseId), nameof(PurchaseOrder.RequisitionId), nameof(PurchaseOrder.PaymentTypeId),
            nameof(PurchaseOrder.PaymentDays), nameof(PurchaseOrder.ExpectedReceiveDate), nameof(PurchaseOrder.ItemTypeId),
            nameof(PurchaseOrder.ReferenceNo), nameof(PurchaseOrder.ShippingAddress), nameof(PurchaseOrder.Remarks),
            nameof(PurchaseOrder.IsVatInclusive), nameof(PurchaseOrder.HeaderDiscount), nameof(PurchaseOrder.DeliveryCharge),
            nameof(PurchaseOrder.LaborCharge), nameof(PurchaseOrder.OtherCharge), nameof(PurchaseOrder.TotalQuantity),
            nameof(PurchaseOrder.ReceivedQuantity), nameof(PurchaseOrder.ItemTotalAmount), nameof(PurchaseOrder.TotalVatAmount),
            nameof(PurchaseOrder.NetAmount), nameof(PurchaseOrder.Status), nameof(PurchaseOrder.ReviewedAt),
            nameof(PurchaseOrder.ReviewedById), nameof(PurchaseOrder.ReviewComment), nameof(PurchaseOrder.AccountId),
            nameof(PurchaseOrder.BusinessUnitId),
            .. MasterRecordMapping.AuditColumns,
            nameof(PurchaseOrder.CustomData),
        ]);
    }
}

internal sealed class PurchaseOrderLineConfiguration : IEntityTypeConfiguration<PurchaseOrderLine>
{
    public void Configure(EntityTypeBuilder<PurchaseOrderLine> builder)
    {
        builder.ToTable("PurchaseOrderLine");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("PurchaseOrderLineID").UseIdentityColumn();
        builder.Property(x => x.PurchaseOrderId).HasColumnName("PurchaseOrderID");
        builder.Property(x => x.ItemId).HasColumnName("ItemID");
        builder.Property(x => x.UomId).HasColumnName("UomID");
        builder.Property(x => x.Quantity).HasPrecision(18, 6);
        builder.Property(x => x.ReceivedQuantity).HasPrecision(18, 6);
        builder.Property(x => x.Price).HasPrecision(18, 6);
        builder.Property(x => x.TotalAmount).HasPrecision(18, 2);
        builder.Property(x => x.Discount).HasColumnName("LineDiscount").HasPrecision(18, 2);
        builder.Property(x => x.VatPercentage).HasPrecision(18, 4);
        builder.Property(x => x.VatAmount).HasPrecision(18, 2);
        builder.Property(x => x.NetAmount).HasPrecision(18, 2);
        builder.Property(x => x.Specification).HasMaxLength(1000);

        builder.HasOne(x => x.Item).WithMany().HasForeignKey(x => x.ItemId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Uom).WithMany().HasForeignKey(x => x.UomId).OnDelete(DeleteBehavior.Restrict);
    }
}
