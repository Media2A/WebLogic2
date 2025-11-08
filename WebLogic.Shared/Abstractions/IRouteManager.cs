using WebLogic.Shared.Models;

namespace WebLogic.Shared.Abstractions;

/// <summary>
/// Manages all routes from extensions
/// </summary>
public interface IRouteManager
{
    /// <summary>
    /// Register routes from all loaded extensions
    /// </summary>
    Task RegisterExtensionRoutesAsync();

    /// <summary>
    /// Find a matching route for the given request
    /// </summary>
    RouteMatch? MatchRoute(string path, string httpMethod);

    /// <summary>
    /// Get all registered routes
    /// </summary>
    IReadOnlyList<RegisteredRoute> GetAllRoutes();

    /// <summary>
    /// Get routes for a specific extension
    /// </summary>
    IReadOnlyList<RegisteredRoute> GetRoutesByExtension(string extensionId);

    /// <summary>
    /// Reload routes from all extensions
    /// </summary>
    Task ReloadRoutesAsync();

    /// <summary>
    /// Clear all routes
    /// </summary>
    void ClearRoutes();
}

/// <summary>
/// Represents a matched route with extracted parameters
/// </summary>
public class RouteMatch
{
    public required RegisteredRoute Route { get; init; }
    public Dictionary<string, string> Parameters { get; init; } = new();
}
