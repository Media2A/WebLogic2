using CodeLogic;
using CodeLogic.Abstractions;
using WebLogic.Server.Core.Configuration;
using WebLogic.Shared.Abstractions;
using WebLogic.Shared.Models;

namespace WebLogic.Server.Extensions;

/// <summary>
/// Manages loading and lifecycle of WebLogic extensions
/// </summary>
public class ExtensionManager : IExtensionManager
{
    private readonly Framework _codeLogic;
    private readonly WebLogicServerOptions _options;
    private readonly CodeLogic.Abstractions.ILogger? _logger;
    private readonly List<IWebLogicExtension> _loadedExtensions = new();
    private readonly object _lock = new();

    public IReadOnlyCollection<IWebLogicExtension> LoadedExtensions
    {
        get
        {
            lock (_lock)
            {
                return _loadedExtensions.AsReadOnly();
            }
        }
    }

    public ExtensionManager(
        Framework codeLogic,
        WebLogicServerOptions options,
        CodeLogic.Abstractions.ILogger? logger = null)
    {
        _codeLogic = codeLogic;
        _options = options;
        _logger = logger;
    }

    /// <summary>
    /// Load and initialize all WebLogic extensions from the libraries directory
    /// </summary>
    public async Task<ExtensionLoadResult> LoadExtensionsAsync()
    {
        var result = new ExtensionLoadResult();

        if (!_options.AutoLoadExtensions)
        {
            _logger?.Info("Extension auto-load is disabled");
            return result;
        }

        _logger?.Info("Loading WebLogic extensions...");

        try
        {
            // Get all loaded library instances from CodeLogic2
            var allLibraries = _codeLogic.Libraries.GetLoadedLibraryInstances();
            result.TotalFound = allLibraries.Count();

            foreach (var library in allLibraries)
            {
                // Check if this library implements IWebLogicExtension
                if (library is IWebLogicExtension extension)
                {
                    try
                    {
                        // The library is already loaded and initialized by CodeLogic2
                        // We just need to track it and validate its manifest

                        var manifest = extension.ExtensionManifest;
                        if (manifest == null)
                        {
                            throw new InvalidOperationException("Extension manifest is null");
                        }

                        lock (_lock)
                        {
                            _loadedExtensions.Add(extension);
                        }

                        result.LoadedExtensionIds.Add(manifest.Id);
                        result.SuccessfullyLoaded++;

                        _logger?.Info($"Extension loaded: {manifest.Id} v{manifest.Version} by {manifest.Author}");
                    }
                    catch (Exception ex)
                    {
                        var extensionId = extension.ExtensionManifest?.Id ?? library.GetType().Name;
                        result.Failed++;
                        result.Errors.Add(new ExtensionLoadError
                        {
                            ExtensionId = extensionId,
                            ErrorMessage = ex.Message,
                            Exception = ex
                        });

                        _logger?.Error($"Failed to load extension {extensionId}", ex);
                    }
                }
            }

            if (result.SuccessfullyLoaded > 0)
            {
                _logger?.Info($"Successfully loaded {result.SuccessfullyLoaded} WebLogic extension(s)");
            }
            else
            {
                _logger?.Warning("No WebLogic extensions found");
            }

            if (result.Failed > 0)
            {
                _logger?.Warning($"{result.Failed} extension(s) failed to load");
            }
        }
        catch (Exception ex)
        {
            _logger?.Error("Error during extension loading", ex);
            result.Errors.Add(new ExtensionLoadError
            {
                ExtensionId = "ExtensionManager",
                ErrorMessage = $"Critical error: {ex.Message}",
                Exception = ex
            });
        }

        return result;
    }

    /// <summary>
    /// Get a specific extension by its ID
    /// </summary>
    public IWebLogicExtension? GetExtension(string extensionId)
    {
        lock (_lock)
        {
            return _loadedExtensions.FirstOrDefault(e =>
                e.ExtensionManifest?.Id?.Equals(extensionId, StringComparison.OrdinalIgnoreCase) == true);
        }
    }

    /// <summary>
    /// Get extension by type
    /// </summary>
    public T? GetExtension<T>() where T : class, IWebLogicExtension
    {
        lock (_lock)
        {
            return _loadedExtensions.OfType<T>().FirstOrDefault();
        }
    }

    /// <summary>
    /// Reload a specific extension
    /// </summary>
    public async Task<bool> ReloadExtensionAsync(string extensionId)
    {
        try
        {
            var extension = GetExtension(extensionId);
            if (extension == null)
            {
                _logger?.Warning($"Extension not found: {extensionId}");
                return false;
            }

            _logger?.Info($"Reloading extension: {extensionId}");

            // Unload the extension
            await extension.OnUnloadAsync();

            // Note: CodeLogic2's LibraryManager doesn't support hot reloading yet
            // Extensions need to be reloaded by restarting the application
            _logger?.Warning($"Extension unloaded. Please restart the application to reload: {extensionId}");

            // Remove from loaded extensions list
            lock (_lock)
            {
                _loadedExtensions.Remove(extension);
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger?.Error($"Error reloading extension {extensionId}", ex);
            return false;
        }
    }

    /// <summary>
    /// Unload all extensions
    /// </summary>
    public async Task UnloadAllAsync()
    {
        _logger?.Info("Unloading all extensions...");

        List<IWebLogicExtension> extensionsToUnload;
        lock (_lock)
        {
            extensionsToUnload = new List<IWebLogicExtension>(_loadedExtensions);
        }

        foreach (var extension in extensionsToUnload)
        {
            try
            {
                var extensionId = extension.ExtensionManifest?.Id ?? "unknown";
                _logger?.Debug($"Unloading extension: {extensionId}");
                await extension.OnUnloadAsync();
            }
            catch (Exception ex)
            {
                _logger?.Error($"Error unloading extension", ex);
            }
        }

        lock (_lock)
        {
            _loadedExtensions.Clear();
        }

        _logger?.Info("All extensions unloaded");
    }

    /// <summary>
    /// Get all database model types from all loaded extensions
    /// </summary>
    public IEnumerable<Type> GetAllDatabaseModels()
    {
        var models = new List<Type>();

        lock (_lock)
        {
            foreach (var extension in _loadedExtensions)
            {
                try
                {
                    var extensionModels = extension.GetDatabaseModels();
                    if (extensionModels != null)
                    {
                        models.AddRange(extensionModels);
                    }
                }
                catch (Exception ex)
                {
                    var extensionId = extension.ExtensionManifest?.Id ?? "unknown";
                    _logger?.Error($"Error getting database models from extension {extensionId}", ex);
                }
            }
        }

        return models;
    }

    /// <summary>
    /// Check health status of all extensions
    /// </summary>
    public async Task<Dictionary<string, bool>> HealthCheckAllAsync()
    {
        var results = new Dictionary<string, bool>();

        List<IWebLogicExtension> extensions;
        lock (_lock)
        {
            extensions = new List<IWebLogicExtension>(_loadedExtensions);
        }

        foreach (var extension in extensions)
        {
            var extensionId = extension.ExtensionManifest?.Id ?? "unknown";
            try
            {
                var healthResult = await extension.HealthCheckAsync();
                results[extensionId] = healthResult.IsHealthy;
            }
            catch (Exception ex)
            {
                _logger?.Error($"Health check failed for extension {extensionId}", ex);
                results[extensionId] = false;
            }
        }

        return results;
    }
}
