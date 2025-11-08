using WebLogic.Shared.Models.Themes;

namespace WebLogic.Shared.Abstractions;

/// <summary>
/// Manages themes for the WebLogic system
/// </summary>
public interface IThemeManager
{
    /// <summary>
    /// Get the currently active theme
    /// </summary>
    Theme GetActiveTheme();

    /// <summary>
    /// Get a theme by ID
    /// </summary>
    Theme? GetTheme(string themeId);

    /// <summary>
    /// Get all available themes
    /// </summary>
    IEnumerable<Theme> GetAllThemes();

    /// <summary>
    /// Set the active theme
    /// </summary>
    void SetActiveTheme(string themeId);

    /// <summary>
    /// Load/reload all themes from disk
    /// </summary>
    void RefreshThemes();

    /// <summary>
    /// Get a template file path from the active theme (with fallback to parent themes)
    /// </summary>
    string? GetTemplatePath(string templateName, string? extension = null);

    /// <summary>
    /// Get a layout file path from the active theme (with fallback to parent themes)
    /// </summary>
    string? GetLayoutPath(string layoutName);

    /// <summary>
    /// Get a partial file path from the active theme (with fallback to parent themes)
    /// </summary>
    string? GetPartialPath(string partialName);

    /// <summary>
    /// Get an asset file path from the active theme (with fallback to parent themes)
    /// </summary>
    string? GetAssetPath(string assetPath);

    /// <summary>
    /// Check if a theme exists
    /// </summary>
    bool ThemeExists(string themeId);

    /// <summary>
    /// Get theme setting value
    /// </summary>
    T? GetThemeSetting<T>(string key, T? defaultValue = default);
}
