using CodeLogic;
using CodeLogic.Models;
using CodeLogic.Startup;
using CodeLogic.Discovery;
using CL.Core.Utilities;
using CL.MySQL2;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;
using System.IO.Compression;
using WebLogic.Server.Core.Extensions;
using WebLogic.Server.Services;
using WebLogic.Shared.Abstractions;

var builder = WebApplication.CreateBuilder(args);

// ============================================================================
// STEP 1: Initialize CodeLogic2 Framework
// ============================================================================

Console.WriteLine("=".PadRight(80, '='));
Console.WriteLine("WebLogic.Server - Modern ASP.NET Core Web Application Framework");
Console.WriteLine("=".PadRight(80, '='));
Console.WriteLine();

Console.WriteLine("Initializing CodeLogic Framework...\n");
var options = FrameworkOptions.CreateDefault(AppDomain.CurrentDomain.BaseDirectory);
var framework = new Framework(options);
var initResult = await framework.InitializeAsync();
if (!initResult.IsSuccess)
{
    Console.WriteLine($"❌ Framework initialization failed: {initResult.ErrorMessage}");
    return;
}

// ============================================================================
// STEP 2: Load Core Libraries (like MySQL2)
// ============================================================================

var loadResult = await framework.LoadLibraryAsync("cl.mysql2");
if (!loadResult.IsSuccess)
{
    Console.WriteLine($"❌ Failed to load MySQL2Library: {loadResult.ErrorMessage}");
    return;
}

var mysql2 = framework.Libraries.GetLibrary("cl.mysql2") as MySQL2Library;
if (mysql2 == null)
{
    Console.WriteLine("❌ Failed to retrieve MySQL2Library instance after loading.");
    return;
}

// ============================================================================
// STEP 3: Register Core Services and Libraries with ASP.NET Core DI
// ============================================================================

builder.Services.AddSingleton(framework);
builder.Services.AddSingleton(framework.Configuration);
builder.Services.AddSingleton(framework.Cache);
builder.Services.AddSingleton(framework.Logger);
builder.Services.AddSingleton(framework.Libraries);
builder.Services.AddSingleton(mysql2); // <-- Register the loaded library

// Keep the existing manual NetUtils initialization for now
CL.NetUtils.NetUtilsLibrary? netUtils = null;
var enableNetUtils = true; // Set to false to disable
if (enableNetUtils)
{
    netUtils = new CL.NetUtils.NetUtilsLibrary();
    // ... (rest of netUtils initialization as it was)
    builder.Services.AddSingleton(netUtils);
}

// ============================================================================
// STEP 4: Register and Discover External Modules
// ============================================================================

var extensionsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "libraries");
framework.RegisterDiscoveryProvider(
    new FileSystemLibraryDiscoveryProvider(
        extensionsPath,
        "WebLogic.Modules.*.dll",
        framework.Logger
    )
);
await framework.DiscoverAndLoadLibrariesAsync();


// ============================================================================
// STEP 5: Add WebLogic.Server services
// ============================================================================

builder.Services.AddWebLogicServer(options =>
{
    options.ServerName = "WebLogic.Server Demo";
    options.EnableDebugMode = true;
    options.EnableHttpsRedirect = false;  // Disabled for local dev
    options.EnableRateLimiting = true;
    options.GlobalRateLimit = 100;
    options.RateLimitWindow = TimeSpan.FromMinutes(1);
    options.EnableDnsblCheck = false;  // Set to true to enable DNSBL checking
    options.EnableIpGeolocation = false;  // Set to true to enable IP geolocation logging
    options.EnableSessionTracking = true;
    options.EnableCMS = true;
    options.EnableAPI = true;
    options.AutoLoadExtensions = true;
    options.AutoSyncDatabaseTables = true;

    // Authentication Security Settings
    options.MaxFailedLoginAttempts = 5;
    options.AccountLockoutDuration = TimeSpan.FromMinutes(15);
    options.EnableLoginAttemptLogging = true;
    options.LoginAttemptLogRetention = TimeSpan.FromDays(90);
    options.EnableLoginAttemptLogCleanup = true;

    // Database Logging Settings
    options.EnableDatabaseLogging = true;
    options.LogSecurity = true;
    options.LogAuthentication = true;
    options.LogRateLimit = true;
    options.LogSystem = true;
    options.LogUserAction = true;
    options.LogExtension = true;
    options.LogApi = false;              // Can be verbose
    options.LogDatabase = false;         // Can be verbose
    options.LogSession = false;          // Can be verbose
    options.LogIpReputation = false;     // Only if using DNSBL
    options.MinimumLogLevel = WebLogic.Server.Models.Database.LogLevel.Info;
    options.LogRetentionPeriod = TimeSpan.FromDays(90);
    options.EnableLogCleanup = true;
});

// ============================================================================
// STEP 6: Configure Kestrel
// ============================================================================

builder.WebHost.ConfigureKestrel(serverOptions =>
{
    serverOptions.AddServerHeader = false;
    serverOptions.Limits.Http2.MaxStreamsPerConnection = 100;
    serverOptions.Limits.MaxRequestBodySize = 100 * 1024 * 1024; // 100MB
});

// ============================================================================
// STEP 7: Add ASP.NET Core services
// ============================================================================

builder.Services.AddHttpContextAccessor();

// Session configuration - MySQL-backed for persistence
builder.Services.AddSingleton<IDistributedCache, MySQL2DistributedCache>();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(1);
    options.Cookie.Name = "weblogic.session";
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest; // Allow HTTP in dev, HTTPS in prod
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.IsEssential = true;
});

// Response compression
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
    options.Providers.Add<BrotliCompressionProvider>();
    options.Providers.Add<GzipCompressionProvider>();
});

builder.Services.Configure<BrotliCompressionProviderOptions>(options =>
{
    options.Level = System.IO.Compression.CompressionLevel.Fastest;
});

builder.Services.Configure<GzipCompressionProviderOptions>(options =>
{
    options.Level = System.IO.Compression.CompressionLevel.Fastest;
});

// ============================================================================
// STEP 8: Build the application
// ============================================================================

var app = builder.Build();

// ============================================================================
// STEP 9: Configure CLU_Web from CL.Core
// ============================================================================

var httpContextAccessor = app.Services.GetRequiredService<IHttpContextAccessor>();
CLU_Web.Configure(httpContextAccessor);

// ============================================================================
// STEP 10: Invoke OnApplicationStarting lifecycle hooks
// ============================================================================

await framework.InvokeApplicationStartingAsync(app.Services);

// ============================================================================
// STEP 11: Initialize WebLogic database
// ============================================================================

await app.Services.InitializeWebLogicDatabaseAsync("Default");

// ============================================================================
// STEP 12: Initialize API Explorer
// ============================================================================

app.Services.InitializeApiExplorer();

// ============================================================================
// STEP 13: Initialize Auth Template Helpers
// ============================================================================

var authTemplateHelpers = app.Services.GetRequiredService<WebLogic.Server.Services.Auth.AuthTemplateHelpers>();
authTemplateHelpers.RegisterHelpers();
Console.WriteLine("✓ Auth template helpers registered");

// ============================================================================
// STEP 14: Seed Auth Data
// ============================================================================

var authDataSeeder = app.Services.GetRequiredService<WebLogic.Server.Services.Auth.AuthDataSeeder>();
await authDataSeeder.SeedAsync();

// ============================================================================
// STEP 15: Invoke OnApplicationStarted lifecycle hooks
// ============================================================================

Console.WriteLine("\n→ Invoking OnApplicationStarted lifecycle hooks...");
await framework.InvokeApplicationStartedAsync(app.Services);
Console.WriteLine("✓ OnApplicationStarted lifecycle hooks completed");

// ============================================================================
// STEP 16: Configure middleware pipeline
// ============================================================================

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

app.UseResponseCompression();
app.UseWebLogicHttpsRedirect();       // HTTPS redirect (if enabled)
app.UseWebLogicCors();                // CORS handling
app.UseSession();                     // ASP.NET Core sessions
app.UseWebLogicSessionTracking();     // Database session tracking
app.UseMiddleware<WebLogic.Server.Core.Middleware.AuthenticationMiddleware>(); // Authentication
app.UseWebLogicSecurity();            // Rate limiting & IP filtering
app.UseApiRouter();                   // API endpoint routing
app.UseWebLogicExtensionRouter();     // Extension route handling

// ============================================================================
// STEP 17: Add WebLogic test route (fallback)
// ============================================================================

app.MapGet("/", async () =>
{
    var html = @"<!DOCTYPE html>
<html>
<head>
    <title>WebLogic.Server</title>
    <style>
        body {
            font-family: system-ui, -apple-system, sans-serif;
            max-width: 800px;
            margin: 50px auto;
            padding: 20px;
            background: #f5f5f5;
        }
        .container {
            background: white;
            padding: 40px;
            border-radius: 8px;
            box-shadow: 0 2px 10px rgba(0,0,0,0.1);
        }
        h1 {
            color: #333;
            border-bottom: 33px solid #4CAF50;
            padding-bottom: 10px;
        }
        .status {
            background: #e8f5e9;
            padding: 15px;
            border-radius: 4px;
            margin: 20px 0;
        }
        .status-item {
            display: flex;
            justify-content: space-between;
            padding: 5px 0;
        }
        .success {
            color: #4CAF50;
            font-weight: bold;
        }
        code {
            background: #f5f5f5;
            padding: 2px 6px;
            border-radius: 3px;
            font-family: 'Courier New', monospace;
        }
    </style>
</head>
<body>
    <div class='container'>
        <h1>🚀 WebLogic.Server</h1>
        <p>Modern ASP.NET Core Web Application Framework</p>

        <div class='status'>
            <div class='status-item'>
                <span>Status:</span>
                <span class='success'>✓ Running</span>
            </div>
            <div class='status-item'>
                <span>Framework:</span>
                <span>ASP.NET Core 10</span>
            </div>
            <div class='status-item'>
                <span>CodeLogic2:</span>
                <span class='success'>✓ Initialized</span>
            </div>
        </div>

        <h2>Next Steps</h2>
        <ol>
            <li>Configure database connection in <code>config/mysql.json</code></li>
            <li>Create or install extensions in <code>extensions/</code> folder</li>
            <li>Run the application and access the admin panel</li>
        </ol>

        <p style='text-align: center; margin-top: 40px; color: #666;'>
            Powered by CodeLogic2 Framework
        </p>
    </div>
</body>
</html>";

    return Results.Content(html, "text/html");
});

// ============================================================================
// STEP 18: Start the server
// ============================================================================

Console.WriteLine();
Console.WriteLine("=".PadRight(80, '='));
Console.WriteLine("✓ WebLogic.Server initialized successfully");
Console.WriteLine("=".PadRight(80, '='));
Console.WriteLine();

// Configure graceful shutdown
var appLifetime = app.Services.GetRequiredService<IHostApplicationLifetime>();
appLifetime.ApplicationStopping.Register(() =>
{
    // Shutdown lifecycle hooks are called by Framework.ShutdownAsync()
    framework.ShutdownAsync().GetAwaiter().GetResult();
});

await app.RunAsync();
