using WebLogic.Server.Core.Configuration;

namespace WebLogic.Server.Core.Middleware;

/// <summary>
/// Middleware for redirecting HTTP requests to HTTPS
/// </summary>
public class HttpsRedirectMiddleware
{
    private readonly RequestDelegate _next;
    private readonly WebLogicServerOptions _options;

    public HttpsRedirectMiddleware(
        RequestDelegate next,
        WebLogicServerOptions options)
    {
        _next = next;
        _options = options;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (_options.EnableHttpsRedirect && !context.Request.IsHttps)
        {
            var httpsUrl = $"https://{context.Request.Host}{context.Request.Path}{context.Request.QueryString}";
            context.Response.Redirect(httpsUrl, permanent: true);
            return;
        }

        await _next(context);
    }
}
