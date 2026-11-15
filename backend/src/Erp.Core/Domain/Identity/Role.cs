namespace Erp.Core.Domain.Identity;

/// <summary>An account-defined bundle of permissions, assigned to users per business unit.</summary>
public class Role : BaseEntity, IAccountOwned
{
    public Guid AccountId { get; set; }

    public required string Name { get; set; }

    public string? Description { get; set; }

    /// <summary>Created by the platform (e.g. Administrator); cannot be deleted by clients.</summary>
    public bool IsSystem { get; set; }

    public bool IsActive { get; set; } = true;

    public ICollection<RolePermission> Permissions { get; } = [];
}

public class RolePermission
{
    public Guid RoleId { get; set; }

    public Role? Role { get; set; }

    /// <summary>Permission code, <c>{module}.{feature}.{action}</c>.</summary>
    public required string PermissionCode { get; set; }
}

/// <summary>Catalog row for a permission, synchronised at startup from the registered permission providers.</summary>
public class Permission
{
    public required string Code { get; set; }

    public required string Module { get; set; }

    public required string Feature { get; set; }

    public required string Action { get; set; }

    public required string Description { get; set; }
}
