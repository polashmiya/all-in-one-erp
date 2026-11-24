using Erp.Core.Domain.Accounting;
using Erp.Core.Domain.Partners;
using Erp.Core.Domain.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Erp.Infrastructure.Persistence.Configurations;

internal sealed class ChartOfAccountConfiguration : IEntityTypeConfiguration<ChartOfAccount>
{
    public void Configure(EntityTypeBuilder<ChartOfAccount> builder)
    {
        builder.MapRecord("ChartOfAccount", "ChartOfAccID");
        builder.Property(x => x.Code).HasColumnName("ChartOfAccCode").HasMaxLength(50);
        builder.Property(x => x.Name).HasColumnName("ChartOfAccName").HasMaxLength(300);
        builder.Property(x => x.Type).HasColumnName("ChartOfAccType");
        builder.Property(x => x.ParentId).HasColumnName("ParentID");
        builder.Property(x => x.Description).HasMaxLength(500);

        builder.HasIndex(x => new { x.AccountId, x.Code }).IsUnique();
        builder.HasIndex(x => new { x.AccountId, x.Name }).IsUnique();
        builder.HasOne(x => x.Parent).WithMany().HasForeignKey(x => x.ParentId).OnDelete(DeleteBehavior.NoAction);

        builder.ColumnOrder(
        [
            nameof(ChartOfAccount.Id), nameof(ChartOfAccount.Code), nameof(ChartOfAccount.Name), nameof(ChartOfAccount.Type),
            nameof(ChartOfAccount.IsGroup), nameof(ChartOfAccount.ParentId), nameof(ChartOfAccount.Description),
            nameof(ChartOfAccount.AccountId),
            .. MasterRecordMapping.AuditColumns,
            nameof(ChartOfAccount.CustomData),
        ]);
    }
}

internal sealed class PostingAccountConfiguration : IEntityTypeConfiguration<PostingAccount>
{
    public void Configure(EntityTypeBuilder<PostingAccount> builder)
    {
        builder.ToTable("PostingAccount");
        builder.Property(x => x.Key).HasColumnName("PostingKey").HasMaxLength(50);
        builder.Property(x => x.ChartOfAccountId).HasColumnName("ChartOfAccID");
        builder.HasIndex(x => new { x.AccountId, x.Key }).IsUnique();
        builder.HasOne<Account>().WithMany().HasForeignKey(x => x.AccountId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.ChartOfAccount).WithMany().HasForeignKey(x => x.ChartOfAccountId).OnDelete(DeleteBehavior.NoAction);
    }
}

internal sealed class JournalConfiguration : IEntityTypeConfiguration<Journal>
{
    public void Configure(EntityTypeBuilder<Journal> builder)
    {
        builder.MapRecord("SubLedgerHeader", "SubLedgerHeaderID");
        builder.Property(x => x.BusinessUnitId).HasColumnName("BusinessUnitID");
        builder.Property(x => x.Code).HasColumnName("SubLedgerCode").HasMaxLength(100);
        builder.Property(x => x.Narration).HasMaxLength(1500);
        builder.Property(x => x.Feature).HasColumnName("TransactionType").HasMaxLength(150);
        builder.Property(x => x.DocumentId).HasColumnName("TransactionRefID");
        builder.Property(x => x.DocumentCode).HasColumnName("TransactionRefCode").HasMaxLength(100);
        builder.Property(x => x.IsManual).HasColumnName("IsManualJV");
        builder.Property(x => x.TotalAmount).HasPrecision(18, 2);
        builder.Property(x => x.ReviewedAt).HasColumnType("datetime").HasConversion(MasterRecordMapping.Utc);
        builder.Property(x => x.ReviewedById).HasColumnName("ReviewedByID");
        builder.Property(x => x.ReviewComment).HasMaxLength(500);

        builder.HasIndex(x => new { x.BusinessUnitId, x.Code }).IsUnique();
        builder.HasIndex(x => new { x.BusinessUnitId, x.Feature, x.DocumentId, x.IsReversal }).IsUnique()
            .HasFilter("[TransactionRefID] IS NOT NULL");
        builder.HasIndex(x => new { x.BusinessUnitId, x.IsActive, x.Status, x.JournalDate });

        builder.HasOne<BusinessUnit>().WithMany().HasForeignKey(x => x.BusinessUnitId).OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(x => x.Lines).WithOne(x => x.Journal).HasForeignKey(x => x.JournalId).OnDelete(DeleteBehavior.Cascade);

        builder.ColumnOrder(
        [
            nameof(Journal.Id), nameof(Journal.Code), nameof(Journal.JournalDate), nameof(Journal.Narration), nameof(Journal.Feature),
            nameof(Journal.DocumentId), nameof(Journal.DocumentCode), nameof(Journal.IsManual), nameof(Journal.TotalAmount),
            nameof(Journal.Status), nameof(Journal.ReviewedAt), nameof(Journal.ReviewedById), nameof(Journal.ReviewComment),
            nameof(Journal.AccountId), nameof(Journal.BusinessUnitId),
            .. MasterRecordMapping.AuditColumns,
            nameof(Journal.CustomData),
            nameof(Journal.IsReversal),
        ]);
    }
}

internal sealed class JournalLineConfiguration : IEntityTypeConfiguration<JournalLine>
{
    public void Configure(EntityTypeBuilder<JournalLine> builder)
    {
        builder.ToTable("SubLedgerRow");
        builder.Property(x => x.Id).HasColumnName("RowID").UseIdentityColumn();
        builder.Property(x => x.JournalId).HasColumnName("SubLedgerHeaderID");
        builder.Property(x => x.ChartOfAccountId).HasColumnName("ChartOfAccID");
        builder.Property(x => x.SupplierId).HasColumnName("PartnerID");
        builder.Property(x => x.Amount).HasPrecision(18, 2);
        builder.Property(x => x.Remarks).HasMaxLength(500);

        builder.HasIndex(x => new { x.ChartOfAccountId, x.JournalId }).IncludeProperties(x => x.Amount);
        builder.HasIndex(x => new { x.SupplierId, x.JournalId }).IncludeProperties(x => x.Amount);

        builder.HasOne(x => x.ChartOfAccount).WithMany().HasForeignKey(x => x.ChartOfAccountId).OnDelete(DeleteBehavior.NoAction);
        builder.HasOne<Supplier>().WithMany().HasForeignKey(x => x.SupplierId).OnDelete(DeleteBehavior.NoAction);
    }
}
