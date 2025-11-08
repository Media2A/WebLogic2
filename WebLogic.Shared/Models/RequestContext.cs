using Microsoft.AspNetCore.Http;

namespace WebLogic.Shared.Models;

/// <summary>
/// Context containing all HTTP request information
/// </summary>
public class RequestContext
{
    /// <summary>
    /// HTTP headers
    /// </summary>
    public required IReadOnlyDictionary<string, string> Headers { get; init; }

    /// <summary>
    /// Query string parameters
    /// </summary>
    public required Dictionary<string, string> QueryParameters { get; init; }

    /// <summary>
    /// Route parameters extracted from URL pattern (e.g., {id}, {slug})
    /// </summary>
    public Dictionary<string, string> RouteParameters { get; init; } = new();

    /// <summary>
    /// Form data (POST)
    /// </summary>
    public required Dictionary<string, object> FormData { get; init; }

    /// <summary>
    /// Session data
    /// </summary>
    public required Dictionary<string, string> SessionData { get; init; }

    /// <summary>
    /// Cookie values
    /// </summary>
    public required IReadOnlyDictionary<string, string> ClientCookies { get; init; }

    /// <summary>
    /// URL path segments (e.g., /blog/post/123 -> ["blog", "post", "123"])
    /// </summary>
    public required List<string> RoutingPaths { get; init; }

    /// <summary>
    /// Client information (IP, User-Agent, etc.)
    /// </summary>
    public required ClientInfo ClientInformation { get; init; }

    /// <summary>
    /// User permissions (if authenticated)
    /// </summary>
    public List<string>? UserPermissions { get; set; }

    /// <summary>
    /// Service provider for DI
    /// </summary>
    public required IServiceProvider ServiceProvider { get; init; }

    /// <summary>
    /// Raw HTTP context
    /// </summary>
    public required HttpContext HttpContext { get; init; }

    /// <summary>
    /// Create RequestContext from HttpContext
    /// </summary>
    public static async Task<RequestContext> CreateAsync(HttpContext context)
    {
        // Parse headers
        var headers = context.Request.Headers
            .ToDictionary(h => h.Key, h => h.Value.ToString());

        // Parse query parameters
        var queryParams = context.Request.Query
            .ToDictionary(q => q.Key, q => q.Value.ToString());

        // Parse form data
        var formData = new Dictionary<string, object>();
        if (context.Request.HasFormContentType)
        {
            var form = await context.Request.ReadFormAsync();
            foreach (var field in form)
            {
                formData[field.Key] = field.Value.ToString()!;
            }
            foreach (var file in form.Files)
            {
                formData[file.Name] = file;
            }
        }

        // Parse session data
        var sessionData = new Dictionary<string, string>();
        // Session data will be populated by session middleware

        // Parse cookies
        var cookies = context.Request.Cookies
            .ToDictionary(c => c.Key, c => c.Value);

        // Parse routing paths
        var path = context.Request.Path.Value ?? "/";
        var routingPaths = path.Split('/', StringSplitOptions.RemoveEmptyEntries).ToList();

        // Client information
        var userAgent = context.Request.Headers["User-Agent"].ToString();
        var clientInfo = new ClientInfo
        {
            IpAddress = context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            UserAgent = userAgent,
            IsBot = IsWebCrawler(userAgent)
        };

        return new RequestContext
        {
            Headers = headers,
            QueryParameters = queryParams,
            FormData = formData,
            SessionData = sessionData,
            ClientCookies = cookies,
            RoutingPaths = routingPaths,
            ClientInformation = clientInfo,
            ServiceProvider = context.RequestServices,
            HttpContext = context
        };
    }

    private static bool IsWebCrawler(string userAgent)
    {
        if (string.IsNullOrEmpty(userAgent))
            return false;

        var botPatterns = new[] { "bot", "crawler", "spider", "slurp", "scraper" };
        return botPatterns.Any(pattern => userAgent.Contains(pattern, StringComparison.OrdinalIgnoreCase));
    }
}

/// <summary>
/// Client connection information
/// </summary>
public class ClientInfo
{
    public required string IpAddress { get; init; }
    public required string UserAgent { get; init; }
    public required bool IsBot { get; init; }
}
