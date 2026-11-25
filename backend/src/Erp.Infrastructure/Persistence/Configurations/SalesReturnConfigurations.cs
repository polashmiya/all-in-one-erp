using Erp.Core.Domain.Accounting;
using Erp.Core.Domain.Identity;
using Erp.Core.Domain.Metadata;
using Erp.Core.Domain.Partners;
using Erp.Core.Domain.Sales;
using Erp.Core.Domain.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Erp.Infrastructure.Persistence.Configurations;

internal sealed class SalesReturnConfiguration : IEntityTypeConfiguration<SalesReturn>
{
    public void Configure(EntityTypeBuilder<SalesReturn> builder)
    {
        builder.MapColumns();
        builder.MapRecord("SalesReturn", "SalesReturnID");
        builder.Property(x => x.Code).HasColumnName("SalesReturnCode").HasMaxLength(50);
        builder.Property(x => x.DeliveryId).HasColumnName("SalesDeliveryID");
        builder.Property(x => x.DeliveryCode).HasColumnName("SalesDeliveryCode").HasMaxLength(50);
        builder.Property(x => x.OrderId).HasColumnName("CustomerOrderID");
        builder.Property(x => x.OrderCode).HasColumnName("CustomerOrderCode").HasMaxLength(50);
        builder.Property(x => x.IsVatInclusive).HasColumnName("isInclusive");
        builder.Property(x => x.BankAccountId).HasColumnName("BankAccID");
        builder.Property(x => x.BankAccountName).HasColumnName("BankAccName");
        builder.Property(x => x.TotalAit).HasColumnName("TotalAITAmount");
        builder.Property(x => x.LossAmount).HasColumnName("SalesReturnLossAmount");
        builder.Property(x => x.Remarks).HasMaxLength(2000);
        builder.Property(x => x.Narration).HasColumnType("nvarchar(max)");
        builder.Property(x => x.ReviewedAt).HasColumnName("ApproveDatetime").HasColumnType("datetime").HasConversion(MasterRecordMapping.Utc);
        builder.Property(x => x.ReviewedById).HasColumnName("ApproveByID");
        builder.Property(x => x.ReviewComment).HasColumnName("RejectReason");
        builder.Property(x => x.PublicId).HasColumnName("SalesReturnGuID");
        builder.Property(x => x.CustomData).HasColumnType("nvarchar(max)");

        builder.HasIndex(x => new { x.BusinessUnitId, x.Code }).IsUnique();
        builder.HasIndex(x => new { x.BusinessUnitId, x.IsActive, x.Status, x.ReturnDate });

        builder.HasOne<BusinessUnit>().WithMany().HasForeignKey(x => x.BusinessUnitId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<SalesDelivery>().WithMany().HasForeignKey(x => x.DeliveryId).OnDelete(DeleteBehavior.NoAction);
        builder.HasOne<SalesOrder>().WithMany().HasForeignKey(x => x.OrderId).OnDelete(DeleteBehavior.NoAction);
        builder.HasOne<Customer>().WithMany().HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.NoAction);
        builder.HasOne<Office>().WithMany().HasForeignKey(x => x.OfficeId).OnDelete(DeleteBehavior.NoAction);
        builder.HasOne<Warehouse>().WithMany().HasForeignKey(x => x.WarehouseId).OnDelete(DeleteBehavior.NoAction);
        builder.HasOne<Employee>().WithMany().HasForeignKey(x => x.SalesForceId).OnDelete(DeleteBehavior.NoAction);
        builder.HasOne<Employee>().WithMany().HasForeignKey(x => x.ReceivedById).OnDelete(DeleteBehavior.NoAction);
        builder.HasOne<BankAccount>().WithMany().HasForeignKey(x => x.BankAccountId).OnDelete(DeleteBehavior.NoAction);
        builder.HasOne<LookupValue>().WithMany().HasForeignKey(x => x.ReturnTypeId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<LookupValue>().WithMany().HasForeignKey(x => x.ReturnReasonId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<LookupValue>().WithMany().HasForeignKey(x => x.DeliveryMethodId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<LookupValue>().WithMany().HasForeignKey(x => x.PaymentMethodId).OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(x => x.Lines).WithOne().HasForeignKey(x => x.ReturnId).OnDelete(DeleteBehavior.Cascade);

        builder.ColumnOrder([nameof(SalesReturn.Id), nameof(SalesReturn.Code)]);
    }
}

internal sealed class SalesReturnLineConfiguration : IEntityTypeConfiguration<SalesReturnLine>
{
    public void Configure(EntityTypeBuilder<SalesReturnLine> builder)
    {
        builder.ToTable("SalesReturnLine");
        builder.HasKey(x => x.Id);
        builder.MapColumns();
        builder.Property(x => x.Id).UseIdentityColumn();
        builder.Property(x => x.ReturnId).HasColumnName("SalesReturnID");
        builder.Property(x => x.DeliveryLineId).HasColumnName("SalesDeliveryLineID");
        builder.Property(x => x.Sequence).HasColumnName("ViewSequenceID");
        builder.Property(x => x.ItemName).HasMaxLength(1000);
        builder.Property(x => x.ItemDescription).HasMaxLength(2000);
        builder.Property(x => x.UomName).HasMaxLength(100);
        builder.Property(x => x.DiscountAmount).HasColumnName("LineDiscount");
        builder.Property(x => x.TotalValue).HasColumnName("NetAmount");
        builder.Property(x => x.LossAmount).HasColumnName("SalesReturnLossAmount");
        builder.Property(x => x.Batch).HasColumnType("nvarchar(max)");
        builder.Property(x => x.ItemSerial).HasColumnType("nvarchar(max)");

        builder.HasOne(x => x.Item).WithMany().HasForeignKey(x => x.ItemId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Uom).WithMany().HasForeignKey(x => x.UomId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<SalesDeliveryLine>().WithMany().HasForeignKey(x => x.DeliveryLineId).OnDelete(DeleteBehavior.NoAction);
        builder.ColumnOrder([nameof(SalesLine.Id)]);
    }
}

internal sealed class SalesCollectionConfiguration : IEntityTypeConfiguration<SalesCollection>
{
    public void Configure(EntityTypeBuilder<SalesCollection> builder)
    {
        builder.MapColumns();
        builder.MapRecord("SalesCollection", "SalesCollectionID");
        builder.Property(x => x.Code).HasColumnName("SalesCollectionCode").HasMaxLength(50);
        builder.Property(x => x.BankAccountId).HasColumnName("BankAccID");
        builder.Property(x => x.BankAccountName).HasColumnName("BankAccName");
        builder.Property(x => x.Remarks).HasMaxLength(2000);
        builder.Property(x => x.Narration).HasColumnType("nvarchar(max)");
        builder.Property(x => x.ReviewedAt).HasColumnName("ApproveDatetime").HasColumnType("datetime").HasConversion(MasterRecordMapping.Utc);
        builder.Property(x => x.ReviewedById).HasColumnName("ApproveByID");
        builder.Property(x => x.ReviewComment).HasColumnName("RejectReason");
        builder.Property(x => x.PublicId).HasColumnName("SalesCollectionGuID");
        builder.Property(x => x.CustomData).HasColumnType("nvarchar(max)");

        builder.HasIndex(x => new { x.BusinessUnitId, x.Code }).IsUnique();
        builder.HasIndex(x => new { x.BusinessUnitId, x.IsActive, x.Status, x.CollectionDate });

        builder.HasOne<BusinessUnit>().WithMany().HasForeignKey(x => x.BusinessUnitId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Customer>().WithMany().HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.NoAction);
        builder.HasOne<Office>().WithMany().HasForeignKey(x => x.OfficeId).OnDelete(DeleteBehavior.NoAction);
        builder.HasOne<Employee>().WithMany().HasForeignKey(x => x.CollectedById).OnDelete(DeleteBehavior.NoAction);
        builder.HasOne<BankAccount>().WithMany().HasForeignKey(x => x.BankAccountId).OnDelete(DeleteBehavior.NoAction);
        builder.HasOne<LookupValue>().WithMany().HasForeignKey(x => x.PaymentMethodId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<LookupValue>().WithMany().HasForeignKey(x => x.InstrumentTypeId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<LookupValue>().WithMany().HasForeignKey(x => x.CurrencyId).OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(x => x.Lines).WithOne().HasForeignKey(x => x.CollectionId).OnDelete(DeleteBehavior.Cascade);

        builder.ColumnOrder([nameof(SalesCollection.Id), nameof(SalesCollection.Code)]);
    }
}

internal sealed class SalesCollectionLineConfiguration : IEntityTypeConfiguration<SalesCollectionLine>
{
    public void Configure(EntityTypeBuilder<SalesCollectionLine> builder)
    {
        builder.ToTable("SalesCollectionLine");
        builder.HasKey(x => x.Id);
        builder.MapColumns();
        builder.Property(x => x.Id).UseIdentityColumn();
        builder.Property(x => x.CollectionId).HasColumnName("SalesCollectionID");
        builder.Property(x => x.DeliveryId).HasColumnName("SalesDeliveryID");
        builder.Property(x => x.DeliveryCode).HasColumnName("SalesDeliveryCode").HasMaxLength(50);

        builder.HasOne<SalesDelivery>().WithMany().HasForeignKey(x => x.DeliveryId).OnDelete(DeleteBehavior.NoAction);
    }
}
