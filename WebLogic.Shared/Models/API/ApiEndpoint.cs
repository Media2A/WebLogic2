namespace WebLogic.Shared.Models.API;

/// <summary>
/// Represents an API endpoint definition
/// </summary>
public class ApiEndpoint
{
    /// <summary>
    /// Unique endpoint identifier
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// API version (e.g., "v1", "v2")
    /// </summary>
    public required string Version { get; init; }

    /// <summary>
    /// HTTP method (GET, POST, PUT, DELETE, PATCH)
    /// </summary>
    public required string Method { get; init; }

    /// <summary>
    /// Endpoint path (e.g., "/users", "/users/{id}")
    /// </summary>
    public required string Path { get; init; }

    /// <summary>
    /// Full route pattern including version (e.g., "/api/v1/users/{id}")
    /// </summary>
    public required string FullRoute { get; init; }

    /// <summary>
    /// Handler function for this endpoint
    /// </summary>
    public required Func<ApiRequest, Task<ApiResponse>> Handler { get; init; }

    /// <summary>
    /// Endpoint description for documentation
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Endpoint tags/categories
    /// </summary>
    public string[] Tags { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Required permissions to access this endpoint
    /// </summary>
    public string[] RequiredPermissions { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Whether authentication is required
    /// </summary>
    public bool RequiresAuth { get; init; } = false;

    /// <summary>
    /// Rate limit override for this endpoint (requests per minute)
    /// </summary>
    public int? RateLimit { get; init; }

    /// <summary>
    /// Request body schema (for documentation)
    /// </summary>
    public Type? RequestBodyType { get; init; }

    /// <summary>
    /// Response schema (for documentation)
    /// </summary>
    public Type? ResponseType { get; init; }

    /// <summary>
    /// Whether this endpoint is deprecated
    /// </summary>
    public bool IsDeprecated { get; init; } = false;

    /// <summary>
    /// Deprecation message
    /// </summary>
    public string? DeprecationMessage { get; init; }

    /// <summary>
    /// Extension/module that registered this endpoint
    /// </summary>
    public string? ExtensionId { get; init; }

    /// <summary>
    /// Custom metadata
    /// </summary>
    public Dictionary<string, object>? Metadata { get; init; }
}
