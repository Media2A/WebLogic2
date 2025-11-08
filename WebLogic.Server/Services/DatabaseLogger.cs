using CL.MySQL2;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using System.Text.Json;
using WebLogic.Server.Core.Configuration;
using WebLogic.Server.Models.Database;
using LogLevel = WebLogic.Server.Models.Database.LogLevel;

namespace WebLogic.Server.Services;

/// <summary>
/// Centralized database logging service with category-based filtering
/// </summary>
public class DatabaseLogger
{
    private readonly MySQL2Library _mysql;
    private readonly WebLogicServerOptions _options;
    private readonly IHttpContextAccessor? _httpContextAccessor;
    private const string ConnectionId = "Default";

    public DatabaseLogger(
        MySQL2Library mysql,
        IOptions<WebLogicServerOptions> options,
        IHttpContextAccessor? httpContextAccessor = null)
    {
        _mysql = mysql;
        _options = options.Value;
        _httpContextAccessor = httpContextAccessor;
    }

    /// <summary>
    /// Log a message to the database if enabled for the category and level
    /// </summary>
    public async Task LogAsync(
        LogCategory category,
        LogLevel level,
        string message,
        object? details = null,
        Guid? userId = null,
        string? username = null,
        string? source = null,
        Exception? exception = null)
    {
        // Check if database logging is enabled
        if (!_options.EnableDatabaseLogging)
        {
            return;
        }

        // Check if this log level should be persisted
        if (level < _options.MinimumLogLevel)
        {
            return;
        }

        // Check if this category is enabled
        if (!IsCategoryEnabled(category))
        {
            return;
        }

        try
        {
            var httpContext = _httpContextAccessor?.HttpContext;

            var logEntry = new LogEntry
            {
                Id = Guid.NewGuid(),
                Category = category,
                Level = level,
                Message = message,
                Details = details != null ? JsonSerializer.Serialize(details) : null,
                UserId = userId,
                Username = username,
                IpAddress = httpContext?.Connection.RemoteIpAddress?.ToString(),
                UserAgent = httpContext?.Request.Headers["User-Agent"].ToString(),
                RequestPath = httpContext?.Request.Path.Value,
                HttpMethod = httpContext?.Request.Method,
                Exception = exception?.ToString(),
                Source = source,
                CreatedAt = DateTime.UtcNow
            };

            var repo = _mysql.GetRepository<LogEntry>(ConnectionId);
            await repo.InsertAsync(logEntry);
        }
        catch (Exception ex)
        {
            // Don't throw - logging failures shouldn't break the application
            Console.WriteLine($"[DatabaseLogger] Failed to log: {ex.Message}");
        }
    }

    /// <summary>
    /// Log with HTTP context information
    /// </summary>
    public async Task LogHttpAsync(
        LogCategory category,
        LogLevel level,
        string message,
        int? statusCode = null,
        int? durationMs = null,
        object? details = null,
        Guid? userId = null,
        string? username = null,
        string? source = null,
        Exception? exception = null)
    {
        if (!_options.EnableDatabaseLogging || level < _options.MinimumLogLevel || !IsCategoryEnabled(category))
        {
            return;
        }

        try
        {
            var httpContext = _httpContextAccessor?.HttpContext;

            var logEntry = new LogEntry
            {
                Id = Guid.NewGuid(),
                Category = category,
                Level = level,
                Message = message,
                Details = details != null ? JsonSerializer.Serialize(details) : null,
                UserId = userId,
                Username = username,
                IpAddress = httpContext?.Connection.RemoteIpAddress?.ToString(),
                UserAgent = httpContext?.Request.Headers["User-Agent"].ToString(),
                RequestPath = httpContext?.Request.Path.Value,
                HttpMethod = httpContext?.Request.Method,
                StatusCode = statusCode,
                DurationMs = durationMs,
                Exception = exception?.ToString(),
                Source = source,
                CreatedAt = DateTime.UtcNow
            };

            var repo = _mysql.GetRepository<LogEntry>(ConnectionId);
            await repo.InsertAsync(logEntry);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[DatabaseLogger] Failed to log: {ex.Message}");
        }
    }

    /// <summary>
    /// Log a security event
    /// </summary>
    public Task LogSecurityAsync(
        LogLevel level,
        string message,
        Guid? userId = null,
        string? username = null,
        object? details = null,
        string? source = null)
    {
        return LogAsync(LogCategory.Security, level, message, details, userId, username, source);
    }

    /// <summary>
    /// Log an authentication event
    /// </summary>
    public Task LogAuthenticationAsync(
        LogLevel level,
        string message,
        Guid? userId = null,
        string? username = null,
        object? details = null,
        string? source = null)
    {
        return LogAsync(LogCategory.Authentication, level, message, details, userId, username, source);
    }

    /// <summary>
    /// Log a rate limit event
    /// </summary>
    public Task LogRateLimitAsync(
        LogLevel level,
        string message,
        string? ipAddress = null,
        object? details = null,
        string? source = null)
    {
        return LogAsync(LogCategory.RateLimit, level, message, details, null, null, source);
    }

    /// <summary>
    /// Log an API request
    /// </summary>
    public Task LogApiAsync(
        LogLevel level,
        string message,
        int? statusCode = null,
        int? durationMs = null,
        Guid? userId = null,
        object? details = null,
        string? source = null)
    {
        return LogHttpAsync(LogCategory.Api, level, message, statusCode, durationMs, details, userId, null, source);
    }

    /// <summary>
    /// Log a system event
    /// </summary>
    public Task LogSystemAsync(
        LogLevel level,
        string message,
        object? details = null,
        string? source = null,
        Exception? exception = null)
    {
        return LogAsync(LogCategory.System, level, message, details, null, null, source, exception);
    }

    /// <summary>
    /// Log a user action
    /// </summary>
    public Task LogUserActionAsync(
        string message,
        Guid userId,
        string? username = null,
        object? details = null,
        string? source = null)
    {
        return LogAsync(LogCategory.UserAction, LogLevel.Info, message, details, userId, username, source);
    }

    /// <summary>
    /// Log an extension event
    /// </summary>
    public Task LogExtensionAsync(
        LogLevel level,
        string message,
        string extensionId,
        object? details = null,
        Exception? exception = null)
    {
        return LogAsync(LogCategory.Extension, level, message, details, null, null, extensionId, exception);
    }

    /// <summary>
    /// Log a session event
    /// </summary>
    public Task LogSessionAsync(
        LogLevel level,
        string message,
        Guid? userId = null,
        string? username = null,
        object? details = null)
    {
        return LogAsync(LogCategory.Session, level, message, details, userId, username, "SessionManager");
    }

    /// <summary>
    /// Log an IP reputation check
    /// </summary>
    public Task LogIpReputationAsync(
        LogLevel level,
        string message,
        string ipAddress,
        object? details = null)
    {
        return LogAsync(LogCategory.IpReputation, level, message, details, null, null, "IpReputationService");
    }

    /// <summary>
    /// Check if a log category is enabled in configuration
    /// </summary>
    private bool IsCategoryEnabled(LogCategory category)
    {
        return category switch
        {
            LogCategory.Security => _options.LogSecurity,
            LogCategory.RateLimit => _options.LogRateLimit,
            LogCategory.Authentication => _options.LogAuthentication,
            LogCategory.Api => _options.LogApi,
            LogCategory.System => _options.LogSystem,
            LogCategory.Database => _options.LogDatabase,
            LogCategory.UserAction => _options.LogUserAction,
            LogCategory.Extension => _options.LogExtension,
            LogCategory.IpReputation => _options.LogIpReputation,
            LogCategory.Session => _options.LogSession,
            LogCategory.General => true, // Always enabled
            _ => false
        };
    }

    /// <summary>
    /// Get recent logs by category
    /// </summary>
    public async Task<List<LogEntry>> GetRecentLogsAsync(
        LogCategory? category = null,
        LogLevel? minLevel = null,
        int limit = 100,
        int offset = 0)
    {
        try
        {
            var repo = _mysql.GetRepository<LogEntry>(ConnectionId);
            var allLogs = await repo.GetAllAsync();

            if (!allLogs.Success || allLogs.Data == null)
            {
                return new List<LogEntry>();
            }

            var query = allLogs.Data.AsQueryable();

            if (category.HasValue)
            {
                query = query.Where(l => l.Category == category.Value);
            }

            if (minLevel.HasValue)
            {
                query = query.Where(l => l.Level >= minLevel.Value);
            }

            return query
                .OrderByDescending(l => l.CreatedAt)
                .Skip(offset)
                .Take(limit)
                .ToList();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[DatabaseLogger] Failed to get logs: {ex.Message}");
            return new List<LogEntry>();
        }
    }

    /// <summary>
    /// Clean up old logs based on retention period
    /// </summary>
    public async Task<int> CleanupOldLogsAsync()
    {
        if (!_options.EnableLogCleanup)
        {
            return 0;
        }

        try
        {
            var cutoffDate = DateTime.UtcNow.Subtract(_options.LogRetentionPeriod);
            var repo = _mysql.GetRepository<LogEntry>(ConnectionId);

            // Get all logs and filter in memory (MySQL2Library limitation)
            var allLogs = await repo.GetAllAsync();
            if (!allLogs.Success || allLogs.Data == null)
            {
                return 0;
            }

            var oldLogs = allLogs.Data.Where(l => l.CreatedAt < cutoffDate).ToList();
            var deletedCount = 0;

            foreach (var log in oldLogs)
            {
                var result = await repo.DeleteAsync(log.Id);
                if (result.Success)
                {
                    deletedCount++;
                }
            }

            Console.WriteLine($"[DatabaseLogger] Cleaned up {deletedCount} old logs (older than {cutoffDate:yyyy-MM-dd})");
            return deletedCount;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[DatabaseLogger] Failed to cleanup logs: {ex.Message}");
            return 0;
        }
    }
}
