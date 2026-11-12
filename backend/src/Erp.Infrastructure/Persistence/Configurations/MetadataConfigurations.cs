using Erp.Core.Domain.Metadata;
using Erp.Core.Domain.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Erp.Infrastructure.Persistence.Configurations;

internal sealed class FeatureSectionConfiguration : IEntityTypeConfiguration<FeatureSection>
{
    public void Configure(EntityTypeBuilder<FeatureSection> builder)
    {
        builder.ToTable("FeatureSections");
        builder.Property(s => s.Key).HasMaxLength(50);
        builder.Property(s => s.LabelEn).HasMaxLength(100);
        builder.Property(s => s.LabelBn).HasMaxLength(100);
        builder.HasIndex(s => new { s.FeatureId, s.Key }).IsUnique();

        builder.HasOne(s => s.Feature).WithMany().HasForeignKey(s => s.FeatureId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(s => s.Parent).WithMany().HasForeignKey(s => s.ParentId).OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class FeatureFieldConfiguration : IEntityTypeConfiguration<FeatureField>
{
    public void Configure(EntityTypeBuilder<FeatureField> builder)
    {
        builder.ToTable("FeatureFields");
        builder.Property(f => f.Key).HasMaxLength(50);
        builder.Property(f => f.DataType).HasMaxLength(20);
        builder.Property(f => f.LookupSource).HasMaxLength(50);
        builder.Property(f => f.ParentFieldKey).HasMaxLength(50);
        builder.Property(f => f.LabelEn).HasMaxLength(100);
        builder.Property(f => f.LabelBn).HasMaxLength(100);
        builder.Property(f => f.HelpEn).HasMaxLength(300);
        builder.Property(f => f.HelpBn).HasMaxLength(300);
        builder.Property(f => f.PlaceholderEn).HasMaxLength(100);
        builder.Property(f => f.PlaceholderBn).HasMaxLength(100);
        builder.Property(f => f.DefaultValue).HasMaxLength(200);
        builder.Property(f => f.MinValue).HasPrecision(18, 4);
        builder.Property(f => f.MaxValue).HasPrecision(18, 4);
        builder.HasIndex(f => new { f.FeatureId, f.AccountId, f.Key }).IsUnique().HasFilter(null);

        builder.HasOne(f => f.Feature).WithMany().HasForeignKey(f => f.FeatureId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(f => f.Section).WithMany().HasForeignKey(f => f.SectionId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Account>().WithMany().HasForeignKey(f => f.AccountId).OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class AccountSectionSettingConfiguration : IEntityTypeConfiguration<AccountSectionSetting>
{
    public void Configure(EntityTypeBuilder<AccountSectionSetting> builder)
    {
        builder.ToTable("AccountSectionSettings");
        builder.Property(s => s.LabelEn).HasMaxLength(100);
        builder.Property(s => s.LabelBn).HasMaxLength(100);
        builder.HasIndex(s => new { s.AccountId, s.BusinessUnitId, s.SectionId }).IsUnique().HasFilter(null);

        builder.HasOne<Account>().WithMany().HasForeignKey(s => s.AccountId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<BusinessUnit>().WithMany().HasForeignKey(s => s.BusinessUnitId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(s => s.Section).WithMany().HasForeignKey(s => s.SectionId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<FeatureSection>().WithMany().HasForeignKey(s => s.ParentId).OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class AccountFieldSettingConfiguration : IEntityTypeConfiguration<AccountFieldSetting>
{
    public void Configure(EntityTypeBuilder<AccountFieldSetting> builder)
    {
        builder.ToTable("AccountFieldSettings");
        builder.Property(s => s.LabelEn).HasMaxLength(100);
        builder.Property(s => s.LabelBn).HasMaxLength(100);
        builder.Property(s => s.HelpEn).HasMaxLength(300);
        builder.Property(s => s.HelpBn).HasMaxLength(300);
        builder.Property(s => s.PlaceholderEn).HasMaxLength(100);
        builder.Property(s => s.PlaceholderBn).HasMaxLength(100);
        builder.Property(s => s.DefaultValue).HasMaxLength(200);
        builder.Property(s => s.MinValue).HasPrecision(18, 4);
        builder.Property(s => s.MaxValue).HasPrecision(18, 4);
        builder.HasIndex(s => new { s.AccountId, s.BusinessUnitId, s.FieldId }).IsUnique().HasFilter(null);

        builder.HasOne<Account>().WithMany().HasForeignKey(s => s.AccountId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<BusinessUnit>().WithMany().HasForeignKey(s => s.BusinessUnitId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(s => s.Field).WithMany().HasForeignKey(s => s.FieldId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<FeatureSection>().WithMany().HasForeignKey(s => s.SectionId).OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class LookupTypeConfiguration : IEntityTypeConfiguration<LookupType>
{
    public void Configure(EntityTypeBuilder<LookupType> builder)
    {
        builder.ToTable("LookupTypes");
        builder.Property(t => t.Key).HasMaxLength(50);
        builder.Property(t => t.NameEn).HasMaxLength(100);
        builder.Property(t => t.NameBn).HasMaxLength(100);
        builder.HasIndex(t => t.Key).IsUnique();
    }
}

internal sealed class LookupValueConfiguration : IEntityTypeConfiguration<LookupValue>
{
    public void Configure(EntityTypeBuilder<LookupValue> builder)
    {
        builder.MapRecord("LookupValues", "LookupValueID");
        builder.Property(v => v.LookupTypeId).HasColumnName("LookupTypeID");
        builder.Property(v => v.Code).HasMaxLength(50);
        builder.Property(v => v.NameEn).HasMaxLength(200);
        builder.Property(v => v.NameBn).HasMaxLength(200);
        builder.HasIndex(v => new { v.AccountId, v.LookupTypeId, v.Code }).IsUnique();

        builder.HasOne(v => v.LookupType).WithMany().HasForeignKey(v => v.LookupTypeId).OnDelete(DeleteBehavior.Restrict);
    }
}
