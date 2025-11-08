using CL.MySQL2.Models;

namespace WebLogic.Server.Models.Auth;

/// <summary>
/// User role model for role-based access control
/// </summary>
[Table(Name = "wls_roles", Engine = TableEngine.InnoDB, Charset = Charset.Utf8mb4)]
public class Role
{
    /// <summary>
    /// Unique role identifier (GUID)
    /// </summary>
    [Column(Name = "id", DataType = DataType.Uuid, Primary = true, NotNull = true)]
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Role name (unique, lowercase identifier)
    /// </summary>
    [Column(Name = "name", DataType = DataType.VarChar, Size = 100, NotNull = true, Unique = true, Index = true)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Display name for UI
    /// </summary>
    [Column(Name = "display_name", DataType = DataType.VarChar, Size = 100, NotNull = true)]
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Role description
    /// </summary>
    [Column(Name = "description", DataType = DataType.VarChar, Size = 500)]
    public string? Description { get; set; }

    /// <summary>
    /// Whether this is a system role (cannot be deleted)
    /// </summary>
    [Column(Name = "is_system_role", DataType = DataType.Bool, DefaultValue = "0")]
    public bool IsSystemRole { get; set; } = false;

    /// <summary>
    /// When the role was created
    /// </summary>
    [Column(Name = "created_at", DataType = DataType.DateTime, DefaultValue = "CURRENT_TIMESTAMP")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When the role was last updated
    /// </summary>
    [Column(Name = "updated_at", DataType = DataType.DateTime, DefaultValue = "CURRENT_TIMESTAMP")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
