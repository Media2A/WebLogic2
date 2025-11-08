using CL.MySQL2.Models;

namespace WebLogic.Server.Models.Auth;

/// <summary>
/// Permission model for fine-grained access control
/// </summary>
[Table(Name = "wls_permissions", Engine = TableEngine.InnoDB, Charset = Charset.Utf8mb4)]
public class Permission
{
    /// <summary>
    /// Unique permission identifier (GUID)
    /// </summary>
    [Column(Name = "id", DataType = DataType.Uuid, Primary = true, NotNull = true)]
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Permission name (unique identifier, format: resource.action)
    /// Examples: "blog.create", "user.delete", "admin.access"
    /// </summary>
    [Column(Name = "name", DataType = DataType.VarChar, Size = 100, NotNull = true, Unique = true, Index = true)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Resource this permission applies to
    /// Examples: "blog", "user", "admin", "cms"
    /// </summary>
    [Column(Name = "resource", DataType = DataType.VarChar, Size = 50, NotNull = true, Index = true)]
    public string Resource { get; set; } = string.Empty;

    /// <summary>
    /// Action allowed on the resource
    /// Examples: "create", "read", "update", "delete", "access", "*"
    /// </summary>
    [Column(Name = "action", DataType = DataType.VarChar, Size = 50, NotNull = true)]
    public string Action { get; set; } = string.Empty;

    /// <summary>
    /// Human-readable description
    /// </summary>
    [Column(Name = "description", DataType = DataType.VarChar, Size = 255)]
    public string? Description { get; set; }

    /// <summary>
    /// When the permission was created
    /// </summary>
    [Column(Name = "created_at", DataType = DataType.DateTime, DefaultValue = "CURRENT_TIMESTAMP")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
