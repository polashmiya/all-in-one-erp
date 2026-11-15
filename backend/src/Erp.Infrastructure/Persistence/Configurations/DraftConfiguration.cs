using Erp.Core.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Erp.Infrastructure.Persistence.Configurations;

internal sealed class DraftConfiguration : IEntityTypeConfiguration<Draft>
{
    public void Configure(EntityTypeBuilder<Draft> builder)
    {
        builder.ToTable("Drafts");
        builder.Property(d => d.Surface).HasMaxLength(16);
        builder.Property(d => d.Feature).HasMaxLength(150);
        builder.Property(d => d.Title).HasMaxLength(200);
        builder.HasIndex(d => new { d.Surface, d.AccountId, d.BusinessUnitId, d.Feature });
    }
}
