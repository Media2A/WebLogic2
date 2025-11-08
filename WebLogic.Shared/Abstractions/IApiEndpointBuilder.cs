using WebLogic.Shared.Models.API;

namespace WebLogic.Shared.Abstractions;

/// <summary>
/// Fluent builder for API endpoints
/// </summary>
public interface IApiEndpointBuilder
{
    /// <summary>
    /// Set endpoint version
    /// </summary>
    IApiEndpointBuilder Version(string version);

    /// <summary>
    /// Set endpoint path
    /// </summary>
    IApiEndpointBuilder Path(string path);

    /// <summary>
    /// Set HTTP GET method
    /// </summary>
    IApiEndpointBuilder Get();

    /// <summary>
    /// Set HTTP POST method
    /// </summary>
    IApiEndpointBuilder Post();

    /// <summary>
    /// Set HTTP PUT method
    /// </summary>
    IApiEndpointBuilder Put();

    /// <summary>
    /// Set HTTP DELETE method
    /// </summary>
    IApiEndpointBuilder Delete();

    /// <summary>
    /// Set HTTP PATCH method
    /// </summary>
    IApiEndpointBuilder Patch();

    /// <summary>
    /// Set custom HTTP method
    /// </summary>
    IApiEndpointBuilder Method(string method);

    /// <summary>
    /// Set endpoint description
    /// </summary>
    IApiEndpointBuilder Description(string description);

    /// <summary>
    /// Add tags/categories
    /// </summary>
    IApiEndpointBuilder Tags(params string[] tags);

    /// <summary>
    /// Require authentication
    /// </summary>
    IApiEndpointBuilder RequiresAuth(bool required = true);

    /// <summary>
    /// Set required permissions
    /// </summary>
    IApiEndpointBuilder RequiresPermissions(params string[] permissions);

    /// <summary>
    /// Set rate limit override
    /// </summary>
    IApiEndpointBuilder RateLimit(int requestsPerMinute);

    /// <summary>
    /// Set request body type (for documentation)
    /// </summary>
    IApiEndpointBuilder RequestBody<T>() where T : class;

    /// <summary>
    /// Set response type (for documentation)
    /// </summary>
    IApiEndpointBuilder Response<T>() where T : class;

    /// <summary>
    /// Mark as deprecated
    /// </summary>
    IApiEndpointBuilder Deprecated(string? message = null);

    /// <summary>
    /// Add custom metadata
    /// </summary>
    IApiEndpointBuilder WithMetadata(string key, object value);

    /// <summary>
    /// Set the handler function
    /// </summary>
    IApiEndpointBuilder Handler(Func<ApiRequest, Task<ApiResponse>> handler);

    /// <summary>
    /// Build and register the endpoint
    /// </summary>
    ApiEndpoint Build();
}
