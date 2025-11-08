using WebLogic.Shared.Models;

namespace WebLogic.Shared.Abstractions;

/// <summary>
/// Delegate for route handlers
/// </summary>
public delegate Task<RouteResponse> RouteHandler(RequestContext context);

/// <summary>
/// Builder for registering extension routes
/// </summary>
public interface IExtensionRouteBuilder
{
    /// <summary>
    /// Register a route with a handler
    /// </summary>
    IExtensionRouteBuilder Map(
        string path,
        RouteHandler handler,
        int priority = 0);

    /// <summary>
    /// Register a route with HTTP method constraint
    /// </summary>
    IExtensionRouteBuilder Map(
        string path,
        string httpMethod,
        RouteHandler handler,
        int priority = 0);

    /// <summary>
    /// Register a GET route
    /// </summary>
    IExtensionRouteBuilder MapGet(
        string path,
        RouteHandler handler,
        int priority = 0);

    /// <summary>
    /// Register a POST route
    /// </summary>
    IExtensionRouteBuilder MapPost(
        string path,
        RouteHandler handler,
        int priority = 0);

    /// <summary>
    /// Register a PUT route
    /// </summary>
    IExtensionRouteBuilder MapPut(
        string path,
        RouteHandler handler,
        int priority = 0);

    /// <summary>
    /// Register a DELETE route
    /// </summary>
    IExtensionRouteBuilder MapDelete(
        string path,
        RouteHandler handler,
        int priority = 0);

    /// <summary>
    /// Register a PATCH route
    /// </summary>
    IExtensionRouteBuilder MapPatch(
        string path,
        RouteHandler handler,
        int priority = 0);

    /// <summary>
    /// Register a page route (returns HTML)
    /// </summary>
    IExtensionRouteBuilder MapPage(
        string path,
        Func<RequestContext, Task<string>> pageHandler,
        int priority = 0);

    /// <summary>
    /// Register a component route (returns HTML fragment)
    /// </summary>
    IExtensionRouteBuilder MapComponent(
        string path,
        Func<RequestContext, Task<string>> componentHandler,
        int priority = 0);

    /// <summary>
    /// Register a JSON API route
    /// </summary>
    IExtensionRouteBuilder MapJson(
        string path,
        Func<RequestContext, Task<object>> jsonHandler,
        int priority = 0);

    /// <summary>
    /// Register a route group with a prefix
    /// </summary>
    IExtensionRouteBuilder MapGroup(
        string prefix,
        Action<IExtensionRouteBuilder> configure);

    /// <summary>
    /// Get all registered routes
    /// </summary>
    IReadOnlyList<RegisteredRoute> GetRegisteredRoutes();

    /// <summary>
    /// Clear all registered routes
    /// </summary>
    void Clear();
}

/// <summary>
/// Represents a registered route with its handler
/// </summary>
public class RegisteredRoute
{
    public required string Path { get; init; }
    public required RouteHandler Handler { get; init; }
    public string? HttpMethod { get; init; }
    public int Priority { get; init; }
    public required string ExtensionId { get; init; }
    public RouteType RouteType { get; init; }
    public DateTime RegisteredAt { get; init; }
    public Dictionary<string, object> Metadata { get; init; } = new();
}

/// <summary>
/// Type of route
/// </summary>
public enum RouteType
{
    /// <summary>
    /// Custom route handler
    /// </summary>
    Custom,

    /// <summary>
    /// Page route (returns HTML)
    /// </summary>
    Page,

    /// <summary>
    /// Component route (returns HTML fragment)
    /// </summary>
    Component,

    /// <summary>
    /// JSON API route
    /// </summary>
    Json
}
