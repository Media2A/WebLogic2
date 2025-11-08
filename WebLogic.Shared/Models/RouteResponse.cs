namespace WebLogic.Shared.Models;

/// <summary>
/// Response from a route handler
/// </summary>
public class RouteResponse
{
    /// <summary>
    /// HTTP status code
    /// </summary>
    public int StatusCode { get; set; } = 200;

    /// <summary>
    /// Content type (e.g., "text/html", "application/json")
    /// </summary>
    public string ContentType { get; set; } = "text/html";

    /// <summary>
    /// Response body
    /// </summary>
    public string? Body { get; set; }

    /// <summary>
    /// Response headers
    /// </summary>
    public Dictionary<string, string> Headers { get; set; } = new();

    /// <summary>
    /// Cookies to set
    /// </summary>
    public Dictionary<string, string> Cookies { get; set; } = new();

    /// <summary>
    /// Redirect URL (if redirecting)
    /// </summary>
    public string? RedirectUrl { get; set; }

    /// <summary>
    /// Whether this is a redirect response
    /// </summary>
    public bool IsRedirect => !string.IsNullOrEmpty(RedirectUrl);

    /// <summary>
    /// Binary content (for file downloads, images, etc.)
    /// </summary>
    public byte[]? BinaryContent { get; set; }

    /// <summary>
    /// File name for downloads
    /// </summary>
    public string? FileName { get; set; }

    // Helper methods for common responses

    /// <summary>
    /// Create an HTML response
    /// </summary>
    public static RouteResponse Html(string html, int statusCode = 200)
    {
        return new RouteResponse
        {
            StatusCode = statusCode,
            ContentType = "text/html; charset=utf-8",
            Body = html
        };
    }

    /// <summary>
    /// Create a JSON response
    /// </summary>
    public static RouteResponse Json(object data, int statusCode = 200)
    {
        return new RouteResponse
        {
            StatusCode = statusCode,
            ContentType = "application/json; charset=utf-8",
            Body = System.Text.Json.JsonSerializer.Serialize(data)
        };
    }

    /// <summary>
    /// Create a plain text response
    /// </summary>
    public static RouteResponse Text(string text, int statusCode = 200)
    {
        return new RouteResponse
        {
            StatusCode = statusCode,
            ContentType = "text/plain; charset=utf-8",
            Body = text
        };
    }

    /// <summary>
    /// Create a redirect response
    /// </summary>
    public static RouteResponse Redirect(string url, bool permanent = false)
    {
        return new RouteResponse
        {
            StatusCode = permanent ? 301 : 302,
            RedirectUrl = url
        };
    }

    /// <summary>
    /// Create a not found response
    /// </summary>
    public static RouteResponse NotFound(string? message = null)
    {
        return new RouteResponse
        {
            StatusCode = 404,
            ContentType = "text/html; charset=utf-8",
            Body = message ?? "404 - Not Found"
        };
    }

    /// <summary>
    /// Create an unauthorized response
    /// </summary>
    public static RouteResponse Unauthorized(string? message = null)
    {
        return new RouteResponse
        {
            StatusCode = 401,
            ContentType = "text/html; charset=utf-8",
            Body = message ?? "401 - Unauthorized"
        };
    }

    /// <summary>
    /// Create a forbidden response
    /// </summary>
    public static RouteResponse Forbidden(string? message = null)
    {
        return new RouteResponse
        {
            StatusCode = 403,
            ContentType = "text/html; charset=utf-8",
            Body = message ?? "403 - Forbidden"
        };
    }

    /// <summary>
    /// Create a bad request response
    /// </summary>
    public static RouteResponse BadRequest(string? message = null)
    {
        return new RouteResponse
        {
            StatusCode = 400,
            ContentType = "text/html; charset=utf-8",
            Body = message ?? "400 - Bad Request"
        };
    }

    /// <summary>
    /// Create a file download response
    /// </summary>
    public static RouteResponse File(byte[] content, string fileName, string contentType = "application/octet-stream")
    {
        return new RouteResponse
        {
            StatusCode = 200,
            ContentType = contentType,
            BinaryContent = content,
            FileName = fileName
        };
    }
}
