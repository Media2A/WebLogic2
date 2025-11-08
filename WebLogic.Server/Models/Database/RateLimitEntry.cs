using CL.MySQL2.Models;

namespace WebLogic.Server.Models.Database;

/// <summary>
/// Database model for rate limiting tracking
/// Table: wls_rate_limits
/// </summary>
[Table(Name = "wls_rate_limits", Engine = TableEngine.InnoDB, Charset = Charset.Utf8mb4)]
[CompositeIndex("idx_ip_window", "ip_address", "window_start")]
public class RateLimitEntry
{
    [Column(Name = "id", DataType = DataType.Int, Primary = true, AutoIncrement = true)]
    public int Id { get; set; }

    [Column(Name = "ip_address", DataType = DataType.VarChar, Size = 45, NotNull = true, Index = true)]
    public string IpAddress { get; set; } = string.Empty;

    [Column(Name = "endpoint", DataType = DataType.VarChar, Size = 255, NotNull = true)]
    public string Endpoint { get; set; } = string.Empty;

    [Column(Name = "request_count", DataType = DataType.Int, NotNull = true, DefaultValue = "1")]
    public int RequestCount { get; set; } = 1;

    [Column(Name = "window_start", DataType = DataType.DateTime, NotNull = true, DefaultValue = "CURRENT_TIMESTAMP")]
    public DateTime WindowStart { get; set; }

    [Column(Name = "is_banned", DataType = DataType.TinyInt, DefaultValue = "0")]
    public bool IsBanned { get; set; }

    [Column(Name = "ban_until", DataType = DataType.DateTime)]
    public DateTime? BanUntil { get; set; }

    [Column(Name = "created_at", DataType = DataType.DateTime, NotNull = true, DefaultValue = "CURRENT_TIMESTAMP")]
    public DateTime CreatedAt { get; set; }

    [Column(Name = "updated_at", DataType = DataType.DateTime, NotNull = true, DefaultValue = "CURRENT_TIMESTAMP", OnUpdateCurrentTimestamp = true)]
    public DateTime UpdatedAt { get; set; }
}
