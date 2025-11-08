using CodeLogic.Abstractions;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace WebLogic.Shared.Abstractions;

/// <summary>
/// Interface for WebLogic extensions that integrate with CodeLogic2 framework
/// </summary>
public interface IWebLogicExtension : ILibrary
{
    /// <summary>
    /// Extension manifest with metadata
    /// </summary>
    ExtensionManifest ExtensionManifest { get; }

    /// <summary>
    /// Register services with the DI container
    /// </summary>
    void ConfigureServices(IServiceCollection services);

    /// <summary>
    /// Configure middleware pipeline (if needed)
    /// </summary>
    void ConfigureMiddleware(IApplicationBuilder app);

    /// <summary>
    /// Register routes for this extension
    /// </summary>
    void RegisterRoutes(IExtensionRouteBuilder routes);

    /// <summary>
    /// Register API endpoints
    /// </summary>
    void RegisterAPIs(IApiManager apiManager);

    /// <summary>
    /// Get database model types for automatic table sync
    /// </summary>
    IEnumerable<Type> GetDatabaseModels();
}

/// <summary>
/// Extension metadata
/// </summary>
public record ExtensionManifest
{
    /// <summary>
    /// Unique extension ID (e.g., "weblogic.demo")
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// Display name
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Semantic version
    /// </summary>
    public required string Version { get; init; }

    /// <summary>
    /// Author name
    /// </summary>
    public required string Author { get; init; }

    /// <summary>
    /// Description
    /// </summary>
    public required string Description { get; init; }

    /// <summary>
    /// Database connection ID to use (e.g., "Default", "Demo")
    /// </summary>
    public string? DatabaseConnectionId { get; init; }

    /// <summary>
    /// Extension dependencies (other extension IDs)
    /// </summary>
    public IReadOnlyList<string> Dependencies { get; init; } = Array.Empty<string>();
}
