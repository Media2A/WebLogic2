using CL.MySQL2.Models;

namespace WebLogic.Server.Models.Database;

/// <summary>
/// Database model for route definitions
/// Table: wls_routes
/// </summary>
[Table(Name = "wls_routes", Engine = TableEngine.InnoDB, Charset = Charset.Utf8mb4)]
[CompositeIndex("idx_route_extension", "extension_id", "is_active")]
public class Route
{
    [Column(Name = "id", DataType = DataType.Int, Primary = true, AutoIncrement = true)]
    public int Id { get; set; }

    [Column(Name = "route_path", DataType = DataType.VarChar, Size = 500, NotNull = true, Unique = true, Index = true)]
    public string RoutePath { get; set; } = string.Empty;

    [Column(Name = "route_type", DataType = DataType.VarChar, Size = 50, NotNull = true)]
    public string RouteType { get; set; } = string.Empty;  // page, component, js, css, api

    [Column(Name = "handler_data", DataType = DataType.Text, NotNull = true)]
    public string HandlerData { get; set; } = string.Empty;  // JSON data for handler

    [Column(Name = "extension_id", DataType = DataType.VarChar, Size = 100, NotNull = true, Index = true)]
    public string ExtensionId { get; set; } = string.Empty;

    [Column(Name = "priority", DataType = DataType.Int, DefaultValue = "0")]
    public int Priority { get; set; }

    [Column(Name = "access_count", DataType = DataType.BigInt, DefaultValue = "0", Unsigned = true)]
    public long AccessCount { get; set; }

    [Column(Name = "is_active", DataType = DataType.TinyInt, DefaultValue = "1")]
    public bool IsActive { get; set; } = true;

    [Column(Name = "created_at", DataType = DataType.DateTime, NotNull = true, DefaultValue = "CURRENT_TIMESTAMP")]
    public DateTime CreatedAt { get; set; }

    [Column(Name = "updated_at", DataType = DataType.DateTime, NotNull = true, DefaultValue = "CURRENT_TIMESTAMP", OnUpdateCurrentTimestamp = true)]
    public DateTime UpdatedAt { get; set; }
}
