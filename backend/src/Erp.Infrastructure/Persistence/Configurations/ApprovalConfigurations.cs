using Erp.Core.Domain.Approvals;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Erp.Infrastructure.Persistence.Configurations;

internal sealed class ApprovalLevelConfiguration : IEntityTypeConfiguration<ApprovalLevel>
{
    public void Configure(EntityTypeBuilder<ApprovalLevel> builder)
    {
        builder.ToTable("ApprovalLevels");
        builder.Property(x => x.Feature).HasMaxLength(150);
        builder.Property(x => x.Name).HasMaxLength(100);
        builder.HasIndex(x => new { x.AccountId, x.Feature, x.BusinessUnitId, x.Level });
    }
}

internal sealed class ApprovalStepConfiguration : IEntityTypeConfiguration<ApprovalStep>
{
    public void Configure(EntityTypeBuilder<ApprovalStep> builder)
    {
        builder.ToTable("ApprovalSteps");
        builder.Property(x => x.Feature).HasMaxLength(150);
        builder.Property(x => x.Name).HasMaxLength(100);
        builder.Property(x => x.Comment).HasMaxLength(500);
        builder.HasIndex(x => new { x.Feature, x.DocumentId, x.Level });
        builder.HasIndex(x => new { x.BusinessUnitId, x.Status, x.Feature });
        builder.HasIndex(x => x.ActedById);
    }
}
