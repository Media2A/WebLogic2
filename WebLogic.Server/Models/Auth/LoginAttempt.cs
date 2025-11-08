using CL.MySQL2.Models;

namespace WebLogic.Server.Models.Auth;

/// <summary>
/// Login attempt record for security auditing and tracking
/// </summary>
[Table(Name = "wls_login_attempts", Engine = TableEngine.InnoDB, Charset = Charset.Utf8mb4)]
public class LoginAttempt
{
    /// <summary>
    /// Unique identifier for the login attempt
    /// </summary>
    [Column(Name = "id", DataType = DataType.Uuid, Primary = true, NotNull = true)]
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// User ID if user was found (null if user doesn't exist)
    /// </summary>
    [Column(Name = "user_id", DataType = DataType.Uuid, NotNull = false, Index = true)]
    public Guid? UserId { get; set; }

    /// <summary>
    /// Username or email used in the attempt
    /// </summary>
    [Column(Name = "username_or_email", DataType = DataType.VarChar, Size = 255, NotNull = true, Index = true)]
    public string UsernameOrEmail { get; set; } = string.Empty;

    /// <summary>
    /// IP address of the client
    /// </summary>
    [Column(Name = "ip_address", DataType = DataType.VarChar, Size = 45, NotNull = true, Index = true)]
    public string IpAddress { get; set; } = string.Empty;

    /// <summary>
    /// User agent string from the browser
    /// </summary>
    [Column(Name = "user_agent", DataType = DataType.VarChar, Size = 500, NotNull = false)]
    public string? UserAgent { get; set; }

    /// <summary>
    /// Whether the login attempt was successful
    /// </summary>
    [Column(Name = "is_successful", DataType = DataType.Bool, NotNull = true, Index = true)]
    public bool IsSuccessful { get; set; }

    /// <summary>
    /// Reason for failure (if applicable)
    /// </summary>
    [Column(Name = "failure_reason", DataType = DataType.VarChar, Size = 255, NotNull = false)]
    public string? FailureReason { get; set; }

    /// <summary>
    /// Timestamp of the attempt
    /// </summary>
    [Column(Name = "attempted_at", DataType = DataType.DateTime, NotNull = true, DefaultValue = "CURRENT_TIMESTAMP", Index = true)]
    public DateTime AttemptedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Country code from IP geolocation (if available)
    /// </summary>
    [Column(Name = "country_code", DataType = DataType.VarChar, Size = 2, NotNull = false)]
    public string? CountryCode { get; set; }

    /// <summary>
    /// Additional metadata in JSON format
    /// </summary>
    [Column(Name = "metadata", DataType = DataType.VarChar, Size = 1000, NotNull = false)]
    public string? Metadata { get; set; }
}
