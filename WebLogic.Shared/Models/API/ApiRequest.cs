using System.Text.Json;

namespace WebLogic.Shared.Models.API;

/// <summary>
/// Represents an API request with parsed data
/// </summary>
public class ApiRequest
{
    /// <summary>
    /// Original RequestContext
    /// </summary>
    public required RequestContext Context { get; init; }

    /// <summary>
    /// API version from route (e.g., "v1")
    /// </summary>
    public required string Version { get; init; }

    /// <summary>
    /// HTTP method (GET, POST, etc.)
    /// </summary>
    public required string Method { get; init; }

    /// <summary>
    /// Request path without version (e.g., "/users/123")
    /// </summary>
    public required string Path { get; init; }

    /// <summary>
    /// Route parameters (e.g., {id} from /users/{id})
    /// </summary>
    public required Dictionary<string, string> Params { get; init; }

    /// <summary>
    /// Query string parameters
    /// </summary>
    public required Dictionary<string, string> Query { get; init; }

    /// <summary>
    /// Request headers
    /// </summary>
    public required IReadOnlyDictionary<string, string> Headers { get; init; }

    /// <summary>
    /// Raw request body as string
    /// </summary>
    public string? Body { get; init; }

    /// <summary>
    /// Parsed JSON body (if Content-Type is application/json)
    /// </summary>
    public JsonDocument? JsonBody { get; init; }

    /// <summary>
    /// Authenticated user ID (if authenticated)
    /// </summary>
    public int? UserId { get; init; }

    /// <summary>
    /// User permissions (if authenticated)
    /// </summary>
    public string[] UserPermissions { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Client IP address
    /// </summary>
    public required string ClientIp { get; init; }

    /// <summary>
    /// Request timestamp
    /// </summary>
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Parse body as JSON to specified type
    /// </summary>
    public T? ParseBody<T>() where T : class
    {
        if (string.IsNullOrEmpty(Body))
            return null;

        try
        {
            return JsonSerializer.Deserialize<T>(Body);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Get header value (case-insensitive)
    /// </summary>
    public string? GetHeader(string name)
    {
        var key = Headers.Keys.FirstOrDefault(k => k.Equals(name, StringComparison.OrdinalIgnoreCase));
        return key != null ? Headers[key] : null;
    }

    /// <summary>
    /// Check if user has required permission
    /// </summary>
    public bool HasPermission(string permission)
    {
        return UserPermissions.Contains(permission, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Check if user has any of the required permissions
    /// </summary>
    public bool HasAnyPermission(params string[] permissions)
    {
        return permissions.Any(p => HasPermission(p));
    }

    /// <summary>
    /// Check if user has all required permissions
    /// </summary>
    public bool HasAllPermissions(params string[] permissions)
    {
        return permissions.All(p => HasPermission(p));
    }
}
