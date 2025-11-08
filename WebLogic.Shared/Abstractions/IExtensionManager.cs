using WebLogic.Shared.Models;

namespace WebLogic.Shared.Abstractions;

/// <summary>
/// Manages loading, initialization, and lifecycle of WebLogic extensions
/// </summary>
public interface IExtensionManager
{
    /// <summary>
    /// All currently loaded extensions
    /// </summary>
    IReadOnlyCollection<IWebLogicExtension> LoadedExtensions { get; }

    /// <summary>
    /// Load and initialize all extensions from the extensions directory
    /// </summary>
    Task<ExtensionLoadResult> LoadExtensionsAsync();

    /// <summary>
    /// Get a specific extension by its ID
    /// </summary>
    IWebLogicExtension? GetExtension(string extensionId);

    /// <summary>
    /// Get extension by type
    /// </summary>
    T? GetExtension<T>() where T : class, IWebLogicExtension;

    /// <summary>
    /// Reload a specific extension (unload and reload)
    /// </summary>
    Task<bool> ReloadExtensionAsync(string extensionId);

    /// <summary>
    /// Unload all extensions
    /// </summary>
    Task UnloadAllAsync();

    /// <summary>
    /// Get all database model types from all loaded extensions
    /// </summary>
    IEnumerable<Type> GetAllDatabaseModels();

    /// <summary>
    /// Check health status of all extensions
    /// </summary>
    Task<Dictionary<string, bool>> HealthCheckAllAsync();
}
