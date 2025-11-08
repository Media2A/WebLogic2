using CL.MySQL2.Models;

namespace WebLogic.Server.Models.Session;

/// <summary>
/// Database-backed session storage for ASP.NET Core distributed cache
/// Table: wls_session_cache
/// </summary>
[Table(Name = "wls_session_cache", Engine = TableEngine.InnoDB, Charset = Charset.Utf8mb4)]
public class SessionData
{
    /// <summary>
    /// Session ID (unique key)
    /// </summary>
    [Column(Name = "session_id", DataType = DataType.VarChar, Size = 449, Primary = true, NotNull = true, Index = true)]
    public string SessionId { get; set; } = string.Empty;

    /// <summary>
    /// Serialized session data
    /// </summary>
    [Column(Name = "data", DataType = DataType.LongBlob, NotNull = true)]
    public byte[] Data { get; set; } = Array.Empty<byte>();

    /// <summary>
    /// Session expiration time (UTC)
    /// </summary>
    [Column(Name = "expires_at", DataType = DataType.DateTime, NotNull = true, Index = true)]
    public DateTime ExpiresAt { get; set; }

    /// <summary>
    /// Sliding expiration window in seconds
    /// </summary>
    [Column(Name = "sliding_expiration_seconds", DataType = DataType.BigInt, NotNull = false)]
    public long? SlidingExpirationSeconds { get; set; }

    /// <summary>
    /// When the session was created
    /// </summary>
    [Column(Name = "created_at", DataType = DataType.DateTime, NotNull = true, DefaultValue = "CURRENT_TIMESTAMP")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When the session was last accessed
    /// </summary>
    [Column(Name = "last_accessed_at", DataType = DataType.DateTime, NotNull = true)]
    public DateTime LastAccessedAt { get; set; } = DateTime.UtcNow;
}
