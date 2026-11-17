using Erp.Core.Domain.Identity;
using Erp.Core.Domain.Locations;
using Erp.Core.Domain.Partners;
using Erp.Core.Domain.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Erp.Infrastructure.Persistence.Configurations;

internal static class PartnerMapping
{
    public static void MapPartner<T>(this EntityTypeBuilder<T> builder, string table, params string[] own) where T : PartnerRecord
    {
        builder.ColumnOrder(
        [
            nameof(PartnerRecord.Id), nameof(PartnerRecord.Code), nameof(PartnerRecord.Name), nameof(PartnerRecord.BillingName),
            nameof(PartnerRecord.GroupId), nameof(PartnerRecord.Mobile), nameof(PartnerRecord.Email), nameof(PartnerRecord.ContactPerson),
            nameof(PartnerRecord.ContactMobile), nameof(PartnerRecord.Address), nameof(PartnerRecord.City), nameof(PartnerRecord.DistrictId),
            nameof(PartnerRecord.ThanaId), nameof(PartnerRecord.IsForeign), nameof(PartnerRecord.CreditLimit), nameof(PartnerRecord.PaymentDays),
            .. own,
            nameof(PartnerRecord.BinNumber), nameof(PartnerRecord.TinNumber), nameof(PartnerRecord.TradeLicenseNumber), nameof(PartnerRecord.Nid),
            nameof(PartnerRecord.CentralVatRegistrationNo), nameof(PartnerRecord.IsVatNotApplicable), nameof(PartnerRecord.IsVdsApplicable),
            nameof(PartnerRecord.Remarks), nameof(PartnerRecord.AccountId), nameof(PartnerRecord.BusinessUnitId),
            .. MasterRecordMapping.AuditColumns,
            nameof(PartnerRecord.CustomData),
        ]);

        builder.MapRecord(table, $"{table}ID");
        builder.Property(x => x.BusinessUnitId).HasColumnName("BusinessUnitID");
        builder.Property(x => x.Code).HasColumnName($"{table}Code").HasMaxLength(50);
        builder.Property(x => x.Name).HasColumnName($"{table}Name").HasMaxLength(500);
        builder.Property(x => x.BillingName).HasColumnName($"{table}BillingName").HasMaxLength(500);
        builder.Property(x => x.GroupId).HasColumnName($"{table}GroupID");
        builder.Property(x => x.Mobile).HasColumnName("MobileNo").HasMaxLength(50);
        builder.Property(x => x.Email).HasMaxLength(100);
        builder.Property(x => x.ContactPerson).HasColumnName("OtherContactName").HasMaxLength(200);
        builder.Property(x => x.ContactMobile).HasColumnName("OtherContactNumber").HasMaxLength(50);
        builder.Property(x => x.Address).HasMaxLength(300);
        builder.Property(x => x.City).HasMaxLength(100);
        builder.Property(x => x.DistrictId).HasColumnName("DistrictID");
        builder.Property(x => x.ThanaId).HasColumnName("ThanaID");
        builder.Property(x => x.CreditLimit).HasPrecision(18, 4);
        builder.Property(x => x.BinNumber).HasMaxLength(250);
        builder.Property(x => x.TinNumber).HasColumnName("TINNumber").HasMaxLength(150);
        builder.Property(x => x.TradeLicenseNumber).HasMaxLength(150);
        builder.Property(x => x.Nid).HasColumnName("NID").HasMaxLength(100);
        builder.Property(x => x.CentralVatRegistrationNo).HasColumnName("CentralVATRegistrationNo").HasMaxLength(200);
        builder.Property(x => x.IsVatNotApplicable).HasColumnName("IsVATNotApplicable");
        builder.Property(x => x.IsVdsApplicable).HasColumnName("isVDSApplicable");
        builder.HasIndex(x => new { x.BusinessUnitId, x.Code }).IsUnique();

        builder.HasOne<BusinessUnit>().WithMany().HasForeignKey(x => x.BusinessUnitId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<District>().WithMany().HasForeignKey(x => x.DistrictId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Thana>().WithMany().HasForeignKey(x => x.ThanaId).OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder.MapPartner("Customer",
            nameof(Customer.PriceTypeId), nameof(Customer.TerritoryId), nameof(Customer.SalesRepresentativeId),
            nameof(Customer.DateOfBirth), nameof(Customer.GenderId), nameof(Customer.Profession), nameof(Customer.MarriageDate));
        builder.Property(x => x.PriceTypeId).HasColumnName("PriceTypeID");
        builder.Property(x => x.TerritoryId).HasColumnName("TerritoryID");
        builder.Property(x => x.SalesRepresentativeId).HasColumnName("SalesRepresentativeID");
        builder.Property(x => x.GenderId).HasColumnName("GenderID");
        builder.Property(x => x.Profession).HasMaxLength(200);

        builder.HasOne<Employee>().WithMany().HasForeignKey(x => x.SalesRepresentativeId).OnDelete(DeleteBehavior.NoAction);
    }
}

internal sealed class SupplierConfiguration : IEntityTypeConfiguration<Supplier>
{
    public void Configure(EntityTypeBuilder<Supplier> builder)
    {
        builder.MapPartner("Supplier", nameof(Supplier.Tolerance));
        builder.Property(x => x.Tolerance).HasPrecision(18, 6);
    }
}
