using WebLogic.Shared.Models.API;

namespace WebLogic.Shared.Abstractions;

/// <summary>
/// Manages API endpoint registration and discovery
/// </summary>
public interface IApiManager
{
    /// <summary>
    /// Register an API endpoint
    /// </summary>
    void RegisterEndpoint(ApiEndpoint endpoint);

    /// <summary>
    /// Create a new endpoint builder
    /// </summary>
    IApiEndpointBuilder CreateEndpoint(string? extensionId = null);

    /// <summary>
    /// Get all registered endpoints
    /// </summary>
    IReadOnlyList<ApiEndpoint> GetAllEndpoints();

    /// <summary>
    /// Get endpoints by version
    /// </summary>
    IReadOnlyList<ApiEndpoint> GetEndpointsByVersion(string version);

    /// <summary>
    /// Get endpoints by tag
    /// </summary>
    IReadOnlyList<ApiEndpoint> GetEndpointsByTag(string tag);

    /// <summary>
    /// Get endpoint by ID
    /// </summary>
    ApiEndpoint? GetEndpoint(string id);

    /// <summary>
    /// Find matching endpoint for request
    /// </summary>
    ApiEndpoint? FindEndpoint(string method, string path);

    /// <summary>
    /// Get all API versions
    /// </summary>
    IReadOnlyList<string> GetVersions();

    /// <summary>
    /// Get all tags
    /// </summary>
    IReadOnlyList<string> GetTags();

    /// <summary>
    /// Check if endpoint exists
    /// </summary>
    bool EndpointExists(string id);

    /// <summary>
    /// Remove endpoint by ID
    /// </summary>
    bool RemoveEndpoint(string id);

    /// <summary>
    /// Remove all endpoints for extension
    /// </summary>
    void RemoveEndpointsByExtension(string extensionId);

    /// <summary>
    /// Get API discovery document (for API explorer)
    /// </summary>
    ApiDiscoveryDocument GetDiscoveryDocument();
}

/// <summary>
/// API discovery document for documentation
/// </summary>
public class ApiDiscoveryDocument
{
    public string Title { get; set; } = "WebLogic API";
    public string Version { get; set; } = "1.0";
    public string Description { get; set; } = "WebLogic Server API Documentation";
    public string[] Versions { get; set; } = Array.Empty<string>();
    public string[] Tags { get; set; } = Array.Empty<string>();
    public ApiEndpoint[] Endpoints { get; set; } = Array.Empty<ApiEndpoint>();
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
}
