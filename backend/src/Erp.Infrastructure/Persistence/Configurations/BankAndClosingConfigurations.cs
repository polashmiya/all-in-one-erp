using Erp.Core.Domain.Accounting;
using Erp.Core.Domain.Metadata;
using Erp.Core.Domain.Procurement;
using Erp.Core.Domain.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Erp.Infrastructure.Persistence.Configurations;

internal sealed class BankAccountConfiguration : IEntityTypeConfiguration<BankAccount>
{
    public void Configure(EntityTypeBuilder<BankAccount> builder)
    {
        builder.MapRecord("BankAccount", "BankAccID");
        builder.Property(x => x.BusinessUnitId).HasColumnName("BusinessUnitID");
        builder.Property(x => x.Name).HasColumnName("BankAccName").HasMaxLength(100);
        builder.Property(x => x.Type).HasColumnName("BankAccType");
        builder.Property(x => x.BankName).HasMaxLength(100);
        builder.Property(x => x.BranchName).HasMaxLength(100);
        builder.Property(x => x.AccountNo).HasColumnName("BankAccNumber").HasMaxLength(100);
        builder.Property(x => x.RoutingNo).HasMaxLength(50);
        builder.Property(x => x.ChartOfAccountId).HasColumnName("ChartOfAccID");

        builder.HasIndex(x => new { x.BusinessUnitId, x.Name }).IsUnique();
        builder.HasOne<BusinessUnit>().WithMany().HasForeignKey(x => x.BusinessUnitId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.ChartOfAccount).WithMany().HasForeignKey(x => x.ChartOfAccountId).OnDelete(DeleteBehavior.NoAction);

        builder.ColumnOrder(
        [
            nameof(BankAccount.Id), nameof(BankAccount.Name), nameof(BankAccount.Type), nameof(BankAccount.BankName),
            nameof(BankAccount.BranchName), nameof(BankAccount.AccountNo), nameof(BankAccount.RoutingNo),
            nameof(BankAccount.ChartOfAccountId), nameof(BankAccount.AccountId), nameof(BankAccount.BusinessUnitId),
            .. MasterRecordMapping.AuditColumns,
            nameof(BankAccount.CustomData),
        ]);
    }
}

internal sealed class AccountingSettingConfiguration : IEntityTypeConfiguration<AccountingSetting>
{
    public void Configure(EntityTypeBuilder<AccountingSetting> builder)
    {
        builder.ToTable("AccountingSetting");
        builder.HasIndex(x => x.AccountId).IsUnique();
        builder.HasOne<Account>().WithMany().HasForeignKey(x => x.AccountId).OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class PeriodClosingConfiguration : IEntityTypeConfiguration<PeriodClosing>
{
    public void Configure(EntityTypeBuilder<PeriodClosing> builder)
    {
        builder.MapRecord("PeriodClosing", "PeriodClosingID");
        builder.Property(x => x.BusinessUnitId).HasColumnName("BusinessUnitID");
        builder.HasIndex(x => new { x.BusinessUnitId, x.IsYear, x.PeriodEnd }).IsUnique().HasFilter("[isActive] = 1");
        builder.HasOne<BusinessUnit>().WithMany().HasForeignKey(x => x.BusinessUnitId).OnDelete(DeleteBehavior.Restrict);

        builder.ColumnOrder(
        [
            nameof(PeriodClosing.Id), nameof(PeriodClosing.PeriodStart), nameof(PeriodClosing.PeriodEnd), nameof(PeriodClosing.IsYear),
            nameof(PeriodClosing.AccountId), nameof(PeriodClosing.BusinessUnitId),
            .. MasterRecordMapping.AuditColumns,
        ]);
    }
}

internal sealed class TaxDepositConfiguration : IEntityTypeConfiguration<TaxDeposit>
{
    public void Configure(EntityTypeBuilder<TaxDeposit> builder)
    {
        builder.MapRecord("TaxDeposit", "TaxDepositID");
        builder.Property(x => x.BusinessUnitId).HasColumnName("BusinessUnitID");
        builder.Property(x => x.Code).HasColumnName("TaxDepositCode").HasMaxLength(100);
        builder.Property(x => x.Type).HasColumnName("TaxType");
        builder.Property(x => x.PaymentMethodId).HasColumnName("PaymentMethodID");
        builder.Property(x => x.BankAccountId).HasColumnName("BankAccountID");
        builder.Property(x => x.ChallanNo).HasMaxLength(100);
        builder.Property(x => x.Remarks).HasMaxLength(1000);
        builder.Property(x => x.TotalAmount).HasPrecision(18, 2);
        builder.Property(x => x.ReviewedAt).HasColumnType("datetime").HasConversion(MasterRecordMapping.Utc);
        builder.Property(x => x.ReviewedById).HasColumnName("ReviewedByID");
        builder.Property(x => x.ReviewComment).HasMaxLength(500);

        builder.HasIndex(x => new { x.BusinessUnitId, x.Code }).IsUnique();
        builder.HasIndex(x => new { x.BusinessUnitId, x.IsActive, x.Status, x.DepositDate });

        builder.HasOne<BusinessUnit>().WithMany().HasForeignKey(x => x.BusinessUnitId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<LookupValue>().WithMany().HasForeignKey(x => x.PaymentMethodId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<BankAccount>().WithMany().HasForeignKey(x => x.BankAccountId).OnDelete(DeleteBehavior.NoAction);
        builder.HasMany(x => x.Lines).WithOne().HasForeignKey(x => x.TaxDepositId).OnDelete(DeleteBehavior.Cascade);

        builder.ColumnOrder(
        [
            nameof(TaxDeposit.Id), nameof(TaxDeposit.Code), nameof(TaxDeposit.DepositDate), nameof(TaxDeposit.Type),
            nameof(TaxDeposit.PaymentMethodId), nameof(TaxDeposit.BankAccountId), nameof(TaxDeposit.ChallanNo),
            nameof(TaxDeposit.ChallanDate), nameof(TaxDeposit.Remarks), nameof(TaxDeposit.TotalAmount), nameof(TaxDeposit.Status),
            nameof(TaxDeposit.ReviewedAt), nameof(TaxDeposit.ReviewedById), nameof(TaxDeposit.ReviewComment),
            nameof(TaxDeposit.AccountId), nameof(TaxDeposit.BusinessUnitId),
            .. MasterRecordMapping.AuditColumns,
            nameof(TaxDeposit.CustomData),
        ]);
    }
}

internal sealed class TaxDepositLineConfiguration : IEntityTypeConfiguration<TaxDepositLine>
{
    public void Configure(EntityTypeBuilder<TaxDepositLine> builder)
    {
        builder.ToTable("TaxDepositLine");
        builder.Property(x => x.Id).HasColumnName("TaxDepositLineID").UseIdentityColumn();
        builder.Property(x => x.TaxDepositId).HasColumnName("TaxDepositID");
        builder.Property(x => x.PurchasePaymentId).HasColumnName("PurchasePaymentID");
        builder.Property(x => x.Amount).HasPrecision(18, 2);
        builder.HasOne<PurchasePayment>().WithMany().HasForeignKey(x => x.PurchasePaymentId).OnDelete(DeleteBehavior.NoAction);
    }
}
