using System.Text.Json;
using System.Text.Json.Serialization;

namespace WebLogic.Shared.Models.API;

/// <summary>
/// Standardized API response
/// </summary>
public class ApiResponse
{
    /// <summary>
    /// HTTP status code
    /// </summary>
    [JsonPropertyName("statusCode")]
    public int StatusCode { get; set; } = 200;

    /// <summary>
    /// Whether the request was successful
    /// </summary>
    [JsonPropertyName("success")]
    public bool Success { get; set; } = true;

    /// <summary>
    /// Response data (any type)
    /// </summary>
    [JsonPropertyName("data")]
    public object? Data { get; set; }

    /// <summary>
    /// Error message (if failed)
    /// </summary>
    [JsonPropertyName("error")]
    public string? Error { get; set; }

    /// <summary>
    /// Detailed error information
    /// </summary>
    [JsonPropertyName("errors")]
    public Dictionary<string, string[]>? Errors { get; set; }

    /// <summary>
    /// Response message
    /// </summary>
    [JsonPropertyName("message")]
    public string? Message { get; set; }

    /// <summary>
    /// Pagination metadata (for list responses)
    /// </summary>
    [JsonPropertyName("pagination")]
    public PaginationMetadata? Pagination { get; set; }

    /// <summary>
    /// Response metadata
    /// </summary>
    [JsonPropertyName("meta")]
    public Dictionary<string, object>? Meta { get; set; }

    /// <summary>
    /// Request timestamp
    /// </summary>
    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Custom headers to add to response
    /// </summary>
    [JsonIgnore]
    public Dictionary<string, string> Headers { get; set; } = new();

    // Factory methods for common responses

    /// <summary>
    /// Create a successful response with data
    /// </summary>
    public static ApiResponse Ok(object? data = null, string? message = null)
    {
        return new ApiResponse
        {
            StatusCode = 200,
            Success = true,
            Data = data,
            Message = message
        };
    }

    /// <summary>
    /// Create a created response (201)
    /// </summary>
    public static ApiResponse Created(object? data = null, string? message = null)
    {
        return new ApiResponse
        {
            StatusCode = 201,
            Success = true,
            Data = data,
            Message = message ?? "Resource created successfully"
        };
    }

    /// <summary>
    /// Create a no content response (204)
    /// </summary>
    public static ApiResponse NoContent()
    {
        return new ApiResponse
        {
            StatusCode = 204,
            Success = true
        };
    }

    /// <summary>
    /// Create a bad request response (400)
    /// </summary>
    public static ApiResponse BadRequest(string error, Dictionary<string, string[]>? errors = null)
    {
        return new ApiResponse
        {
            StatusCode = 400,
            Success = false,
            Error = error,
            Errors = errors
        };
    }

    /// <summary>
    /// Create an unauthorized response (401)
    /// </summary>
    public static ApiResponse Unauthorized(string error = "Unauthorized")
    {
        return new ApiResponse
        {
            StatusCode = 401,
            Success = false,
            Error = error
        };
    }

    /// <summary>
    /// Create a forbidden response (403)
    /// </summary>
    public static ApiResponse Forbidden(string error = "Forbidden")
    {
        return new ApiResponse
        {
            StatusCode = 403,
            Success = false,
            Error = error
        };
    }

    /// <summary>
    /// Create a not found response (404)
    /// </summary>
    public static ApiResponse NotFound(string error = "Resource not found")
    {
        return new ApiResponse
        {
            StatusCode = 404,
            Success = false,
            Error = error
        };
    }

    /// <summary>
    /// Create a conflict response (409)
    /// </summary>
    public static ApiResponse Conflict(string error = "Conflict")
    {
        return new ApiResponse
        {
            StatusCode = 409,
            Success = false,
            Error = error
        };
    }

    /// <summary>
    /// Create a validation error response (422)
    /// </summary>
    public static ApiResponse ValidationError(Dictionary<string, string[]> errors)
    {
        return new ApiResponse
        {
            StatusCode = 422,
            Success = false,
            Error = "Validation failed",
            Errors = errors
        };
    }

    /// <summary>
    /// Create an internal server error response (500)
    /// </summary>
    public static ApiResponse ServerError(string error = "Internal server error")
    {
        return new ApiResponse
        {
            StatusCode = 500,
            Success = false,
            Error = error
        };
    }

    /// <summary>
    /// Create a successful response (convenience method)
    /// </summary>
    public static ApiResponse SuccessResponse(object? data = null, int statusCode = 200)
    {
        return new ApiResponse
        {
            StatusCode = statusCode,
            Success = true,
            Data = data
        };
    }

    /// <summary>
    /// Create an error response (convenience method)
    /// </summary>
    public static ApiResponse ErrorResponse(string error, int statusCode = 500)
    {
        return new ApiResponse
        {
            StatusCode = statusCode,
            Success = false,
            Error = error
        };
    }

    /// <summary>
    /// Create a paginated response
    /// </summary>
    public static ApiResponse Paginated<T>(IEnumerable<T> items, int page, int pageSize, int totalItems)
    {
        var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

        return new ApiResponse
        {
            StatusCode = 200,
            Success = true,
            Data = items,
            Pagination = new PaginationMetadata
            {
                Page = page,
                PageSize = pageSize,
                TotalItems = totalItems,
                TotalPages = totalPages,
                HasNextPage = page < totalPages,
                HasPreviousPage = page > 1
            }
        };
    }

    /// <summary>
    /// Serialize to JSON string
    /// </summary>
    public string ToJson()
    {
        return JsonSerializer.Serialize(this, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        });
    }
}

/// <summary>
/// Pagination metadata for list responses
/// </summary>
public class PaginationMetadata
{
    [JsonPropertyName("page")]
    public int Page { get; set; }

    [JsonPropertyName("pageSize")]
    public int PageSize { get; set; }

    [JsonPropertyName("totalItems")]
    public int TotalItems { get; set; }

    [JsonPropertyName("totalPages")]
    public int TotalPages { get; set; }

    [JsonPropertyName("hasNextPage")]
    public bool HasNextPage { get; set; }

    [JsonPropertyName("hasPreviousPage")]
    public bool HasPreviousPage { get; set; }
}
