using CL.MySQL2.Models;

namespace WebLogic.Server.Models.CMS;

/// <summary>
/// CMS Content Block model for reusable content snippets
/// </summary>
[Table(Name = "wls_content_blocks", Engine = TableEngine.InnoDB, Charset = Charset.Utf8mb4)]
[CompositeIndex("idx_block_key", "block_key", "is_active")]
public class ContentBlock
{
    [Column(Name = "id", DataType = DataType.Int, Primary = true, AutoIncrement = true)]
    public int Id { get; set; }

    [Column(Name = "block_key", DataType = DataType.VarChar, Size = 100, NotNull = true, Unique = true, Index = true)]
    public string BlockKey { get; set; } = string.Empty;

    [Column(Name = "name", DataType = DataType.VarChar, Size = 255, NotNull = true)]
    public string Name { get; set; } = string.Empty;

    [Column(Name = "description", DataType = DataType.VarChar, Size = 500)]
    public string? Description { get; set; }

    [Column(Name = "content", DataType = DataType.Text, NotNull = true)]
    public string Content { get; set; } = string.Empty;

    [Column(Name = "content_type", DataType = DataType.VarChar, Size = 50, NotNull = true, DefaultValue = "html")]
    public string ContentType { get; set; } = "html"; // html, markdown, text, json

    [Column(Name = "category", DataType = DataType.VarChar, Size = 100, Index = true)]
    public string? Category { get; set; }

    [Column(Name = "is_active", DataType = DataType.TinyInt, NotNull = true, DefaultValue = "1", Index = true)]
    public bool IsActive { get; set; } = true;

    [Column(Name = "cache_duration", DataType = DataType.Int, NotNull = true, DefaultValue = "0")]
    public int CacheDuration { get; set; } // in seconds, 0 = no cache

    // Custom settings (JSON)
    [Column(Name = "settings", DataType = DataType.Text)]
    public string? Settings { get; set; }

    // Timestamps
    [Column(Name = "created_at", DataType = DataType.DateTime, NotNull = true, DefaultValue = "CURRENT_TIMESTAMP")]
    public DateTime CreatedAt { get; set; }

    [Column(Name = "updated_at", DataType = DataType.DateTime, NotNull = true, DefaultValue = "CURRENT_TIMESTAMP", OnUpdateCurrentTimestamp = true)]
    public DateTime UpdatedAt { get; set; }
}
