using System.Text.RegularExpressions;
using WebLogic.Shared.Abstractions;
using WebLogic.Shared.Models.API;

namespace WebLogic.Server.Services;

/// <summary>
/// Manages API endpoint registration and routing
/// </summary>
public class ApiManager : IApiManager
{
    private readonly Dictionary<string, ApiEndpoint> _endpoints = new();
    private readonly object _lock = new();

    public void RegisterEndpoint(ApiEndpoint endpoint)
    {
        lock (_lock)
        {
            if (_endpoints.ContainsKey(endpoint.Id))
            {
                throw new InvalidOperationException($"API endpoint with ID '{endpoint.Id}' is already registered");
            }

            _endpoints[endpoint.Id] = endpoint;
        }
    }

    public IApiEndpointBuilder CreateEndpoint(string? extensionId = null)
    {
        return new ApiEndpointBuilder(extensionId);
    }

    public IReadOnlyList<ApiEndpoint> GetAllEndpoints()
    {
        lock (_lock)
        {
            return _endpoints.Values.ToList();
        }
    }

    public IReadOnlyList<ApiEndpoint> GetEndpointsByVersion(string version)
    {
        version = version.StartsWith("v") ? version : $"v{version}";

        lock (_lock)
        {
            return _endpoints.Values
                .Where(e => e.Version.Equals(version, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }
    }

    public IReadOnlyList<ApiEndpoint> GetEndpointsByTag(string tag)
    {
        lock (_lock)
        {
            return _endpoints.Values
                .Where(e => e.Tags.Contains(tag, StringComparer.OrdinalIgnoreCase))
                .ToList();
        }
    }

    public ApiEndpoint? GetEndpoint(string id)
    {
        lock (_lock)
        {
            return _endpoints.TryGetValue(id, out var endpoint) ? endpoint : null;
        }
    }

    public ApiEndpoint? FindEndpoint(string method, string path)
    {
        lock (_lock)
        {
            // Normalize method
            method = method.ToUpper();

            // Try exact match first
            var exactMatch = _endpoints.Values.FirstOrDefault(e =>
                e.Method.Equals(method, StringComparison.OrdinalIgnoreCase) &&
                e.FullRoute.Equals(path, StringComparison.OrdinalIgnoreCase));

            if (exactMatch != null)
                return exactMatch;

            // Try pattern matching for routes with parameters
            foreach (var endpoint in _endpoints.Values)
            {
                if (!endpoint.Method.Equals(method, StringComparison.OrdinalIgnoreCase))
                    continue;

                if (IsRouteMatch(endpoint.FullRoute, path))
                    return endpoint;
            }

            return null;
        }
    }

    public IReadOnlyList<string> GetVersions()
    {
        lock (_lock)
        {
            return _endpoints.Values
                .Select(e => e.Version)
                .Distinct()
                .OrderBy(v => v)
                .ToList();
        }
    }

    public IReadOnlyList<string> GetTags()
    {
        lock (_lock)
        {
            return _endpoints.Values
                .SelectMany(e => e.Tags)
                .Distinct()
                .OrderBy(t => t)
                .ToList();
        }
    }

    public bool EndpointExists(string id)
    {
        lock (_lock)
        {
            return _endpoints.ContainsKey(id);
        }
    }

    public bool RemoveEndpoint(string id)
    {
        lock (_lock)
        {
            return _endpoints.Remove(id);
        }
    }

    public void RemoveEndpointsByExtension(string extensionId)
    {
        lock (_lock)
        {
            var toRemove = _endpoints.Values
                .Where(e => e.ExtensionId?.Equals(extensionId, StringComparison.OrdinalIgnoreCase) == true)
                .Select(e => e.Id)
                .ToList();

            foreach (var id in toRemove)
            {
                _endpoints.Remove(id);
            }
        }
    }

    public ApiDiscoveryDocument GetDiscoveryDocument()
    {
        lock (_lock)
        {
            return new ApiDiscoveryDocument
            {
                Title = "WebLogic API",
                Version = "1.0",
                Description = "WebLogic Server RESTful API",
                Versions = GetVersions().ToArray(),
                Tags = GetTags().ToArray(),
                Endpoints = _endpoints.Values.ToArray(),
                GeneratedAt = DateTime.UtcNow
            };
        }
    }

    // Helper methods

    private bool IsRouteMatch(string routePattern, string requestPath)
    {
        // Convert route pattern to regex
        // Example: /api/v1/users/{id} -> ^/api/v1/users/([^/]+)$
        var pattern = Regex.Escape(routePattern);
        pattern = Regex.Replace(pattern, @"\\\{[^}]+\\\}", "([^/]+)");
        pattern = $"^{pattern}$";

        var regex = new Regex(pattern, RegexOptions.IgnoreCase);
        return regex.IsMatch(requestPath);
    }

    /// <summary>
    /// Extract route parameters from request path
    /// </summary>
    public Dictionary<string, string> ExtractRouteParams(string routePattern, string requestPath)
    {
        var parameters = new Dictionary<string, string>();

        // Get parameter names from pattern
        var paramNames = new List<string>();
        var paramMatches = Regex.Matches(routePattern, @"\{([^}]+)\}");
        foreach (Match match in paramMatches)
        {
            paramNames.Add(match.Groups[1].Value);
        }

        // Convert route pattern to regex
        var pattern = Regex.Escape(routePattern);
        pattern = Regex.Replace(pattern, @"\\\{[^}]+\\\}", "([^/]+)");
        pattern = $"^{pattern}$";

        var regex = new Regex(pattern, RegexOptions.IgnoreCase);
        var match2 = regex.Match(requestPath);

        if (match2.Success)
        {
            for (int i = 0; i < paramNames.Count && i < match2.Groups.Count - 1; i++)
            {
                parameters[paramNames[i]] = match2.Groups[i + 1].Value;
            }
        }

        return parameters;
    }
}
