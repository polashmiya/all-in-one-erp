using Erp.Core.Domain.Locations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Erp.Infrastructure.Persistence.Configurations;

internal sealed class DistrictConfiguration : IEntityTypeConfiguration<District>
{
    public void Configure(EntityTypeBuilder<District> builder)
    {
        builder.ToTable("District");
        builder.Property(x => x.Id).HasColumnName("DistrictID").ValueGeneratedNever();
        builder.Property(x => x.Name).HasColumnName("DistrictName").HasMaxLength(100);
        builder.Property(x => x.NameBn).HasColumnName("DistrictNameBn").HasMaxLength(100);
        builder.HasIndex(x => x.Name).IsUnique();
    }
}

internal sealed class ThanaConfiguration : IEntityTypeConfiguration<Thana>
{
    public void Configure(EntityTypeBuilder<Thana> builder)
    {
        builder.ToTable("Thana");
        builder.Property(x => x.Id).HasColumnName("ThanaID").ValueGeneratedNever();
        builder.Property(x => x.DistrictId).HasColumnName("DistrictID");
        builder.Property(x => x.Name).HasColumnName("ThanaName").HasMaxLength(100);
        builder.Property(x => x.NameBn).HasColumnName("ThanaNameBn").HasMaxLength(100);
        builder.HasIndex(x => new { x.DistrictId, x.Name }).IsUnique();

        builder.HasOne<District>().WithMany().HasForeignKey(x => x.DistrictId).OnDelete(DeleteBehavior.Restrict);
    }
}
