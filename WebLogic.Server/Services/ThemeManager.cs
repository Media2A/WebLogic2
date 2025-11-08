using System.Collections.Concurrent;
using System.Text.Json;
using WebLogic.Shared.Abstractions;
using WebLogic.Shared.Models.Themes;

namespace WebLogic.Server.Services;

/// <summary>
/// Manages themes for the WebLogic system
/// </summary>
public class ThemeManager : IThemeManager
{
    private readonly string _themesDirectory;
    private readonly ConcurrentDictionary<string, Theme> _themes = new();
    private string _activeThemeId = "default";

    public ThemeManager(string? themesDirectory = null)
    {
        _themesDirectory = themesDirectory ?? Path.Combine(Directory.GetCurrentDirectory(), "themes");

        // Ensure themes directory exists
        Directory.CreateDirectory(_themesDirectory);

        // Load all themes
        RefreshThemes();
    }

    /// <summary>
    /// Get the currently active theme
    /// </summary>
    public Theme GetActiveTheme()
    {
        if (_themes.TryGetValue(_activeThemeId, out var theme))
        {
            return theme;
        }

        // Fallback to default theme
        if (_themes.TryGetValue("default", out var defaultTheme))
        {
            return defaultTheme;
        }

        throw new InvalidOperationException("No active theme found and default theme is missing");
    }

    /// <summary>
    /// Get a theme by ID
    /// </summary>
    public Theme? GetTheme(string themeId)
    {
        _themes.TryGetValue(themeId, out var theme);
        return theme;
    }

    /// <summary>
    /// Get all available themes
    /// </summary>
    public IEnumerable<Theme> GetAllThemes()
    {
        return _themes.Values;
    }

    /// <summary>
    /// Set the active theme
    /// </summary>
    public void SetActiveTheme(string themeId)
    {
        if (!_themes.ContainsKey(themeId))
        {
            throw new ArgumentException($"Theme '{themeId}' not found", nameof(themeId));
        }

        // Mark old theme as inactive
        if (_themes.TryGetValue(_activeThemeId, out var oldTheme))
        {
            oldTheme.IsActive = false;
        }

        // Mark new theme as active
        _activeThemeId = themeId;
        if (_themes.TryGetValue(_activeThemeId, out var newTheme))
        {
            newTheme.IsActive = true;
        }
    }

    /// <summary>
    /// Load/reload all themes from disk
    /// </summary>
    public void RefreshThemes()
    {
        _themes.Clear();

        if (!Directory.Exists(_themesDirectory))
        {
            Console.WriteLine($"Themes directory not found: {_themesDirectory}");
            return;
        }

        // Scan for theme directories
        var themeDirs = Directory.GetDirectories(_themesDirectory);

        foreach (var themeDir in themeDirs)
        {
            try
            {
                var manifestPath = Path.Combine(themeDir, "theme.json");
                if (!File.Exists(manifestPath))
                {
                    Console.WriteLine($"Skipping directory (no theme.json): {themeDir}");
                    continue;
                }

                // Load manifest
                var manifestJson = File.ReadAllText(manifestPath);
                var manifest = JsonSerializer.Deserialize<ThemeManifest>(manifestJson, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (manifest == null)
                {
                    Console.WriteLine($"Failed to parse theme manifest: {manifestPath}");
                    continue;
                }

                var theme = new Theme
                {
                    Manifest = manifest,
                    Path = themeDir,
                    IsActive = manifest.Id == _activeThemeId
                };

                _themes[manifest.Id] = theme;
                Console.WriteLine($"Loaded theme: {manifest.Name} ({manifest.Id})");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading theme from {themeDir}: {ex.Message}");
            }
        }

        // Resolve parent themes
        foreach (var theme in _themes.Values)
        {
            if (!string.IsNullOrEmpty(theme.Manifest.ParentTheme))
            {
                if (_themes.TryGetValue(theme.Manifest.ParentTheme, out var parentTheme))
                {
                    theme.ParentTheme = parentTheme;
                }
            }
        }

        Console.WriteLine($"Loaded {_themes.Count} theme(s)");
    }

    /// <summary>
    /// Get a template file path from the active theme (with fallback to parent themes)
    /// </summary>
    public string? GetTemplatePath(string templateName, string? extension = null)
    {
        var theme = GetActiveTheme();
        return FindFileInThemeHierarchy(theme, theme.TemplatesPath, templateName, extension);
    }

    /// <summary>
    /// Get a layout file path from the active theme (with fallback to parent themes)
    /// </summary>
    public string? GetLayoutPath(string layoutName)
    {
        var theme = GetActiveTheme();
        return FindFileInThemeHierarchy(theme, theme.LayoutsPath, layoutName);
    }

    /// <summary>
    /// Get a partial file path from the active theme (with fallback to parent themes)
    /// </summary>
    public string? GetPartialPath(string partialName)
    {
        var theme = GetActiveTheme();
        return FindFileInThemeHierarchy(theme, theme.PartialsPath, partialName);
    }

    /// <summary>
    /// Get an asset file path from the active theme (with fallback to parent themes)
    /// </summary>
    public string? GetAssetPath(string assetPath)
    {
        var theme = GetActiveTheme();
        var fullPath = Path.Combine(theme.AssetsPath, assetPath);

        if (File.Exists(fullPath))
        {
            return fullPath;
        }

        // Check parent theme
        if (theme.ParentTheme != null)
        {
            var parentPath = Path.Combine(theme.ParentTheme.AssetsPath, assetPath);
            if (File.Exists(parentPath))
            {
                return parentPath;
            }
        }

        return null;
    }

    /// <summary>
    /// Check if a theme exists
    /// </summary>
    public bool ThemeExists(string themeId)
    {
        return _themes.ContainsKey(themeId);
    }

    /// <summary>
    /// Get theme setting value
    /// </summary>
    public T? GetThemeSetting<T>(string key, T? defaultValue = default)
    {
        var theme = GetActiveTheme();

        if (theme.Manifest.Settings.TryGetValue(key, out var value))
        {
            try
            {
                if (value is JsonElement jsonElement)
                {
                    return jsonElement.Deserialize<T>();
                }

                return (T?)value;
            }
            catch
            {
                return defaultValue;
            }
        }

        return defaultValue;
    }

    /// <summary>
    /// Find a file in the theme hierarchy (current theme -> parent theme)
    /// </summary>
    private string? FindFileInThemeHierarchy(Theme theme, string directory, string filename, string? extension = null)
    {
        // Add .html extension if not specified
        if (!filename.Contains('.'))
        {
            filename = $"{filename}.{extension ?? "html"}";
        }

        var fullPath = Path.Combine(directory, filename);

        if (File.Exists(fullPath))
        {
            return fullPath;
        }

        // Check subdirectories (for organized template structures)
        if (Directory.Exists(directory))
        {
            var searchPattern = Path.GetFileName(filename);
            var files = Directory.GetFiles(directory, searchPattern, SearchOption.AllDirectories);
            if (files.Length > 0)
            {
                return files[0];
            }
        }

        // Check parent theme
        if (theme.ParentTheme != null)
        {
            var parentDirectory = directory.Replace(theme.Path, theme.ParentTheme.Path);
            return FindFileInThemeHierarchy(theme.ParentTheme, parentDirectory, filename, extension);
        }

        return null;
    }
}
