using System.Text.RegularExpressions;
using CodeLogic.Abstractions;
using WebLogic.Shared.Abstractions;
using WebLogic.Shared.Models;

namespace WebLogic.Server.Extensions;

/// <summary>
/// Manages all routes from extensions
/// </summary>
public class RouteManager : IRouteManager
{
    private readonly IExtensionManager _extensionManager;
    private readonly CodeLogic.Abstractions.ILogger? _logger;
    private readonly List<RegisteredRoute> _routes = new();
    private readonly object _lock = new();

    public RouteManager(
        IExtensionManager extensionManager,
        CodeLogic.Abstractions.ILogger? logger = null)
    {
        _extensionManager = extensionManager;
        _logger = logger;
    }

    /// <summary>
    /// Register routes from all loaded extensions
    /// </summary>
    public async Task RegisterExtensionRoutesAsync()
    {
        lock (_lock)
        {
            _routes.Clear();
        }

        var extensions = _extensionManager.LoadedExtensions;
        if (extensions.Count == 0)
        {
            _logger?.Info("No extensions loaded, skipping route registration");
            return;
        }

        _logger?.Info($"Registering routes from {extensions.Count} extension(s)...");

        foreach (var extension in extensions)
        {
            try
            {
                var extensionId = extension.ExtensionManifest?.Id ?? "unknown";
                var routeBuilder = new ExtensionRouteBuilder(extensionId);

                // Call extension's RegisterRoutes method
                extension.RegisterRoutes(routeBuilder);

                var extensionRoutes = routeBuilder.GetRegisteredRoutes();

                lock (_lock)
                {
                    _routes.AddRange(extensionRoutes);
                }

                _logger?.Info($"  ✓ {extensionId}: {extensionRoutes.Count} route(s) registered");
            }
            catch (Exception ex)
            {
                var extensionId = extension.ExtensionManifest?.Id ?? "unknown";
                _logger?.Error($"Failed to register routes for extension {extensionId}", ex);
            }
        }

        // Sort routes by priority (higher priority first)
        lock (_lock)
        {
            _routes.Sort((a, b) => b.Priority.CompareTo(a.Priority));
        }

        await Task.CompletedTask;
    }

    /// <summary>
    /// Find a matching route for the given request
    /// </summary>
    public RouteMatch? MatchRoute(string path, string httpMethod)
    {
        path = NormalizePath(path);
        httpMethod = httpMethod.ToUpper();

        List<RegisteredRoute> routes;
        lock (_lock)
        {
            routes = new List<RegisteredRoute>(_routes);
        }

        foreach (var route in routes)
        {
            // Check HTTP method if specified
            if (!string.IsNullOrEmpty(route.HttpMethod) && route.HttpMethod != httpMethod)
                continue;

            // Try to match the path
            var match = MatchPath(route.Path, path);
            if (match != null)
            {
                return new RouteMatch
                {
                    Route = route,
                    Parameters = match
                };
            }
        }

        return null;
    }

    /// <summary>
    /// Get all registered routes
    /// </summary>
    public IReadOnlyList<RegisteredRoute> GetAllRoutes()
    {
        lock (_lock)
        {
            return _routes.AsReadOnly();
        }
    }

    /// <summary>
    /// Get routes for a specific extension
    /// </summary>
    public IReadOnlyList<RegisteredRoute> GetRoutesByExtension(string extensionId)
    {
        lock (_lock)
        {
            return _routes
                .Where(r => r.ExtensionId.Equals(extensionId, StringComparison.OrdinalIgnoreCase))
                .ToList()
                .AsReadOnly();
        }
    }

    /// <summary>
    /// Reload routes from all extensions
    /// </summary>
    public async Task ReloadRoutesAsync()
    {
        _logger?.Info("Reloading all routes...");
        await RegisterExtensionRoutesAsync();
    }

    /// <summary>
    /// Clear all routes
    /// </summary>
    public void ClearRoutes()
    {
        lock (_lock)
        {
            _routes.Clear();
        }
        _logger?.Info("All routes cleared");
    }

    /// <summary>
    /// Match a route pattern against a path
    /// </summary>
    private Dictionary<string, string>? MatchPath(string pattern, string path)
    {
        pattern = NormalizePath(pattern);
        path = NormalizePath(path);

        // Exact match (no parameters)
        if (pattern == path)
            return new Dictionary<string, string>();

        // Check if pattern has parameters
        if (!pattern.Contains('{'))
            return null;

        // Convert pattern to regex
        // Example: /blog/{slug} -> ^/blog/(?<slug>[^/]+)$
        // Example: /blog/{year}/{month} -> ^/blog/(?<year>[^/]+)/(?<month>[^/]+)$

        var regexPattern = "^" + Regex.Replace(pattern, @"\{(\w+)\}", match =>
        {
            var paramName = match.Groups[1].Value;
            return $"(?<{paramName}>[^/]+)";
        }) + "$";

        var regex = new Regex(regexPattern, RegexOptions.IgnoreCase);
        var match = regex.Match(path);

        if (!match.Success)
            return null;

        // Extract parameters
        var parameters = new Dictionary<string, string>();
        foreach (Group group in match.Groups)
        {
            if (!string.IsNullOrEmpty(group.Name) && group.Name != "0")
            {
                parameters[group.Name] = group.Value;
            }
        }

        return parameters;
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
