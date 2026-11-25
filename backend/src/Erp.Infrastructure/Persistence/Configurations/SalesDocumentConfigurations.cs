using System.Reflection;
using Erp.Core.Domain.Accounting;
using Erp.Core.Domain.Identity;
using Erp.Core.Domain.Metadata;
using Erp.Core.Domain.Partners;
using Erp.Core.Domain.Sales;
using Erp.Core.Domain.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Erp.Infrastructure.Persistence.Configurations;

internal static class SalesMapping
{
    private const int TextLength = 500;

    public static void MapSalesDocument<T>(this EntityTypeBuilder<T> builder, string table, params string[] lookups) where T : SalesDocument
    {
        builder.MapColumns();
        builder.MapRecord(table, $"{table}ID");
        builder.Property(x => x.Code).HasColumnName($"{table}Code").HasMaxLength(50);
        builder.Property(x => x.Remarks).HasMaxLength(2000);
        builder.Property(x => x.PriceTypeName).HasColumnName("PriceType").HasMaxLength(250);
        builder.Property(x => x.CustomerPo).HasColumnName("CustomerPO").HasMaxLength(100);
        builder.Property(x => x.BankAccountId).HasColumnName("BankAccID");
        builder.Property(x => x.BankAccountName).HasColumnName("BankAccName");
        builder.Property(x => x.IsVatInclusive).HasColumnName("isInclusive");
        builder.Property(x => x.OtherChargeAccountId).HasColumnName("ChartOfAccIDOtherCharge");
        builder.Property(x => x.ExtraChargeAccountId).HasColumnName("ChartOfAccIDExtraOtherCharge");
        builder.Property(x => x.DiscountItemTotal).HasColumnName("DiscoundItemTotalPrice");
        builder.Property(x => x.TotalAit).HasColumnName("TotalAITAmount");
        builder.Property(x => x.ReviewedAt).HasColumnName("ApproveDatetime").HasColumnType("datetime").HasConversion(MasterRecordMapping.Utc);
        builder.Property(x => x.ReviewedById).HasColumnName("ApproveByID");
        builder.Property(x => x.ReviewComment).HasColumnName("RejectReason");
        builder.Property(x => x.PublicId).HasColumnName($"{table}GuID");
        builder.Property(x => x.CustomData).HasColumnType("nvarchar(max)");

        builder.HasIndex(x => new { x.BusinessUnitId, x.Code }).IsUnique();
        builder.HasIndex(x => new { x.BusinessUnitId, x.IsActive, x.Status, x.OrderDate });

        builder.HasOne<BusinessUnit>().WithMany().HasForeignKey(x => x.BusinessUnitId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Customer>().WithMany().HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.NoAction);
        builder.HasOne<Customer>().WithMany().HasForeignKey(x => x.ReferredByPartnerId).OnDelete(DeleteBehavior.NoAction);
        builder.HasOne<Office>().WithMany().HasForeignKey(x => x.OfficeId).OnDelete(DeleteBehavior.NoAction);
        builder.HasOne<Warehouse>().WithMany().HasForeignKey(x => x.WarehouseId).OnDelete(DeleteBehavior.NoAction);
        builder.HasOne<Employee>().WithMany().HasForeignKey(x => x.SalesForceId).OnDelete(DeleteBehavior.NoAction);
        builder.HasOne<BankAccount>().WithMany().HasForeignKey(x => x.BankAccountId).OnDelete(DeleteBehavior.NoAction);
        builder.HasOne<ChartOfAccount>().WithMany().HasForeignKey(x => x.OtherChargeAccountId).OnDelete(DeleteBehavior.NoAction);
        builder.HasOne<ChartOfAccount>().WithMany().HasForeignKey(x => x.ExtraChargeAccountId).OnDelete(DeleteBehavior.NoAction);
        foreach (var lookup in (string[])
            [
                nameof(SalesDocument.PriceTypeId), nameof(SalesDocument.PaymentTypeId), nameof(SalesDocument.PaymentMethodId),
                nameof(SalesDocument.ShippingAddressId), nameof(SalesDocument.FreeTypeId), nameof(SalesDocument.CurrencyId), .. lookups,
            ])
        {
            builder.HasOne(typeof(LookupValue)).WithMany().HasForeignKey(lookup).OnDelete(DeleteBehavior.Restrict);
        }

        builder.ColumnOrder([nameof(SalesDocument.Id), nameof(SalesDocument.Code)]);
    }

    public static void MapSalesLine<T>(this EntityTypeBuilder<T> builder, string table) where T : SalesDocumentLine
    {
        builder.ToTable(table);
        builder.HasKey(x => x.Id);
        builder.MapColumns();
        builder.Property(x => x.Id).UseIdentityColumn();
        builder.Property(x => x.Sequence).HasColumnName("ViewSequenceID");
        builder.Property(x => x.ItemName).HasMaxLength(1000);
        builder.Property(x => x.ItemDescription).HasMaxLength(2000);
        builder.Property(x => x.UomName).HasMaxLength(100);
        builder.Property(x => x.DiscountAmount).HasColumnName("LineDiscount");
        builder.Property(x => x.TotalValue).HasColumnName("NetAmount");
        builder.Property(x => x.OfferItemQuantity).HasColumnName("OfferItemQty");
        builder.Property(x => x.PiNo).HasColumnName("PINo").HasMaxLength(150);
        builder.Property(x => x.BdPortQuantity).HasColumnName("numBdPortQty");
        builder.Property(x => x.ForeignPortQuantity).HasColumnName("numForeignPortQty");
        builder.Property(x => x.ShelfLife).HasColumnName("strShelfLife").HasMaxLength(150);

        builder.HasOne(x => x.Item).WithMany().HasForeignKey(x => x.ItemId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Uom).WithMany().HasForeignKey(x => x.UomId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(typeof(LookupValue)).WithMany().HasForeignKey(nameof(SalesDocumentLine.FreeTypeId)).OnDelete(DeleteBehavior.Restrict);
        builder.ColumnOrder([nameof(SalesLine.Id)]);
    }

    private static void MapColumns<T>(this EntityTypeBuilder<T> builder) where T : class
    {
        foreach (var property in typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            var type = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;
            if (type == typeof(decimal)) builder.Property(property.Name).HasPrecision(18, 6);
            else if (type == typeof(string)) builder.Property(property.Name).HasMaxLength(TextLength);
            else if (property.Name.Length > 2 && property.Name.EndsWith("Id", StringComparison.Ordinal))
                builder.Property(property.Name).HasColumnName($"{property.Name[..^2]}ID");
        }
    }
}

internal sealed class SalesOrderConfiguration : IEntityTypeConfiguration<SalesOrder>
{
    public void Configure(EntityTypeBuilder<SalesOrder> builder)
    {
        builder.MapSalesDocument("CustomerOrder",
            nameof(SalesOrder.SalesTermId), nameof(SalesOrder.LcTypeId), nameof(SalesOrder.PaymentTermsId));
        builder.Property(x => x.AdvanceRemainingAmount).HasComputedColumnSql("[AdvanceAmount] - [AdjustedAmount]");
        builder.Property(x => x.WalletCommission).HasColumnName("WalletCommision");
        builder.Property(x => x.CustomerPoAmount).HasColumnName("POAmount");
        builder.Property(x => x.DmsInvoice).HasColumnName("DMSInvoise");
        builder.Property(x => x.IsForeign).HasColumnName("IsForeigne");

        builder.HasOne<SalesQuotation>().WithMany().HasForeignKey(x => x.SalesQuotationId).OnDelete(DeleteBehavior.NoAction);
        builder.HasMany(x => x.Lines).WithOne().HasForeignKey(x => x.OrderId).OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class SalesOrderLineConfiguration : IEntityTypeConfiguration<SalesOrderLine>
{
    public void Configure(EntityTypeBuilder<SalesOrderLine> builder)
    {
        builder.MapSalesLine("CustomerOrderLine");
        builder.Property(x => x.OrderId).HasColumnName("CustomerOrderID");
        builder.Property(x => x.ScheduledQuantity).HasColumnName("ScheduledQty");
        builder.Property(x => x.LineDiscountRate)
            .HasComputedColumnSql("CONVERT([decimal](18,2), CASE WHEN [Quantity] = 0 THEN 0 ELSE [LineDiscount] / [Quantity] END)");
        builder.Property(x => x.SellingItemQuantityForOffer).HasColumnName("SellingItemQtyForOffer");
        builder.Property(x => x.FobRate).HasColumnName("FOBRate");
        builder.Property(x => x.VatAmount).HasColumnName("Vat");
        builder.Property(x => x.SdAmount).HasColumnName("Sd");
    }
}

internal sealed class SalesDeliveryConfiguration : IEntityTypeConfiguration<SalesDelivery>
{
    public void Configure(EntityTypeBuilder<SalesDelivery> builder)
    {
        builder.MapSalesDocument("SalesDelivery", nameof(SalesDelivery.DeliveryMethodId), nameof(SalesDelivery.InstallmentTypeId));
        builder.Property(x => x.Type).HasColumnName("DeliveryTypeID");
        builder.Property(x => x.OrderId).HasColumnName("CustomerOrderID");
        builder.Property(x => x.OrderCode).HasColumnName("CustomerOrderCode").HasMaxLength(50);
        builder.Property(x => x.Narration).HasColumnType("nvarchar(max)");
        builder.Property(x => x.CommissionPercentage).HasColumnName("ComissionPercentage");
        builder.Property(x => x.InstallmentTypeName).HasColumnName("InstallmentType");
        builder.Property(x => x.ConfirmedDate).HasColumnType("datetime").HasConversion(MasterRecordMapping.Utc);
        builder.Property(x => x.GrnNo).HasColumnName("strGRNNo");
        builder.Property(x => x.GrnDate).HasColumnName("dteGRNDate");
        builder.Property(x => x.GrnAmount).HasColumnName("numGRNAmount");
        builder.Property(x => x.GrnCollectionDate).HasColumnName("GRNCollectionDate");
        builder.Property(x => x.CollectedById).HasColumnName("intCollectedByID");
        builder.Property(x => x.CollectedByName).HasColumnName("strCollectedByName");
        builder.Property(x => x.Attachment).HasColumnName("strSalesDeliveryAttatchment");
        builder.Property(x => x.OutletId).HasColumnName("intOutletID");
        builder.Property(x => x.OutletName).HasColumnName("strOutletName");
        builder.Property(x => x.InvoicePdfCount).HasColumnName("InvoicePDFCount");

        builder.HasOne<SalesOrder>().WithMany().HasForeignKey(x => x.OrderId).OnDelete(DeleteBehavior.NoAction);
        builder.HasOne<Employee>().WithMany().HasForeignKey(x => x.CourierOrEmployeeId).OnDelete(DeleteBehavior.NoAction);
        builder.HasOne<Employee>().WithMany().HasForeignKey(x => x.CollectedById).OnDelete(DeleteBehavior.NoAction);
        builder.HasMany(x => x.Lines).WithOne().HasForeignKey(x => x.DeliveryId).OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class SalesDeliveryLineConfiguration : IEntityTypeConfiguration<SalesDeliveryLine>
{
    public void Configure(EntityTypeBuilder<SalesDeliveryLine> builder)
    {
        builder.MapSalesLine("SalesDeliveryLine");
        builder.Property(x => x.DeliveryId).HasColumnName("SalesDeliveryID");
        builder.Property(x => x.OrderId).HasColumnName("intCustomerOrderID");
        builder.Property(x => x.OrderCode).HasColumnName("strCustomerOrderCode").HasMaxLength(50);
        builder.Property(x => x.OrderLineId).HasColumnName("intCustomerOrderLineID");
        builder.Property(x => x.PackQuantity).HasColumnName("PackQty").HasMaxLength(50);
        builder.Property(x => x.ConfirmationReturnQuantity).HasColumnName("ConfirmationReturnQty");
        builder.Property(x => x.ExchangeQuantity).HasColumnName("ExchangeQty");
        builder.Property(x => x.RejectReason).HasColumnName("strRejectReason").HasMaxLength(1000);
        builder.Property(x => x.OriginalDeliveryLineId).HasColumnName("intOriginalDeliveryLineID");
        builder.Property(x => x.ItemSerial).HasColumnType("nvarchar(max)");
        builder.Property(x => x.Batch).HasColumnType("nvarchar(max)");

        builder.HasOne<SalesOrderLine>().WithMany().HasForeignKey(x => x.OrderLineId).OnDelete(DeleteBehavior.NoAction);
        builder.HasOne<Warehouse>().WithMany().HasForeignKey(x => x.WarehouseId).OnDelete(DeleteBehavior.NoAction);
    }
}
