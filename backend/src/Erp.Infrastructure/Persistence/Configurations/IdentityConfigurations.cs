using Erp.Core.Domain.Identity;
using Erp.Core.Domain.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Erp.Infrastructure.Persistence.Configurations;

internal sealed class PanelUserConfiguration : IEntityTypeConfiguration<PanelUser>
{
    public void Configure(EntityTypeBuilder<PanelUser> builder)
    {
        builder.ToTable("PanelUsers");
        builder.Property(u => u.Email).HasMaxLength(256);
        builder.Property(u => u.FullName).HasMaxLength(200);
        builder.Property(u => u.PasswordHash).HasMaxLength(100);
        builder.Property(u => u.Role).HasConversion<string>().HasMaxLength(32);
        builder.Property(u => u.SecurityStamp).HasDefaultValueSql("NEWID()");
        builder.HasIndex(u => u.Email).IsUnique();
    }
}

internal sealed class UserSessionConfiguration : IEntityTypeConfiguration<UserSession>
{
    public void Configure(EntityTypeBuilder<UserSession> builder)
    {
        builder.ToTable("UserSessions");
        builder.Property(s => s.Surface).HasMaxLength(16);
        builder.Property(s => s.TokenHash).HasMaxLength(64);
        builder.Property(s => s.IpAddress).HasMaxLength(64);
        builder.Property(s => s.UserAgent).HasMaxLength(500);
        builder.HasIndex(s => s.TokenHash).IsUnique();
        builder.HasIndex(s => new { s.Surface, s.UserId });
        builder.HasIndex(s => s.FamilyId);
    }
}

internal sealed class LoginEventConfiguration : IEntityTypeConfiguration<LoginEvent>
{
    public void Configure(EntityTypeBuilder<LoginEvent> builder)
    {
        builder.ToTable("LoginEvents");
        builder.Property(e => e.Surface).HasMaxLength(16);
        builder.Property(e => e.Login).HasMaxLength(256);
        builder.Property(e => e.Result).HasMaxLength(32);
        builder.Property(e => e.IpAddress).HasMaxLength(64);
        builder.Property(e => e.UserAgent).HasMaxLength(500);
        builder.HasIndex(e => new { e.Surface, e.UserId, e.CreatedAt });
    }
}

internal sealed class ErpUserConfiguration : IEntityTypeConfiguration<ErpUser>
{
    public void Configure(EntityTypeBuilder<ErpUser> builder)
    {
        builder.ToTable("ErpUsers");
        builder.Property(u => u.Username).HasMaxLength(50);
        builder.Property(u => u.Phone).HasMaxLength(32);
        builder.HasIndex(u => u.EmployeeId).IsUnique().HasFilter("[EmployeeId] IS NOT NULL");
        builder.HasOne<Employee>().WithMany().HasForeignKey(u => u.EmployeeId).OnDelete(DeleteBehavior.NoAction);
        builder.HasIndex(u => u.Username).IsUnique();
        builder.Property(u => u.Email).HasMaxLength(256);
        builder.Property(u => u.FullName).HasMaxLength(200);
        builder.Property(u => u.PasswordHash).HasMaxLength(100);
        builder.Property(u => u.SecurityStamp).HasDefaultValueSql("NEWID()");
        builder.HasIndex(u => u.Email).IsUnique();

        builder.HasOne(u => u.Account)
            .WithMany()
            .HasForeignKey(u => u.AccountId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<BusinessUnit>()
            .WithMany()
            .HasForeignKey(u => u.LastBusinessUnitId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}

internal sealed class UserBusinessUnitConfiguration : IEntityTypeConfiguration<UserBusinessUnit>
{
    public void Configure(EntityTypeBuilder<UserBusinessUnit> builder)
    {
        builder.ToTable("UserBusinessUnits");
        builder.HasIndex(x => new { x.UserId, x.BusinessUnitId }).IsUnique();

        builder.HasOne<Account>().WithMany().HasForeignKey(x => x.AccountId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.User).WithMany(u => u.BusinessUnits).HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.BusinessUnit).WithMany().HasForeignKey(x => x.BusinessUnitId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.Role).WithMany().HasForeignKey(x => x.RoleId).OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> builder)
    {
        builder.ToTable("Roles");
        builder.Property(r => r.Name).HasMaxLength(100);
        builder.Property(r => r.Description).HasMaxLength(500);
        builder.HasIndex(r => new { r.AccountId, r.Name }).IsUnique();

        builder.HasOne<Account>().WithMany().HasForeignKey(r => r.AccountId).OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(r => r.Permissions).WithOne(p => p.Role).HasForeignKey(p => p.RoleId).OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class RolePermissionConfiguration : IEntityTypeConfiguration<RolePermission>
{
    public void Configure(EntityTypeBuilder<RolePermission> builder)
    {
        builder.ToTable("RolePermissions");
        builder.HasKey(p => new { p.RoleId, p.PermissionCode });
        builder.Property(p => p.PermissionCode).HasMaxLength(150);
        builder.HasOne<Permission>().WithMany().HasForeignKey(p => p.PermissionCode).OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class PermissionConfiguration : IEntityTypeConfiguration<Permission>
{
    public void Configure(EntityTypeBuilder<Permission> builder)
    {
        builder.ToTable("Permissions");
        builder.HasKey(p => p.Code);
        builder.Property(p => p.Code).HasMaxLength(150);
        builder.Property(p => p.Module).HasMaxLength(50);
        builder.Property(p => p.Feature).HasMaxLength(50);
        builder.Property(p => p.Action).HasMaxLength(50);
        builder.Property(p => p.Description).HasMaxLength(200);
    }
}

internal sealed class PasswordResetCodeConfiguration : IEntityTypeConfiguration<PasswordResetCode>
{
    public void Configure(EntityTypeBuilder<PasswordResetCode> builder)
    {
        builder.ToTable("PasswordResetCodes");
        builder.Property(c => c.Surface).HasMaxLength(16);
        builder.Property(c => c.CodeHash).HasMaxLength(64);
        builder.HasIndex(c => new { c.Surface, c.UserId, c.CreatedAt });
    }
}

internal sealed class EmployeeConfiguration : IEntityTypeConfiguration<Employee>
{
    public void Configure(EntityTypeBuilder<Employee> builder)
    {
        builder.MapRecord("Employee", "EmployeeID");
        builder.Property(x => x.Code).HasMaxLength(50);
        builder.Property(x => x.Name).HasMaxLength(200);
        builder.Property(x => x.NameBn).HasMaxLength(200);
        builder.Property(x => x.Mobile).HasMaxLength(32);
        builder.Property(x => x.Email).HasMaxLength(256);
        builder.Property(x => x.OfficialContact).HasMaxLength(100);
        builder.Property(x => x.EmergencyMobile).HasMaxLength(32);
        builder.Property(x => x.FatherName).HasMaxLength(200);
        builder.Property(x => x.MotherName).HasMaxLength(200);
        builder.Property(x => x.Nid).HasMaxLength(50);
        builder.Property(x => x.PresentAddress).HasMaxLength(500);
        builder.Property(x => x.PermanentAddress).HasMaxLength(500);
        builder.Property(x => x.Location).HasMaxLength(200);
        builder.Property(x => x.AttendanceId).HasMaxLength(50);
        builder.Property(x => x.CardNumber).HasMaxLength(50);
        builder.HasIndex(x => new { x.AccountId, x.Code }).IsUnique();
        builder.HasIndex(x => x.HomeBusinessUnitId);

        builder.HasOne<BusinessUnit>().WithMany().HasForeignKey(x => x.HomeBusinessUnitId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Office>().WithMany().HasForeignKey(x => x.OfficeId).OnDelete(DeleteBehavior.NoAction);
        builder.HasOne<Warehouse>().WithMany().HasForeignKey(x => x.WarehouseId).OnDelete(DeleteBehavior.NoAction);
        builder.HasOne<Employee>().WithMany().HasForeignKey(x => x.SupervisorId).OnDelete(DeleteBehavior.NoAction);
        builder.HasOne<Employee>().WithMany().HasForeignKey(x => x.LineManagerId).OnDelete(DeleteBehavior.NoAction);
    }
}
