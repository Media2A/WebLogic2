namespace WebLogic.Shared.Models.Themes;

/// <summary>
/// Represents a loaded theme with its manifest and file paths
/// </summary>
public class Theme
{
    /// <summary>
    /// Theme manifest
    /// </summary>
    public required ThemeManifest Manifest { get; set; }

    /// <summary>
    /// Absolute path to theme directory
    /// </summary>
    public required string Path { get; set; }

    /// <summary>
    /// Path to layouts directory
    /// </summary>
    public string LayoutsPath => System.IO.Path.Combine(Path, "layouts");

    /// <summary>
    /// Path to templates directory
    /// </summary>
    public string TemplatesPath => System.IO.Path.Combine(Path, "templates");

    /// <summary>
    /// Path to partials directory
    /// </summary>
    public string PartialsPath => System.IO.Path.Combine(Path, "partials");

    /// <summary>
    /// Path to assets directory
    /// </summary>
    public string AssetsPath => System.IO.Path.Combine(Path, "assets");

    /// <summary>
    /// Whether this theme is currently active
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// Parent theme (if theme inheritance is used)
    /// </summary>
    public Theme? ParentTheme { get; set; }
}
