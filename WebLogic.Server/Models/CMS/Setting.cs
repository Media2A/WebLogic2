using CL.MySQL2.Models;

namespace WebLogic.Server.Models.CMS;

/// <summary>
/// CMS Setting model for site-wide configuration
/// </summary>
[Table(Name = "wls_settings", Engine = TableEngine.InnoDB, Charset = Charset.Utf8mb4)]
[CompositeIndex("idx_setting_group", "setting_group", "setting_key")]
public class Setting
{
    [Column(Name = "id", DataType = DataType.Int, Primary = true, AutoIncrement = true)]
    public int Id { get; set; }

    [Column(Name = "setting_group", DataType = DataType.VarChar, Size = 100, NotNull = true, Index = true)]
    public string SettingGroup { get; set; } = string.Empty; // general, seo, email, social, etc.

    [Column(Name = "setting_key", DataType = DataType.VarChar, Size = 100, NotNull = true, Unique = true, Index = true)]
    public string SettingKey { get; set; } = string.Empty;

    [Column(Name = "setting_value", DataType = DataType.Text)]
    public string? SettingValue { get; set; }

    [Column(Name = "value_type", DataType = DataType.VarChar, Size = 50, NotNull = true, DefaultValue = "string")]
    public string ValueType { get; set; } = "string"; // string, int, bool, json, array

    [Column(Name = "description", DataType = DataType.VarChar, Size = 500)]
    public string? Description { get; set; }

    [Column(Name = "is_public", DataType = DataType.TinyInt, NotNull = true, DefaultValue = "0")]
    public bool IsPublic { get; set; } // Can be accessed by frontend

    [Column(Name = "is_encrypted", DataType = DataType.TinyInt, NotNull = true, DefaultValue = "0")]
    public bool IsEncrypted { get; set; } // For sensitive data

    [Column(Name = "sort_order", DataType = DataType.Int, NotNull = true, DefaultValue = "0")]
    public int SortOrder { get; set; }

    // Timestamps
    [Column(Name = "created_at", DataType = DataType.DateTime, NotNull = true, DefaultValue = "CURRENT_TIMESTAMP")]
    public DateTime CreatedAt { get; set; }

    [Column(Name = "updated_at", DataType = DataType.DateTime, NotNull = true, DefaultValue = "CURRENT_TIMESTAMP", OnUpdateCurrentTimestamp = true)]
    public DateTime UpdatedAt { get; set; }
}
