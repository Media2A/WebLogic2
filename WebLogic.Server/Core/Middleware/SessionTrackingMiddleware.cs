using CL.Core.Utilities;
using CL.MySQL2;
using CodeLogic.Abstractions;
using WebLogic.Server.Core.Configuration;
using WebLogic.Server.Models.Database;

namespace WebLogic.Server.Core.Middleware;

/// <summary>
/// Middleware for tracking sessions in the database
/// </summary>
public class SessionTrackingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly WebLogicServerOptions _options;
    private readonly CodeLogic.Abstractions.ILogger? _logger;

    public SessionTrackingMiddleware(
        RequestDelegate next,
        WebLogicServerOptions options,
        CodeLogic.Abstractions.ILogger? logger = null)
    {
        _next = next;
        _options = options;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, MySQL2Library mysql)
    {
        if (!_options.EnableSessionTracking)
        {
            await _next(context);
            return;
        }

        try
        {
            var sessionId = context.Session.Id;
            if (string.IsNullOrEmpty(sessionId))
            {
                // Session not yet initialized, let it initialize first
                await _next(context);
                return;
            }

            var repository = mysql.GetRepository<Session>(_options.DefaultDatabaseConnectionId);
            if (repository == null)
            {
                _logger?.Warning("Session tracking disabled: Database repository not available");
                await _next(context);
                return;
            }

            // Try to get existing session
            var existingSession = await repository.GetByColumnAsync("session_id", sessionId);

            if (existingSession.Data == null)
            {
                // Create new session record
                var newSession = new Session
                {
                    SessionId = sessionId,
                    ClientId = Guid.NewGuid().ToString(),
                    IpAddress = GetClientIp(context),
                    IpForward = context.Request.Headers["X-Forwarded-For"].ToString(),
                    UserAgent = context.Request.Headers["User-Agent"].ToString(),
                    IsBot = IsWebCrawler(context.Request.Headers["User-Agent"].ToString()),
                    CreatedAt = DateTime.UtcNow,
                    LastActivity = DateTime.UtcNow,
                    LastUrl = context.Request.Path.Value ?? "/"
                };

                var insertResult = await repository.InsertAsync(newSession);
                if (insertResult.Success)
                {
                    _logger?.Debug($"New session created: {sessionId}");
                }
            }
            else
            {
                // Update existing session
                var session = existingSession.Data;
                session.LastActivity = DateTime.UtcNow;
                session.LastUrl = context.Request.Path.Value ?? "/";

                await repository.UpdateAsync(session);
            }
        }
        catch (Exception ex)
        {
            _logger?.Error("Error tracking session", ex);
            // Don't fail the request if session tracking fails
        }

        await _next(context);
    }

    private string GetClientIp(HttpContext context)
    {
        var forwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwardedFor))
        {
            return forwardedFor.Split(',')[0].Trim();
        }

        return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }

    private bool IsWebCrawler(string userAgent)
    {
        if (string.IsNullOrEmpty(userAgent))
            return false;

        var botPatterns = new[] { "bot", "crawler", "spider", "slurp", "scraper", "crawl" };
        return botPatterns.Any(pattern => userAgent.Contains(pattern, StringComparison.OrdinalIgnoreCase));
    }
}
