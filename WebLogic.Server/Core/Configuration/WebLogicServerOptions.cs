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
