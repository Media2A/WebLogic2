namespace WebLogic.Shared.Models.Themes;

/// <summary>
/// Theme manifest containing metadata and configuration
/// </summary>
public class ThemeManifest
{
    /// <summary>
    /// Unique theme identifier (e.g., "default", "blog-modern")
    /// </summary>
    public required string Id { get; set; }

    /// <summary>
    /// Display name
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Theme version (semver)
    /// </summary>
    public required string Version { get; set; }

    /// <summary>
    /// Theme author
    /// </summary>
    public string? Author { get; set; }

    /// <summary>
    /// Theme description
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Screenshot/preview image path (relative to theme directory)
    /// </summary>
    public string? Screenshot { get; set; }

    /// <summary>
    /// Parent theme ID (for theme inheritance)
    /// </summary>
    public string? ParentTheme { get; set; }

    /// <summary>
    /// Supported extensions (empty = all extensions)
    /// </summary>
    public List<string> SupportedExtensions { get; set; } = new();

    /// <summary>
    /// Theme tags for categorization
    /// </summary>
    public List<string> Tags { get; set; } = new();

    /// <summary>
    /// Custom configuration/settings
    /// </summary>
    public Dictionary<string, object> Settings { get; set; } = new();
}
