using CL.MySQL2.Models;

namespace WebLogic.Server.Models.Database;

/// <summary>
/// Log entry categories for database logging
/// </summary>
public enum LogCategory
{
    /// <summary>
    /// Security-related logs (login attempts, access violations, etc.)
    /// </summary>
    Security = 1,

    /// <summary>
    /// Rate limiting and throttling logs
    /// </summary>
    RateLimit = 2,

    /// <summary>
    /// Authentication and authorization logs
    /// </summary>
    Authentication = 3,

    /// <summary>
    /// API request/response logs
    /// </summary>
    Api = 4,

    /// <summary>
    /// System events (startup, shutdown, errors)
    /// </summary>
    System = 5,

    /// <summary>
    /// Database operations (queries, migrations)
    /// </summary>
    Database = 6,

    /// <summary>
    /// User actions (CRUD operations on resources)
    /// </summary>
    UserAction = 7,

    /// <summary>
    /// Extension/module events
    /// </summary>
    Extension = 8,

    /// <summary>
    /// DNSBL and IP reputation checks
    /// </summary>
    IpReputation = 9,

    /// <summary>
    /// Session management events
    /// </summary>
    Session = 10,

    /// <summary>
    /// General application logs
    /// </summary>
    General = 99
}

/// <summary>
/// Log severity levels
/// </summary>
public enum LogLevel
{
    /// <summary>
    /// Detailed debug information
    /// </summary>
    Debug = 1,

    /// <summary>
    /// Informational messages
    /// </summary>
    Info = 2,

    /// <summary>
    /// Warning messages
    /// </summary>
    Warning = 3,

    /// <summary>
    /// Error messages
    /// </summary>
    Error = 4,

    /// <summary>
    /// Critical system failures
    /// </summary>
    Critical = 5
}

/// <summary>
/// Database log entry for centralized logging
/// </summary>
[Table(Name = "wls_logs", Engine = TableEngine.InnoDB, Charset = Charset.Utf8mb4)]
public class LogEntry
{
    /// <summary>
    /// Unique identifier for the log entry
    /// </summary>
    [Column(Name = "id", DataType = DataType.Uuid, Primary = true, NotNull = true)]
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Log category (Security, RateLimit, etc.)
    /// </summary>
    [Column(Name = "category", DataType = DataType.Int, NotNull = true, Index = true)]
    public LogCategory Category { get; set; }

    /// <summary>
    /// Log severity level
    /// </summary>
    [Column(Name = "level", DataType = DataType.Int, NotNull = true, Index = true)]
    public LogLevel Level { get; set; }

    /// <summary>
    /// Log message
    /// </summary>
    [Column(Name = "message", DataType = DataType.VarChar, Size = 2000, NotNull = true)]
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Additional details in JSON format
    /// </summary>
    [Column(Name = "details", DataType = DataType.Text, NotNull = false)]
    public string? Details { get; set; }

    /// <summary>
    /// User ID if action is user-related
    /// </summary>
    [Column(Name = "user_id", DataType = DataType.Uuid, NotNull = false, Index = true)]
    public Guid? UserId { get; set; }

    /// <summary>
    /// Username if available
    /// </summary>
    [Column(Name = "username", DataType = DataType.VarChar, Size = 255, NotNull = false, Index = true)]
    public string? Username { get; set; }

    /// <summary>
    /// IP address of the client
    /// </summary>
    [Column(Name = "ip_address", DataType = DataType.VarChar, Size = 45, NotNull = false, Index = true)]
    public string? IpAddress { get; set; }

    /// <summary>
    /// User agent string
    /// </summary>
    [Column(Name = "user_agent", DataType = DataType.VarChar, Size = 500, NotNull = false)]
    public string? UserAgent { get; set; }

    /// <summary>
    /// Request path/URL if applicable
    /// </summary>
    [Column(Name = "request_path", DataType = DataType.VarChar, Size = 500, NotNull = false, Index = true)]
    public string? RequestPath { get; set; }

    /// <summary>
    /// HTTP method if applicable
    /// </summary>
    [Column(Name = "http_method", DataType = DataType.VarChar, Size = 10, NotNull = false)]
    public string? HttpMethod { get; set; }

    /// <summary>
    /// Response status code if applicable
    /// </summary>
    [Column(Name = "status_code", DataType = DataType.Int, NotNull = false)]
    public int? StatusCode { get; set; }

    /// <summary>
    /// Processing duration in milliseconds
    /// </summary>
    [Column(Name = "duration_ms", DataType = DataType.Int, NotNull = false)]
    public int? DurationMs { get; set; }

    /// <summary>
    /// Exception stack trace if error
    /// </summary>
    [Column(Name = "exception", DataType = DataType.Text, NotNull = false)]
    public string? Exception { get; set; }

    /// <summary>
    /// Source component/service that generated the log
    /// </summary>
    [Column(Name = "source", DataType = DataType.VarChar, Size = 100, NotNull = false, Index = true)]
    public string? Source { get; set; }

    /// <summary>
    /// Timestamp when the log was created
    /// </summary>
    [Column(Name = "created_at", DataType = DataType.DateTime, NotNull = true, DefaultValue = "CURRENT_TIMESTAMP", Index = true)]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
