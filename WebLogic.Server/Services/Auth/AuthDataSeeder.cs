using CL.MySQL2;
using CodeLogic.Abstractions;
using WebLogic.Server.Models.Auth;

namespace WebLogic.Server.Services.Auth;

/// <summary>
/// Seeds default auth data (roles, permissions, admin user)
/// </summary>
public class AuthDataSeeder
{
    private readonly MySQL2Library _mysql;
    private readonly UserService _userService;
    private readonly CodeLogic.Abstractions.ILogger? _logger;
    private const string ConnectionId = "Default";

    public AuthDataSeeder(MySQL2Library mysql, UserService userService, CodeLogic.Abstractions.ILogger? logger = null)
    {
        _mysql = mysql;
        _userService = userService;
        _logger = logger;
    }

    /// <summary>
    /// Seed all default auth data (only runs on first deployment)
    /// </summary>
    public async Task SeedAsync()
    {
        // Check if already deployed
        var dataDir = Path.Combine(AppContext.BaseDirectory, "data");
        var deployedMarker = Path.Combine(dataDir, ".deployed");

        if (File.Exists(deployedMarker))
        {
            Console.WriteLine("→ Auth data already seeded (deployment marker found)");
            return;
        }

        Console.WriteLine("\n→ Seeding auth data (first deployment)...");

        try
        {
            // Step 1: Create default roles
            var roles = await CreateDefaultRolesAsync();

            // Step 2: Create default permissions
            var permissions = await CreateDefaultPermissionsAsync();

            // Step 3: Assign permissions to roles
            await AssignPermissionsToRolesAsync(roles, permissions);

            // NOTE: Default admin user creation is handled by Admin extension's OnApplicationStartedAsync

            // Create deployment marker
            Directory.CreateDirectory(dataDir);
            await File.WriteAllTextAsync(deployedMarker, $"Deployed at: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");

            Console.WriteLine("✓ Auth data seeded successfully");
            Console.WriteLine($"✓ Deployment marker created: {deployedMarker}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"✗ Auth data seeding failed: {ex.Message}");
            _logger?.Error("Auth data seeding failed", ex);
        }
    }

    private async Task<Dictionary<string, Role>> CreateDefaultRolesAsync()
    {
        var roles = new Dictionary<string, Role>();
        var roleRepo = _mysql.GetRepository<Role>(ConnectionId);

        var roleDefinitions = new[]
        {
            ("superadmin", "Super Administrator", "Full system access", true),
            ("admin", "Administrator", "Administrative access", true),
            ("editor", "Editor", "Content management access", true),
            ("user", "User", "Standard user access", true)
        };

        var allRolesResult = await roleRepo.GetAllAsync();
        var existingRoles = allRolesResult.Success && allRolesResult.Data != null
            ? allRolesResult.Data.ToDictionary(r => r.Name)
            : new Dictionary<string, Role>();

        foreach (var (name, displayName, description, isSystem) in roleDefinitions)
        {
            if (existingRoles.TryGetValue(name, out var existingRole))
            {
                roles[name] = existingRole;
                Console.WriteLine($"  • Role '{name}' already exists");
            }
            else
            {
                var role = new Role
                {
                    Id = Guid.NewGuid(),
                    Name = name,
                    DisplayName = displayName,
                    Description = description,
                    IsSystemRole = isSystem,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                var result = await roleRepo.InsertAsync(role);
                if (result.Success)
                {
                    Console.WriteLine($"  ✓ Created role '{name}'");
                    roles[name] = role;
                }
                else
                {
                    Console.WriteLine($"  ✗ Failed to create role '{name}': {result.ErrorMessage}");
                }
            }
        }

        return roles;
    }

    private async Task<Dictionary<string, Permission>> CreateDefaultPermissionsAsync()
    {
        var permissions = new Dictionary<string, Permission>();
        var permRepo = _mysql.GetRepository<Permission>(ConnectionId);

        var permDefinitions = new[]
        {
            // Super admin
            ("*.*", "*", "*", "Full system access"),

            // User management
            ("user.view", "user", "view", "View users"),
            ("user.create", "user", "create", "Create users"),
            ("user.update", "user", "update", "Update users"),
            ("user.delete", "user", "delete", "Delete users"),

            // Role management
            ("role.view", "role", "view", "View roles"),
            ("role.create", "role", "create", "Create roles"),
            ("role.update", "role", "update", "Update roles"),
            ("role.delete", "role", "delete", "Delete roles"),

            // Blog management
            ("blog.view", "blog", "view", "View blog posts"),
            ("blog.create", "blog", "create", "Create blog posts"),
            ("blog.update", "blog", "update", "Update blog posts"),
            ("blog.delete", "blog", "delete", "Delete blog posts"),
            ("blog.publish", "blog", "publish", "Publish blog posts"),

            // Admin panel
            ("admin.access", "admin", "access", "Access admin panel"),

            // System settings
            ("system.settings", "system", "settings", "Manage system settings")
        };

        var allPermsResult = await permRepo.GetAllAsync();
        var existingPermissions = allPermsResult.Success && allPermsResult.Data != null
            ? allPermsResult.Data.ToDictionary(p => p.Name)
            : new Dictionary<string, Permission>();

        foreach (var (name, resource, action, description) in permDefinitions)
        {
            if (existingPermissions.TryGetValue(name, out var existingPermission))
            {
                permissions[name] = existingPermission;
            }
            else
            {
                var permission = new Permission
                {
                    Id = Guid.NewGuid(),
                    Name = name,
                    Resource = resource,
                    Action = action,
                    Description = description,
                    CreatedAt = DateTime.UtcNow
                };

                var result = await permRepo.InsertAsync(permission);
                if (result.Success)
                {
                    permissions[name] = permission;
                }
                else
                {
                    Console.WriteLine($"  ✗ Failed to create permission '{name}': {result.ErrorMessage}");
                }
            }
        }

        Console.WriteLine($"  ✓ Processed {permissions.Count} permissions");
        return permissions;
    }

    private async Task AssignPermissionsToRolesAsync(
        Dictionary<string, Role> roles,
        Dictionary<string, Permission> permissions)
    {
        var rolePermRepo = _mysql.GetRepository<RolePermission>(ConnectionId);
        var allRolePermsResult = await rolePermRepo.GetAllAsync();
        var existingRolePermissions = allRolePermsResult.Success && allRolePermsResult.Data != null
            ? new HashSet<(Guid, Guid)>(allRolePermsResult.Data.Select(rp => (rp.RoleId, rp.PermissionId)))
            : new HashSet<(Guid, Guid)>();

        // Super Admin - all permissions
        if (roles.ContainsKey("superadmin") && permissions.ContainsKey("*.*"))
        {
            await AssignPermissionToRoleAsync(rolePermRepo, roles["superadmin"].Id, permissions["*.*"].Id, existingRolePermissions);
        }

        // Admin - most permissions except super admin wildcard
        if (roles.ContainsKey("admin"))
        {
            var adminPerms = new[] {
                "user.view", "user.create", "user.update",
                "role.view",
                "blog.view", "blog.create", "blog.update", "blog.delete", "blog.publish",
                "admin.access",
                "system.settings"
            };

            foreach (var permName in adminPerms)
            {
                if (permissions.ContainsKey(permName))
                {
                    await AssignPermissionToRoleAsync(rolePermRepo, roles["admin"].Id, permissions[permName].Id, existingRolePermissions);
                }
            }
        }

        // Editor - blog management
        if (roles.ContainsKey("editor"))
        {
            var editorPerms = new[] {
                "blog.view", "blog.create", "blog.update", "blog.publish",
                "admin.access"
            };

            foreach (var permName in editorPerms)
            {
                if (permissions.ContainsKey(permName))
                {
                    await AssignPermissionToRoleAsync(rolePermRepo, roles["editor"].Id, permissions[permName].Id, existingRolePermissions);
                }
            }
        }

        // User - basic permissions
        if (roles.ContainsKey("user"))
        {
            var userPerms = new[] { "blog.view" };

            foreach (var permName in userPerms)
            {
                if (permissions.ContainsKey(permName))
                {
                    await AssignPermissionToRoleAsync(rolePermRepo, roles["user"].Id, permissions[permName].Id, existingRolePermissions);
                }
            }
        }

        Console.WriteLine("  ✓ Assigned permissions to roles");
    }

    private async Task AssignPermissionToRoleAsync(dynamic repo, Guid roleId, Guid permissionId, HashSet<(Guid, Guid)> existingRolePermissions)
    {
        if (existingRolePermissions.Contains((roleId, permissionId)))
        {
            return; // Already assigned
        }

        var rolePermission = new RolePermission
        {
            RoleId = roleId,
            PermissionId = permissionId,
            CreatedAt = DateTime.UtcNow
        };

        await repo.InsertAsync(rolePermission);
    }
}
