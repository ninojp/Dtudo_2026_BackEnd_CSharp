namespace ApiIdentity.Models;

public sealed class PermissionDefinition
{
    public string Key { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public ICollection<RolePermission> RolePermissions { get; } = new List<RolePermission>();
}
