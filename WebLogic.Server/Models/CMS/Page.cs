using CL.MySQL2.Models;

namespace WebLogic.Server.Models.CMS;

/// <summary>
/// CMS Page model for hierarchical content pages
/// </summary>
[Table(Name = "wls_pages", Engine = TableEngine.InnoDB, Charset = Charset.Utf8mb4)]
[CompositeIndex("idx_page_parent", "parent_id", "is_published")]
[CompositeIndex("idx_page_slug", "slug", "is_published")]
public class Page
{
    [Column(Name = "id", DataType = DataType.Int, Primary = true, AutoIncrement = true)]
    public int Id { get; set; }

    [Column(Name = "parent_id", DataType = DataType.Int, Index = true)]
    public int? ParentId { get; set; }

    [Column(Name = "title", DataType = DataType.VarChar, Size = 255, NotNull = true)]
    public string Title { get; set; } = string.Empty;

    [Column(Name = "slug", DataType = DataType.VarChar, Size = 255, NotNull = true, Index = true)]
    public string Slug { get; set; } = string.Empty;

    [Column(Name = "full_path", DataType = DataType.VarChar, Size = 1000, NotNull = true, Index = true)]
    public string FullPath { get; set; } = string.Empty;

    [Column(Name = "content", DataType = DataType.Text, NotNull = true)]
    public string Content { get; set; } = string.Empty;

    [Column(Name = "excerpt", DataType = DataType.VarChar, Size = 500)]
    public string? Excerpt { get; set; }

    [Column(Name = "template", DataType = DataType.VarChar, Size = 100)]
    public string? Template { get; set; }

    [Column(Name = "layout", DataType = DataType.VarChar, Size = 100)]
    public string? Layout { get; set; }

    [Column(Name = "author_id", DataType = DataType.Int, Index = true)]
    public int? AuthorId { get; set; }

    [Column(Name = "is_published", DataType = DataType.TinyInt, NotNull = true, DefaultValue = "0", Index = true)]
    public bool IsPublished { get; set; }

    [Column(Name = "published_at", DataType = DataType.DateTime)]
    public DateTime? PublishedAt { get; set; }

    [Column(Name = "sort_order", DataType = DataType.Int, NotNull = true, DefaultValue = "0")]
    public int SortOrder { get; set; }

    // SEO Fields
    [Column(Name = "meta_title", DataType = DataType.VarChar, Size = 255)]
    public string? MetaTitle { get; set; }

    [Column(Name = "meta_description", DataType = DataType.VarChar, Size = 500)]
    public string? MetaDescription { get; set; }

    [Column(Name = "meta_keywords", DataType = DataType.VarChar, Size = 500)]
    public string? MetaKeywords { get; set; }

    [Column(Name = "og_image", DataType = DataType.VarChar, Size = 500)]
    public string? OgImage { get; set; }

    // Custom Fields (JSON)
    [Column(Name = "custom_data", DataType = DataType.Text)]
    public string? CustomData { get; set; }

    // Timestamps
    [Column(Name = "created_at", DataType = DataType.DateTime, NotNull = true, DefaultValue = "CURRENT_TIMESTAMP")]
    public DateTime CreatedAt { get; set; }

    [Column(Name = "updated_at", DataType = DataType.DateTime, NotNull = true, DefaultValue = "CURRENT_TIMESTAMP", OnUpdateCurrentTimestamp = true)]
    public DateTime UpdatedAt { get; set; }
}
