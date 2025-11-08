using System.Text.RegularExpressions;
using WebLogic.Server.Models.Auth;
using WebLogic.Shared.Abstractions;

namespace WebLogic.Server.Services.Auth;

/// <summary>
/// Template helpers for authentication and authorization
/// </summary>
public class AuthTemplateHelpers
{
    private readonly AuthService _authService;
    private readonly ITemplateEngine _templateEngine;

    public AuthTemplateHelpers(AuthService authService, ITemplateEngine templateEngine)
    {
        _authService = authService;
        _templateEngine = templateEngine;
    }

    /// <summary>
    /// Register all auth-related template helpers
    /// </summary>
    public void RegisterHelpers()
    {
        // Register helper for current user info
        _templateEngine.RegisterHelper("currentUser", data =>
        {
            if (data is User user)
            {
                return user.Username;
            }
            return string.Empty;
        });

        _templateEngine.RegisterHelper("currentUserEmail", data =>
        {
            if (data is User user)
            {
                return user.Email;
            }
            return string.Empty;
        });

        _templateEngine.RegisterHelper("currentUserFullName", data =>
        {
            if (data is User user)
            {
                return user.FullName;
            }
            return string.Empty;
        });
    }

    /// <summary>
    /// Process auth-specific template directives
    /// {{#ifAuth}}...{{/ifAuth}}
    /// {{#ifRole "admin"}}...{{/ifRole}}
    /// {{#ifPerm "blog.create"}}...{{/ifPerm}}
    /// </summary>
    public string ProcessAuthDirectives(string template, User? currentUser)
    {
        // Process {{#ifAuth}}...{{else}}...{{/ifAuth}}
        template = ProcessIfAuth(template, currentUser);

        // Process {{#ifRole "roleName"}}...{{else}}...{{/ifRole}}
        template = ProcessIfRole(template, currentUser);

        // Process {{#ifPerm "permission.name"}}...{{else}}...{{/ifPerm}}
        template = ProcessIfPerm(template, currentUser);

        return template;
    }

    private string ProcessIfAuth(string template, User? currentUser)
    {
        var pattern = @"\{\{#ifAuth\}\}(.*?)(?:\{\{#else\}\}(.*?))?\{\{/ifAuth\}\}";
        return Regex.Replace(template, pattern, match =>
        {
            var trueBlock = match.Groups[1].Value;
            var falseBlock = match.Groups.Count > 2 ? match.Groups[2].Value : string.Empty;

            return currentUser != null ? trueBlock : falseBlock;
        }, RegexOptions.Singleline);
    }

    private string ProcessIfRole(string template, User? currentUser)
    {
        var pattern = @"\{\{#ifRole\s+[""']([^""']+)[""']\}\}(.*?)(?:\{\{#else\}\}(.*?))?\{\{/ifRole\}\}";
        return Regex.Replace(template, pattern, match =>
        {
            var roleName = match.Groups[1].Value;
            var trueBlock = match.Groups[2].Value;
            var falseBlock = match.Groups.Count > 3 ? match.Groups[3].Value : string.Empty;

            if (currentUser == null)
            {
                return falseBlock;
            }

            // Check if user has the role (synchronous, should be cached)
            var hasRole = _authService.HasRoleAsync(currentUser.Id, roleName).GetAwaiter().GetResult();
            return hasRole ? trueBlock : falseBlock;
        }, RegexOptions.Singleline);
    }

    private string ProcessIfPerm(string template, User? currentUser)
    {
        var pattern = @"\{\{#ifPerm\s+[""']([^""']+)[""']\}\}(.*?)(?:\{\{#else\}\}(.*?))?\{\{/ifPerm\}\}";
        return Regex.Replace(template, pattern, match =>
        {
            var permissionName = match.Groups[1].Value;
            var trueBlock = match.Groups[2].Value;
            var falseBlock = match.Groups.Count > 3 ? match.Groups[3].Value : string.Empty;

            if (currentUser == null)
            {
                return falseBlock;
            }

            // Check if user has the permission (synchronous, should be cached)
            var hasPerm = _authService.HasPermissionAsync(currentUser.Id, permissionName).GetAwaiter().GetResult();
            return hasPerm ? trueBlock : falseBlock;
        }, RegexOptions.Singleline);
    }
}
