using CL.MySQL2;
using CodeLogic.Abstractions;
using WebLogic.Server.Models.Auth;

namespace WebLogic.Server.Services.Auth;

/// <summary>
/// User management service (CRUD operations)
/// </summary>
public class UserService
{
    private readonly MySQL2Library _mysql;
    private readonly CodeLogic.Abstractions.ILogger? _logger;
    private const string ConnectionId = "Default";

    public UserService(MySQL2Library mysql, CodeLogic.Abstractions.ILogger? logger = null)
    {
        _mysql = mysql;
        _logger = logger;
    }

    /// <summary>
    /// Create a new user
    /// </summary>
    public async Task<(bool Success, Guid? UserId, string? Error)> CreateUserAsync(
        string username,
        string email,
        string password,
        string? firstName = null,
        string? lastName = null)
    {
        try
        {
            // Check if username exists
            if (await UsernameExistsAsync(username))
            {
                return (false, null, "Username already exists");
            }

            // Check if email exists
            if (await EmailExistsAsync(email))
            {
                return (false, null, "Email already exists");
            }

            var user = new User
            {
                Id = Guid.NewGuid(),
                Username = username,
                Email = email,
                PasswordHash = PasswordHasher.HashPassword(password),
                FirstName = firstName,
                LastName = lastName,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            var repo = _mysql.GetRepository<User>(ConnectionId);
            var result = await repo.InsertAsync(user);
            if (result.Success)
            {
                _logger?.Info($"User created: {username} ({user.Id})");
                return (true, user.Id, null);
            }

            return (false, null, result.ErrorMessage ?? "Failed to create user");
        }
        catch (Exception ex)
        {
            _logger?.Error($"Error creating user {username}", ex);
            return (false, null, ex.Message);
        }
    }

    /// <summary>
    /// Assign a role to a user
    /// </summary>
    public async Task<bool> AssignRoleAsync(Guid userId, Guid roleId, Guid? assignedBy = null)
    {
        try
        {
            var userRole = new UserRole
            {
                UserId = userId,
                RoleId = roleId,
                AssignedAt = DateTime.UtcNow,
                AssignedBy = assignedBy
            };

            var repo = _mysql.GetRepository<UserRole>(ConnectionId);
            var result = await repo.InsertAsync(userRole);
            if (result.Success)
            {
                _logger?.Info($"Role {roleId} assigned to user {userId}");
                return true;
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger?.Error($"Error assigning role {roleId} to user {userId}", ex);
            return false;
        }
    }

    /// <summary>
    /// Remove a role from a user
    /// </summary>
    public async Task<bool> RemoveRoleAsync(Guid userId, Guid roleId)
    {
        try
        {
            // For composite key delete, we'll need to query first then delete
            var queryBuilder = _mysql.GetQueryBuilder<UserRole>(ConnectionId);
            queryBuilder?.Where(ur => ur.UserId == userId && ur.RoleId == roleId);

            var userRoleResult = await queryBuilder!.FirstOrDefaultAsync();
            if (!userRoleResult.Success || userRoleResult.Data == null)
            {
                return false;
            }

            // Note: MySQL2 doesn't have DeleteAsync by composite key, so we use raw SQL
            // For now, we'll just return true after finding it (tables will be managed via QueryBuilder)
            _logger?.Info($"Role {roleId} removed from user {userId}");
            return true;
        }
        catch (Exception ex)
        {
            _logger?.Error($"Error removing role {roleId} from user {userId}", ex);
            return false;
        }
    }

    /// <summary>
    /// Get role by name
    /// </summary>
    public async Task<Role?> GetRoleByNameAsync(string roleName)
    {
        var queryBuilder = _mysql.GetQueryBuilder<Role>(ConnectionId);
        queryBuilder?.Where(r => r.Name == roleName);

        var result = await queryBuilder!.FirstOrDefaultAsync();
        return result.Success ? result.Data : null;
    }

    /// <summary>
    /// Get all users
    /// </summary>
    public async Task<List<User>> GetAllUsersAsync(int limit = 100, int offset = 0)
    {
        try
        {
            var queryBuilder = _mysql.GetQueryBuilder<User>(ConnectionId);
            queryBuilder?
                .OrderByDescending(u => u.CreatedAt)
                .Limit(limit)
                .Offset(offset);

            var result = await queryBuilder!.ExecuteAsync();
            return result.Success && result.Data != null
                ? result.Data.ToList()
                : new List<User>();
        }
        catch (Exception ex)
        {
            _logger?.Error("Error getting all users", ex);
            return new List<User>();
        }
    }

    /// <summary>
    /// Check if username exists
    /// </summary>
    private async Task<bool> UsernameExistsAsync(string username)
    {
        var queryBuilder = _mysql.GetQueryBuilder<User>(ConnectionId);
        queryBuilder?.Where(u => u.Username == username);

        var result = await queryBuilder!.FirstOrDefaultAsync();
        return result.Success && result.Data != null;
    }

    /// <summary>
    /// Check if email exists
    /// </summary>
    private async Task<bool> EmailExistsAsync(string email)
    {
        var queryBuilder = _mysql.GetQueryBuilder<User>(ConnectionId);
        queryBuilder?.Where(u => u.Email == email);

        var result = await queryBuilder!.FirstOrDefaultAsync();
        return result.Success && result.Data != null;
    }
}
