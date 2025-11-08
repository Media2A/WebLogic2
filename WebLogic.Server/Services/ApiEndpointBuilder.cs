using WebLogic.Shared.Abstractions;
using WebLogic.Shared.Models.API;

namespace WebLogic.Server.Services;

/// <summary>
/// Fluent builder for creating API endpoints
/// </summary>
public class ApiEndpointBuilder : IApiEndpointBuilder
{
    private string _version = "v1";
    private string _path = "/";
    private string _method = "GET";
    private string? _description;
    private List<string> _tags = new();
    private bool _requiresAuth = false;
    private List<string> _requiredPermissions = new();
    private int? _rateLimit;
    private Type? _requestBodyType;
    private Type? _responseType;
    private bool _isDeprecated = false;
    private string? _deprecationMessage;
    private Func<ApiRequest, Task<ApiResponse>>? _handler;
    private Dictionary<string, object> _metadata = new();
    private readonly string? _extensionId;

    public ApiEndpointBuilder(string? extensionId = null)
    {
        _extensionId = extensionId;
    }

    public IApiEndpointBuilder Version(string version)
    {
        _version = version.TrimStart('v');
        return this;
    }

    public IApiEndpointBuilder Path(string path)
    {
        _path = path.StartsWith('/') ? path : $"/{path}";
        return this;
    }

    public IApiEndpointBuilder Get()
    {
        _method = "GET";
        return this;
    }

    public IApiEndpointBuilder Post()
    {
        _method = "POST";
        return this;
    }

    public IApiEndpointBuilder Put()
    {
        _method = "PUT";
        return this;
    }

    public IApiEndpointBuilder Delete()
    {
        _method = "DELETE";
        return this;
    }

    public IApiEndpointBuilder Patch()
    {
        _method = "PATCH";
        return this;
    }

    public IApiEndpointBuilder Method(string method)
    {
        _method = method.ToUpper();
        return this;
    }

    public IApiEndpointBuilder Description(string description)
    {
        _description = description;
        return this;
    }

    public IApiEndpointBuilder Tags(params string[] tags)
    {
        _tags.AddRange(tags);
        return this;
    }

    public IApiEndpointBuilder RequiresAuth(bool required = true)
    {
        _requiresAuth = required;
        return this;
    }

    public IApiEndpointBuilder RequiresPermissions(params string[] permissions)
    {
        _requiredPermissions.AddRange(permissions);
        if (permissions.Length > 0)
        {
            _requiresAuth = true; // Permissions require auth
        }
        return this;
    }

    public IApiEndpointBuilder RateLimit(int requestsPerMinute)
    {
        _rateLimit = requestsPerMinute;
        return this;
    }

    public IApiEndpointBuilder RequestBody<T>() where T : class
    {
        _requestBodyType = typeof(T);
        return this;
    }

    public IApiEndpointBuilder Response<T>() where T : class
    {
        _responseType = typeof(T);
        return this;
    }

    public IApiEndpointBuilder Deprecated(string? message = null)
    {
        _isDeprecated = true;
        _deprecationMessage = message ?? "This endpoint is deprecated";
        return this;
    }

    public IApiEndpointBuilder WithMetadata(string key, object value)
    {
        _metadata[key] = value;
        return this;
    }

    public IApiEndpointBuilder Handler(Func<ApiRequest, Task<ApiResponse>> handler)
    {
        _handler = handler;
        return this;
    }

    public ApiEndpoint Build()
    {
        if (_handler == null)
        {
            throw new InvalidOperationException("Handler is required for API endpoint");
        }

        // Generate unique ID
        var id = $"{_method}:{_version}:{_path}";

        // Build full route pattern
        var fullRoute = $"/api/v{_version}{_path}";

        return new ApiEndpoint
        {
            Id = id,
            Version = $"v{_version}",
            Method = _method,
            Path = _path,
            FullRoute = fullRoute,
            Handler = _handler,
            Description = _description,
            Tags = _tags.ToArray(),
            RequiredPermissions = _requiredPermissions.ToArray(),
            RequiresAuth = _requiresAuth,
            RateLimit = _rateLimit,
            RequestBodyType = _requestBodyType,
            ResponseType = _responseType,
            IsDeprecated = _isDeprecated,
            DeprecationMessage = _deprecationMessage,
            ExtensionId = _extensionId,
            Metadata = _metadata.Count > 0 ? _metadata : null
        };
    }
}
