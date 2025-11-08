using System.Collections.Concurrent;
using CL.NetUtils;
using CodeLogic.Abstractions;
using WebLogic.Server.Core.Configuration;

namespace WebLogic.Server.Core.Middleware;

/// <summary>
/// Advanced security middleware with DNSBL checking, IP geolocation, rate limiting, and IP filtering
/// Powered by CL.NetUtils library
/// </summary>
public class SecurityMiddleware
{
    private readonly RequestDelegate _next;
    private readonly WebLogicServerOptions _options;
    private readonly CodeLogic.Abstractions.ILogger? _logger;
    private readonly NetUtilsLibrary? _netUtils;
    private readonly ConcurrentDictionary<string, RateLimitInfo> _rateLimits = new();
    private readonly ConcurrentDictionary<string, DateTime> _bannedIps = new();
    private readonly ConcurrentDictionary<string, bool> _dnsblCache = new();

    public SecurityMiddleware(
        RequestDelegate next,
        WebLogicServerOptions options,
        CodeLogic.Abstractions.ILogger? logger = null,
        NetUtilsLibrary? netUtils = null)
    {
        _next = next;
        _options = options;
        _logger = logger;
        _netUtils = netUtils;

        // Start cleanup task
        _ = Task.Run(CleanupLoopAsync);
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var clientIp = GetClientIp(context);

        // Step 1: Check DNSBL (if NetUtils is available)
        if (_netUtils != null && _options.EnableDnsblCheck)
        {
            var isBlacklisted = await CheckDnsblAsync(clientIp);
            if (isBlacklisted)
            {
                context.Response.StatusCode = 403;
                await context.Response.WriteAsync("Access denied: IP is blacklisted on DNSBL");
                _logger?.Warning($"DNSBL blocked IP: {clientIp}");
                return;
            }
        }

        // Step 2: Check if IP is banned
        if (_bannedIps.TryGetValue(clientIp, out var banUntil))
        {
            if (DateTime.UtcNow < banUntil)
            {
                context.Response.StatusCode = 429;
                context.Response.Headers["Retry-After"] = ((int)(banUntil - DateTime.UtcNow).TotalSeconds).ToString();
                await context.Response.WriteAsync("Too many requests. You are temporarily banned.");
                _logger?.Warning($"Banned IP attempted access: {clientIp}");
                return;
            }
            else
            {
                // Ban expired, remove from dictionary
                _bannedIps.TryRemove(clientIp, out _);
            }
        }

        // Step 3: Rate limiting (if enabled)
        if (_options.EnableRateLimiting)
        {
            var now = DateTime.UtcNow;
            var rateLimitInfo = _rateLimits.GetOrAdd(clientIp, _ => new RateLimitInfo
            {
                WindowStart = now,
                RequestCount = 0
            });

            bool shouldBan = false;
            int requestCount = 0;

            lock (rateLimitInfo)
            {
                // Reset window if expired
                if (now - rateLimitInfo.WindowStart > _options.RateLimitWindow)
                {
                    rateLimitInfo.WindowStart = now;
                    rateLimitInfo.RequestCount = 0;
                }

                rateLimitInfo.RequestCount++;
                requestCount = rateLimitInfo.RequestCount;

                // Check if limit exceeded
                if (rateLimitInfo.RequestCount > _options.GlobalRateLimit)
                {
                    shouldBan = true;
                }
            }

            if (shouldBan)
            {
                // Ban the IP
                var banUntilTime = now.Add(_options.RateLimitBanDuration);
                _bannedIps.TryAdd(clientIp, banUntilTime);

                context.Response.StatusCode = 429;
                context.Response.Headers["Retry-After"] = ((int)_options.RateLimitBanDuration.TotalSeconds).ToString();
                await context.Response.WriteAsync($"Rate limit exceeded. Banned for {_options.RateLimitBanDuration.TotalMinutes} minutes.");

                // Log with geolocation if available
                await LogBanWithGeoLocationAsync(clientIp, requestCount);
                return;
            }
        }

        // Step 4: Log IP geolocation for first request (if NetUtils available)
        if (_netUtils != null && _options.EnableIpGeolocation)
        {
            _ = Task.Run(() => LogIpGeolocationAsync(clientIp));
        }

        await _next(context);
    }

    /// <summary>
    /// Check if IP is blacklisted on DNSBL using CL.NetUtils
    /// </summary>
    private async Task<bool> CheckDnsblAsync(string clientIp)
    {
        // Check cache first
        if (_dnsblCache.TryGetValue(clientIp, out var cachedResult))
        {
            return cachedResult;
        }

        try
        {
            if (_netUtils == null) return false;

            var dnsblChecker = _netUtils.GetDnsblChecker();
            var result = await dnsblChecker.CheckIpAsync(clientIp);

            // Cache result for 1 hour
            _dnsblCache.TryAdd(clientIp, result.IsBlacklisted);

            if (result.IsBlacklisted)
            {
                _logger?.Warning($"IP {clientIp} is blacklisted on {result.MatchedService}");
            }

            return result.IsBlacklisted;
        }
        catch (Exception ex)
        {
            _logger?.Error($"DNSBL check failed for {clientIp}", ex);
            return false; // Don't block on error
        }
    }

    /// <summary>
    /// Log IP ban with geolocation information
    /// </summary>
    private async Task LogBanWithGeoLocationAsync(string clientIp, int requestCount)
    {
        if (_netUtils == null)
        {
            _logger?.Warning($"IP banned for rate limit violation: {clientIp} ({requestCount} requests)");
            return;
        }

        try
        {
            var ipLocationService = _netUtils.GetIpLocationService();
            var location = await ipLocationService.LookupIpAsync(clientIp);

            if (location != null && !string.IsNullOrEmpty(location.CountryName))
            {
                _logger?.Warning($"IP banned: {clientIp} ({requestCount} requests) - Location: {location.CityName}, {location.CountryName} ({location.CountryCode})");
            }
            else
            {
                _logger?.Warning($"IP banned for rate limit violation: {clientIp} ({requestCount} requests)");
            }
        }
        catch (Exception ex)
        {
            _logger?.Warning($"IP banned for rate limit violation: {clientIp} ({requestCount} requests) - Geolocation lookup failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Log IP geolocation for analytics
    /// </summary>
    private async Task LogIpGeolocationAsync(string clientIp)
    {
        if (_netUtils == null) return;

        try
        {
            var ipLocationService = _netUtils.GetIpLocationService();
            var location = await ipLocationService.LookupIpAsync(clientIp);

            if (location != null && !string.IsNullOrEmpty(location.CountryName))
            {
                _logger?.Debug($"Request from: {clientIp} - {location.CityName}, {location.CountryName} ({location.CountryCode}) - ISP: {location.Isp}");
            }
        }
        catch (Exception)
        {
            // Silent fail for geolocation logging
        }
    }

    private string GetClientIp(HttpContext context)
    {
        // Try X-Forwarded-For first (for proxy scenarios)
        var forwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwardedFor))
        {
            return forwardedFor.Split(',')[0].Trim();
        }

        // Fall back to direct connection
        return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }

    private async Task CleanupLoopAsync()
    {
        while (true)
        {
            try
            {
                await Task.Delay(TimeSpan.FromMinutes(5));

                var now = DateTime.UtcNow;

                // Clean up old rate limit entries
                var expiredRateLimits = _rateLimits
                    .Where(kvp => now - kvp.Value.WindowStart > TimeSpan.FromHours(1))
                    .Select(kvp => kvp.Key)
                    .ToList();

                foreach (var ip in expiredRateLimits)
                {
                    _rateLimits.TryRemove(ip, out _);
                }

                // Clean up expired bans
                var expiredBans = _bannedIps
                    .Where(kvp => now >= kvp.Value)
                    .Select(kvp => kvp.Key)
                    .ToList();

                foreach (var ip in expiredBans)
                {
                    _bannedIps.TryRemove(ip, out _);
                }

                // Clean up old DNSBL cache entries (older than 1 hour)
                if (_dnsblCache.Count > 1000)
                {
                    _dnsblCache.Clear();
                }

                if (expiredRateLimits.Count > 0 || expiredBans.Count > 0)
                {
                    _logger?.Debug($"Cleanup: Removed {expiredRateLimits.Count} rate limit entries and {expiredBans.Count} bans");
                }
            }
            catch (Exception ex)
            {
                _logger?.Error("Error in security middleware cleanup", ex);
            }
        }
    }

    private class RateLimitInfo
    {
        public DateTime WindowStart { get; set; }
        public int RequestCount { get; set; }
    }
}
