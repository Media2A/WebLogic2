using CL.MySQL2.Models;

namespace WebLogic.Server.Models.Auth;

/// <summary>
/// Junction table linking users to roles (many-to-many)
/// </summary>
[Table(Name = "wls_user_roles", Engine = TableEngine.InnoDB, Charset = Charset.Utf8mb4)]
public class UserRole
{
    /// <summary>
    /// User ID
    /// </summary>
    [Column(Name = "user_id", DataType = DataType.VarChar, Size = 36, Primary = true, NotNull = true, Index = true)]
    public Guid UserId { get; set; }

    /// <summary>
    /// Role ID
    /// </summary>
    [Column(Name = "role_id", DataType = DataType.VarChar, Size = 36, Primary = true, NotNull = true, Index = true)]
    public Guid RoleId { get; set; }

    /// <summary>
    /// When the role was assigned to the user
    /// </summary>
    [Column(Name = "assigned_at", DataType = DataType.DateTime, DefaultValue = "CURRENT_TIMESTAMP")]
    public DateTime AssignedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Who assigned this role (null if system-assigned)
    /// </summary>
    [Column(Name = "assigned_by", DataType = DataType.VarChar, Size = 36)]
    public Guid? AssignedBy { get; set; }
}
