using Erp.Core.Domain.Navigation;
using Erp.Core.Domain.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Erp.Infrastructure.Persistence.Configurations;

internal sealed class PanelMenuItemConfiguration : IEntityTypeConfiguration<PanelMenuItem>
{
    public void Configure(EntityTypeBuilder<PanelMenuItem> builder)
    {
        builder.ToTable("PanelMenuItems");
        builder.Property(m => m.Key).HasMaxLength(100);
        builder.Property(m => m.LabelEn).HasMaxLength(100);
        builder.Property(m => m.LabelBn).HasMaxLength(100);
        builder.Property(m => m.Icon).HasMaxLength(50);
        builder.Property(m => m.Path).HasMaxLength(200);
        builder.HasIndex(m => m.Key).IsUnique();

        builder.HasOne(m => m.Parent)
            .WithMany(m => m.Children)
            .HasForeignKey(m => m.ParentId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class ErpModuleConfiguration : IEntityTypeConfiguration<ErpModule>
{
    public void Configure(EntityTypeBuilder<ErpModule> builder)
    {
        builder.ToTable("ErpModules");
        builder.Property(m => m.Key).HasMaxLength(50);
        builder.Property(m => m.NameEn).HasMaxLength(100);
        builder.Property(m => m.NameBn).HasMaxLength(100);
        builder.Property(m => m.Icon).HasMaxLength(50);
        builder.HasIndex(m => m.Key).IsUnique();
    }
}

internal sealed class ErpSubModuleConfiguration : IEntityTypeConfiguration<ErpSubModule>
{
    public void Configure(EntityTypeBuilder<ErpSubModule> builder)
    {
        builder.ToTable("ErpSubModules");
        builder.Property(s => s.Key).HasMaxLength(50);
        builder.Property(s => s.NameEn).HasMaxLength(100);
        builder.Property(s => s.NameBn).HasMaxLength(100);
        builder.Property(s => s.Icon).HasMaxLength(50);
        builder.HasIndex(s => new { s.ModuleId, s.Key }).IsUnique();

        builder.HasOne(s => s.Module).WithMany(m => m.SubModules).HasForeignKey(s => s.ModuleId).OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class ErpFeatureConfiguration : IEntityTypeConfiguration<ErpFeature>
{
    public void Configure(EntityTypeBuilder<ErpFeature> builder)
    {
        builder.ToTable("ErpFeatures");
        builder.Property(f => f.Key).HasMaxLength(50);
        builder.Property(f => f.Code).HasMaxLength(101);
        builder.Property(f => f.NameEn).HasMaxLength(100);
        builder.Property(f => f.NameBn).HasMaxLength(100);
        builder.HasIndex(f => f.Code).IsUnique();

        builder.HasOne(f => f.SubModule).WithMany(s => s.Features).HasForeignKey(f => f.SubModuleId).OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class BusinessUnitFeatureConfiguration : IEntityTypeConfiguration<BusinessUnitFeature>
{
    public void Configure(EntityTypeBuilder<BusinessUnitFeature> builder)
    {
        builder.ToTable("BusinessUnitFeatures");
        builder.HasIndex(x => new { x.BusinessUnitId, x.FeatureId }).IsUnique();

        builder.HasOne<Account>().WithMany().HasForeignKey(x => x.AccountId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<BusinessUnit>().WithMany().HasForeignKey(x => x.BusinessUnitId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.Feature).WithMany().HasForeignKey(x => x.FeatureId).OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class AccountFeatureConfiguration : IEntityTypeConfiguration<AccountFeature>
{
    public void Configure(EntityTypeBuilder<AccountFeature> builder)
    {
        builder.ToTable("AccountFeatures");
        builder.HasIndex(x => new { x.AccountId, x.FeatureId }).IsUnique();

        builder.HasOne<Account>().WithMany().HasForeignKey(x => x.AccountId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.Feature).WithMany().HasForeignKey(x => x.FeatureId).OnDelete(DeleteBehavior.Cascade);
    }
}
