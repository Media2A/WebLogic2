using CL.MySQL2.Models;

namespace WebLogic.Server.Models.CMS;

/// <summary>
/// CMS Menu Item model for individual menu entries
/// </summary>
[Table(Name = "wls_menu_items", Engine = TableEngine.InnoDB, Charset = Charset.Utf8mb4)]
[CompositeIndex("idx_menu_parent", "menu_id", "parent_id", "sort_order")]
public class MenuItem
{
    [Column(Name = "id", DataType = DataType.Int, Primary = true, AutoIncrement = true)]
    public int Id { get; set; }

    [Column(Name = "menu_id", DataType = DataType.Int, NotNull = true, Index = true)]
    public int MenuId { get; set; }

    [Column(Name = "parent_id", DataType = DataType.Int, Index = true)]
    public int? ParentId { get; set; }

    [Column(Name = "title", DataType = DataType.VarChar, Size = 255, NotNull = true)]
    public string Title { get; set; } = string.Empty;

    [Column(Name = "url", DataType = DataType.VarChar, Size = 500, NotNull = true)]
    public string Url { get; set; } = string.Empty;

    [Column(Name = "target", DataType = DataType.VarChar, Size = 20)]
    public string? Target { get; set; } // _blank, _self, etc.

    [Column(Name = "icon", DataType = DataType.VarChar, Size = 100)]
    public string? Icon { get; set; }

    [Column(Name = "css_class", DataType = DataType.VarChar, Size = 100)]
    public string? CssClass { get; set; }

    [Column(Name = "sort_order", DataType = DataType.Int, NotNull = true, DefaultValue = "0")]
    public int SortOrder { get; set; }

    [Column(Name = "is_active", DataType = DataType.TinyInt, NotNull = true, DefaultValue = "1", Index = true)]
    public bool IsActive { get; set; } = true;

    // Custom attributes (JSON)
    [Column(Name = "attributes", DataType = DataType.Text)]
    public string? Attributes { get; set; }

    // Timestamps
    [Column(Name = "created_at", DataType = DataType.DateTime, NotNull = true, DefaultValue = "CURRENT_TIMESTAMP")]
    public DateTime CreatedAt { get; set; }

    [Column(Name = "updated_at", DataType = DataType.DateTime, NotNull = true, DefaultValue = "CURRENT_TIMESTAMP", OnUpdateCurrentTimestamp = true)]
    public DateTime UpdatedAt { get; set; }
}
