namespace WebLogic.Server.Core.Configuration;

/// <summary>
/// Configuration options for WebLogic.Server
/// </summary>
public class WebLogicServerOptions
{
    // ============================================================================
    // General Settings
    // ============================================================================

    /// <summary>
    /// Server display name
    /// </summary>
    public string ServerName { get; set; } = "WebLogic.Server";

    /// <summary>
    /// Enable debug mode (detailed logging, exception details)
    /// </summary>
    public bool EnableDebugMode { get; set; } = false;

    // ============================================================================
    // Security Settings
    // ============================================================================

    /// <summary>
    /// Enable HTTPS redirect
    /// </summary>
    public bool EnableHttpsRedirect { get; set; } = true;

    /// <summary>
    /// Enable global rate limiting
    /// </summary>
    public bool EnableRateLimiting { get; set; } = true;

    /// <summary>
    /// Maximum requests per window (global)
    /// </summary>
    public int GlobalRateLimit { get; set; } = 1000;

    /// <summary>
    /// Rate limit time window
    /// </summary>
    public TimeSpan RateLimitWindow { get; set; } = TimeSpan.FromMinutes(1);

    /// <summary>
    /// Ban duration for rate limit violations
    /// </summary>
    public TimeSpan RateLimitBanDuration { get; set; } = TimeSpan.FromMinutes(10);

    /// <summary>
    /// Enable DNSBL (DNS Blacklist) checking using CL.NetUtils
    /// </summary>
    public bool EnableDnsblCheck { get; set; } = false;

    /// <summary>
    /// Enable IP geolocation logging using CL.NetUtils
    /// </summary>
    public bool EnableIpGeolocation { get; set; } = false;

    // ============================================================================
    // Authentication Security Settings
    // ============================================================================

    /// <summary>
    /// Maximum failed login attempts before account lockout
    /// </summary>
    public int MaxFailedLoginAttempts { get; set; } = 5;

    /// <summary>
    /// Account lockout duration after max failed attempts
    /// </summary>
    public TimeSpan AccountLockoutDuration { get; set; } = TimeSpan.FromMinutes(15);

    /// <summary>
    /// Enable login attempt logging to database (for security auditing)
    /// </summary>
    public bool EnableLoginAttemptLogging { get; set; } = true;

    /// <summary>
    /// Automatically clean up old login attempt logs after specified duration
    /// </summary>
    public TimeSpan LoginAttemptLogRetention { get; set; } = TimeSpan.FromDays(90);

    /// <summary>
    /// Enable automatic cleanup of expired login attempt logs
    /// </summary>
    public bool EnableLoginAttemptLogCleanup { get; set; } = true;

    // ============================================================================
    // Database Logging Settings
    // ============================================================================

    /// <summary>
    /// Enable centralized database logging
    /// </summary>
    public bool EnableDatabaseLogging { get; set; } = true;

    /// <summary>
    /// Enable Security category logging (login attempts, access violations)
    /// </summary>
    public bool LogSecurity { get; set; } = true;

    /// <summary>
    /// Enable Rate Limit category logging
    /// </summary>
    public bool LogRateLimit { get; set; } = true;

    /// <summary>
    /// Enable Authentication category logging
    /// </summary>
    public bool LogAuthentication { get; set; } = true;

    /// <summary>
    /// Enable API category logging (requests/responses)
    /// </summary>
    public bool LogApi { get; set; } = false;

    /// <summary>
    /// Enable System category logging (startup, shutdown, errors)
    /// </summary>
    public bool LogSystem { get; set; } = true;

    /// <summary>
    /// Enable Database category logging (queries, migrations)
    /// </summary>
    public bool LogDatabase { get; set; } = false;

    /// <summary>
    /// Enable User Action category logging (CRUD operations)
    /// </summary>
    public bool LogUserAction { get; set; } = true;

    /// <summary>
    /// Enable Extension category logging
    /// </summary>
    public bool LogExtension { get; set; } = true;

    /// <summary>
    /// Enable IP Reputation category logging (DNSBL checks)
    /// </summary>
    public bool LogIpReputation { get; set; } = false;

    /// <summary>
    /// Enable Session category logging
    /// </summary>
    public bool LogSession { get; set; } = false;

    /// <summary>
    /// Minimum log level to persist to database
    /// </summary>
    public Models.Database.LogLevel MinimumLogLevel { get; set; } = Models.Database.LogLevel.Info;

    /// <summary>
    /// Log retention period (logs older than this will be cleaned up)
    /// </summary>
    public TimeSpan LogRetentionPeriod { get; set; } = TimeSpan.FromDays(90);

    /// <summary>
    /// Enable automatic cleanup of old logs
    /// </summary>
    public bool EnableLogCleanup { get; set; } = true;

    // ============================================================================
    // Feature Toggles
    // ============================================================================

    /// <summary>
    /// Enable CMS features
    /// </summary>
    public bool EnableCMS { get; set; } = true;

    /// <summary>
    /// Enable API system
    /// </summary>
    public bool EnableAPI { get; set; } = true;

    /// <summary>
    /// Enable session tracking in database
    /// </summary>
    public bool EnableSessionTracking { get; set; } = true;

    /// <summary>
    /// Session timeout duration
    /// </summary>
    public TimeSpan SessionTimeout { get; set; } = TimeSpan.FromHours(1);

    /// <summary>
    /// Enable cron job system
    /// </summary>
    public bool EnableCronJobs { get; set; } = true;

    // ============================================================================
    // CORS Settings
    // ============================================================================

    /// <summary>
    /// Allowed CORS origins (* for wildcard)
    /// </summary>
    public string[] AllowedOrigins { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Allow credentials in CORS requests
    /// </summary>
    public bool AllowCredentials { get; set; } = true;

    // ============================================================================
    // API Settings
    // ============================================================================

    /// <summary>
    /// Dedicated API hostname (e.g., "api.example.com")
    /// If null, API is accessible at /api/*
    /// </summary>
    public string? ApiHostname { get; set; }

    /// <summary>
    /// Enable API explorer page
    /// </summary>
    public bool EnableApiExplorer { get; set; } = true;

    /// <summary>
    /// Enable API discovery endpoint
    /// </summary>
    public bool EnableApiDiscovery { get; set; } = true;

    // ============================================================================
    // Storage Settings
    // ============================================================================

    /// <summary>
    /// Default storage provider (FileSystem, S3, etc.)
    /// </summary>
    public string DefaultStorageProvider { get; set; } = "FileSystem";

    /// <summary>
    /// Theme storage provider
    /// </summary>
    public string ThemeStorageProvider { get; set; } = "FileSystem";

    /// <summary>
    /// Base path for file system storage
    /// </summary>
    public string FileStorageBasePath { get; set; } = "storage";

    /// <summary>
    /// Storage configuration
    /// </summary>
    public StorageConfiguration Storage { get; set; } = new();

    // ============================================================================
    // Cookie Settings
    // ============================================================================

    /// <summary>
    /// Cookie domain (null for current domain)
    /// </summary>
    public string? CookieDomain { get; set; }

    /// <summary>
    /// Session cookie name
    /// </summary>
    public string SessionCookieName { get; set; } = "weblogic.session";

    // ============================================================================
    // Extension Settings
    // ============================================================================

    /// <summary>
    /// Auto-load all discovered extensions on startup
    /// </summary>
    public bool AutoLoadExtensions { get; set; } = true;

    /// <summary>
    /// Auto-sync database tables for extensions
    /// </summary>
    public bool AutoSyncDatabaseTables { get; set; } = true;

    // ============================================================================
    // Database Settings
    // ============================================================================

    /// <summary>
    /// Default database connection ID for core tables
    /// </summary>
    public string DefaultDatabaseConnectionId { get; set; } = "Default";

    /// <summary>
    /// Table name prefix (e.g., "wls_")
    /// </summary>
    public string TablePrefix { get; set; } = "wls_";
}
