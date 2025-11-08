using CL.MySQL2.Models;

namespace WebLogic.Server.Models.Auth;

/// <summary>
/// User account model with GUID-based identity
/// </summary>
[Table(Name = "wls_users", Engine = TableEngine.InnoDB, Charset = Charset.Utf8mb4)]
public class User
{
    /// <summary>
    /// Unique user identifier (GUID)
    /// </summary>
    [Column(Name = "id", DataType = DataType.Uuid, Primary = true, NotNull = true)]
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// User's email address (unique)
    /// </summary>
    [Column(Name = "email", DataType = DataType.VarChar, Size = 255, NotNull = true, Unique = true, Index = true)]
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Username for login (unique)
    /// </summary>
    [Column(Name = "username", DataType = DataType.VarChar, Size = 100, NotNull = true, Unique = true, Index = true)]
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// BCrypt password hash
    /// </summary>
    [Column(Name = "password_hash", DataType = DataType.VarChar, Size = 255, NotNull = true)]
    public string PasswordHash { get; set; } = string.Empty;

    /// <summary>
    /// User's first name
    /// </summary>
    [Column(Name = "first_name", DataType = DataType.VarChar, Size = 100)]
    public string? FirstName { get; set; }

    /// <summary>
    /// User's last name
    /// </summary>
    [Column(Name = "last_name", DataType = DataType.VarChar, Size = 100)]
    public string? LastName { get; set; }

    /// <summary>
    /// Whether the account is active
    /// </summary>
    [Column(Name = "is_active", DataType = DataType.Bool, DefaultValue = "1")]
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Whether email has been verified
    /// </summary>
    [Column(Name = "is_email_verified", DataType = DataType.Bool, DefaultValue = "0")]
    public bool IsEmailVerified { get; set; } = false;

    /// <summary>
    /// Email verification token
    /// </summary>
    [Column(Name = "email_verification_token", DataType = DataType.VarChar, Size = 255)]
    public string? EmailVerificationToken { get; set; }

    /// <summary>
    /// Password reset token
    /// </summary>
    [Column(Name = "password_reset_token", DataType = DataType.VarChar, Size = 255)]
    public string? PasswordResetToken { get; set; }

    /// <summary>
    /// When the password reset token expires
    /// </summary>
    [Column(Name = "password_reset_expires", DataType = DataType.DateTime)]
    public DateTime? PasswordResetExpires { get; set; }

    /// <summary>
    /// Last login timestamp
    /// </summary>
    [Column(Name = "last_login_at", DataType = DataType.DateTime)]
    public DateTime? LastLoginAt { get; set; }

    /// <summary>
    /// Last login IP address
    /// </summary>
    [Column(Name = "last_login_ip", DataType = DataType.VarChar, Size = 45)]
    public string? LastLoginIp { get; set; }

    /// <summary>
    /// Count of failed login attempts
    /// </summary>
    [Column(Name = "failed_login_attempts", DataType = DataType.Int, DefaultValue = "0")]
    public int FailedLoginAttempts { get; set; } = 0;

    /// <summary>
    /// Account locked until this time (null = not locked)
    /// </summary>
    [Column(Name = "locked_until", DataType = DataType.DateTime)]
    public DateTime? LockedUntil { get; set; }

    /// <summary>
    /// When the user was created
    /// </summary>
    [Column(Name = "created_at", DataType = DataType.DateTime, DefaultValue = "CURRENT_TIMESTAMP")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When the user was last updated
    /// </summary>
    [Column(Name = "updated_at", DataType = DataType.DateTime, DefaultValue = "CURRENT_TIMESTAMP")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Full name (computed property)
    /// </summary>
    [Ignore]
    public string FullName => string.IsNullOrWhiteSpace(FirstName) && string.IsNullOrWhiteSpace(LastName)
        ? Username
        : $"{FirstName} {LastName}".Trim();

    /// <summary>
    /// Is the account locked?
    /// </summary>
    [Ignore]
    public bool IsLocked => LockedUntil.HasValue && LockedUntil.Value > DateTime.UtcNow;
}
