using System.Text.Json;
using WebLogic.Shared.Abstractions;
using WebLogic.Shared.Models;
using WLRouteHandler = WebLogic.Shared.Abstractions.RouteHandler;

namespace WebLogic.Server.Extensions;

/// <summary>
/// Implementation of route builder for extensions
/// </summary>
public class ExtensionRouteBuilder : IExtensionRouteBuilder
{
    private readonly List<RegisteredRoute> _routes = new();
    private readonly string _extensionId;
    private readonly string? _groupPrefix;

    public ExtensionRouteBuilder(string extensionId, string? groupPrefix = null)
    {
        _extensionId = extensionId;
        _groupPrefix = groupPrefix;
    }

    /// <summary>
    /// Register a route with a handler
    /// </summary>
    public IExtensionRouteBuilder Map(string path, WLRouteHandler handler, int priority = 0)
    {
        var fullPath = CombinePaths(_groupPrefix, path);

        _routes.Add(new RegisteredRoute
        {
            Path = fullPath,
            Handler = handler,
            Priority = priority,
            ExtensionId = _extensionId,
            RouteType = RouteType.Custom,
            RegisteredAt = DateTime.UtcNow
        });

        return this;
    }

    /// <summary>
    /// Register a route with HTTP method constraint
    /// </summary>
    public IExtensionRouteBuilder Map(string path, string httpMethod, WLRouteHandler handler, int priority = 0)
    {
        var fullPath = CombinePaths(_groupPrefix, path);

        _routes.Add(new RegisteredRoute
        {
            Path = fullPath,
            Handler = handler,
            HttpMethod = httpMethod.ToUpper(),
            Priority = priority,
            ExtensionId = _extensionId,
            RouteType = RouteType.Custom,
            RegisteredAt = DateTime.UtcNow
        });

        return this;
    }

    /// <summary>
    /// Register a GET route
    /// </summary>
    public IExtensionRouteBuilder MapGet(string path, WLRouteHandler handler, int priority = 0)
    {
        return Map(path, "GET", handler, priority);
    }

    /// <summary>
    /// Register a POST route
    /// </summary>
    public IExtensionRouteBuilder MapPost(string path, WLRouteHandler handler, int priority = 0)
    {
        return Map(path, "POST", handler, priority);
    }

    /// <summary>
    /// Register a PUT route
    /// </summary>
    public IExtensionRouteBuilder MapPut(string path, WLRouteHandler handler, int priority = 0)
    {
        return Map(path, "PUT", handler, priority);
    }

    /// <summary>
    /// Register a DELETE route
    /// </summary>
    public IExtensionRouteBuilder MapDelete(string path, WLRouteHandler handler, int priority = 0)
    {
        return Map(path, "DELETE", handler, priority);
    }

    /// <summary>
    /// Register a PATCH route
    /// </summary>
    public IExtensionRouteBuilder MapPatch(string path, WLRouteHandler handler, int priority = 0)
    {
        return Map(path, "PATCH", handler, priority);
    }

    /// <summary>
    /// Register a page route (returns HTML)
    /// </summary>
    public IExtensionRouteBuilder MapPage(string path, Func<RequestContext, Task<string>> pageHandler, int priority = 0)
    {
        var fullPath = CombinePaths(_groupPrefix, path);

        WLRouteHandler wrapper = async (context) =>
        {
            var html = await pageHandler(context);
            return RouteResponse.Html(html);
        };

        _routes.Add(new RegisteredRoute
        {
            Path = fullPath,
            Handler = wrapper,
            Priority = priority,
            ExtensionId = _extensionId,
            RouteType = RouteType.Page,
            RegisteredAt = DateTime.UtcNow
        });

        return this;
    }

    /// <summary>
    /// Register a component route (returns HTML fragment)
    /// </summary>
    public IExtensionRouteBuilder MapComponent(string path, Func<RequestContext, Task<string>> componentHandler, int priority = 0)
    {
        var fullPath = CombinePaths(_groupPrefix, path);

        WLRouteHandler wrapper = async (context) =>
        {
            var html = await componentHandler(context);
            return RouteResponse.Html(html);
        };

        _routes.Add(new RegisteredRoute
        {
            Path = fullPath,
            Handler = wrapper,
            Priority = priority,
            ExtensionId = _extensionId,
            RouteType = RouteType.Component,
            RegisteredAt = DateTime.UtcNow
        });

        return this;
    }

    /// <summary>
    /// Register a JSON API route
    /// </summary>
    public IExtensionRouteBuilder MapJson(string path, Func<RequestContext, Task<object>> jsonHandler, int priority = 0)
    {
        var fullPath = CombinePaths(_groupPrefix, path);

        WLRouteHandler wrapper = async (context) =>
        {
            var data = await jsonHandler(context);
            return RouteResponse.Json(data);
        };

        _routes.Add(new RegisteredRoute
        {
            Path = fullPath,
            Handler = wrapper,
            Priority = priority,
            ExtensionId = _extensionId,
            RouteType = RouteType.Json,
            RegisteredAt = DateTime.UtcNow
        });

        return this;
    }

    /// <summary>
    /// Register a route group with a prefix
    /// </summary>
    public IExtensionRouteBuilder MapGroup(string prefix, Action<IExtensionRouteBuilder> configure)
    {
        var fullPrefix = CombinePaths(_groupPrefix, prefix);
        var groupBuilder = new ExtensionRouteBuilder(_extensionId, fullPrefix);
        configure(groupBuilder);

        // Add all routes from the group builder to this builder
        _routes.AddRange(groupBuilder.GetRegisteredRoutes());

        return this;
    }

    /// <summary>
    /// Get all registered routes
    /// </summary>
    public IReadOnlyList<RegisteredRoute> GetRegisteredRoutes()
    {
        return _routes.AsReadOnly();
    }

    /// <summary>
    /// Clear all registered routes
    /// </summary>
    public void Clear()
    {
        _routes.Clear();
    }

    /// <summary>
    /// Combine path segments
    /// </summary>
    private static string CombinePaths(string? prefix, string path)
    {
        if (string.IsNullOrEmpty(prefix))
            return NormalizePath(path);

        prefix = NormalizePath(prefix);
        path = NormalizePath(path);

        if (path.StartsWith('/'))
            path = path.Substring(1);

        return $"{prefix}/{path}";
    }

    /// <summary>
    /// Normalize path to start with / and not end with /
    /// </summary>
    private static string NormalizePath(string path)
    {
        if (string.IsNullOrEmpty(path))
            return "/";

        // Ensure starts with /
        if (!path.StartsWith('/'))
            path = "/" + path;

        // Remove trailing / (unless it's the root)
        if (path.Length > 1 && path.EndsWith('/'))
            path = path.Substring(0, path.Length - 1);

        return path;
    }
}
