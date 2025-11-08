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

        Console.WriteLine($"[AuthenticationMiddleware] Path: {context.Request.Path}");
        Console.WriteLine($"[AuthenticationMiddleware] Session UserId: '{userIdString ?? "(null)"}'");
        Console.WriteLine($"[AuthenticationMiddleware] Session IsAvailable: {context.Session.IsAvailable}");
        Console.WriteLine($"[AuthenticationMiddleware] Session Id: {context.Session.Id}");

        if (!string.IsNullOrEmpty(userIdString) && Guid.TryParse(userIdString, out var userId))
        {
            Console.WriteLine($"[AuthenticationMiddleware] Found valid UserId in session: {userId}");

            // Get user from database
            var user = await _authService.GetUserByIdAsync(userId);

            Console.WriteLine($"[AuthenticationMiddleware] User lookup result: {(user != null ? $"Found {user.Username}" : "Not found")}");

            if (user != null && user.IsActive && !user.IsLocked)
            {
                Console.WriteLine($"[AuthenticationMiddleware] User authenticated: {user.Username}");

                // Store current user in HttpContext.Items for access throughout the request
                context.Items["CurrentUser"] = user;
                context.Items["CurrentUserId"] = user.Id;
                context.Items["IsAuthenticated"] = true;
            }
            else
            {
                Console.WriteLine($"[AuthenticationMiddleware] User invalid or inactive");

                // Invalid user - clear session
                context.Session.Remove("UserId");
                context.Items["IsAuthenticated"] = false;
            }
        }
        else
        {
            Console.WriteLine($"[AuthenticationMiddleware] No valid UserId in session");
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
        Console.WriteLine($"[SignInAsync] Starting sign-in for user: {user.Username} (ID: {user.Id})");
        Console.WriteLine($"[SignInAsync] Session ID before: {context.Session.Id}");
        Console.WriteLine($"[SignInAsync] Session IsAvailable: {context.Session.IsAvailable}");

        context.Session.SetString("UserId", user.Id.ToString());
        Console.WriteLine($"[SignInAsync] Set session UserId: {user.Id}");

        context.Items["CurrentUser"] = user;
        context.Items["CurrentUserId"] = user.Id;
        context.Items["IsAuthenticated"] = true;
        Console.WriteLine($"[SignInAsync] Set HttpContext items");

        // Ensure session is committed
        Console.WriteLine($"[SignInAsync] Committing session...");
        await context.Session.CommitAsync();
        Console.WriteLine($"[SignInAsync] Session committed successfully");
        Console.WriteLine($"[SignInAsync] Session ID after: {context.Session.Id}");

        // Verify it was set
        var verifyUserId = context.Session.GetString("UserId");
        Console.WriteLine($"[SignInAsync] Verification - UserId in session: {verifyUserId}");
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
