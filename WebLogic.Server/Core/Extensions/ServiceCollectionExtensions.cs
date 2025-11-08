using CL.MySQL2;
using CodeLogic;
using WebLogic.Server.Core.Configuration;
using WebLogic.Shared.Abstractions;

namespace WebLogic.Server.Core.Extensions;

/// <summary>
/// Extension methods for registering WebLogic services
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Add WebLogic.Server services to the DI container
    /// </summary>
    public static IServiceCollection AddWebLogicServer(
        this IServiceCollection services,
        Action<WebLogicServerOptions>? configure = null)
    {
        // Create and configure options
        var options = new WebLogicServerOptions();
        configure?.Invoke(options);

        // Register options using IOptions pattern
        services.AddSingleton(options);
        services.Configure<WebLogicServerOptions>(opts =>
        {
            opts.ServerName = options.ServerName;
            opts.EnableDebugMode = options.EnableDebugMode;
            opts.EnableHttpsRedirect = options.EnableHttpsRedirect;
            opts.EnableRateLimiting = options.EnableRateLimiting;
            opts.GlobalRateLimit = options.GlobalRateLimit;
            opts.RateLimitWindow = options.RateLimitWindow;
            opts.RateLimitBanDuration = options.RateLimitBanDuration;
            opts.EnableDnsblCheck = options.EnableDnsblCheck;
            opts.EnableIpGeolocation = options.EnableIpGeolocation;
            opts.MaxFailedLoginAttempts = options.MaxFailedLoginAttempts;
            opts.AccountLockoutDuration = options.AccountLockoutDuration;
            opts.EnableLoginAttemptLogging = options.EnableLoginAttemptLogging;
            opts.LoginAttemptLogRetention = options.LoginAttemptLogRetention;
            opts.EnableLoginAttemptLogCleanup = options.EnableLoginAttemptLogCleanup;
            opts.EnableCMS = options.EnableCMS;
            opts.EnableAPI = options.EnableAPI;
            opts.EnableSessionTracking = options.EnableSessionTracking;
            opts.SessionTimeout = options.SessionTimeout;
            opts.EnableCronJobs = options.EnableCronJobs;
            opts.AllowedOrigins = options.AllowedOrigins;
            opts.AllowCredentials = options.AllowCredentials;
            opts.ApiHostname = options.ApiHostname;
            opts.EnableApiExplorer = options.EnableApiExplorer;
            opts.EnableApiDiscovery = options.EnableApiDiscovery;
            opts.DefaultStorageProvider = options.DefaultStorageProvider;
            opts.ThemeStorageProvider = options.ThemeStorageProvider;
            opts.FileStorageBasePath = options.FileStorageBasePath;
            opts.Storage = options.Storage;
            opts.CookieDomain = options.CookieDomain;
            opts.SessionCookieName = options.SessionCookieName;
            opts.AutoLoadExtensions = options.AutoLoadExtensions;
            opts.AutoSyncDatabaseTables = options.AutoSyncDatabaseTables;
            opts.DefaultDatabaseConnectionId = options.DefaultDatabaseConnectionId;
            opts.TablePrefix = options.TablePrefix;
        });

        // Register Extension Manager
        services.AddSingleton<IExtensionManager, Server.Extensions.ExtensionManager>();

        // Register Route Manager
        services.AddSingleton<IRouteManager, Server.Extensions.RouteManager>();

        // Register Theme Manager
        services.AddSingleton<IThemeManager>(sp =>
        {
            // Use data/themes directory (CodeLogic data path)
            var themesDir = Path.Combine(AppContext.BaseDirectory, "data", "themes");
            return new Server.Services.ThemeManager(themesDir);
        });

        // Register Template Engine
        services.AddSingleton<ITemplateEngine>(sp =>
        {
            var themeManager = sp.GetRequiredService<IThemeManager>();
            var activeTheme = themeManager.GetActiveTheme();

            // Use active theme's templates path
            var templatesDir = activeTheme.TemplatesPath;
            return new Server.Services.TemplateEngine(templatesDir);
        });

        // Register Storage Provider
        services.AddSingleton<IStorageProvider>(sp =>
        {
            var storageConfig = options.Storage;

            if (storageConfig.DefaultProvider.Equals("local", StringComparison.OrdinalIgnoreCase))
            {
                var rootPath = Path.IsPathRooted(storageConfig.Local.RootPath)
                    ? storageConfig.Local.RootPath
                    : Path.Combine(Directory.GetCurrentDirectory(), storageConfig.Local.RootPath);

                return new Server.Services.LocalStorageProvider(rootPath, storageConfig.Local.BaseUrl);
            }

            // Default to local storage if provider not found
            var defaultPath = Path.Combine(Directory.GetCurrentDirectory(), "uploads");
            return new Server.Services.LocalStorageProvider(defaultPath, "/uploads");
        });

        // Register API Manager
        services.AddSingleton<IApiManager, Server.Services.ApiManager>();

        // Register API Explorer Service
        services.AddSingleton<Server.Services.ApiExplorerService>();

        // Register Auth Services
        services.AddSingleton<Server.Services.Auth.AuthService>();
        services.AddSingleton<Server.Services.Auth.UserService>();
        services.AddSingleton<Server.Services.Auth.AuthTemplateHelpers>();
        services.AddSingleton<Server.Services.Auth.AuthDataSeeder>();

        // Register Database Logger
        services.AddSingleton<Server.Services.DatabaseLogger>();

        return services;
    }

    /// <summary>
    /// Initialize API Explorer endpoints
    /// </summary>
    public static IServiceProvider InitializeApiExplorer(this IServiceProvider serviceProvider)
    {
        var options = serviceProvider.GetService<WebLogicServerOptions>();

        if (options?.EnableAPI == true && options.EnableApiExplorer)
        {
            Console.WriteLine("→ Initializing API Explorer...");

            var explorerService = serviceProvider.GetService<Server.Services.ApiExplorerService>();
            if (explorerService != null)
            {
                explorerService.RegisterEndpoints();
                Console.WriteLine("✓ API Explorer endpoints registered");
                Console.WriteLine("  → Discovery: GET /api/v1/discovery");
                Console.WriteLine("  → Explorer UI: GET /api/v1/explorer");
            }
            else
            {
                Console.WriteLine("⚠ ApiExplorerService not found");
            }

            Console.WriteLine();
        }

        return serviceProvider;
    }

    /// <summary>
    /// Configure services for all loaded extensions
    /// </summary>
    public static IServiceCollection ConfigureExtensionServices(
        this IServiceCollection services,
        IServiceProvider tempServiceProvider)
    {
        var extensionManager = tempServiceProvider.GetService<IExtensionManager>();
        if (extensionManager == null)
        {
            Console.WriteLine("⚠ ExtensionManager not found. Skipping extension service configuration.");
            return services;
        }

        var extensions = extensionManager.LoadedExtensions;
        if (extensions.Count == 0)
        {
            Console.WriteLine("→ No extensions loaded. Skipping extension service configuration.");
            return services;
        }

        Console.WriteLine($"→ Configuring services for {extensions.Count} extension(s)...");

        foreach (var extension in extensions)
        {
            try
            {
                var extensionId = extension.ExtensionManifest?.Id ?? "unknown";
                extension.ConfigureServices(services);
                Console.WriteLine($"  ✓ {extensionId} services configured");
            }
            catch (Exception ex)
            {
                var extensionId = extension.ExtensionManifest?.Id ?? "unknown";
                Console.WriteLine($"  ✗ {extensionId} service configuration failed: {ex.Message}");
            }
        }

        Console.WriteLine();

        return services;
    }

    /// <summary>
    /// Initialize WebLogic: Load extensions and sync database tables
    /// </summary>
    public static async Task<IServiceProvider> InitializeWebLogicDatabaseAsync(
        this IServiceProvider serviceProvider,
        string connectionId = "Default")
    {
        var mysql = serviceProvider.GetService<MySQL2Library>();
        var options = serviceProvider.GetService<WebLogicServerOptions>();
        var extensionManager = serviceProvider.GetService<IExtensionManager>();

        if (mysql == null)
        {
            Console.WriteLine("⚠ MySQL2Library not found in DI container. Skipping database initialization.");
            return serviceProvider;
        }

        if (options == null || !options.AutoSyncDatabaseTables)
        {
            Console.WriteLine("⚠ Auto-sync disabled. Skipping database table synchronization.");
            return serviceProvider;
        }

        Console.WriteLine("\n=== WebLogic Initialization ===\n");

        // Step 1: Load extensions
        if (extensionManager != null && options.AutoLoadExtensions)
        {
            Console.WriteLine("→ Loading extensions...");
            var loadResult = await extensionManager.LoadExtensionsAsync();

            if (loadResult.SuccessfullyLoaded > 0)
            {
                Console.WriteLine($"✓ Loaded {loadResult.SuccessfullyLoaded} extension(s)");
            }
            else
            {
                Console.WriteLine("  No extensions loaded");
            }

            if (loadResult.Failed > 0)
            {
                Console.WriteLine($"⚠ {loadResult.Failed} extension(s) failed to load");
                foreach (var error in loadResult.Errors)
                {
                    Console.WriteLine($"  ✗ {error.ExtensionId}: {error.ErrorMessage}");
                }
            }

            Console.WriteLine();
        }

        // Step 2: Sync core database tables
        Console.WriteLine($"→ Syncing core tables to connection '{connectionId}'...");

        try
        {
            var coreModelTypes = new[]
            {
                typeof(Models.Database.Session),
                typeof(Models.Session.SessionData),  // ASP.NET Core distributed cache sessions
                typeof(Models.Database.Route),
                typeof(Models.Database.CronJob),
                typeof(Models.Database.RateLimitEntry),
                typeof(Models.Database.LogEntry)
            };

            var results = await mysql.SyncTablesAsync(coreModelTypes, connectionId, createBackup: true);

            var successCount = results.Count(r => r.Value);
            var failedCount = results.Count(r => !r.Value);

            Console.WriteLine($"✓ Core tables synced: {successCount} succeeded, {failedCount} failed");

            if (failedCount > 0)
            {
                Console.WriteLine("\nFailed tables:");
                foreach (var failed in results.Where(r => !r.Value))
                {
                    Console.WriteLine($"  ✗ {failed.Key}");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"✗ Core table sync failed: {ex.Message}");
        }

        // Step 2a: Sync Auth tables (Users, Roles, Permissions, LoginAttempts)
        Console.WriteLine("\n→ Syncing Auth tables...");

        try
        {
            var authModelTypes = new[]
            {
                typeof(Models.Auth.User),
                typeof(Models.Auth.Role),
                typeof(Models.Auth.Permission),
                typeof(Models.Auth.UserRole),
                typeof(Models.Auth.RolePermission),
                typeof(Models.Auth.LoginAttempt),
                typeof(Models.Session.SessionData),
                typeof(Models.Database.LogEntry)
            };

            var authResults = await mysql.SyncTablesAsync(authModelTypes, connectionId, createBackup: true);

            var authSuccessCount = authResults.Count(r => r.Value);
            var authFailedCount = authResults.Count(r => !r.Value);

            Console.WriteLine($"✓ Auth tables synced: {authSuccessCount} succeeded, {authFailedCount} failed");

            if (authFailedCount > 0)
            {
                Console.WriteLine("\nFailed Auth tables:");
                foreach (var failed in authResults.Where(r => !r.Value))
                {
                    Console.WriteLine($"  ✗ {failed.Key}");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"✗ Auth table sync failed: {ex.Message}");
        }

        // Step 2b: Sync CMS tables
        if (options.EnableCMS)
        {
            Console.WriteLine("\n→ Syncing CMS tables...");

            try
            {
                var cmsModelTypes = new[]
                {
                    typeof(Models.CMS.Page),
                    typeof(Models.CMS.Media),
                    typeof(Models.CMS.ContentBlock),
                    typeof(Models.CMS.Menu),
                    typeof(Models.CMS.MenuItem),
                    typeof(Models.CMS.Setting)
                };

                var cmsResults = await mysql.SyncTablesAsync(cmsModelTypes, connectionId, createBackup: true);

                var cmsSuccessCount = cmsResults.Count(r => r.Value);
                var cmsFailedCount = cmsResults.Count(r => !r.Value);

                Console.WriteLine($"✓ CMS tables synced: {cmsSuccessCount} succeeded, {cmsFailedCount} failed");

                if (cmsFailedCount > 0)
                {
                    Console.WriteLine("\nFailed CMS tables:");
                    foreach (var failed in cmsResults.Where(r => !r.Value))
                    {
                        Console.WriteLine($"  ✗ {failed.Key}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ CMS table sync failed: {ex.Message}");
            }
        }

        // Step 3: Sync extension database tables
        if (extensionManager != null)
        {
            Console.WriteLine("\n→ Syncing extension tables...");

            try
            {
                var extensionModels = extensionManager.GetAllDatabaseModels().ToArray();

                if (extensionModels.Length > 0)
                {
                    var extensionResults = await mysql.SyncTablesAsync(extensionModels, connectionId, createBackup: true);

                    var extSuccessCount = extensionResults.Count(r => r.Value);
                    var extFailedCount = extensionResults.Count(r => !r.Value);

                    Console.WriteLine($"✓ Extension tables synced: {extSuccessCount} succeeded, {extFailedCount} failed");

                    if (extFailedCount > 0)
                    {
                        Console.WriteLine("\nFailed extension tables:");
                        foreach (var failed in extensionResults.Where(r => !r.Value))
                        {
                            Console.WriteLine($"  ✗ {failed.Key}");
                        }
                    }
                }
                else
                {
                    Console.WriteLine("  No extension database models to sync");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Extension table sync failed: {ex.Message}");
            }
        }

        // Step 4: Register extension API endpoints
        var apiManager = serviceProvider.GetService<IApiManager>();
        if (apiManager != null && extensionManager != null && extensionManager.LoadedExtensions.Count > 0 && options.EnableAPI)
        {
            Console.WriteLine("\n→ Registering extension API endpoints...");
            try
            {
                foreach (var extension in extensionManager.LoadedExtensions)
                {
                    var extensionId = extension.ExtensionManifest?.Id ?? "unknown";
                    extension.RegisterAPIs(apiManager);
                }

                var totalEndpoints = apiManager.GetAllEndpoints().Count;
                Console.WriteLine($"✓ {totalEndpoints} API endpoint(s) registered");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ API registration failed: {ex.Message}");
            }
        }

        // Step 5: Register extension routes
        var routeManager = serviceProvider.GetService<IRouteManager>();
        if (routeManager != null && extensionManager != null && extensionManager.LoadedExtensions.Count > 0)
        {
            Console.WriteLine("\n→ Registering extension routes...");
            try
            {
                await routeManager.RegisterExtensionRoutesAsync();

                var totalRoutes = routeManager.GetAllRoutes().Count;
                Console.WriteLine($"✓ {totalRoutes} route(s) registered");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Route registration failed: {ex.Message}");
            }
        }

        Console.WriteLine();

        return serviceProvider;
    }
}
