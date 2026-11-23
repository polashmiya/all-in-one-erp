using Erp.Core.Domain.Identity;
using Erp.Core.Domain.Metadata;
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
