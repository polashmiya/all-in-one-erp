using Erp.Core.Domain.Identity;
using Erp.Core.Domain.Metadata;
using Erp.Core.Domain.Partners;
using Erp.Core.Domain.Sales;
using Erp.Core.Domain.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Erp.Infrastructure.Persistence.Configurations;

internal sealed class SalesQuotationConfiguration : IEntityTypeConfiguration<SalesQuotation>
{
    public void Configure(EntityTypeBuilder<SalesQuotation> builder)
    {
        builder.MapRecord("SalesQuotationHeader", "SalesQuotationID");
        builder.Property(x => x.BusinessUnitId).HasColumnName("BusinessUnitID");
        builder.Property(x => x.Code).HasColumnName("SalesQuotationCode").HasMaxLength(100);
        builder.Property(x => x.QuotationTypeId).HasColumnName("QuotationTypeID");
        builder.Property(x => x.Version).HasColumnName("SQVersion").HasMaxLength(100);
        builder.Property(x => x.VersionDate).HasColumnName("VersionCreateDate");
        builder.Property(x => x.OfficeId).HasColumnName("OfficeID");
        builder.Property(x => x.CustomerId).HasColumnName("PartnerID");
        builder.Property(x => x.CustomerName).HasColumnName("PartnerName").HasMaxLength(500);
        builder.Property(x => x.SalesForceId).HasColumnName("SalesForceID");
        builder.Property(x => x.SalesForceName).HasMaxLength(200);
        builder.Property(x => x.ReferenceId).HasColumnName("ReferenceID");
        builder.Property(x => x.Reference).HasMaxLength(1250);
        builder.Property(x => x.BillingContactPerson).HasMaxLength(300);
        builder.Property(x => x.BillingPhoneNo).HasMaxLength(300);
        builder.Property(x => x.BillingAddress).HasMaxLength(300);
        builder.Property(x => x.ShippingContactPerson).HasMaxLength(300);
        builder.Property(x => x.ShippingAddressId).HasColumnName("ShippingAddressID");
        builder.Property(x => x.ShippingAddressName).HasMaxLength(300);
        builder.Property(x => x.IsVatInclusive).HasColumnName("isVATInclusive");
        builder.Property(x => x.DiscountPercentage).HasPrecision(18, 4);
        builder.Property(x => x.DiscountAmount).HasPrecision(18, 4);
        builder.Property(x => x.DeliveryCharge).HasPrecision(18, 4);
        builder.Property(x => x.ExtraCharge).HasPrecision(18, 4);
        builder.Property(x => x.TotalAmount).HasPrecision(18, 4);
        builder.Property(x => x.Subjects).HasMaxLength(1250);
        builder.Property(x => x.ReviewedAt).HasColumnName("ApproveDate").HasColumnType("datetime").HasConversion(MasterRecordMapping.Utc);
        builder.Property(x => x.ReviewedById).HasColumnName("ApproveByID");
        builder.Property(x => x.ReviewComment).HasColumnName("RejectReason").HasMaxLength(500);
        builder.Property(x => x.IsExpired).HasColumnName("isExpired")
            .HasComputedColumnSql("CONVERT([bit], CASE WHEN [ValidityDate] < CONVERT([date], GETDATE()) THEN 1 ELSE 0 END)");
        builder.Property(x => x.IsOrdered).HasColumnName("isSO");
        builder.Property(x => x.OrderStatus).HasColumnName("SoStatusID");
        builder.Property(x => x.OrderStatusName).HasColumnName("SoStatusName")
            .HasComputedColumnSql("CASE [SoStatusID] WHEN 0 THEN N'Created' WHEN 1 THEN N'Not Created' ELSE N'Rejected' END");
        builder.Property(x => x.PublicId).HasColumnName("SalesQuotationGuID");

        builder.HasIndex(x => new { x.BusinessUnitId, x.Code, x.Version }).IsUnique();
        builder.HasIndex(x => new { x.BusinessUnitId, x.IsActive, x.Status, x.QuotationDate });

        builder.HasOne<BusinessUnit>().WithMany().HasForeignKey(x => x.BusinessUnitId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Office>().WithMany().HasForeignKey(x => x.OfficeId).OnDelete(DeleteBehavior.NoAction);
        builder.HasOne<Customer>().WithMany().HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.NoAction);
        builder.HasOne<Employee>().WithMany().HasForeignKey(x => x.SalesForceId).OnDelete(DeleteBehavior.NoAction);
        builder.HasOne<SalesQuotation>().WithMany().HasForeignKey(x => x.ReferenceId).OnDelete(DeleteBehavior.NoAction);
        builder.HasOne<LookupValue>().WithMany().HasForeignKey(x => x.QuotationTypeId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<LookupValue>().WithMany().HasForeignKey(x => x.ShippingAddressId).OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(x => x.Lines).WithOne().HasForeignKey(x => x.QuotationId).OnDelete(DeleteBehavior.Cascade);

        builder.ColumnOrder(
        [
            nameof(SalesQuotation.Id), nameof(SalesQuotation.Code), nameof(SalesQuotation.QuotationTypeId),
            nameof(SalesQuotation.Version), nameof(SalesQuotation.VersionDate), nameof(SalesQuotation.OfficeId),
            nameof(SalesQuotation.CustomerId), nameof(SalesQuotation.CustomerName), nameof(SalesQuotation.QuotationDate),
            nameof(SalesQuotation.ValidityDate), nameof(SalesQuotation.SalesForceId), nameof(SalesQuotation.SalesForceName),
            nameof(SalesQuotation.ReferenceId), nameof(SalesQuotation.Reference), nameof(SalesQuotation.BillingContactPerson),
            nameof(SalesQuotation.BillingPhoneNo), nameof(SalesQuotation.BillingAddress), nameof(SalesQuotation.ShippingContactPerson),
            nameof(SalesQuotation.ShippingAddressId), nameof(SalesQuotation.ShippingAddressName), nameof(SalesQuotation.IsVatInclusive),
            nameof(SalesQuotation.IsGrossDiscount), nameof(SalesQuotation.DiscountPercentage), nameof(SalesQuotation.DiscountAmount),
            nameof(SalesQuotation.DeliveryCharge), nameof(SalesQuotation.ExtraCharge), nameof(SalesQuotation.TotalAmount),
            nameof(SalesQuotation.Subjects), nameof(SalesQuotation.BodyNarration), nameof(SalesQuotation.FooterNarration),
            nameof(SalesQuotation.Narration), nameof(SalesQuotation.Status), nameof(SalesQuotation.ReviewedAt),
            nameof(SalesQuotation.ReviewedById), nameof(SalesQuotation.ReviewComment), nameof(SalesQuotation.IsExpired),
            nameof(SalesQuotation.IsOrdered), nameof(SalesQuotation.OrderStatus), nameof(SalesQuotation.OrderStatusName),
            nameof(SalesQuotation.PublicId), nameof(SalesQuotation.AccountId), nameof(SalesQuotation.BusinessUnitId),
            .. MasterRecordMapping.AuditColumns,
            nameof(SalesQuotation.CustomData),
        ]);
    }
}

internal sealed class SalesQuotationLineConfiguration : IEntityTypeConfiguration<SalesQuotationLine>
{
    public void Configure(EntityTypeBuilder<SalesQuotationLine> builder)
    {
        builder.ToTable("SalesQuotationRow");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("SalesQuotationRowID").UseIdentityColumn();
        builder.Property(x => x.QuotationId).HasColumnName("SalesQuotationHeaderID");
        builder.Property(x => x.Sequence).HasColumnName("ViewSequenceID");
        builder.Property(x => x.ItemId).HasColumnName("ItemID");
        builder.Property(x => x.ItemCode).HasMaxLength(100);
        builder.Property(x => x.ItemName).HasMaxLength(1000);
        builder.Property(x => x.UomId).HasColumnName("UomID");
        builder.Property(x => x.UomName).HasMaxLength(100);
        builder.Property(x => x.Quantity).HasPrecision(18, 6);
        builder.Property(x => x.Price).HasPrecision(18, 6);
        builder.Property(x => x.TotalAmount).HasPrecision(18, 6);
        builder.Property(x => x.DiscountPercentage).HasPrecision(18, 6);
        builder.Property(x => x.DiscountAmount).HasPrecision(18, 6);
        builder.Property(x => x.IsDiscountPerQuantity).HasColumnName("isDiscountPerQuantity");
        builder.Property(x => x.SdPercentage).HasColumnName("SDPercentage").HasPrecision(18, 6);
        builder.Property(x => x.SdAmount).HasColumnName("SDAmount").HasPrecision(18, 6);
        builder.Property(x => x.VatPercentage).HasColumnName("VATPercentage").HasPrecision(18, 6);
        builder.Property(x => x.VatAmount).HasColumnName("VATAmount").HasPrecision(18, 6);
        builder.Property(x => x.AitPercentage).HasPrecision(18, 4);
        builder.Property(x => x.AitAmount).HasPrecision(18, 4);
        builder.Property(x => x.TotalValue).HasPrecision(18, 6);
        builder.Property(x => x.DeliveryDate).HasMaxLength(300);
        builder.Property(x => x.ApproveQuantity).HasPrecision(18, 6);
        builder.Property(x => x.ApprovePrice).HasPrecision(18, 6);
        builder.Property(x => x.ApproveAmount).HasPrecision(18, 6);
        builder.Property(x => x.IsActive).HasColumnName("isActive");

        builder.HasOne(x => x.Item).WithMany().HasForeignKey(x => x.ItemId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Uom).WithMany().HasForeignKey(x => x.UomId).OnDelete(DeleteBehavior.Restrict);
    }
}
