namespace WebLogic.Shared.Abstractions;

/// <summary>
/// Template engine for rendering HTML templates with data
/// </summary>
public interface ITemplateEngine
{
    /// <summary>
    /// Render a template string with data
    /// </summary>
    string Render(string template, object? data = null);

    /// <summary>
    /// Render a template string with data asynchronously
    /// </summary>
    Task<string> RenderAsync(string template, object? data = null);

    /// <summary>
    /// Render a template file with data
    /// </summary>
    string RenderFile(string templatePath, object? data = null);

    /// <summary>
    /// Render a template file with data asynchronously
    /// </summary>
    Task<string> RenderFileAsync(string templatePath, object? data = null);

    /// <summary>
    /// Register a partial template
    /// </summary>
    void RegisterPartial(string name, string template);

    /// <summary>
    /// Register a helper function
    /// </summary>
    void RegisterHelper(string name, Func<object?, string> helper);

    /// <summary>
    /// Clear template cache
    /// </summary>
    void ClearCache();
}
