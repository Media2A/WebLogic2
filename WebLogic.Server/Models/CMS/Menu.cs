using CL.MySQL2.Models;

namespace WebLogic.Server.Models.CMS;

/// <summary>
/// CMS Menu model for navigation menus
/// </summary>
[Table(Name = "wls_menus", Engine = TableEngine.InnoDB, Charset = Charset.Utf8mb4)]
public class Menu
{
    [Column(Name = "id", DataType = DataType.Int, Primary = true, AutoIncrement = true)]
    public int Id { get; set; }

    [Column(Name = "menu_key", DataType = DataType.VarChar, Size = 100, NotNull = true, Unique = true, Index = true)]
    public string MenuKey { get; set; } = string.Empty;

    [Column(Name = "name", DataType = DataType.VarChar, Size = 255, NotNull = true)]
    public string Name { get; set; } = string.Empty;

    [Column(Name = "description", DataType = DataType.VarChar, Size = 500)]
    public string? Description { get; set; }

    [Column(Name = "location", DataType = DataType.VarChar, Size = 100)]
    public string? Location { get; set; } // header, footer, sidebar, etc.

    [Column(Name = "is_active", DataType = DataType.TinyInt, NotNull = true, DefaultValue = "1", Index = true)]
    public bool IsActive { get; set; } = true;

    // Timestamps
    [Column(Name = "created_at", DataType = DataType.DateTime, NotNull = true, DefaultValue = "CURRENT_TIMESTAMP")]
    public DateTime CreatedAt { get; set; }

    [Column(Name = "updated_at", DataType = DataType.DateTime, NotNull = true, DefaultValue = "CURRENT_TIMESTAMP", OnUpdateCurrentTimestamp = true)]
    public DateTime UpdatedAt { get; set; }
}
