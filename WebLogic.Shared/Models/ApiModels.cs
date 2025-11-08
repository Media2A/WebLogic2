namespace WebLogic.Shared.Models;

/// <summary>
/// Definition of an API endpoint
/// </summary>
public class ApiEndpointDefinition
{
    /// <summary>
    /// Endpoint name (used in URL)
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Description of what this endpoint does
    /// </summary>
    public required string Description { get; init; }

    /// <summary>
    /// HTTP method
    /// </summary>
    public required HttpMethodType Method { get; init; }

    /// <summary>
    /// Required authentication level
    /// </summary>
    public required ApiAuthLevel AuthLevel { get; init; }

    /// <summary>
    /// Handler function
    /// </summary>
    public required Func<RequestContext, Task<ApiResponse>> Handler { get; init; }

    /// <summary>
    /// Maximum requests per window (for rate limiting)
    /// </summary>
    public int? MaxRequestsPerWindow { get; init; }

    /// <summary>
    /// Rate limit time window
    /// </summary>
    public TimeSpan? RateLimitWindow { get; init; }

    /// <summary>
    /// Ban duration if rate limit exceeded
    /// </summary>
    public TimeSpan? RateLimitBanDuration { get; init; }

    /// <summary>
    /// Custom error message for rate limit
    /// </summary>
    public string? RateLimitErrorMessage { get; init; }
}

/// <summary>
/// API response model
/// </summary>
public record ApiResponse
{
    /// <summary>
    /// HTTP status code
    /// </summary>
    public int StatusCode { get; init; } = 200;

    /// <summary>
    /// Content type
    /// </summary>
    public string ContentType { get; init; } = "application/json";

    /// <summary>
    /// Response data (will be JSON serialized)
    /// </summary>
    public object? Data { get; init; }

    /// <summary>
    /// Error message (if any)
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// Create success response
    /// </summary>
    public static ApiResponse Ok(object data) => new() { Data = data };

    /// <summary>
    /// Create error response
    /// </summary>
    public static ApiResponse Error(string message, int statusCode = 400) =>
        new() { StatusCode = statusCode, ErrorMessage = message };

    /// <summary>
    /// Create not found response
    /// </summary>
    public static ApiResponse NotFound(string message = "Resource not found") =>
        new() { StatusCode = 404, ErrorMessage = message };

    /// <summary>
    /// Create unauthorized response
    /// </summary>
    public static ApiResponse Unauthorized(string message = "Unauthorized") =>
        new() { StatusCode = 401, ErrorMessage = message };

    /// <summary>
    /// Create forbidden response
    /// </summary>
    public static ApiResponse Forbidden(string message = "Forbidden") =>
        new() { StatusCode = 403, ErrorMessage = message };
}

/// <summary>
/// API authentication levels
/// </summary>
public enum ApiAuthLevel
{
    /// <summary>
    /// No authentication required
    /// </summary>
    None,

    /// <summary>
    /// User must be logged in
    /// </summary>
    User,

    /// <summary>
    /// User must have admin role
    /// </summary>
    Admin
}

/// <summary>
/// API group information (for discovery)
/// </summary>
public record ApiGroupInfo
{
    public required string GroupId { get; init; }
    public required string GroupName { get; init; }
    public required string Description { get; init; }
    public required string ExtensionId { get; init; }
    public required ApiEndpointInfo[] Endpoints { get; init; }
}

/// <summary>
/// API endpoint information (for discovery)
/// </summary>
public record ApiEndpointInfo
{
    public required string Name { get; init; }
    public required string Description { get; init; }
    public required string Method { get; init; }
    public required string AuthLevel { get; init; }
}
