using Microsoft.AspNetCore.Http;
using WebLogic.Shared.Models.API;

namespace WebLogic.Server.API;

/// <summary>
/// Context wrapper for API requests with convenience methods
/// </summary>
public class ApiContext
{
    private readonly ApiRequest _request;

    public ApiContext(ApiRequest request)
    {
        _request = request;
    }

    /// <summary>
    /// Original API request
    /// </summary>
    public ApiRequest Request => _request;

    /// <summary>
    /// Route parameters helper
    /// </summary>
    public RouteParamsHelper RouteParams => new RouteParamsHelper(_request.Params);

    /// <summary>
    /// Query parameters helper
    /// </summary>
    public QueryParamsHelper Query => new QueryParamsHelper(_request.Query);

    /// <summary>
    /// Parse JSON body as specified type
    /// </summary>
    public Task<T?> GetJsonBodyAsync<T>() where T : class
    {
        return Task.FromResult(_request.ParseBody<T>());
    }

    /// <summary>
    /// Helper class for route parameters
    /// </summary>
    public class RouteParamsHelper
    {
        private readonly Dictionary<string, string> _params;

        public RouteParamsHelper(Dictionary<string, string> parameters)
        {
            _params = parameters;
        }

        public string? Get(string key)
        {
            return _params.TryGetValue(key, out var value) ? value : null;
        }

        public int? GetInt(string key)
        {
            if (_params.TryGetValue(key, out var value) && int.TryParse(value, out var intValue))
            {
                return intValue;
            }
            return null;
        }

        public bool GetBool(string key, bool defaultValue = false)
        {
            if (_params.TryGetValue(key, out var value) && bool.TryParse(value, out var boolValue))
            {
                return boolValue;
            }
            return defaultValue;
        }
    }

    /// <summary>
    /// Helper class for query parameters
    /// </summary>
    public class QueryParamsHelper
    {
        private readonly Dictionary<string, string> _query;

        public QueryParamsHelper(Dictionary<string, string> query)
        {
            _query = query;
        }

        public string? Get(string key, string? defaultValue = null)
        {
            return _query.TryGetValue(key, out var value) ? value : defaultValue;
        }

        public int GetInt(string key, int defaultValue = 0)
        {
            if (_query.TryGetValue(key, out var value) && int.TryParse(value, out var intValue))
            {
                return intValue;
            }
            return defaultValue;
        }

        public int? GetInt(string key, int? defaultValue)
        {
            if (_query.TryGetValue(key, out var value) && int.TryParse(value, out var intValue))
            {
                return intValue;
            }
            return defaultValue;
        }

        public bool GetBool(string key, bool defaultValue = false)
        {
            if (_query.TryGetValue(key, out var value) && bool.TryParse(value, out var boolValue))
            {
                return boolValue;
            }
            return defaultValue;
        }
    }
}
