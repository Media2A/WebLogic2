using Microsoft.AspNetCore.Http;
using WebLogic.Server.Models.Auth;
using WebLogic.Server.Services.Auth;

namespace WebLogic.Server.Core.Middleware;

/// <summary>
/// Middleware for authentication - validates sessions and injects current user
/// </summary>
public class AuthenticationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly AuthService _authService;

    public AuthenticationMiddleware(RequestDelegate next, AuthService authService)
    {
        _next = next;
        _authService = authService;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Try to get user ID from session or cookie
        var userIdString = context.Session.GetString("UserId");

        if (!string.IsNullOrEmpty(userIdString) && Guid.TryParse(userIdString, out var userId))
        {
            // Get user from database
            var user = await _authService.GetUserByIdAsync(userId);

            if (user != null && user.IsActive && !user.IsLocked)
            {
                // Store current user in HttpContext.Items for access throughout the request
                context.Items["CurrentUser"] = user;
                context.Items["CurrentUserId"] = user.Id;
                context.Items["IsAuthenticated"] = true;
            }
            else
            {
                // Invalid user - clear session
                context.Session.Remove("UserId");
                context.Items["IsAuthenticated"] = false;
            }
        }
        else
        {
            context.Items["IsAuthenticated"] = false;
        }

        await _next(context);
    }
}

/// <summary>
/// Helper extensions for authentication
/// </summary>
public static class AuthenticationExtensions
{
    /// <summary>
    /// Get the current authenticated user
    /// </summary>
    public static User? GetCurrentUser(this HttpContext context)
    {
        return context.Items.TryGetValue("CurrentUser", out var user) ? user as User : null;
    }

    /// <summary>
    /// Get the current user ID
    /// </summary>
    public static Guid? GetCurrentUserId(this HttpContext context)
    {
        return context.Items.TryGetValue("CurrentUserId", out var userId) ? userId as Guid? : null;
    }

    /// <summary>
    /// Check if the current user is authenticated
    /// </summary>
    public static bool IsAuthenticated(this HttpContext context)
    {
        return context.Items.TryGetValue("IsAuthenticated", out var isAuth) && isAuth is true;
    }

    /// <summary>
    /// Sign in a user (creates session)
    /// </summary>
    public static async Task SignInAsync(this HttpContext context, User user)
    {
        context.Session.SetString("UserId", user.Id.ToString());
        context.Items["CurrentUser"] = user;
        context.Items["CurrentUserId"] = user.Id;
        context.Items["IsAuthenticated"] = true;

        // Ensure session is committed
        await context.Session.CommitAsync();
    }

    /// <summary>
    /// Sign out the current user
    /// </summary>
    public static async Task SignOutAsync(this HttpContext context)
    {
        context.Session.Remove("UserId");
        context.Items.Remove("CurrentUser");
        context.Items.Remove("CurrentUserId");
        context.Items["IsAuthenticated"] = false;

        // Clear the entire session
        context.Session.Clear();
        await context.Session.CommitAsync();
    }
}
