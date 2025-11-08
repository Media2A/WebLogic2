using CL.MySQL2;
using Microsoft.Extensions.Caching.Distributed;
using WebLogic.Server.Models.Session;

namespace WebLogic.Server.Services;

/// <summary>
/// MySQL2-backed distributed cache implementation for ASP.NET Core sessions
/// </summary>
public class MySQL2DistributedCache : IDistributedCache
{
    private readonly MySQL2Library _mysql;
    private readonly string _connectionId = "Default";

    public MySQL2DistributedCache(MySQL2Library mysql)
    {
        _mysql = mysql;
    }

    public byte[]? Get(string key)
    {
        return GetAsync(key).GetAwaiter().GetResult();
    }

    public async Task<byte[]?> GetAsync(string key, CancellationToken token = default)
    {
        try
        {
            Console.WriteLine($"[MySQL2DistributedCache] GetAsync called - Key: '{key}', Key length: {key.Length}");

            // Testing: QueryBuilder should work now after the fix!
            var qb = _mysql.GetQueryBuilder<SessionData>(_connectionId);
            qb.Where(s => s.SessionId == key);
            var result = await qb.FirstOrDefaultAsync();

            Console.WriteLine($"[MySQL2DistributedCache] Query result - Success: {result.Success}, Data found: {result.Data != null}");

            if (!result.Success)
            {
                Console.WriteLine($"[MySQL2DistributedCache] QUERY FAILED - Error: {result.ErrorMessage}");
                if (result.Exception != null)
                {
                    Console.WriteLine($"[MySQL2DistributedCache] Exception: {result.Exception.Message}");
                    Console.WriteLine($"[MySQL2DistributedCache] Stack trace: {result.Exception.StackTrace}");
                }
                return null;
            }

            if (!result.Success || result.Data == null)
            {
                Console.WriteLine($"[MySQL2DistributedCache] Session not found for key: '{key}'");
                return null;
            }

            var session = result.Data;
            Console.WriteLine($"[MySQL2DistributedCache] Session found - SessionId: '{session.SessionId}', ExpiresAt: {session.ExpiresAt}, Data size: {session.Data.Length} bytes");

            // Check if expired
            if (session.ExpiresAt < DateTime.UtcNow)
            {
                Console.WriteLine($"[MySQL2DistributedCache] Session expired, removing...");
                await RemoveAsync(key, token);
                return null;
            }

            // Update sliding expiration if configured
            if (session.SlidingExpirationSeconds.HasValue)
            {
                var updateQb = _mysql.GetQueryBuilder<SessionData>(_connectionId);
                updateQb.Where(s => s.SessionId == key);

                var newExpiresAt = DateTime.UtcNow.AddSeconds(session.SlidingExpirationSeconds.Value);
                await updateQb.UpdateAsync(new Dictionary<string, object>
                {
                    ["expires_at"] = newExpiresAt,
                    ["last_accessed_at"] = DateTime.UtcNow
                });
                Console.WriteLine($"[MySQL2DistributedCache] Updated sliding expiration to: {newExpiresAt}");
            }

            return session.Data;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[MySQL2DistributedCache] Error getting session {key}: {ex.Message}");
            Console.WriteLine($"[MySQL2DistributedCache] Stack trace: {ex.StackTrace}");
            return null;
        }
    }

    public void Set(string key, byte[] value, DistributedCacheEntryOptions options)
    {
        SetAsync(key, value, options).GetAwaiter().GetResult();
    }

    public async Task SetAsync(string key, byte[] value, DistributedCacheEntryOptions options, CancellationToken token = default)
    {
        Console.WriteLine($"[MySQL2DistributedCache] SetAsync called - Key: {key}, Data size: {value.Length} bytes");

        try
        {
            var expiresAt = DateTime.UtcNow.AddHours(1); // Default 1 hour
            long? slidingExpirationSeconds = null;

            if (options.AbsoluteExpiration.HasValue)
            {
                expiresAt = options.AbsoluteExpiration.Value.UtcDateTime;
                Console.WriteLine($"[MySQL2DistributedCache] Using AbsoluteExpiration: {expiresAt}");
            }
            else if (options.AbsoluteExpirationRelativeToNow.HasValue)
            {
                expiresAt = DateTime.UtcNow.Add(options.AbsoluteExpirationRelativeToNow.Value);
                Console.WriteLine($"[MySQL2DistributedCache] Using AbsoluteExpirationRelativeToNow: {expiresAt}");
            }
            else if (options.SlidingExpiration.HasValue)
            {
                slidingExpirationSeconds = (long)options.SlidingExpiration.Value.TotalSeconds;
                expiresAt = DateTime.UtcNow.Add(options.SlidingExpiration.Value);
                Console.WriteLine($"[MySQL2DistributedCache] Using SlidingExpiration: {slidingExpirationSeconds}s");
            }
            else
            {
                Console.WriteLine($"[MySQL2DistributedCache] Using default expiration: 1 hour");
            }

            // Use Repository instead of QueryBuilder to avoid column mapping bug
            var repo = _mysql.GetRepository<SessionData>(_connectionId);

            // Check if session exists
            Console.WriteLine($"[MySQL2DistributedCache] Checking if session exists...");
            var existingResult = await repo.GetByColumnAsync("session_id", key, cancellationToken: token);

            if (existingResult.Success && existingResult.Data != null)
            {
                Console.WriteLine($"[MySQL2DistributedCache] Session exists, updating...");
                // Update existing session
                var session = existingResult.Data;
                session.Data = value;
                session.ExpiresAt = expiresAt;
                session.SlidingExpirationSeconds = slidingExpirationSeconds;
                session.LastAccessedAt = DateTime.UtcNow;

                var updateResult = await repo.UpdateAsync(session, token);

                if (updateResult.Success)
                {
                    Console.WriteLine($"[MySQL2DistributedCache] Session updated successfully");
                }
                else
                {
                    Console.WriteLine($"[MySQL2DistributedCache] Failed to update session {key}: {updateResult.ErrorMessage}");
                }
            }
            else
            {
                Console.WriteLine($"[MySQL2DistributedCache] Session does not exist, inserting new...");
                // Insert new session
                var session = new SessionData
                {
                    SessionId = key,
                    Data = value,
                    ExpiresAt = expiresAt,
                    SlidingExpirationSeconds = slidingExpirationSeconds,
                    CreatedAt = DateTime.UtcNow,
                    LastAccessedAt = DateTime.UtcNow
                };

                Console.WriteLine($"[MySQL2DistributedCache] Inserting session - Key: '{key}', Key length: {key.Length}");
                var insertResult = await repo.InsertAsync(session, token);

                if (insertResult.Success)
                {
                    Console.WriteLine($"[MySQL2DistributedCache] Session inserted successfully");
                }
                else
                {
                    Console.WriteLine($"[MySQL2DistributedCache] Failed to insert session {key}: {insertResult.ErrorMessage}");
                    if (insertResult.Exception != null)
                    {
                        Console.WriteLine($"[MySQL2DistributedCache] Exception: {insertResult.Exception.Message}");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[MySQL2DistributedCache] Error setting session {key}: {ex.Message}");
            Console.WriteLine($"[MySQL2DistributedCache] Stack trace: {ex.StackTrace}");
        }
    }

    public void Refresh(string key)
    {
        RefreshAsync(key).GetAwaiter().GetResult();
    }

    public async Task RefreshAsync(string key, CancellationToken token = default)
    {
        try
        {
            // Use Repository instead of QueryBuilder to avoid column mapping bug
            var repo = _mysql.GetRepository<SessionData>(_connectionId);
            var result = await repo.GetByColumnAsync("session_id", key, cancellationToken: token);

            if (!result.Success || result.Data == null)
            {
                return;
            }

            var session = result.Data;

            if (session.SlidingExpirationSeconds.HasValue)
            {
                session.ExpiresAt = DateTime.UtcNow.AddSeconds(session.SlidingExpirationSeconds.Value);
                session.LastAccessedAt = DateTime.UtcNow;
                await repo.UpdateAsync(session, token);
                Console.WriteLine($"[MySQL2DistributedCache] Refreshed session {key}, new expiration: {session.ExpiresAt}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[MySQL2DistributedCache] Error refreshing session {key}: {ex.Message}");
        }
    }

    public void Remove(string key)
    {
        RemoveAsync(key).GetAwaiter().GetResult();
    }

    public async Task RemoveAsync(string key, CancellationToken token = default)
    {
        try
        {
            // Use Repository instead of QueryBuilder to avoid column mapping bug
            var repo = _mysql.GetRepository<SessionData>(_connectionId);

            // First get the session to find its ID (primary key)
            var getResult = await repo.GetByColumnAsync("session_id", key, cancellationToken: token);

            if (getResult.Success && getResult.Data != null)
            {
                var result = await repo.DeleteAsync(key, token);

                if (result.Success)
                {
                    Console.WriteLine($"[MySQL2DistributedCache] Removed session {key}");
                }
                else
                {
                    Console.WriteLine($"[MySQL2DistributedCache] Failed to remove session {key}: {result.ErrorMessage}");
                }
            }
            else
            {
                Console.WriteLine($"[MySQL2DistributedCache] Session {key} not found for removal");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[MySQL2DistributedCache] Error removing session {key}: {ex.Message}");
        }
    }

    /// <summary>
    /// Clean up expired sessions (call this periodically)
    /// </summary>
    public async Task CleanupExpiredSessionsAsync()
    {
        try
        {
            // NOTE: Still using QueryBuilder here because Repository doesn't have bulk delete with WHERE conditions
            // This is acceptable since cleanup is not on the critical path and DELETE queries don't have the
            // column mapping issue (only SELECT queries are affected by the bug)
            var qb = _mysql.GetQueryBuilder<SessionData>(_connectionId);
            qb.Where(s => s.ExpiresAt < DateTime.UtcNow);

            var result = await qb.DeleteAsync();

            if (result.Success && result.Data > 0)
            {
                Console.WriteLine($"[MySQL2DistributedCache] Cleaned up {result.Data} expired sessions");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[MySQL2DistributedCache] Error cleaning up expired sessions: {ex.Message}");
        }
    }
}
