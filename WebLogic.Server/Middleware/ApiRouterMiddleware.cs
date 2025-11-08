using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using WebLogic.Shared.Abstractions;
using WebLogic.Shared.Models;
using WebLogic.Shared.Models.API;
using WebLogic.Server.Core.Middleware;
using ApiResponse = WebLogic.Shared.Models.API.ApiResponse;

namespace WebLogic.Server.Middleware;

/// <summary>
/// Middleware for handling API requests
/// </summary>
public class ApiRouterMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IApiManager _apiManager;

    public ApiRouterMiddleware(RequestDelegate next, IApiManager apiManager)
    {
        _next = next;
        _apiManager = apiManager;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value ?? "/";

        // Only handle /api/* requests
        if (!path.StartsWith("/api/", StringComparison.OrdinalIgnoreCase))
        {
            await _next(context);
            return;
        }

        try
        {
            await HandleApiRequest(context);
        }
        catch (Exception ex)
        {
            await HandleException(context, ex);
        }
    }

    private async Task HandleApiRequest(HttpContext context)
    {
        var method = context.Request.Method.ToUpper();
        var path = context.Request.Path.Value ?? "/";

        // Find matching endpoint
        var endpoint = _apiManager.FindEndpoint(method, path);

        if (endpoint == null)
        {
            await WriteApiResponse(context, ApiResponse.NotFound("API endpoint not found"));
            return;
        }

        // Check if deprecated
        if (endpoint.IsDeprecated)
        {
            context.Response.Headers["X-API-Deprecated"] = "true";
            if (!string.IsNullOrEmpty(endpoint.DeprecationMessage))
            {
                context.Response.Headers["X-API-Deprecation-Message"] = endpoint.DeprecationMessage;
            }
        }

        // Create RequestContext
        var requestContext = await RequestContext.CreateAsync(context);

        // Parse API request
        var apiRequest = await ParseApiRequest(context, requestContext, endpoint);

        // Check authentication
        if (endpoint.RequiresAuth && apiRequest.UserId == null)
        {
            await WriteApiResponse(context, ApiResponse.Unauthorized("Authentication required"));
            return;
        }

        // Check permissions
        if (endpoint.RequiredPermissions.Length > 0)
        {
            if (!endpoint.RequiredPermissions.All(p => apiRequest.HasPermission(p)))
            {
                await WriteApiResponse(context, ApiResponse.Forbidden("Insufficient permissions"));
                return;
            }
        }

        // Execute handler
        ApiResponse response;
        try
        {
            response = await endpoint.Handler(apiRequest);
        }
        catch (JsonException)
        {
            await WriteApiResponse(context, ApiResponse.BadRequest("Invalid JSON in request body"));
            return;
        }
        catch (Exception ex)
        {
            // Log error here if logger is available
            await WriteApiResponse(context, ApiResponse.ServerError($"Internal server error: {ex.Message}"));
            return;
        }

        // Write response
        await WriteApiResponse(context, response);
    }

    private async Task<ApiRequest> ParseApiRequest(HttpContext context, RequestContext requestContext, ApiEndpoint endpoint)
    {
        // Extract version from path (e.g., /api/v1/users -> v1)
        var pathParts = context.Request.Path.Value?.Split('/', StringSplitOptions.RemoveEmptyEntries) ?? Array.Empty<string>();
        var version = pathParts.Length > 1 && pathParts[1].StartsWith("v", StringComparison.OrdinalIgnoreCase)
            ? pathParts[1]
            : "v1";

        // Extract route parameters
        var routeParams = _apiManager is Server.Services.ApiManager manager
            ? manager.ExtractRouteParams(endpoint.FullRoute, context.Request.Path.Value ?? "/")
            : new Dictionary<string, string>();

        // Parse query parameters
        var queryParams = context.Request.Query
            .ToDictionary(q => q.Key, q => q.Value.ToString());

        // Enable buffering to allow multiple reads of the request body
        context.Request.EnableBuffering();

        // Read request body
        string? body = null;
        JsonDocument? jsonBody = null;

        if (context.Request.ContentLength > 0 || context.Request.Body.CanRead)
        {
            using var reader = new StreamReader(context.Request.Body, Encoding.UTF8, leaveOpen: true);
            body = await reader.ReadToEndAsync();

            Console.WriteLine($"[ApiRouter] Read body - Length: {body?.Length ?? 0}, Content: {(body?.Length > 100 ? body.Substring(0, 100) + "..." : body)}");

            if (!string.IsNullOrEmpty(body) &&
                context.Request.ContentType?.Contains("application/json", StringComparison.OrdinalIgnoreCase) == true)
            {
                try
                {
                    jsonBody = JsonDocument.Parse(body);
                    Console.WriteLine($"[ApiRouter] JSON parsed successfully");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[ApiRouter] JSON parse failed: {ex.Message}");
                }
            }

            // Reset stream position for potential re-reading
            if (context.Request.Body.CanSeek)
            {
                context.Request.Body.Position = 0;
            }
        }
        else
        {
            Console.WriteLine($"[ApiRouter] No body to read - ContentLength: {context.Request.ContentLength}, CanRead: {context.Request.Body.CanRead}");
        }

        // Get authentication info from HttpContext (set by AuthenticationMiddleware)
        Guid? userId = null;
        string[] userPermissions = Array.Empty<string>();

        // First, check if user is authenticated via session (set by AuthenticationMiddleware)
        var currentUserId = context.GetCurrentUserId();
        if (currentUserId.HasValue)
        {
            // User is authenticated via session
            userId = currentUserId.Value;

            // Get user permissions from auth service if available
            var authService = context.RequestServices.GetService(typeof(WebLogic.Server.Services.Auth.AuthService))
                as WebLogic.Server.Services.Auth.AuthService;

            if (authService != null)
            {
                var permissions = await authService.GetUserPermissionsAsync(currentUserId.Value);
                userPermissions = permissions.Select(p => p.Name).ToArray();
            }
        }
        // Fallback: check for header-based auth (for API keys, etc.)
        else if (context.Request.Headers.ContainsKey("X-User-Id"))
        {
            if (Guid.TryParse(context.Request.Headers["X-User-Id"].ToString(), out var parsedUserId))
            {
                userId = parsedUserId;
            }
        }

        if (context.Request.Headers.ContainsKey("X-User-Permissions"))
        {
            userPermissions = context.Request.Headers["X-User-Permissions"]
                .ToString()
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        }

        return new ApiRequest
        {
            Context = requestContext,
            Version = version,
            Method = context.Request.Method.ToUpper(),
            Path = context.Request.Path.Value ?? "/",
            Params = routeParams,
            Query = queryParams,
            Headers = requestContext.Headers,
            Body = body,
            JsonBody = jsonBody,
            UserId = userId,
            UserPermissions = userPermissions,
            ClientIp = requestContext.ClientInformation.IpAddress
        };
    }

    private async Task WriteApiResponse(HttpContext context, ApiResponse apiResponse)
    {
        context.Response.StatusCode = apiResponse.StatusCode;

        // Check if custom content type is set (e.g., for HTML responses)
        var hasCustomContentType = apiResponse.Headers.ContainsKey("Content-Type");

        if (!hasCustomContentType)
        {
            context.Response.ContentType = "application/json; charset=utf-8";
        }

        // Add custom headers
        foreach (var header in apiResponse.Headers)
        {
            context.Response.Headers[header.Key] = header.Value;
        }

        // Add CORS headers (basic implementation)
        context.Response.Headers["Access-Control-Allow-Origin"] = "*";
        context.Response.Headers["Access-Control-Allow-Methods"] = "GET, POST, PUT, DELETE, PATCH, OPTIONS";
        context.Response.Headers["Access-Control-Allow-Headers"] = "Content-Type, Authorization, X-User-Id, X-User-Permissions";

        // If HTML content type, write data directly as HTML
        if (hasCustomContentType && apiResponse.Headers["Content-Type"].Contains("text/html"))
        {
            var html = apiResponse.Data?.ToString() ?? "";
            await context.Response.WriteAsync(html);
        }
        else
        {
            // Standard JSON response
            var json = apiResponse.ToJson();
            await context.Response.WriteAsync(json);
        }
    }

    private async Task HandleException(HttpContext context, Exception ex)
    {
        var response = ApiResponse.ServerError($"Unexpected error: {ex.Message}");
        await WriteApiResponse(context, response);
    }
}
