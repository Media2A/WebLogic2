using CL.MySQL2.Models;

namespace WebLogic.Server.Models.CMS;

/// <summary>
/// CMS Media model for file and image management
/// </summary>
[Table(Name = "wls_media", Engine = TableEngine.InnoDB, Charset = Charset.Utf8mb4)]
[CompositeIndex("idx_media_type", "media_type", "created_at")]
public class Media
{
    [Column(Name = "id", DataType = DataType.Int, Primary = true, AutoIncrement = true)]
    public int Id { get; set; }

    [Column(Name = "filename", DataType = DataType.VarChar, Size = 255, NotNull = true)]
    public string Filename { get; set; } = string.Empty;

    [Column(Name = "original_filename", DataType = DataType.VarChar, Size = 255, NotNull = true)]
    public string OriginalFilename { get; set; } = string.Empty;

    [Column(Name = "file_path", DataType = DataType.VarChar, Size = 500, NotNull = true, Index = true)]
    public string FilePath { get; set; } = string.Empty;

    [Column(Name = "url", DataType = DataType.VarChar, Size = 500, NotNull = true)]
    public string Url { get; set; } = string.Empty;

    [Column(Name = "media_type", DataType = DataType.VarChar, Size = 50, NotNull = true, Index = true)]
    public string MediaType { get; set; } = string.Empty; // image, video, document, audio, other

    [Column(Name = "mime_type", DataType = DataType.VarChar, Size = 100, NotNull = true)]
    public string MimeType { get; set; } = string.Empty;

    [Column(Name = "file_size", DataType = DataType.BigInt, NotNull = true)]
    public long FileSize { get; set; }

    // Image-specific fields
    [Column(Name = "width", DataType = DataType.Int)]
    public int? Width { get; set; }

    [Column(Name = "height", DataType = DataType.Int)]
    public int? Height { get; set; }

    [Column(Name = "thumbnail_path", DataType = DataType.VarChar, Size = 500)]
    public string? ThumbnailPath { get; set; }

    // Metadata
    [Column(Name = "title", DataType = DataType.VarChar, Size = 255)]
    public string? Title { get; set; }

    [Column(Name = "alt_text", DataType = DataType.VarChar, Size = 255)]
    public string? AltText { get; set; }

    [Column(Name = "description", DataType = DataType.Text)]
    public string? Description { get; set; }

    [Column(Name = "uploaded_by", DataType = DataType.Int, Index = true)]
    public int? UploadedBy { get; set; }

    // Storage info
    [Column(Name = "storage_provider", DataType = DataType.VarChar, Size = 50, NotNull = true, DefaultValue = "local")]
    public string StorageProvider { get; set; } = "local"; // local, s3, azure, etc.

    [Column(Name = "storage_key", DataType = DataType.VarChar, Size = 500)]
    public string? StorageKey { get; set; }

    // Custom metadata (JSON)
    [Column(Name = "metadata", DataType = DataType.Text)]
    public string? Metadata { get; set; }

    // Timestamps
    [Column(Name = "created_at", DataType = DataType.DateTime, NotNull = true, DefaultValue = "CURRENT_TIMESTAMP", Index = true)]
    public DateTime CreatedAt { get; set; }

    [Column(Name = "updated_at", DataType = DataType.DateTime, NotNull = true, DefaultValue = "CURRENT_TIMESTAMP", OnUpdateCurrentTimestamp = true)]
    public DateTime UpdatedAt { get; set; }
}
