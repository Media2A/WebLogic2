using CodeLogic.Abstractions;
using WebLogic.Server.Core.Configuration;
using WebLogic.Shared.Abstractions;
using WebLogic.Shared.Models;

namespace WebLogic.Server.Core.Middleware;

/// <summary>
/// Middleware that routes requests to extension handlers
/// </summary>
public class ExtensionRouterMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IRouteManager _routeManager;
    private readonly WebLogicServerOptions _options;
    private readonly CodeLogic.Abstractions.ILogger? _logger;

    public ExtensionRouterMiddleware(
        RequestDelegate next,
        IRouteManager routeManager,
        WebLogicServerOptions options,
        CodeLogic.Abstractions.ILogger? logger = null)
    {
        _next = next;
        _routeManager = routeManager;
        _options = options;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value ?? "/";
        var method = context.Request.Method;

        // Try to match a route
        var match = _routeManager.MatchRoute(path, method);

        if (match == null)
        {
            // No route matched, pass to next middleware
            await _next(context);
            return;
        }

        try
        {
            // Create request context
            var requestContext = await RequestContext.CreateAsync(context);

            // Add route parameters to request context
            foreach (var param in match.Parameters)
            {
                requestContext.RouteParameters[param.Key] = param.Value;
            }

            // Call the route handler
            var response = await match.Route.Handler(requestContext);

            // Write response to HTTP context
            await WriteResponseAsync(context, response);
        }
        catch (Exception ex)
        {
            _logger?.Error($"Error handling route {path}", ex);

            // Return 500 error
            context.Response.StatusCode = 500;
            context.Response.ContentType = "text/html; charset=utf-8";

            if (_options.EnableDebugMode)
            {
                await context.Response.WriteAsync($@"
<!DOCTYPE html>
<html>
<head>
    <title>500 - Internal Server Error</title>
    <style>
        body {{ font-family: system-ui, sans-serif; padding: 40px; background: #f5f5f5; }}
        .error {{ background: white; padding: 20px; border-radius: 8px; box-shadow: 0 2px 10px rgba(0,0,0,0.1); }}
        h1 {{ color: #d32f2f; margin: 0 0 20px 0; }}
        pre {{ background: #f5f5f5; padding: 15px; border-radius: 4px; overflow-x: auto; }}
    </style>
</head>
<body>
    <div class='error'>
        <h1>500 - Internal Server Error</h1>
        <p><strong>Message:</strong> {System.Net.WebUtility.HtmlEncode(ex.Message)}</p>
        <p><strong>Stack Trace:</strong></p>
        <pre>{System.Net.WebUtility.HtmlEncode(ex.StackTrace ?? "No stack trace available")}</pre>
    </div>
</body>
</html>");
            }
            else
            {
                await context.Response.WriteAsync(@"
<!DOCTYPE html>
<html>
<head>
    <title>500 - Internal Server Error</title>
    <style>
        body {{ font-family: system-ui, sans-serif; padding: 40px; background: #f5f5f5; }}
        .error {{ background: white; padding: 20px; border-radius: 8px; box-shadow: 0 2px 10px rgba(0,0,0,0.1); }}
        h1 {{ color: #d32f2f; }}
    </style>
</head>
<body>
    <div class='error'>
        <h1>500 - Internal Server Error</h1>
        <p>An error occurred while processing your request.</p>
    </div>
</body>
</html>");
            }
        }
    }

    /// <summary>
    /// Write RouteResponse to HttpContext
    /// </summary>
    private async Task WriteResponseAsync(HttpContext context, RouteResponse response)
    {
        // Set status code
        context.Response.StatusCode = response.StatusCode;

        // Handle redirect
        if (response.IsRedirect)
        {
            context.Response.Redirect(response.RedirectUrl!, response.StatusCode == 301);
            return;
        }

        // Set content type
        context.Response.ContentType = response.ContentType;

        // Set headers
        foreach (var header in response.Headers)
        {
            context.Response.Headers[header.Key] = header.Value;
        }

        // Set cookies
        foreach (var cookie in response.Cookies)
        {
            context.Response.Cookies.Append(cookie.Key, cookie.Value);
        }

        // Write content
        if (response.BinaryContent != null)
        {
            // Binary content (file download)
            if (!string.IsNullOrEmpty(response.FileName))
            {
                context.Response.Headers["Content-Disposition"] = $"attachment; filename=\"{response.FileName}\"";
            }

            await context.Response.Body.WriteAsync(response.BinaryContent, 0, response.BinaryContent.Length);
        }
        else if (!string.IsNullOrEmpty(response.Body))
        {
            // Text content
            await context.Response.WriteAsync(response.Body);
        }
    }
}
