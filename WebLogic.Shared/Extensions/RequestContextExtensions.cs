using Microsoft.Extensions.DependencyInjection;
using WebLogic.Shared.Abstractions;
using WebLogic.Shared.Models;

namespace WebLogic.Shared.Extensions;

/// <summary>
/// Extension methods for RequestContext
/// </summary>
public static class RequestContextExtensions
{
    /// <summary>
    /// Render a template with data
    /// </summary>
    public static string RenderTemplate(this RequestContext context, string template, object? data = null)
    {
        var templateEngine = context.ServiceProvider.GetService<ITemplateEngine>();
        if (templateEngine == null)
            throw new InvalidOperationException("Template engine not registered");

        return templateEngine.Render(template, data);
    }

    /// <summary>
    /// Render a template file with data
    /// </summary>
    public static string RenderTemplateFile(this RequestContext context, string templatePath, object? data = null)
    {
        var templateEngine = context.ServiceProvider.GetService<ITemplateEngine>();
        if (templateEngine == null)
            throw new InvalidOperationException("Template engine not registered");

        return templateEngine.RenderFile(templatePath, data);
    }

    /// <summary>
    /// Render a template with data and return RouteResponse
    /// </summary>
    public static RouteResponse RenderView(this RequestContext context, string template, object? data = null)
    {
        var html = context.RenderTemplate(template, data);
        return RouteResponse.Html(html);
    }

    /// <summary>
    /// Render a template file with data and return RouteResponse
    /// </summary>
    public static RouteResponse RenderViewFile(this RequestContext context, string templatePath, object? data = null)
    {
        var html = context.RenderTemplateFile(templatePath, data);
        return RouteResponse.Html(html);
    }
}
