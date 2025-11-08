namespace WebLogic.Shared.Models;

/// <summary>
/// Route definition model
/// </summary>
public class RouteDefinition
{
    /// <summary>
    /// Route path (e.g., "/blog" or "/blog/%")
    /// </summary>
    public required string RoutePath { get; set; }

    /// <summary>
    /// Route type (page, component, api, etc.)
    /// </summary>
    public required string RouteType { get; set; }

    /// <summary>
    /// Extension ID that owns this route
    /// </summary>
    public required string ExtensionId { get; set; }

    /// <summary>
    /// Handler function
    /// </summary>
    public required Func<RequestContext, Task> Handler { get; set; }

    /// <summary>
    /// Allowed HTTP methods
    /// </summary>
    public HttpMethodType[] AllowedMethods { get; set; } = new[] { HttpMethodType.GET };

    /// <summary>
    /// Route priority (higher = checked first)
    /// </summary>
    public int Priority { get; set; } = 0;

    /// <summary>
    /// Whether route is active
    /// </summary>
    public bool IsActive { get; set; } = true;
}
