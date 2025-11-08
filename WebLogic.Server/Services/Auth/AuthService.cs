using CL.MySQL2;
using CodeLogic.Abstractions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using WebLogic.Server.Core.Configuration;
using WebLogic.Server.Models.Auth;
using WebLogic.Server.Models.Database;

namespace WebLogic.Server.Services.Auth;

/// <summary>
/// Authentication service for login, logout, and session management
/// </summary>
public class AuthService
{
    private readonly MySQL2Library _mysql;
    private readonly CodeLogic.Abstractions.ILogger? _logger;
    private readonly WebLogicServerOptions _options;
    private readonly IHttpContextAccessor? _httpContextAccessor;
    private readonly DatabaseLogger? _dbLogger;
    private const string ConnectionId = "Default";

    public AuthService(
        MySQL2Library mysql,
        IOptions<WebLogicServerOptions> options,
        CodeLogic.Abstractions.ILogger? logger = null,
        IHttpContextAccessor? httpContextAccessor = null,
        DatabaseLogger? dbLogger = null)
    {
        _mysql = mysql;
        _logger = logger;
        _options = options.Value;
        _httpContextAccessor = httpContextAccessor;
        _dbLogger = dbLogger;
    }

    /// <summary>
    /// Authenticate a user by username/email and password
    /// </summary>
    public async Task<AuthResult> LoginAsync(string usernameOrEmail, string password, string? ipAddress = null)
    {
        User? user = null;
        string? failureReason = null;

        try
        {
            Console.WriteLine($"[AuthService] LoginAsync called - Username: '{usernameOrEmail}', Password length: {password?.Length ?? 0}, IP: {ipAddress}");

            // Find user by username or email - get all and filter in memory to avoid reflection issues
            var userRepo = _mysql.GetRepository<User>(ConnectionId);
            var allUsersResult = await userRepo.GetAllAsync();

            Console.WriteLine($"[AuthService] GetAllAsync result - Success: {allUsersResult.Success}, Data count: {allUsersResult.Data?.Count() ?? 0}");

            if (allUsersResult.Success && allUsersResult.Data != null)
            {
                // Debug: Log all user IDs from database
                foreach (var u in allUsersResult.Data)
                {
                    Console.WriteLine($"[AuthService] DEBUG: User in DB - Username: '{u.Username}', ID: '{u.Id}'");
                }

                user = allUsersResult.Data.FirstOrDefault(u =>
                    u.Username == usernameOrEmail || u.Email == usernameOrEmail);
            }

            if (user == null)
            {
                Console.WriteLine($"[AuthService] User not found for: '{usernameOrEmail}'");
                _logger?.Warning($"Login attempt for non-existent user: {usernameOrEmail}");
                failureReason = "User not found";
                await LogLoginAttemptAsync(null, usernameOrEmail, ipAddress, false, failureReason);
                await _dbLogger?.LogSecurityAsync(Models.Database.LogLevel.Warning, $"Login attempt for non-existent user: {usernameOrEmail}", null, usernameOrEmail, new { ipAddress, failureReason }, "AuthService")!;
                return AuthResult.Failed("Invalid username or password");
            }

            Console.WriteLine($"[AuthService] User found - Username: '{user.Username}', Email: '{user.Email}', IsActive: {user.IsActive}, IsLocked: {user.IsLocked}");
            Console.WriteLine($"[AuthService] User ID from database: '{user.Id}' (IsEmpty: {user.Id == Guid.Empty})");
            Console.WriteLine($"[AuthService] User PasswordHash length: {user.PasswordHash?.Length ?? 0}");
            Console.WriteLine($"[AuthService] User PasswordHash (first 20 chars): {(user.PasswordHash?.Length > 20 ? user.PasswordHash.Substring(0, 20) : user.PasswordHash)}");

            // Check if user is active
            if (!user.IsActive)
            {
                Console.WriteLine($"[AuthService] User is not active");
                _logger?.Warning($"Login attempt for inactive user: {user.Username} ({user.Id})");
                failureReason = "Account is disabled";
                await LogLoginAttemptAsync(user.Id, usernameOrEmail, ipAddress, false, failureReason);
                await _dbLogger?.LogSecurityAsync(Models.Database.LogLevel.Warning, $"Login attempt for disabled account: {user.Username}", user.Id, user.Username, new { ipAddress, failureReason }, "AuthService")!;
                return AuthResult.Failed("Account is disabled");
            }

            // Check if account is locked
            if (user.IsLocked)
            {
                var timeRemaining = user.LockedUntil!.Value - DateTime.UtcNow;
                Console.WriteLine($"[AuthService] User is locked until: {user.LockedUntil}");
                _logger?.Warning($"Login attempt for locked user: {user.Username} ({user.Id})");
                failureReason = $"Account locked until {user.LockedUntil:yyyy-MM-dd HH:mm:ss} UTC";
                await LogLoginAttemptAsync(user.Id, usernameOrEmail, ipAddress, false, failureReason);
                await _dbLogger?.LogSecurityAsync(Models.Database.LogLevel.Warning, $"Login attempt for locked account: {user.Username}", user.Id, user.Username, new { ipAddress, failureReason, lockedUntil = user.LockedUntil, failedAttempts = user.FailedLoginAttempts }, "AuthService")!;
                return AuthResult.Failed($"Account is locked. Try again in {(int)timeRemaining.TotalMinutes} minutes");
            }

            // Verify password
            Console.WriteLine($"[AuthService] Attempting password verification...");
            Console.WriteLine($"[AuthService] Input password: '{password}'");
            Console.WriteLine($"[AuthService] Stored hash: '{user.PasswordHash}'");

            var passwordMatches = PasswordHasher.VerifyPassword(password, user.PasswordHash);
            Console.WriteLine($"[AuthService] Password verification result: {passwordMatches}");

            if (!passwordMatches)
            {
                // Increment failed attempts
                await IncrementFailedLoginAttemptsAsync(user);
                Console.WriteLine($"[AuthService] Password verification FAILED for user: {user.Username}");
                _logger?.Warning($"Failed login attempt for user: {user.Username} ({user.Id}) from IP: {ipAddress}");
                failureReason = "Invalid password";
                await LogLoginAttemptAsync(user.Id, usernameOrEmail, ipAddress, false, failureReason);
                await _dbLogger?.LogAuthenticationAsync(Models.Database.LogLevel.Warning, $"Failed login: Invalid password for user {user.Username}", user.Id, user.Username, new { ipAddress, failureReason, failedAttempts = user.FailedLoginAttempts + 1 }, "AuthService")!;
                return AuthResult.Failed("Invalid username or password");
            }

            // Login successful - reset failed attempts and update last login
            await UpdateSuccessfulLoginAsync(user, ipAddress);

            _logger?.Info($"Successful login: {user.Username} ({user.Id}) from IP: {ipAddress}");

            await LogLoginAttemptAsync(user.Id, usernameOrEmail, ipAddress, true, null);
            await _dbLogger?.LogAuthenticationAsync(Models.Database.LogLevel.Info, $"Successful login: {user.Username}", user.Id, user.Username, new { ipAddress }, "AuthService")!;

            return AuthResult.Success(user);
        }
        catch (Exception ex)
        {
            _logger?.Error($"Login error for {usernameOrEmail}", ex);
            failureReason = $"Exception: {ex.Message}";
            await LogLoginAttemptAsync(user?.Id, usernameOrEmail, ipAddress, false, failureReason);
            return AuthResult.Failed("An error occurred during login");
        }
    }

    /// <summary>
    /// Get user by ID
    /// </summary>
    public async Task<User?> GetUserByIdAsync(Guid userId)
    {
        // Testing: QueryBuilder should work now after the fix!
        var qb = _mysql.GetQueryBuilder<User>(ConnectionId);
        qb.Where(u => u.Id == userId);
        var result = await qb.FirstOrDefaultAsync();
        return result.Success ? result.Data : null;
    }

    /// <summary>
    /// Get user's roles
    /// </summary>
    public async Task<List<Role>> GetUserRolesAsync(Guid userId)
    {
        try
        {
            // Testing: QueryBuilder should work now after the fix!
            var userRolesQuery = _mysql.GetQueryBuilder<UserRole>(ConnectionId);
            userRolesQuery.Where(ur => ur.UserId == userId);

            var userRolesResult = await userRolesQuery.ExecuteAsync();
            if (!userRolesResult.Success || userRolesResult.Data == null)
            {
                return new List<Role>();
            }

            var roleIds = userRolesResult.Data.Select(ur => ur.RoleId).ToList();

            // Get roles by IDs
            var roles = new List<Role>();
            foreach (var roleId in roleIds)
            {
                var roleQuery = _mysql.GetQueryBuilder<Role>(ConnectionId);
                roleQuery.Where(r => r.Id == roleId);
                var roleResult = await roleQuery.FirstOrDefaultAsync();
                if (roleResult.Success && roleResult.Data != null)
                {
                    roles.Add(roleResult.Data);
                }
            }
            return roles;
        }
        catch (Exception ex)
        {
            _logger?.Error($"Error getting user roles for user {userId}", ex);
            return new List<Role>();
        }
    }

    /// <summary>
    /// Get user's permissions (via roles)
    /// </summary>
    public async Task<List<Permission>> GetUserPermissionsAsync(Guid userId)
    {
        try
        {
            var roles = await GetUserRolesAsync(userId);
            if (!roles.Any())
            {
                return new List<Permission>();
            }

            var roleIds = roles.Select(r => r.Id).ToList();

            // Testing: QueryBuilder should work now after the fix!
            var permissionIds = new HashSet<Guid>();
            foreach (var roleId in roleIds)
            {
                var rolePermsQuery = _mysql.GetQueryBuilder<RolePermission>(ConnectionId);
                rolePermsQuery.Where(rp => rp.RoleId == roleId);
                var rolePermsResult = await rolePermsQuery.ExecuteAsync();
                if (rolePermsResult.Success && rolePermsResult.Data != null)
                {
                    foreach (var rp in rolePermsResult.Data)
                    {
                        permissionIds.Add(rp.PermissionId);
                    }
                }
            }

            // Get permissions by IDs
            var permissions = new List<Permission>();
            foreach (var permissionId in permissionIds)
            {
                var permQuery = _mysql.GetQueryBuilder<Permission>(ConnectionId);
                permQuery.Where(p => p.Id == permissionId);
                var permResult = await permQuery.FirstOrDefaultAsync();
                if (permResult.Success && permResult.Data != null)
                {
                    permissions.Add(permResult.Data);
                }
            }
            return permissions;
        }
        catch (Exception ex)
        {
            _logger?.Error($"Error getting user permissions for user {userId}", ex);
            return new List<Permission>();
        }
    }

    /// <summary>
    /// Check if user has a specific permission
    /// </summary>
    public async Task<bool> HasPermissionAsync(Guid userId, string permissionName)
    {
        var permissions = await GetUserPermissionsAsync(userId);

        // Check for exact match or wildcard
        return permissions.Any(p =>
            p.Name == permissionName ||
            p.Name == "*.*" ||
            p.Name == $"{permissionName.Split('.')[0]}.*"
        );
    }

    /// <summary>
    /// Check if user has a specific role
    /// </summary>
    public async Task<bool> HasRoleAsync(Guid userId, string roleName)
    {
        var roles = await GetUserRolesAsync(userId);
        return roles.Any(r => r.Name.Equals(roleName, StringComparison.OrdinalIgnoreCase));
    }

    private async Task IncrementFailedLoginAttemptsAsync(User user)
    {
        user.FailedLoginAttempts++;
        user.UpdatedAt = DateTime.UtcNow;

        // Lock account if max attempts reached (use configured value)
        if (user.FailedLoginAttempts >= _options.MaxFailedLoginAttempts)
        {
            user.LockedUntil = DateTime.UtcNow.Add(_options.AccountLockoutDuration);
            _logger?.Warning($"User account locked due to {user.FailedLoginAttempts} failed login attempts: {user.Username} ({user.Id}). Locked until {user.LockedUntil:yyyy-MM-dd HH:mm:ss} UTC");
        }

        var repo = _mysql.GetRepository<User>(ConnectionId);
        await repo.UpdateAsync(user);
    }

    private async Task UpdateSuccessfulLoginAsync(User user, string? ipAddress)
    {
        user.FailedLoginAttempts = 0;
        user.LockedUntil = null;
        user.LastLoginAt = DateTime.UtcNow;
        user.LastLoginIp = ipAddress;
        user.UpdatedAt = DateTime.UtcNow;

        var repo = _mysql.GetRepository<User>(ConnectionId);
        await repo.UpdateAsync(user);
    }

    /// <summary>
    /// Log a login attempt to the database for security auditing
    /// </summary>
    private async Task LogLoginAttemptAsync(Guid? userId, string usernameOrEmail, string? ipAddress, bool isSuccessful, string? failureReason)
    {
        // Only log if enabled in configuration
        if (!_options.EnableLoginAttemptLogging)
        {
            return;
        }

        try
        {
            var httpContext = _httpContextAccessor?.HttpContext;
            var userAgent = httpContext?.Request.Headers["User-Agent"].ToString();

            var loginAttempt = new LoginAttempt
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                UsernameOrEmail = usernameOrEmail,
                IpAddress = ipAddress ?? "unknown",
                UserAgent = userAgent,
                IsSuccessful = isSuccessful,
                FailureReason = failureReason,
                AttemptedAt = DateTime.UtcNow
            };

            var repo = _mysql.GetRepository<LoginAttempt>(ConnectionId);
            var result = await repo.InsertAsync(loginAttempt);

            if (result.Success)
            {
                Console.WriteLine($"[AuthService] Login attempt logged - User: {usernameOrEmail}, Success: {isSuccessful}, IP: {ipAddress}");
            }
            else
            {
                _logger?.Warning($"Failed to log login attempt: {result.ErrorMessage}");
            }
        }
        catch (Exception ex)
        {
            // Don't fail the login if logging fails
            _logger?.Error($"Error logging login attempt for {usernameOrEmail}", ex);
        }
    }
}

/// <summary>
/// Result of an authentication attempt
/// </summary>
public class AuthResult
{
    public bool IsSuccess { get; set; }
    public string? ErrorMessage { get; set; }
    public User? User { get; set; }

    public static AuthResult Success(User user) => new()
    {
        IsSuccess = true,
        User = user
    };

    public static AuthResult Failed(string errorMessage) => new()
    {
        IsSuccess = false,
        ErrorMessage = errorMessage
    };
}
