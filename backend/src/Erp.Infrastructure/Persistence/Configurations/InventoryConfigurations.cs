using Erp.Core.Domain;
using Erp.Core.Domain.Inventory;
using Erp.Core.Domain.Metadata;
using Erp.Core.Domain.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Erp.Infrastructure.Persistence.Configurations;

internal static class MasterRecordMapping
{
    private static readonly ValueConverter<DateTime, DateTime> Utc =
        new(v => v, v => DateTime.SpecifyKind(v, DateTimeKind.Utc));

    public static void MapRecord<T>(this EntityTypeBuilder<T> builder, string table, string idColumn) where T : MasterRecord
    {
        builder.ToTable(table);
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName(idColumn).UseIdentityColumn();
        builder.Property(x => x.AccountId).HasColumnName("AccountID");
        builder.Property(x => x.IsActive).HasColumnName("isActive");
        builder.Property(x => x.TransactionDate).HasColumnType("datetime").HasConversion(Utc);
        builder.Property(x => x.CreatedAt).HasColumnType("datetime").HasConversion(Utc);
        builder.Property(x => x.CreatedById).HasColumnName("CreatedByID");
        builder.Property(x => x.ModifiedById).HasColumnName("ModifiedByID");
        builder.Property(x => x.ModifiedAt).HasColumnType("datetime").HasConversion(Utc);

        builder.HasOne<Account>().WithMany().HasForeignKey(x => x.AccountId).OnDelete(DeleteBehavior.Restrict);
    }

    public static void MapClassification<T>(this EntityTypeBuilder<T> builder, string table, string idColumn, string nameColumn)
        where T : ItemClassification
    {
        builder.MapRecord(table, idColumn);
        builder.Property(x => x.Name).HasColumnName(nameColumn).HasMaxLength(100);
        builder.Property(x => x.BusinessUnitId).HasColumnName("BusinessUnitID");
        builder.Property(x => x.ImageSource);

        builder.HasOne<BusinessUnit>().WithMany().HasForeignKey(x => x.BusinessUnitId).OnDelete(DeleteBehavior.Restrict);
    }

    public static void ColumnOrder<T>(this EntityTypeBuilder<T> builder, params string[] properties) where T : class
    {
        for (var i = 0; i < properties.Length; i++) builder.Property(properties[i]).HasColumnOrder(i);
    }

    public static readonly string[] AuditColumns =
    [
        nameof(MasterRecord.IsActive),
        nameof(MasterRecord.TransactionDate),
        nameof(MasterRecord.CreatedAt),
        nameof(MasterRecord.CreatedById),
        nameof(MasterRecord.ModifiedById),
        nameof(MasterRecord.ModifiedAt),
    ];
}

internal sealed class ItemDepartmentConfiguration : IEntityTypeConfiguration<ItemDepartment>
{
    public void Configure(EntityTypeBuilder<ItemDepartment> builder)
    {
        builder.MapClassification("ItemDepartment", "DepartmentID", "DepartmentName");
        builder.Property(x => x.Code).HasColumnName("DepartmentCode").HasMaxLength(20);
        builder.HasIndex(x => new { x.BusinessUnitId, x.Code }).IsUnique();
        builder.HasIndex(x => new { x.BusinessUnitId, x.Name }).IsUnique();

        builder.ColumnOrder(
        [
            nameof(ItemDepartment.Id), nameof(ItemDepartment.Name), nameof(ItemDepartment.Code), nameof(ItemDepartment.Serial),
            nameof(ItemDepartment.ImageSource), nameof(ItemDepartment.AccountId), nameof(ItemDepartment.BusinessUnitId),
            .. MasterRecordMapping.AuditColumns,
        ]);
    }
}

internal sealed class ItemCategoryConfiguration : IEntityTypeConfiguration<ItemCategory>
{
    public void Configure(EntityTypeBuilder<ItemCategory> builder)
    {
        builder.MapClassification("ItemCategory", "ItemCategoryID", "ItemCategoryName");
        builder.Property(x => x.Code).HasColumnName("ItemCategoryCode").HasMaxLength(3);
        builder.Property(x => x.DepartmentId).HasColumnName("ItemDepartmentID");
        builder.HasIndex(x => new { x.BusinessUnitId, x.Code }).IsUnique();
        builder.HasIndex(x => new { x.DepartmentId, x.Name }).IsUnique();

        builder.HasOne(x => x.Department).WithMany().HasForeignKey(x => x.DepartmentId).OnDelete(DeleteBehavior.Restrict);

        builder.ColumnOrder(
        [
            nameof(ItemCategory.Id), nameof(ItemCategory.Code), nameof(ItemCategory.Name), nameof(ItemCategory.DepartmentId),
            nameof(ItemCategory.AccountId), nameof(ItemCategory.BusinessUnitId), nameof(ItemCategory.ImageSource),
            .. MasterRecordMapping.AuditColumns,
        ]);
    }
}

internal sealed class ItemSubCategoryConfiguration : IEntityTypeConfiguration<ItemSubCategory>
{
    public void Configure(EntityTypeBuilder<ItemSubCategory> builder)
    {
        builder.MapClassification("ItemSubCategory", "ItemSubCategoryID", "ItemSubCategoryName");
        builder.Property(x => x.DepartmentId).HasColumnName("DepartmentID");
        builder.Property(x => x.CategoryId).HasColumnName("ItemCategoryID");
        builder.HasIndex(x => new { x.CategoryId, x.Name }).IsUnique();

        builder.HasOne(x => x.Department).WithMany().HasForeignKey(x => x.DepartmentId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Category).WithMany().HasForeignKey(x => x.CategoryId).OnDelete(DeleteBehavior.Restrict);

        builder.ColumnOrder(
        [
            nameof(ItemSubCategory.Id), nameof(ItemSubCategory.Name), nameof(ItemSubCategory.DepartmentId),
            nameof(ItemSubCategory.CategoryId), nameof(ItemSubCategory.AccountId), nameof(ItemSubCategory.BusinessUnitId),
            nameof(ItemSubCategory.ImageSource),
            .. MasterRecordMapping.AuditColumns,
        ]);
    }
}

internal sealed class ItemMinorCategoryConfiguration : IEntityTypeConfiguration<ItemMinorCategory>
{
    public void Configure(EntityTypeBuilder<ItemMinorCategory> builder)
    {
        builder.MapClassification("ItemMinorCategory", "ItemMinorCategoryID", "ItemMinorCategoryName");
        builder.Property(x => x.CategoryId).HasColumnName("ItemCategoryID");
        builder.Property(x => x.SubCategoryId).HasColumnName("ItemSubCategoryID");
        builder.HasIndex(x => new { x.SubCategoryId, x.Name }).IsUnique();

        builder.HasOne(x => x.Category).WithMany().HasForeignKey(x => x.CategoryId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.SubCategory).WithMany().HasForeignKey(x => x.SubCategoryId).OnDelete(DeleteBehavior.Restrict);

        builder.ColumnOrder(
        [
            nameof(ItemMinorCategory.Id), nameof(ItemMinorCategory.Name), nameof(ItemMinorCategory.CategoryId),
            nameof(ItemMinorCategory.SubCategoryId), nameof(ItemMinorCategory.AccountId),
            nameof(ItemMinorCategory.BusinessUnitId), nameof(ItemMinorCategory.ImageSource),
            .. MasterRecordMapping.AuditColumns,
        ]);
    }
}

internal sealed class UnitOfMeasureConfiguration : IEntityTypeConfiguration<UnitOfMeasure>
{
    public void Configure(EntityTypeBuilder<UnitOfMeasure> builder)
    {
        builder.MapRecord("UnitOfMeasure", "UomID");
        builder.Property(x => x.Code).HasColumnName("UomCode").HasMaxLength(10);
        builder.Property(x => x.Name).HasColumnName("UomName").HasMaxLength(50);
        builder.Property(x => x.Description).HasColumnName("UomDescription").HasMaxLength(255);
        builder.Property(x => x.RoundingLength).HasColumnName("RoundingLen");
        builder.Property(x => x.IsDecimalAllowed).HasColumnName("isDecimalAllowed");
        builder.Property(x => x.BranchId).HasColumnName("BranchID");
        builder.Property(x => x.IsActive).HasDefaultValue(true).ValueGeneratedNever();
        builder.HasIndex(x => new { x.AccountId, x.Code }).IsUnique();
        builder.HasIndex(x => new { x.AccountId, x.Name }).IsUnique();

        builder.ColumnOrder(
        [
            nameof(UnitOfMeasure.Id), nameof(UnitOfMeasure.Code), nameof(UnitOfMeasure.Name), nameof(UnitOfMeasure.Description),
            nameof(UnitOfMeasure.RoundingLength), nameof(UnitOfMeasure.IsDecimalAllowed), nameof(UnitOfMeasure.AccountId),
            nameof(UnitOfMeasure.BranchId),
            .. MasterRecordMapping.AuditColumns,
        ]);
    }
}

internal sealed class ItemConfiguration : IEntityTypeConfiguration<Item>
{
    public void Configure(EntityTypeBuilder<Item> builder)
    {
        builder.MapRecord("Item", "ItemID");
        builder.Property(x => x.Code).HasColumnName("ItemCode").HasMaxLength(50);
        builder.Property(x => x.BarCode).HasMaxLength(50);
        builder.Property(x => x.HsCode).HasColumnName("HSCode").HasMaxLength(50);
        builder.Property(x => x.Name).HasColumnName("ItemName").HasMaxLength(200);
        builder.Property(x => x.Description).HasColumnName("ItemDescription");
        builder.Property(x => x.ItemTypeId).HasColumnName("ItemTypeID");
        builder.Property(x => x.ItemTypeName).HasMaxLength(200);
        builder.Property(x => x.DepartmentId).HasColumnName("ItemDepartmentID");
        builder.Property(x => x.CategoryId).HasColumnName("ItemCategoryID");
        builder.Property(x => x.SubCategoryId).HasColumnName("ItemSubCategoryID");
        builder.Property(x => x.MinorCategoryId).HasColumnName("ItemMinorCategoryID");
        builder.Property(x => x.SupplierId).HasColumnName("SupplierID");
        builder.Property(x => x.SupplierName).HasMaxLength(100);
        builder.Property(x => x.UomId).HasColumnName("UomID");
        builder.Property(x => x.UomName).HasMaxLength(200);
        builder.Property(x => x.AltUomId).HasColumnName("AltUomID");
        builder.Property(x => x.AltUomName).HasMaxLength(200);
        builder.Property(x => x.AltUomConversionQty).HasPrecision(18, 4);
        builder.Property(x => x.AverageCostPrice).HasPrecision(18, 4);
        builder.Property(x => x.LastCostPrice).HasPrecision(18, 4);
        builder.Property(x => x.SalesPrice).HasPrecision(18, 4);
        builder.Property(x => x.PurchaseSupplyTypeId).HasColumnName("PurchaseSupplyTypeID");
        builder.Property(x => x.PurchaseSupplyTypeName).HasMaxLength(200);
        builder.Property(x => x.PurchaseVatId).HasColumnName("PurchaseVatID");
        builder.Property(x => x.PurchaseVatPercentage).HasPrecision(18, 4);
        builder.Property(x => x.SalesSupplyTypeId).HasColumnName("SalesSupplyTypeID");
        builder.Property(x => x.SalesSupplyTypeName).HasMaxLength(200);
        builder.Property(x => x.SalesVatId).HasColumnName("SalesVatID");
        builder.Property(x => x.SalesVatPercentage).HasPrecision(18, 4);
        builder.Property(x => x.SdPercentage).HasColumnName("SDPercentage").HasPrecision(18, 4);
        builder.Property(x => x.IsVatInclusive).HasColumnName("isVatInclusive");
        builder.Property(x => x.IsVatRebatable).HasColumnName("isVatRebatable");
        builder.Property(x => x.IsSaleable).HasColumnName("isSaleAble");
        builder.Property(x => x.IsPurchase).HasColumnName("isPurchase");
        builder.Property(x => x.IsNegativeSales).HasColumnName("isNegativeSales");
        builder.Property(x => x.IsMultipleSalesPrice).HasColumnName("isMultipleSalesPrice");
        builder.Property(x => x.IsAutoBarcode).HasColumnName("isAutoBarcode");
        builder.Property(x => x.IsChildBarcode).HasColumnName("isChaildBarCode");
        builder.Property(x => x.MasterItemId).HasColumnName("MasterItemID").HasConversion<long?>();
        builder.Property(x => x.BusinessUnitId).HasColumnName("BusinessUnitID");

        builder.HasIndex(x => x.BarCode).HasDatabaseName("IX_Item_BarCode");
        builder.HasIndex(x => new { x.BusinessUnitId, x.Code }).IsUnique();

        builder.HasOne<BusinessUnit>().WithMany().HasForeignKey(x => x.BusinessUnitId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Department).WithMany().HasForeignKey(x => x.DepartmentId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Category).WithMany().HasForeignKey(x => x.CategoryId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.SubCategory).WithMany().HasForeignKey(x => x.SubCategoryId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.MinorCategory).WithMany().HasForeignKey(x => x.MinorCategoryId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Uom).WithMany().HasForeignKey(x => x.UomId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.AltUom).WithMany().HasForeignKey(x => x.AltUomId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<LookupValue>().WithMany().HasForeignKey(x => x.ItemTypeId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<LookupValue>().WithMany().HasForeignKey(x => x.PurchaseSupplyTypeId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<LookupValue>().WithMany().HasForeignKey(x => x.SalesSupplyTypeId).OnDelete(DeleteBehavior.Restrict);

        builder.ColumnOrder(
        [
            nameof(Item.Id), nameof(Item.Code), nameof(Item.BarCode), nameof(Item.HsCode), nameof(Item.Name),
            nameof(Item.Description), nameof(Item.ItemTypeId), nameof(Item.ItemTypeName), nameof(Item.DepartmentId),
            nameof(Item.CategoryId), nameof(Item.SubCategoryId), nameof(Item.MinorCategoryId), nameof(Item.SupplierId),
            nameof(Item.SupplierName), nameof(Item.UomId), nameof(Item.UomName), nameof(Item.AltUomId), nameof(Item.AltUomName),
            nameof(Item.AltUomConversionQty), nameof(Item.IsNoDecimalQty), nameof(Item.AverageCostPrice),
            nameof(Item.LastCostPrice), nameof(Item.SalesPrice), nameof(Item.PurchaseSupplyTypeId),
            nameof(Item.PurchaseSupplyTypeName), nameof(Item.PurchaseVatId), nameof(Item.PurchaseVatPercentage),
            nameof(Item.SalesSupplyTypeId), nameof(Item.SalesSupplyTypeName), nameof(Item.SalesVatId),
            nameof(Item.SalesVatPercentage), nameof(Item.SdPercentage), nameof(Item.IsVatInclusive),
            nameof(Item.IsVatRebatable), nameof(Item.IsSaleable), nameof(Item.IsPurchase), nameof(Item.IsNegativeSales),
            nameof(Item.IsMultipleSalesPrice), nameof(Item.IsAutoBarcode), nameof(Item.IsChildBarcode),
            nameof(Item.MasterItemId), nameof(Item.BarCodeSerial), nameof(Item.ImageSource), nameof(Item.AccountId),
            nameof(Item.BusinessUnitId),
            .. MasterRecordMapping.AuditColumns,
            nameof(Item.CustomData),
        ]);
    }
}
