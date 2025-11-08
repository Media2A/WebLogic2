using CL.MySQL2.Models;

namespace WebLogic.Server.Models.Database;

/// <summary>
/// Database model for session tracking
/// Table: wls_sessions
/// </summary>
[Table(Name = "wls_sessions", Engine = TableEngine.InnoDB, Charset = Charset.Utf8mb4)]
[CompositeIndex("idx_session_user", "session_id", "user_id")]
public class Session
{
    [Column(Name = "id", DataType = DataType.Int, Primary = true, AutoIncrement = true)]
    public int Id { get; set; }

    [Column(Name = "session_id", DataType = DataType.VarChar, Size = 255, NotNull = true, Unique = true, Index = true)]
    public string SessionId { get; set; } = string.Empty;

    [Column(Name = "client_id", DataType = DataType.VarChar, Size = 36, NotNull = true, Index = true)]
    public string ClientId { get; set; } = string.Empty;

    [Column(Name = "user_id", DataType = DataType.Int, Index = true)]
    public int? UserId { get; set; }

    [Column(Name = "ip_address", DataType = DataType.VarChar, Size = 45)]
    public string? IpAddress { get; set; }

    [Column(Name = "ip_forward", DataType = DataType.VarChar, Size = 45)]
    public string? IpForward { get; set; }

    [Column(Name = "user_agent", DataType = DataType.VarChar, Size = 500)]
    public string? UserAgent { get; set; }

    [Column(Name = "is_bot", DataType = DataType.TinyInt, DefaultValue = "0")]
    public bool IsBot { get; set; }

    [Column(Name = "created_at", DataType = DataType.DateTime, NotNull = true, DefaultValue = "CURRENT_TIMESTAMP")]
    public DateTime CreatedAt { get; set; }

    [Column(Name = "last_activity", DataType = DataType.DateTime, NotNull = true, DefaultValue = "CURRENT_TIMESTAMP", OnUpdateCurrentTimestamp = true)]
    public DateTime LastActivity { get; set; }

    [Column(Name = "last_url", DataType = DataType.VarChar, Size = 1000)]
    public string? LastUrl { get; set; }

    [Column(Name = "session_data", DataType = DataType.Text)]
    public string? SessionData { get; set; }
}
