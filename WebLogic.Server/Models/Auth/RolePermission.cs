using CL.MySQL2.Models;

namespace WebLogic.Server.Models.Auth;

/// <summary>
/// Junction table linking roles to permissions (many-to-many)
/// </summary>
[Table(Name = "wls_role_permissions", Engine = TableEngine.InnoDB, Charset = Charset.Utf8mb4)]
public class RolePermission
{
    /// <summary>
    /// Role ID
    /// </summary>
    [Column(Name = "role_id", DataType = DataType.Uuid, Primary = true, NotNull = true, Index = true)]
    public Guid RoleId { get; set; }

    /// <summary>
    /// Permission ID
    /// </summary>
    [Column(Name = "permission_id", DataType = DataType.Uuid, Primary = true, NotNull = true, Index = true)]
    public Guid PermissionId { get; set; }

    /// <summary>
    /// When the permission was assigned to the role
    /// </summary>
    [Column(Name = "created_at", DataType = DataType.DateTime, DefaultValue = "CURRENT_TIMESTAMP")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
