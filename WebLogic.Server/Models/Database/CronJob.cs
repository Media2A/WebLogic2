using CL.MySQL2.Models;

namespace WebLogic.Server.Models.Database;

/// <summary>
/// Database model for cron job definitions
/// Table: wls_cron_jobs
/// </summary>
[Table(Name = "wls_cron_jobs", Engine = TableEngine.InnoDB, Charset = Charset.Utf8mb4)]
[CompositeIndex("idx_enabled_next", "is_enabled", "next_execution")]
public class CronJob
{
    [Column(Name = "id", DataType = DataType.Int, Primary = true, AutoIncrement = true)]
    public int Id { get; set; }

    [Column(Name = "name", DataType = DataType.VarChar, Size = 255, NotNull = true, Index = true)]
    public string Name { get; set; } = string.Empty;

    [Column(Name = "description", DataType = DataType.Text)]
    public string? Description { get; set; }

    [Column(Name = "job_type", DataType = DataType.VarChar, Size = 50, NotNull = true)]
    public string JobType { get; set; } = string.Empty;  // CRON, RUN_ONCE, DATE

    [Column(Name = "schedule", DataType = DataType.VarChar, Size = 100)]
    public string? Schedule { get; set; }  // Cron expression or datetime

    [Column(Name = "handler_type", DataType = DataType.VarChar, Size = 500, NotNull = true)]
    public string HandlerType { get; set; } = string.Empty;  // Full type name

    [Column(Name = "handler_method", DataType = DataType.VarChar, Size = 100, NotNull = true)]
    public string HandlerMethod { get; set; } = string.Empty;

    [Column(Name = "parameters", DataType = DataType.Text)]
    public string? Parameters { get; set; }  // JSON parameters

    [Column(Name = "extension_id", DataType = DataType.VarChar, Size = 100, Index = true)]
    public string? ExtensionId { get; set; }

    [Column(Name = "is_enabled", DataType = DataType.TinyInt, DefaultValue = "1")]
    public bool IsEnabled { get; set; } = true;

    [Column(Name = "execution_count", DataType = DataType.Int, DefaultValue = "0", Unsigned = true)]
    public int ExecutionCount { get; set; }

    [Column(Name = "last_execution", DataType = DataType.DateTime)]
    public DateTime? LastExecution { get; set; }

    [Column(Name = "last_result", DataType = DataType.Text)]
    public string? LastResult { get; set; }

    [Column(Name = "next_execution", DataType = DataType.DateTime)]
    public DateTime? NextExecution { get; set; }

    [Column(Name = "created_at", DataType = DataType.DateTime, NotNull = true, DefaultValue = "CURRENT_TIMESTAMP")]
    public DateTime CreatedAt { get; set; }

    [Column(Name = "updated_at", DataType = DataType.DateTime, NotNull = true, DefaultValue = "CURRENT_TIMESTAMP", OnUpdateCurrentTimestamp = true)]
    public DateTime UpdatedAt { get; set; }
}
