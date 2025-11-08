using WebLogic.Server.Core.Middleware;
using WebLogic.Shared.Abstractions;

namespace WebLogic.Server.Core.Extensions;

/// <summary>
/// Extension methods for registering WebLogic middleware
/// </summary>
public static class MiddlewareExtensions
{
    /// <summary>
    /// Add WebLogic security middleware (rate limiting, IP filtering)
    /// </summary>
    public static IApplicationBuilder UseWebLogicSecurity(this IApplicationBuilder app)
    {
        return app.UseMiddleware<SecurityMiddleware>();
    }

    /// <summary>
    /// Add WebLogic HTTPS redirect middleware
    /// </summary>
    public static IApplicationBuilder UseWebLogicHttpsRedirect(this IApplicationBuilder app)
    {
        return app.UseMiddleware<HttpsRedirectMiddleware>();
    }

    /// <summary>
    /// Add WebLogic CORS middleware
    /// </summary>
    public static IApplicationBuilder UseWebLogicCors(this IApplicationBuilder app)
    {
        return app.UseMiddleware<CorsMiddleware>();
    }

    /// <summary>
    /// Add WebLogic session tracking middleware (database-backed)
    /// </summary>
    public static IApplicationBuilder UseWebLogicSessionTracking(this IApplicationBuilder app)
    {
        return app.UseMiddleware<SessionTrackingMiddleware>();
    }

    /// <summary>
    /// Configure middleware for all loaded extensions
    /// </summary>
    public static IApplicationBuilder UseWebLogicExtensions(this IApplicationBuilder app)
    {
        var extensionManager = app.ApplicationServices.GetService<IExtensionManager>();
        if (extensionManager == null)
        {
            Console.WriteLine("⚠ ExtensionManager not found. Skipping extension middleware configuration.");
            return app;
        }

        var extensions = extensionManager.LoadedExtensions;
        if (extensions.Count == 0)
        {
            Console.WriteLine("→ No extensions loaded. Skipping extension middleware configuration.");
            return app;
        }

        Console.WriteLine($"→ Configuring middleware for {extensions.Count} extension(s)...");

        foreach (var extension in extensions)
        {
            try
            {
                var extensionId = extension.ExtensionManifest?.Id ?? "unknown";
                extension.ConfigureMiddleware(app);
                Console.WriteLine($"  ✓ {extensionId} middleware configured");
            }
            catch (Exception ex)
            {
                var extensionId = extension.ExtensionManifest?.Id ?? "unknown";
                Console.WriteLine($"  ✗ {extensionId} middleware configuration failed: {ex.Message}");
            }
        }

        Console.WriteLine();

        return app;
    }

    /// <summary>
    /// Add WebLogic extension router middleware (routes requests to extensions)
    /// </summary>
    public static IApplicationBuilder UseWebLogicExtensionRouter(this IApplicationBuilder app)
    {
        return app.UseMiddleware<ExtensionRouterMiddleware>();
    }

    /// <summary>
    /// Add API router middleware to the pipeline
    /// </summary>
    public static IApplicationBuilder UseApiRouter(this IApplicationBuilder app)
    {
        // Handle CORS preflight requests
        app.Use(async (context, next) =>
        {
            if (context.Request.Method == "OPTIONS" && context.Request.Path.StartsWithSegments("/api"))
            {
                context.Response.StatusCode = 204;
                context.Response.Headers["Access-Control-Allow-Origin"] = "*";
                context.Response.Headers["Access-Control-Allow-Methods"] = "GET, POST, PUT, DELETE, PATCH, OPTIONS";
                context.Response.Headers["Access-Control-Allow-Headers"] = "Content-Type, Authorization, X-User-Id, X-User-Permissions";
                context.Response.Headers["Access-Control-Max-Age"] = "86400"; // 24 hours
                return;
            }

            await next();
        });

        return app.UseMiddleware<Server.Middleware.ApiRouterMiddleware>();
    }
}
