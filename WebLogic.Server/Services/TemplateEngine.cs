using System.Collections;
using System.Collections.Concurrent;
using System.Net;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using WebLogic.Shared.Abstractions;

namespace WebLogic.Server.Services;

/// <summary>
/// Template engine for rendering HTML templates with data
/// </summary>
public class TemplateEngine : ITemplateEngine
{
    private readonly ConcurrentDictionary<string, string> _partials = new();
    private readonly ConcurrentDictionary<string, Func<object?, string>> _helpers = new();
    private readonly ConcurrentDictionary<string, string> _templateCache = new();
    private readonly string _templatesDirectory;

    public TemplateEngine(string? templatesDirectory = null)
    {
        _templatesDirectory = templatesDirectory ?? "templates";
    }

    /// <summary>
    /// Render a template string with data
    /// </summary>
    public string Render(string template, object? data = null)
    {
        return RenderInternal(template, data);
    }

    /// <summary>
    /// Render a template string with data asynchronously
    /// </summary>
    public Task<string> RenderAsync(string template, object? data = null)
    {
        return Task.FromResult(RenderInternal(template, data));
    }

    /// <summary>
    /// Render a template file with data
    /// </summary>
    public string RenderFile(string templatePath, object? data = null)
    {
        var template = LoadTemplate(templatePath);
        return RenderInternal(template, data);
    }

    /// <summary>
    /// Render a template file with data asynchronously
    /// </summary>
    public async Task<string> RenderFileAsync(string templatePath, object? data = null)
    {
        var template = await LoadTemplateAsync(templatePath);
        return RenderInternal(template, data);
    }

    /// <summary>
    /// Register a partial template
    /// </summary>
    public void RegisterPartial(string name, string template)
    {
        _partials[name] = template;
    }

    /// <summary>
    /// Register a helper function
    /// </summary>
    public void RegisterHelper(string name, Func<object?, string> helper)
    {
        _helpers[name] = helper;
    }

    /// <summary>
    /// Clear template cache
    /// </summary>
    public void ClearCache()
    {
        _templateCache.Clear();
    }

    /// <summary>
    /// Internal rendering logic
    /// </summary>
    private string RenderInternal(string template, object? data)
    {
        if (string.IsNullOrEmpty(template))
            return string.Empty;

        var result = template;

        // Process partials first: {{> partialName}}
        result = RenderPartials(result, data);

        // Process loops: {{#each items}}...{{/each}}
        result = RenderEach(result, data);

        // Process conditionals: {{#if condition}}...{{/if}}
        result = RenderIf(result, data);

        // Process variables: {{variable}} and {{{rawVariable}}}
        result = RenderVariables(result, data);

        return result;
    }

    /// <summary>
    /// Render partials
    /// </summary>
    private string RenderPartials(string template, object? data)
    {
        var pattern = @"\{\{>\s*(\w+)\s*\}\}";
        return Regex.Replace(template, pattern, match =>
        {
            var partialName = match.Groups[1].Value;
            if (_partials.TryGetValue(partialName, out var partial))
            {
                return RenderInternal(partial, data);
            }
            return match.Value; // Keep original if partial not found
        });
    }

    /// <summary>
    /// Render each loops
    /// </summary>
    private string RenderEach(string template, object? data)
    {
        var pattern = @"\{\{#each\s+(\w+(?:\.\w+)*)\}\}(.*?)\{\{/each\}\}";
        return Regex.Replace(template, pattern, match =>
        {
            var path = match.Groups[1].Value;
            var innerTemplate = match.Groups[2].Value;

            var items = GetValue(data, path) as IEnumerable;
            if (items == null)
                return string.Empty;

            var sb = new StringBuilder();
            foreach (var item in items)
            {
                sb.Append(RenderInternal(innerTemplate, item));
            }

            return sb.ToString();
        }, RegexOptions.Singleline);
    }

    /// <summary>
    /// Render if conditionals
    /// </summary>
    private string RenderIf(string template, object? data)
    {
        var pattern = @"\{\{#if\s+(\w+(?:\.\w+)*)\}\}(.*?)(?:\{\{#else\}\}(.*?))?\{\{/if\}\}";
        return Regex.Replace(template, pattern, match =>
        {
            var path = match.Groups[1].Value;
            var trueTemplate = match.Groups[2].Value;
            var falseTemplate = match.Groups[3].Success ? match.Groups[3].Value : string.Empty;

            var value = GetValue(data, path);
            var condition = IsTrue(value);

            return condition
                ? RenderInternal(trueTemplate, data)
                : RenderInternal(falseTemplate, data);
        }, RegexOptions.Singleline);
    }

    /// <summary>
    /// Render variables
    /// </summary>
    private string RenderVariables(string template, object? data)
    {
        // Raw variables: {{{variable}}} - no HTML encoding
        template = Regex.Replace(template, @"\{\{\{(\w+(?:\.\w+)*)\}\}\}", match =>
        {
            var path = match.Groups[1].Value;
            var value = GetValue(data, path);
            return value?.ToString() ?? string.Empty;
        });

        // Encoded variables: {{variable}} - HTML encoded
        template = Regex.Replace(template, @"\{\{(\w+(?:\.\w+)*)\}\}", match =>
        {
            var path = match.Groups[1].Value;
            var value = GetValue(data, path);
            return WebUtility.HtmlEncode(value?.ToString() ?? string.Empty);
        });

        return template;
    }

    /// <summary>
    /// Get value from object by path (supports nested properties)
    /// </summary>
    private object? GetValue(object? obj, string path)
    {
        if (obj == null || string.IsNullOrEmpty(path))
            return null;

        var parts = path.Split('.');
        var current = obj;

        foreach (var part in parts)
        {
            if (current == null)
                return null;

            var type = current.GetType();

            // Try property
            var property = type.GetProperty(part, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
            if (property != null)
            {
                current = property.GetValue(current);
                continue;
            }

            // Try field
            var field = type.GetField(part, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
            if (field != null)
            {
                current = field.GetValue(current);
                continue;
            }

            // Try dictionary
            if (current is IDictionary dict && dict.Contains(part))
            {
                current = dict[part];
                continue;
            }

            // Try helper function
            if (_helpers.TryGetValue(part, out var helper))
            {
                return helper(current);
            }

            return null;
        }

        return current;
    }

    /// <summary>
    /// Check if value is truthy
    /// </summary>
    private bool IsTrue(object? value)
    {
        if (value == null)
            return false;

        if (value is bool b)
            return b;

        if (value is string s)
            return !string.IsNullOrEmpty(s);

        if (value is int i)
            return i != 0;

        if (value is double d)
            return d != 0;

        if (value is ICollection collection)
            return collection.Count > 0;

        return true;
    }

    /// <summary>
    /// Load template from file
    /// </summary>
    private string LoadTemplate(string templatePath)
    {
        if (_templateCache.TryGetValue(templatePath, out var cached))
            return cached;

        var fullPath = Path.Combine(_templatesDirectory, templatePath);
        if (!File.Exists(fullPath))
            throw new FileNotFoundException($"Template not found: {templatePath}");

        var template = File.ReadAllText(fullPath);
        _templateCache[templatePath] = template;
        return template;
    }

    /// <summary>
    /// Load template from file asynchronously
    /// </summary>
    private async Task<string> LoadTemplateAsync(string templatePath)
    {
        if (_templateCache.TryGetValue(templatePath, out var cached))
            return cached;

        var fullPath = Path.Combine(_templatesDirectory, templatePath);
        if (!File.Exists(fullPath))
            throw new FileNotFoundException($"Template not found: {templatePath}");

        var template = await File.ReadAllTextAsync(fullPath);
        _templateCache[templatePath] = template;
        return template;
    }
}
