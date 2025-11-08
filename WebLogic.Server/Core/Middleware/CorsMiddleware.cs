using WebLogic.Server.Core.Configuration;

namespace WebLogic.Server.Core.Middleware;

/// <summary>
/// Middleware for handling CORS (Cross-Origin Resource Sharing)
/// </summary>
public class CorsMiddleware
{
    private readonly RequestDelegate _next;
    private readonly WebLogicServerOptions _options;

    public CorsMiddleware(
        RequestDelegate next,
        WebLogicServerOptions options)
    {
        _next = next;
        _options = options;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var origin = context.Request.Headers["Origin"].ToString();

        if (!string.IsNullOrEmpty(origin) && _options.AllowedOrigins.Length > 0)
        {
            // Check if origin is allowed
            bool isAllowed = false;

            foreach (var allowedOrigin in _options.AllowedOrigins)
            {
                if (allowedOrigin == "*")
                {
                    // Wildcard - allow all origins (but can't use credentials)
                    context.Response.Headers["Access-Control-Allow-Origin"] = origin;
                    context.Response.Headers["Access-Control-Allow-Methods"] = "GET, POST, PUT, DELETE, PATCH, OPTIONS";
                    context.Response.Headers["Access-Control-Allow-Headers"] = "Content-Type, Authorization, X-Requested-With";
                    context.Response.Headers["Vary"] = "Origin";
                    isAllowed = true;
                    break;
                }
                else if (origin.Equals(allowedOrigin, StringComparison.OrdinalIgnoreCase))
                {
                    // Specific origin allowed
                    context.Response.Headers["Access-Control-Allow-Origin"] = origin;
                    context.Response.Headers["Access-Control-Allow-Methods"] = "GET, POST, PUT, DELETE, PATCH, OPTIONS";
                    context.Response.Headers["Access-Control-Allow-Headers"] = "Content-Type, Authorization, X-Requested-With";

                    if (_options.AllowCredentials)
                    {
                        context.Response.Headers["Access-Control-Allow-Credentials"] = "true";
                    }

                    context.Response.Headers["Vary"] = "Origin";
                    isAllowed = true;
                    break;
                }
            }

            // Handle preflight requests
            if (context.Request.Method == "OPTIONS")
            {
                if (isAllowed)
                {
                    context.Response.StatusCode = 204; // No Content
                    return;
                }
                else
                {
                    context.Response.StatusCode = 403; // Forbidden
                    return;
                }
            }
        }

        await _next(context);
    }
}
