using Microsoft.AspNetCore.Identity;

namespace ApiIdentity.Models;

public sealed class RolePermission
{
    public string RoleId { get; set; } = string.Empty;

    public string PermissionKey { get; set; } = string.Empty;

    public IdentityRole? Role { get; set; }

    public PermissionDefinition? Permission { get; set; }
}
