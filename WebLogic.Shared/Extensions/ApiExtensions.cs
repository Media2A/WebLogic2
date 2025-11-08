using Microsoft.Extensions.DependencyInjection;
using WebLogic.Shared.Abstractions;
using WebLogic.Shared.Models;
using ApiResponse = WebLogic.Shared.Models.API.ApiResponse;

namespace WebLogic.Shared.Extensions;

/// <summary>
/// Extension methods for API operations on RequestContext
/// </summary>
public static class ApiExtensions
{
    /// <summary>
    /// Get the API manager from services
    /// </summary>
    public static IApiManager GetApiManager(this RequestContext context)
    {
        var apiManager = context.ServiceProvider.GetService<IApiManager>();
        if (apiManager == null)
        {
            throw new InvalidOperationException("API Manager not registered. Please configure API system in your DI container.");
        }

        return apiManager;
    }

    /// <summary>
    /// Create API response with JSON data
    /// </summary>
    public static RouteResponse ApiJson(this RequestContext context, ApiResponse apiResponse)
    {
        var response = RouteResponse.Json(apiResponse, apiResponse.StatusCode);

        // Add custom headers
        foreach (var header in apiResponse.Headers)
        {
            response.Headers[header.Key] = header.Value;
        }

        return response;
    }

    /// <summary>
    /// Create successful API response
    /// </summary>
    public static RouteResponse ApiOk(this RequestContext context, object? data = null, string? message = null)
    {
        return context.ApiJson(ApiResponse.Ok(data, message));
    }

    /// <summary>
    /// Create created API response (201)
    /// </summary>
    public static RouteResponse ApiCreated(this RequestContext context, object? data = null, string? message = null)
    {
        return context.ApiJson(ApiResponse.Created(data, message));
    }

    /// <summary>
    /// Create no content API response (204)
    /// </summary>
    public static RouteResponse ApiNoContent(this RequestContext context)
    {
        return context.ApiJson(ApiResponse.NoContent());
    }

    /// <summary>
    /// Create bad request API response (400)
    /// </summary>
    public static RouteResponse ApiBadRequest(this RequestContext context, string error, Dictionary<string, string[]>? errors = null)
    {
        return context.ApiJson(ApiResponse.BadRequest(error, errors));
    }

    /// <summary>
    /// Create unauthorized API response (401)
    /// </summary>
    public static RouteResponse ApiUnauthorized(this RequestContext context, string error = "Unauthorized")
    {
        return context.ApiJson(ApiResponse.Unauthorized(error));
    }

    /// <summary>
    /// Create forbidden API response (403)
    /// </summary>
    public static RouteResponse ApiForbidden(this RequestContext context, string error = "Forbidden")
    {
        return context.ApiJson(ApiResponse.Forbidden(error));
    }

    /// <summary>
    /// Create not found API response (404)
    /// </summary>
    public static RouteResponse ApiNotFound(this RequestContext context, string error = "Resource not found")
    {
        return context.ApiJson(ApiResponse.NotFound(error));
    }

    /// <summary>
    /// Create conflict API response (409)
    /// </summary>
    public static RouteResponse ApiConflict(this RequestContext context, string error = "Conflict")
    {
        return context.ApiJson(ApiResponse.Conflict(error));
    }

    /// <summary>
    /// Create validation error API response (422)
    /// </summary>
    public static RouteResponse ApiValidationError(this RequestContext context, Dictionary<string, string[]> errors)
    {
        return context.ApiJson(ApiResponse.ValidationError(errors));
    }

    /// <summary>
    /// Create server error API response (500)
    /// </summary>
    public static RouteResponse ApiServerError(this RequestContext context, string error = "Internal server error")
    {
        return context.ApiJson(ApiResponse.ServerError(error));
    }

    /// <summary>
    /// Create paginated API response
    /// </summary>
    public static RouteResponse ApiPaginated<T>(
        this RequestContext context,
        IEnumerable<T> items,
        int page,
        int pageSize,
        int totalItems)
    {
        return context.ApiJson(ApiResponse.Paginated(items, page, pageSize, totalItems));
    }
}
