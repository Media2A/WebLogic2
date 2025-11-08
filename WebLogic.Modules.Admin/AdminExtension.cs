using CodeLogic.Abstractions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using WebLogic.Shared.Abstractions;
using WebLogic.Shared.Models;
using WebLogic.Shared.Models.API;
using WebLogic.Server.Services.Auth;
using WebLogic.Server.Core.Middleware;
using WebLogic.Server.Models.Auth;
using CL.MySQL2;
using ApiResponse = WebLogic.Shared.Models.API.ApiResponse;

namespace WebLogic.Modules.Admin;

/// <summary>
/// Admin module for WebLogic - User, role, and permission management
/// </summary>
public class AdminExtension : IWebLogicExtension, ILibrary, IApplicationLifecycle
{
    public ILibraryManifest Manifest { get; } = new AdminManifest();
    public ExtensionManifest ExtensionManifest { get; private set; } = null!;

    private IServiceProvider? _services;
    private ITemplateEngine? _templateEngine;
    private IThemeManager? _themeManager;
    private AuthService? _authService;
    private UserService? _userService;
    private MySQL2Library? _mysql;

    public async Task OnLoadAsync(LibraryContext context)
    {
        Console.WriteLine("    [Admin] Admin extension loading...");

        ExtensionManifest = new ExtensionManifest
        {
            Id = "weblogic.modules.admin",
            Name = "Admin Module",
            Version = "1.0.0",
            Author = "WebLogic",
            Description = "Administration panel for managing users, roles, and permissions",
            Dependencies = new[] { "cl.mysql2" }
        };

        await Task.CompletedTask;
    }

    public async Task OnInitializeAsync()
    {
        Console.WriteLine("    [Admin] Admin extension initialized");
        await Task.CompletedTask;
    }

    public async Task OnUnloadAsync()
    {
        Console.WriteLine("    [Admin] Admin extension unloading...");
        await Task.CompletedTask;
    }

    public Task<HealthCheckResult> HealthCheckAsync()
    {
        return Task.FromResult(new HealthCheckResult
        {
            IsHealthy = true,
            Message = "Admin extension is healthy"
        });
    }

    public void ConfigureServices(IServiceCollection services)
    {
        Console.WriteLine("    [Admin] Configuring Admin services...");
        // No additional services needed for now
    }

    public void ConfigureMiddleware(IApplicationBuilder app)
    {
        Console.WriteLine("    [Admin] Configuring Admin middleware...");
        // No additional middleware needed for now
    }

    public void RegisterRoutes(IExtensionRouteBuilder routes)
    {
        Console.WriteLine("    [Admin] Registering Admin routes...");

        // Login page
        routes.MapGet("/admin/login", async context => await HandleLoginPageAsync(context));

        // Dashboard (requires auth)
        routes.MapGet("/admin", async context => await HandleDashboardAsync(context));

        // Logout
        routes.MapGet("/admin/logout", async context => await HandleLogoutAsync(context));

        // Users page
        routes.MapGet("/admin/users", async context => await HandleUsersPageAsync(context));

        // Roles page
        routes.MapGet("/admin/roles", async context => await HandleRolesPageAsync(context));

        // Logs page
        routes.MapGet("/admin/logs", async context => await HandleLogsPageAsync(context));

        Console.WriteLine("    [Admin] ✓ Registered 6 page routes");
    }

    public void RegisterAPIs(IApiManager apiManager)
    {
        Console.WriteLine("    [Admin] Registering Admin API endpoints...");

        var extensionId = ExtensionManifest.Id;

        // ============= AUTH APIs =============

        // Login API
        apiManager.RegisterEndpoint(apiManager.CreateEndpoint(extensionId)
            .Version("1")
            .Path("/admin/api/login")
            .Post()
            .Description("Admin login")
            .Tags("Admin", "Auth")
            .Handler(async req => await HandleLoginApiAsync(new ApiContext(req)))
            .Build());

        // ============= USER MANAGEMENT APIs =============

        // List users with pagination and filtering
        apiManager.RegisterEndpoint(apiManager.CreateEndpoint(extensionId)
            .Version("1")
            .Path("/admin/api/users")
            .Get()
            .Description("List all users with pagination and filtering")
            .Tags("Admin", "Users")
            .Handler(async req => await HandleListUsersAsync(new ApiContext(req)))
            .Build());

        // Get single user by ID
        apiManager.RegisterEndpoint(apiManager.CreateEndpoint(extensionId)
            .Version("1")
            .Path("/admin/api/users/:id")
            .Get()
            .Description("Get user by ID")
            .Tags("Admin", "Users")
            .Handler(async req => await HandleGetUserAsync(new ApiContext(req)))
            .Build());

        // Create new user
        apiManager.RegisterEndpoint(apiManager.CreateEndpoint(extensionId)
            .Version("1")
            .Path("/admin/api/users")
            .Post()
            .Description("Create new user")
            .Tags("Admin", "Users")
            .Handler(async req => await HandleCreateUserAsync(new ApiContext(req)))
            .Build());

        // Update user
        apiManager.RegisterEndpoint(apiManager.CreateEndpoint(extensionId)
            .Version("1")
            .Path("/admin/api/users/:id")
            .Put()
            .Description("Update user")
            .Tags("Admin", "Users")
            .Handler(async req => await HandleUpdateUserAsync(new ApiContext(req)))
            .Build());

        // Delete user
        apiManager.RegisterEndpoint(apiManager.CreateEndpoint(extensionId)
            .Version("1")
            .Path("/admin/api/users/:id")
            .Delete()
            .Description("Delete user")
            .Tags("Admin", "Users")
            .Handler(async req => await HandleDeleteUserAsync(new ApiContext(req)))
            .Build());

        // Assign role to user
        apiManager.RegisterEndpoint(apiManager.CreateEndpoint(extensionId)
            .Version("1")
            .Path("/admin/api/users/:id/roles")
            .Post()
            .Description("Assign role to user")
            .Tags("Admin", "Users", "Roles")
            .Handler(async req => await HandleAssignRoleAsync(new ApiContext(req)))
            .Build());

        // Remove role from user
        apiManager.RegisterEndpoint(apiManager.CreateEndpoint(extensionId)
            .Version("1")
            .Path("/admin/api/users/:id/roles/:roleId")
            .Delete()
            .Description("Remove role from user")
            .Tags("Admin", "Users", "Roles")
            .Handler(async req => await HandleRemoveRoleAsync(new ApiContext(req)))
            .Build());

        // Get user's roles
        apiManager.RegisterEndpoint(apiManager.CreateEndpoint(extensionId)
            .Version("1")
            .Path("/admin/api/users/:id/roles")
            .Get()
            .Description("Get user's roles")
            .Tags("Admin", "Users", "Roles")
            .Handler(async req => await HandleGetUserRolesAsync(new ApiContext(req)))
            .Build());

        // ============= ROLE MANAGEMENT APIs =============

        // List all roles
        apiManager.RegisterEndpoint(apiManager.CreateEndpoint(extensionId)
            .Version("1")
            .Path("/admin/api/roles")
            .Get()
            .Description("List all roles")
            .Tags("Admin", "Roles")
            .Handler(async req => await HandleListRolesAsync(new ApiContext(req)))
            .Build());

        // Get single role by ID
        apiManager.RegisterEndpoint(apiManager.CreateEndpoint(extensionId)
            .Version("1")
            .Path("/admin/api/roles/:id")
            .Get()
            .Description("Get role by ID")
            .Tags("Admin", "Roles")
            .Handler(async req => await HandleGetRoleAsync(new ApiContext(req)))
            .Build());

        // Create new role
        apiManager.RegisterEndpoint(apiManager.CreateEndpoint(extensionId)
            .Version("1")
            .Path("/admin/api/roles")
            .Post()
            .Description("Create new role")
            .Tags("Admin", "Roles")
            .Handler(async req => await HandleCreateRoleAsync(new ApiContext(req)))
            .Build());

        // Update role
        apiManager.RegisterEndpoint(apiManager.CreateEndpoint(extensionId)
            .Version("1")
            .Path("/admin/api/roles/:id")
            .Put()
            .Description("Update role")
            .Tags("Admin", "Roles")
            .Handler(async req => await HandleUpdateRoleAsync(new ApiContext(req)))
            .Build());

        // Delete role
        apiManager.RegisterEndpoint(apiManager.CreateEndpoint(extensionId)
            .Version("1")
            .Path("/admin/api/roles/:id")
            .Delete()
            .Description("Delete role")
            .Tags("Admin", "Roles")
            .Handler(async req => await HandleDeleteRoleAsync(new ApiContext(req)))
            .Build());

        // Get role's permissions
        apiManager.RegisterEndpoint(apiManager.CreateEndpoint(extensionId)
            .Version("1")
            .Path("/admin/api/roles/:id/permissions")
            .Get()
            .Description("Get role's permissions")
            .Tags("Admin", "Roles", "Permissions")
            .Handler(async req => await HandleGetRolePermissionsAsync(new ApiContext(req)))
            .Build());

        // Assign permission to role
        apiManager.RegisterEndpoint(apiManager.CreateEndpoint(extensionId)
            .Version("1")
            .Path("/admin/api/roles/:id/permissions")
            .Post()
            .Description("Assign permission to role")
            .Tags("Admin", "Roles", "Permissions")
            .Handler(async req => await HandleAssignPermissionAsync(new ApiContext(req)))
            .Build());

        // Remove permission from role
        apiManager.RegisterEndpoint(apiManager.CreateEndpoint(extensionId)
            .Version("1")
            .Path("/admin/api/roles/:id/permissions/:permissionId")
            .Delete()
            .Description("Remove permission from role")
            .Tags("Admin", "Roles", "Permissions")
            .Handler(async req => await HandleRemovePermissionAsync(new ApiContext(req)))
            .Build());

        // ============= PERMISSION MANAGEMENT APIs =============

        // List all permissions
        apiManager.RegisterEndpoint(apiManager.CreateEndpoint(extensionId)
            .Version("1")
            .Path("/admin/api/permissions")
            .Get()
            .Description("List all permissions")
            .Tags("Admin", "Permissions")
            .Handler(async req => await HandleListPermissionsAsync(new ApiContext(req)))
            .Build());

        // ============= LOG MANAGEMENT APIs =============

        // List logs with filtering and pagination
        apiManager.RegisterEndpoint(apiManager.CreateEndpoint(extensionId)
            .Version("1")
            .Path("/admin/api/logs")
            .Get()
            .Description("List system logs with filtering and pagination")
            .Tags("Admin", "Logs")
            .Handler(async req => await HandleListLogsAsync(new ApiContext(req)))
            .Build());

        // Get log statistics
        apiManager.RegisterEndpoint(apiManager.CreateEndpoint(extensionId)
            .Version("1")
            .Path("/admin/api/logs/stats")
            .Get()
            .Description("Get log statistics by category and level")
            .Tags("Admin", "Logs")
            .Handler(async req => await HandleLogStatsAsync(new ApiContext(req)))
            .Build());

        // Delete old logs (manual cleanup)
        apiManager.RegisterEndpoint(apiManager.CreateEndpoint(extensionId)
            .Version("1")
            .Path("/admin/api/logs/cleanup")
            .Delete()
            .Description("Manually clean up old logs")
            .Tags("Admin", "Logs")
            .Handler(async req => await HandleLogCleanupAsync(new ApiContext(req)))
            .Build());

        Console.WriteLine("    [Admin] ✓ Registered 21 API endpoints");
    }

    public IEnumerable<Type> GetDatabaseModels()
    {
        // Admin module doesn't define any new database models
        // It uses the existing auth models from WebLogic.Server
        return Array.Empty<Type>();
    }

    // IApplicationLifecycle implementation
    public async Task OnApplicationStartingAsync(IServiceProvider services)
    {
        _services = services;
        _templateEngine = services.GetService<ITemplateEngine>();
        _themeManager = services.GetService<IThemeManager>();
        _authService = services.GetService<AuthService>();
        _userService = services.GetService<UserService>();
        _userService = services.GetService<UserService>();
        _mysql = services.GetService<MySQL2Library>();

        // Ensure admin theme exists
        if (_themeManager != null && _themeManager.ThemeExists("admin"))
        {
            Console.WriteLine("    [Admin] Admin theme found");

            // Register admin template partials
            RegisterAdminPartials();
        }
        else
        {
            Console.WriteLine("    [Admin] ⚠ Admin theme not found - templates may not load correctly");
        }

        Console.WriteLine("    [Admin] Application starting - services captured");
    }

    /// <summary>
    /// Register reusable template partials
    /// </summary>
    private void RegisterAdminPartials()
    {
        if (_templateEngine == null || _themeManager == null) return;

        // Get admin theme
        var adminTheme = _themeManager.GetTheme("admin");
        if (adminTheme == null) return;

        // Register sidebar
        var sidebarPath = _themeManager.GetPartialPath("sidebar");
        if (sidebarPath != null && File.Exists(sidebarPath))
        {
            var sidebarHtml = File.ReadAllText(sidebarPath);
            _templateEngine.RegisterPartial("sidebar", sidebarHtml);
        }

        // Register header
        var headerPath = _themeManager.GetPartialPath("header");
        if (headerPath != null && File.Exists(headerPath))
        {
            var headerHtml = File.ReadAllText(headerPath);
            _templateEngine.RegisterPartial("header", headerHtml);
        }
    }

    /// <summary>
    /// Render a page with layout using ThemeManager
    /// </summary>
    private async Task<string> RenderWithLayoutAsync(ITemplateEngine templateEngine, IThemeManager themeManager, string contentTemplatePath, object? data)
    {
        if (templateEngine == null)
        {
            throw new InvalidOperationException("Template engine not available");
        }

        if (themeManager == null)
        {
            throw new InvalidOperationException("Theme manager not available");
        }

        // Temporarily switch to admin theme
        var originalTheme = themeManager.GetActiveTheme().Manifest.Id;
        if (themeManager.ThemeExists("admin"))
        {
            themeManager.SetActiveTheme("admin");
        }

        try
        {
            // Get template path from theme manager
            var fullContentPath = themeManager.GetTemplatePath(contentTemplatePath);
            if (fullContentPath == null || !File.Exists(fullContentPath))
            {
                throw new FileNotFoundException($"Template not found: {contentTemplatePath}");
            }

            var contentHtml = await File.ReadAllTextAsync(fullContentPath);

            // Render content with data
            var renderedContent = await templateEngine.RenderAsync(contentHtml, data);

            // Load layout from theme manager
            var layoutPath = themeManager.GetLayoutPath("base");
            if (layoutPath == null || !File.Exists(layoutPath))
            {
                throw new FileNotFoundException("Layout not found: base.html");
            }

            var layoutHtml = await File.ReadAllTextAsync(layoutPath);

            // Merge content into layout
            var layoutData = new
            {
                content = renderedContent,
                title = GetProperty<string>(data, "title") ?? "Admin",
                pageTitle = GetProperty<string>(data, "pageTitle") ?? "Admin",
                pageIcon = GetProperty<string>(data, "pageIcon") ?? "speedometer2",
                user = GetProperty<object>(data, "user"),
                isDashboard = GetProperty<bool>(data, "isDashboard"),
                isUsers = GetProperty<bool>(data, "isUsers"),
                isRoles = GetProperty<bool>(data, "isRoles"),
                isPermissions = GetProperty<bool>(data, "isPermissions"),
                isSettings = GetProperty<bool>(data, "isSettings"),
                scripts = GetProperty<string>(data, "scripts") ?? ""
            };

            return await templateEngine.RenderAsync(layoutHtml, layoutData);
        }
        finally
        {
            // Restore original theme
            if (originalTheme != "admin" && themeManager.ThemeExists(originalTheme))
            {
                themeManager.SetActiveTheme(originalTheme);
            }
        }
    }

    /// <summary>
    /// Helper to get property value from anonymous object
    /// </summary>
    private T? GetProperty<T>(object? obj, string propertyName)
    {
        if (obj == null) return default;

        var prop = obj.GetType().GetProperty(propertyName);
        if (prop == null) return default;

        var value = prop.GetValue(obj);
        if (value is T typedValue)
            return typedValue;

        return default;
    }

    public async Task OnApplicationStartedAsync(IServiceProvider services)
    {
        Console.WriteLine("    [Admin] ✓ OnApplicationStartedAsync called");

        // Ensure default admin user exists (runs after auth data seeding)
        await EnsureDefaultAdminUserAsync();

        Console.WriteLine("    [Admin] Application started - Admin extension ready");
    }

    public Task OnApplicationStoppingAsync()
    {
        Console.WriteLine("    [Admin] Application stopping");
        return Task.CompletedTask;
    }

    public Task OnApplicationStoppedAsync()
    {
        Console.WriteLine("    [Admin] Application stopped");
        return Task.CompletedTask;
    }

    private async Task<RouteResponse> HandleLoginPageAsync(RequestContext context)
    {
        // Get services from service provider
        var templateEngine = context.ServiceProvider.GetService<ITemplateEngine>();
        var themeManager = context.ServiceProvider.GetService<IThemeManager>();

        if (templateEngine == null)
        {
            return RouteResponse.Text("Template engine not available");
        }

        if (themeManager == null)
        {
            return RouteResponse.Text("Theme manager not available");
        }

        // Check if already authenticated
        var currentUser = context.HttpContext.GetCurrentUser();
        if (currentUser != null)
        {
            return RouteResponse.Redirect("/admin");
        }

        // Temporarily switch to admin theme
        var originalTheme = themeManager.GetActiveTheme().Manifest.Id;
        if (themeManager.ThemeExists("admin"))
        {
            themeManager.SetActiveTheme("admin");
        }

        try
        {
            // Get login template path from theme manager
            var templatePath = themeManager.GetTemplatePath("login");
            if (templatePath == null || !File.Exists(templatePath))
            {
                return RouteResponse.Text("Login template not found");
            }

            var templateContent = await File.ReadAllTextAsync(templatePath);

            var html = await templateEngine.RenderAsync(templateContent, new
            {
                title = "Admin Login",
                error = context.HttpContext.Request.Query["error"].ToString()
            });

            return RouteResponse.Html(html);
        }
        finally
        {
            // Restore original theme
            if (originalTheme != "admin" && themeManager.ThemeExists(originalTheme))
            {
                themeManager.SetActiveTheme(originalTheme);
            }
        }
    }

    private async Task<RouteResponse> HandleDashboardAsync(RequestContext context)
    {
        // Get services from service provider
        var templateEngine = context.ServiceProvider.GetService<ITemplateEngine>();
        var themeManager = context.ServiceProvider.GetService<IThemeManager>();

        if (templateEngine == null)
        {
            return RouteResponse.Text("Template engine not available");
        }

        if (themeManager == null)
        {
            return RouteResponse.Text("Theme manager not available");
        }

        // Check authentication
        var currentUser = context.HttpContext.GetCurrentUser();
        if (currentUser == null)
        {
            return RouteResponse.Redirect("/admin/login");
        }

        // Get auth service
        var authService = context.ServiceProvider.GetService<AuthService>();
        if (authService == null)
        {
            return RouteResponse.Text("Auth service not available");
        }

        // Check admin access permission
        var hasAccess = await authService.HasPermissionAsync(currentUser.Id, "admin.access");
        if (!hasAccess)
        {
            return RouteResponse.Forbidden("Access denied. You need 'admin.access' permission.");
        }

        // Get dashboard stats (you can make these dynamic)
        var stats = new
        {
            totalUsers = 1,
            totalRoles = 4,
            totalPermissions = 17,
            activeSessions = 1
        };

        // Render with layout
        var html = await RenderWithLayoutAsync(templateEngine, themeManager, "dashboard_content.html", new
        {
            title = "Dashboard",
            pageTitle = "Dashboard",
            pageIcon = "speedometer2",
            isDashboard = true,
            user = currentUser,
            stats = stats
        });

        return RouteResponse.Html(html);
    }

    private async Task<RouteResponse> HandleLogoutAsync(RequestContext context)
    {
        await context.HttpContext.SignOutAsync();
        return RouteResponse.Redirect("/admin/login");
    }

    private async Task<RouteResponse> HandleUsersPageAsync(RequestContext context)
    {
        // Get template engine and theme manager from service provider
        var templateEngine = context.ServiceProvider.GetService<ITemplateEngine>();
        var themeManager = context.ServiceProvider.GetService<IThemeManager>();

        if (templateEngine == null)
        {
            return RouteResponse.Text("Template engine not available");
        }
        if (themeManager == null)
        {
            return RouteResponse.Text("Theme manager not available");
        }

        // Check authentication
        var currentUser = context.HttpContext.GetCurrentUser();
        if (currentUser == null)
        {
            return RouteResponse.Redirect("/admin/login");
        }

        // Get auth service
        var authService = context.ServiceProvider.GetService<AuthService>();
        if (authService == null)
        {
            return RouteResponse.Text("Auth service not available");
        }

        // Check permission
        var hasAccess = await authService.HasPermissionAsync(currentUser.Id, "user.list");
        if (!hasAccess)
        {
            return RouteResponse.Forbidden("Access denied. You need 'user.list' permission.");
        }

        // Temporarily switch to admin theme
        var originalTheme = themeManager.GetActiveTheme().Manifest.Id;
        if (themeManager.ThemeExists("admin"))
        {
            themeManager.SetActiveTheme("admin");
        }

        try
        {
            // Load user scripts from theme manager
            var scriptsPath = themeManager.GetPartialPath("users_scripts");
            var scripts = scriptsPath != null && File.Exists(scriptsPath)
                ? await File.ReadAllTextAsync(scriptsPath)
                : "";

            // Render with layout
            var html = await RenderWithLayoutAsync(templateEngine, themeManager, "users_content.html", new
            {
                title = "Users",
                pageTitle = "User Management",
                pageIcon = "people",
                isUsers = true,
                user = currentUser,
                scripts = scripts
            });

            return RouteResponse.Html(html);
        }
        finally
        {
            // Restore original theme
            if (originalTheme != "admin" && themeManager.ThemeExists(originalTheme))
            {
                themeManager.SetActiveTheme(originalTheme);
            }
        }
    }

    private async Task<RouteResponse> HandleRolesPageAsync(RequestContext context)
    {
        // Get template engine and theme manager from service provider
        var templateEngine = context.ServiceProvider.GetService<ITemplateEngine>();
        var themeManager = context.ServiceProvider.GetService<IThemeManager>();

        if (templateEngine == null)
        {
            return RouteResponse.Text("Template engine not available");
        }
        if (themeManager == null)
        {
            return RouteResponse.Text("Theme manager not available");
        }

        // Check authentication
        var currentUser = context.HttpContext.GetCurrentUser();
        if (currentUser == null)
        {
            return RouteResponse.Redirect("/admin/login");
        }

        // Get auth service
        var authService = context.ServiceProvider.GetService<AuthService>();
        if (authService == null)
        {
            return RouteResponse.Text("Auth service not available");
        }

        // Check permission
        var hasAccess = await authService.HasPermissionAsync(currentUser.Id, "role.list");
        if (!hasAccess)
        {
            return RouteResponse.Forbidden("Access denied. You need 'role.list' permission.");
        }

        // Temporarily switch to admin theme
        var originalTheme = themeManager.GetActiveTheme().Manifest.Id;
        if (themeManager.ThemeExists("admin"))
        {
            themeManager.SetActiveTheme("admin");
        }

        try
        {
            // Load roles scripts from theme manager
            var scriptsPath = themeManager.GetPartialPath("roles_scripts");
            var scripts = scriptsPath != null && File.Exists(scriptsPath)
                ? await File.ReadAllTextAsync(scriptsPath)
                : "";

            // Render with layout
            var html = await RenderWithLayoutAsync(templateEngine, themeManager, "roles_content.html", new
            {
                title = "Roles",
                pageTitle = "Role Management",
                pageIcon = "person-badge",
                isRoles = true,
                user = currentUser,
                scripts = scripts
            });

            return RouteResponse.Html(html);
        }
        finally
        {
            // Restore original theme
            if (originalTheme != "admin" && themeManager.ThemeExists(originalTheme))
            {
                themeManager.SetActiveTheme(originalTheme);
            }
        }
    }

    private async Task<RouteResponse> HandleLogsPageAsync(RequestContext context)
    {
        // Get template engine and theme manager from service provider
        var templateEngine = context.ServiceProvider.GetService<ITemplateEngine>();
        var themeManager = context.ServiceProvider.GetService<IThemeManager>();

        if (templateEngine == null)
        {
            return RouteResponse.Text("Template engine not available");
        }
        if (themeManager == null)
        {
            return RouteResponse.Text("Theme manager not available");
        }

        // Check authentication
        var currentUser = context.HttpContext.GetCurrentUser();
        if (currentUser == null)
        {
            return RouteResponse.Redirect("/admin/login");
        }

        // Get auth service
        var authService = context.ServiceProvider.GetService<AuthService>();
        if (authService == null)
        {
            return RouteResponse.Text("Auth service not available");
        }

        // Check permission
        var hasAccess = await authService.HasPermissionAsync(currentUser.Id, "admin.access");
        if (!hasAccess)
        {
            return RouteResponse.Forbidden("Access denied. You need 'admin.access' permission.");
        }

        // Temporarily switch to admin theme
        var originalTheme = themeManager.GetActiveTheme().Manifest.Id;
        if (themeManager.ThemeExists("admin"))
        {
            themeManager.SetActiveTheme("admin");
        }

        try
        {
            // Load logs scripts from theme manager
            var scriptsPath = themeManager.GetPartialPath("logs_scripts");
            var scripts = scriptsPath != null && File.Exists(scriptsPath)
                ? await File.ReadAllTextAsync(scriptsPath)
                : "";

            // Render with layout
            var html = await RenderWithLayoutAsync(templateEngine, themeManager, "logs_content.html", new
            {
                title = "System Logs",
                pageTitle = "System Logs",
                pageIcon = "file-text",
                isLogs = true,
                user = currentUser,
                scripts = scripts
            });

            return RouteResponse.Html(html);
        }
        finally
        {
            // Restore original theme
            if (originalTheme != "admin" && themeManager.ThemeExists(originalTheme))
            {
                themeManager.SetActiveTheme(originalTheme);
            }
        }
    }

    private async Task<ApiResponse> HandleLoginApiAsync(ApiContext context)
    {
        try
        {
            var request = await context.GetJsonBodyAsync<LoginRequest>();
            Console.WriteLine($"[Admin] Login attempt - request null: {request == null}");

            if (request == null)
            {
                Console.WriteLine($"[Admin] Login failed - request is null");
                return ApiResponse.BadRequest("Invalid request");
            }

            Console.WriteLine($"[Admin] Login attempt - Username: '{request.Username}', Password length: {request.Password?.Length ?? 0}");

            // Get auth service from request service provider
            var authService = context.Request.Context.ServiceProvider.GetService<AuthService>();
            if (authService == null)
            {
                Console.WriteLine($"[Admin] Login failed - AuthService not available");
                return ApiResponse.ServerError("Auth service not available");
            }

            var result = await authService.LoginAsync(
                request.Username,
                request.Password,
                context.Request.Context.HttpContext.Connection.RemoteIpAddress?.ToString()
            );

            Console.WriteLine($"[Admin] LoginAsync result - IsSuccess: {result.IsSuccess}, ErrorMessage: {result.ErrorMessage}");

            if (result.IsSuccess && result.User != null)
            {
                Console.WriteLine($"[Admin] Login successful - Creating session for user: {result.User.Username} (ID: {result.User.Id})");
                Console.WriteLine($"[Admin] Session.IsAvailable before SignIn: {context.Request.Context.HttpContext.Session.IsAvailable}");
                Console.WriteLine($"[Admin] Session.Id before SignIn: {context.Request.Context.HttpContext.Session.Id}");

                // Create session
                await context.Request.Context.HttpContext.SignInAsync(result.User);

                Console.WriteLine($"[Admin] Session.Id after SignIn: {context.Request.Context.HttpContext.Session.Id}");
                Console.WriteLine($"[Admin] Session UserId after SignIn: {context.Request.Context.HttpContext.Session.GetString("UserId")}");

                // Check if Set-Cookie header will be added
                var hasSetCookie = context.Request.Context.HttpContext.Response.Headers.ContainsKey("Set-Cookie");
                Console.WriteLine($"[Admin] Has Set-Cookie header: {hasSetCookie}");

                Console.WriteLine($"[Admin] Login successful - User: {result.User.Username}");

                return ApiResponse.Ok(new
                {
                    user = new
                    {
                        id = result.User.Id,
                        username = result.User.Username,
                        email = result.User.Email,
                        fullName = result.User.FullName
                    }
                }, "Login successful");
            }

            Console.WriteLine($"[Admin] Login failed - {result.ErrorMessage}");
            return ApiResponse.Unauthorized(result.ErrorMessage ?? "Login failed");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Admin] Login exception: {ex.Message}");
            return ApiResponse.ServerError(ex.Message);
        }
    }

    // ============= USER MANAGEMENT API HANDLERS =============

    private async Task<ApiResponse> HandleListUsersAsync(ApiContext context)
    {
        try
        {
            var currentUser = context.Request.Context.HttpContext.GetCurrentUser();
            if (currentUser == null)
            {
                return ApiResponse.Unauthorized("Authentication required");
            }

            if (!await _authService!.HasPermissionAsync(currentUser.Id, "user.list"))
            {
                return ApiResponse.Forbidden("You don't have permission to list users");
            }

            var query = context.Request.Context.HttpContext.Request.Query;
            var page = int.TryParse(query["page"], out var p) ? p : 1;
            var pageSize = int.TryParse(query["pageSize"], out var ps) ? ps : 20;
            var search = query["search"].ToString();
            var isActive = query["isActive"].ToString();

            var qb = _mysql!.GetQueryBuilder<User>("Default");

            // Apply filters
            if (!string.IsNullOrWhiteSpace(search))
            {
                qb.Where(u => u.Username.Contains(search) || u.Email.Contains(search) ||
                             (u.FirstName != null && u.FirstName.Contains(search)) ||
                             (u.LastName != null && u.LastName.Contains(search)));
            }

            if (!string.IsNullOrWhiteSpace(isActive))
            {
                if (bool.TryParse(isActive, out var activeFilter))
                {
                    qb.Where(u => u.IsActive == activeFilter);
                }
            }

            qb.OrderByDescending(u => u.CreatedAt);

            var result = await qb.ExecutePagedAsync(page, pageSize);

            if (!result.Success)
            {
                return ApiResponse.ServerError(result.ErrorMessage ?? "Failed to fetch users");
            }

            // Remove sensitive data
            var users = result.Data!.Items.Select(u => new
            {
                u.Id,
                u.Username,
                u.Email,
                u.FirstName,
                u.LastName,
                u.FullName,
                u.IsActive,
                u.IsEmailVerified,
                u.LastLoginAt,
                u.LastLoginIp,
                u.IsLocked,
                u.CreatedAt,
                u.UpdatedAt
            });

            return ApiResponse.Ok(new
            {
                users,
                pagination = new
                {
                    currentPage = result.Data.PageNumber,
                    pageSize = result.Data.PageSize,
                    totalPages = result.Data.TotalPages,
                    totalItems = result.Data.TotalItems
                }
            });
        }
        catch (Exception ex)
        {
            return ApiResponse.ServerError($"Error listing users: {ex.Message}");
        }
    }

    private async Task<ApiResponse> HandleGetUserAsync(ApiContext context)
    {
        try
        {
            var currentUser = context.Request.Context.HttpContext.GetCurrentUser();
            if (currentUser == null)
            {
                return ApiResponse.Unauthorized("Authentication required");
            }

            if (!await _authService!.HasPermissionAsync(currentUser.Id, "user.read"))
            {
                return ApiResponse.Forbidden("You don't have permission to view users");
            }

            var userId = context.Request.Params.GetValueOrDefault("id");
            if (string.IsNullOrWhiteSpace(userId) || !Guid.TryParse(userId, out var userGuid))
            {
                return ApiResponse.BadRequest("Invalid user ID");
            }

            var repo = _mysql!.GetRepository<User>("Default");
            var result = await repo.GetByIdAsync(userGuid);

            if (!result.Success || result.Data == null)
            {
                return ApiResponse.NotFound("User not found");
            }

            var user = result.Data;

            return ApiResponse.Ok(new
            {
                user = new
                {
                    user.Id,
                    user.Username,
                    user.Email,
                    user.FirstName,
                    user.LastName,
                    user.FullName,
                    user.IsActive,
                    user.IsEmailVerified,
                    user.LastLoginAt,
                    user.LastLoginIp,
                    user.FailedLoginAttempts,
                    user.IsLocked,
                    user.LockedUntil,
                    user.CreatedAt,
                    user.UpdatedAt
                }
            });
        }
        catch (Exception ex)
        {
            return ApiResponse.ServerError($"Error getting user: {ex.Message}");
        }
    }

    private async Task<ApiResponse> HandleCreateUserAsync(ApiContext context)
    {
        try
        {
            var currentUser = context.Request.Context.HttpContext.GetCurrentUser();
            if (currentUser == null)
            {
                return ApiResponse.Unauthorized("Authentication required");
            }

            // Get services from request service provider
            var authService = context.Request.Context.ServiceProvider.GetService<AuthService>();
            var userService = context.Request.Context.ServiceProvider.GetService<UserService>();

            if (authService == null || userService == null)
            {
                return ApiResponse.ServerError("Required services not available");
            }

            if (!await authService.HasPermissionAsync(currentUser.Id, "user.create"))
            {
                return ApiResponse.Forbidden("You don't have permission to create users");
            }

            var request = await context.GetJsonBodyAsync<CreateUserRequest>();
            if (request == null)
            {
                return ApiResponse.BadRequest("Invalid request body");
            }

            // Validation
            if (string.IsNullOrWhiteSpace(request.Username))
            {
                return ApiResponse.BadRequest("Username is required");
            }

            if (string.IsNullOrWhiteSpace(request.Email))
            {
                return ApiResponse.BadRequest("Email is required");
            }

            if (string.IsNullOrWhiteSpace(request.Password))
            {
                return ApiResponse.BadRequest("Password is required");
            }

            if (request.Password.Length < 6)
            {
                return ApiResponse.BadRequest("Password must be at least 6 characters");
            }

            var (success, userId, error) = await userService.CreateUserAsync(
                request.Username,
                request.Email,
                request.Password,
                request.FirstName,
                request.LastName
            );

            if (!success)
            {
                return ApiResponse.BadRequest(error ?? "Failed to create user");
            }

            // Assign role if specified
            if (request.RoleId.HasValue && request.RoleId.Value != Guid.Empty)
            {
                await userService.AssignRoleAsync(userId!.Value, request.RoleId.Value, currentUser.Id);
            }

            return ApiResponse.Ok(new { userId }, "User created successfully");
        }
        catch (Exception ex)
        {
            return ApiResponse.ServerError($"Error creating user: {ex.Message}");
        }
    }

    private async Task<ApiResponse> HandleUpdateUserAsync(ApiContext context)
    {
        try
        {
            var currentUser = context.Request.Context.HttpContext.GetCurrentUser();
            if (currentUser == null)
            {
                return ApiResponse.Unauthorized("Authentication required");
            }

            if (!await _authService!.HasPermissionAsync(currentUser.Id, "user.update"))
            {
                return ApiResponse.Forbidden("You don't have permission to update users");
            }

            var userId = context.Request.Params.GetValueOrDefault("id");
            if (string.IsNullOrWhiteSpace(userId) || !Guid.TryParse(userId, out var userGuid))
            {
                return ApiResponse.BadRequest("Invalid user ID");
            }

            var request = await context.GetJsonBodyAsync<UpdateUserRequest>();
            if (request == null)
            {
                return ApiResponse.BadRequest("Invalid request body");
            }

            var repo = _mysql!.GetRepository<User>("Default");
            var userResult = await repo.GetByIdAsync(userGuid);

            if (!userResult.Success || userResult.Data == null)
            {
                return ApiResponse.NotFound("User not found");
            }

            var user = userResult.Data;

            // Update fields
            if (!string.IsNullOrWhiteSpace(request.Email) && request.Email != user.Email)
            {
                user.Email = request.Email;
            }

            if (!string.IsNullOrWhiteSpace(request.FirstName))
            {
                user.FirstName = request.FirstName;
            }

            if (!string.IsNullOrWhiteSpace(request.LastName))
            {
                user.LastName = request.LastName;
            }

            if (request.IsActive.HasValue)
            {
                user.IsActive = request.IsActive.Value;
            }

            if (request.IsEmailVerified.HasValue)
            {
                user.IsEmailVerified = request.IsEmailVerified.Value;
            }

            // Update password if provided
            if (!string.IsNullOrWhiteSpace(request.NewPassword))
            {
                if (request.NewPassword.Length < 6)
                {
                    return ApiResponse.BadRequest("Password must be at least 6 characters");
                }
                user.PasswordHash = PasswordHasher.HashPassword(request.NewPassword);
            }

            // Clear lock if requested
            if (request.ClearLock.HasValue && request.ClearLock.Value)
            {
                user.LockedUntil = null;
                user.FailedLoginAttempts = 0;
            }

            user.UpdatedAt = DateTime.UtcNow;

            var updateResult = await repo.UpdateAsync(user);

            if (!updateResult.Success)
            {
                return ApiResponse.ServerError(updateResult.ErrorMessage ?? "Failed to update user");
            }

            return ApiResponse.Ok(new { message = "User updated successfully" });
        }
        catch (Exception ex)
        {
            return ApiResponse.ServerError($"Error updating user: {ex.Message}");
        }
    }

    private async Task<ApiResponse> HandleDeleteUserAsync(ApiContext context)
    {
        try
        {
            var currentUser = context.Request.Context.HttpContext.GetCurrentUser();
            if (currentUser == null)
            {
                return ApiResponse.Unauthorized("Authentication required");
            }

            if (!await _authService!.HasPermissionAsync(currentUser.Id, "user.delete"))
            {
                return ApiResponse.Forbidden("You don't have permission to delete users");
            }

            var userId = context.Request.Params.GetValueOrDefault("id");
            if (string.IsNullOrWhiteSpace(userId) || !Guid.TryParse(userId, out var userGuid))
            {
                return ApiResponse.BadRequest("Invalid user ID");
            }

            // Prevent deleting yourself
            if (userGuid == currentUser.Id)
            {
                return ApiResponse.BadRequest("You cannot delete your own account");
            }

            var repo = _mysql!.GetRepository<User>("Default");
            var deleteResult = await repo.DeleteAsync(userGuid);

            if (!deleteResult.Success)
            {
                return ApiResponse.ServerError(deleteResult.ErrorMessage ?? "Failed to delete user");
            }

            return ApiResponse.Ok(new { message = "User deleted successfully" });
        }
        catch (Exception ex)
        {
            return ApiResponse.ServerError($"Error deleting user: {ex.Message}");
        }
    }

    private async Task<ApiResponse> HandleAssignRoleAsync(ApiContext context)
    {
        try
        {
            var currentUser = context.Request.Context.HttpContext.GetCurrentUser();
            if (currentUser == null)
            {
                return ApiResponse.Unauthorized("Authentication required");
            }

            if (!await _authService!.HasPermissionAsync(currentUser.Id, "user.assign_role"))
            {
                return ApiResponse.Forbidden("You don't have permission to assign roles");
            }

            var userId = context.Request.Params.GetValueOrDefault("id");
            if (string.IsNullOrWhiteSpace(userId) || !Guid.TryParse(userId, out var userGuid))
            {
                return ApiResponse.BadRequest("Invalid user ID");
            }

            var request = await context.GetJsonBodyAsync<AssignRoleRequest>();
            if (request == null || request.RoleId == Guid.Empty)
            {
                return ApiResponse.BadRequest("Role ID is required");
            }

            var success = await _userService!.AssignRoleAsync(userGuid, request.RoleId, currentUser.Id);

            if (!success)
            {
                return ApiResponse.ServerError("Failed to assign role");
            }

            return ApiResponse.Ok(new { message = "Role assigned successfully" });
        }
        catch (Exception ex)
        {
            return ApiResponse.ServerError($"Error assigning role: {ex.Message}");
        }
    }

    private async Task<ApiResponse> HandleRemoveRoleAsync(ApiContext context)
    {
        try
        {
            var currentUser = context.Request.Context.HttpContext.GetCurrentUser();
            if (currentUser == null)
            {
                return ApiResponse.Unauthorized("Authentication required");
            }

            if (!await _authService!.HasPermissionAsync(currentUser.Id, "user.remove_role"))
            {
                return ApiResponse.Forbidden("You don't have permission to remove roles");
            }

            var userId = context.Request.Params.GetValueOrDefault("id");
            var roleId = context.Request.Params.GetValueOrDefault("roleId");

            if (string.IsNullOrWhiteSpace(userId) || !Guid.TryParse(userId, out var userGuid))
            {
                return ApiResponse.BadRequest("Invalid user ID");
            }

            if (string.IsNullOrWhiteSpace(roleId) || !Guid.TryParse(roleId, out var roleGuid))
            {
                return ApiResponse.BadRequest("Invalid role ID");
            }

            var success = await _userService!.RemoveRoleAsync(userGuid, roleGuid);

            if (!success)
            {
                return ApiResponse.ServerError("Failed to remove role");
            }

            return ApiResponse.Ok(new { message = "Role removed successfully" });
        }
        catch (Exception ex)
        {
            return ApiResponse.ServerError($"Error removing role: {ex.Message}");
        }
    }

    private async Task<ApiResponse> HandleGetUserRolesAsync(ApiContext context)
    {
        try
        {
            var currentUser = context.Request.Context.HttpContext.GetCurrentUser();
            if (currentUser == null)
            {
                return ApiResponse.Unauthorized("Authentication required");
            }

            if (!await _authService!.HasPermissionAsync(currentUser.Id, "user.read"))
            {
                return ApiResponse.Forbidden("You don't have permission to view user roles");
            }

            var userId = context.Request.Params.GetValueOrDefault("id");
            if (string.IsNullOrWhiteSpace(userId) || !Guid.TryParse(userId, out var userGuid))
            {
                return ApiResponse.BadRequest("Invalid user ID");
            }

            var roles = await _authService!.GetUserRolesAsync(userGuid);

            return ApiResponse.Ok(new { roles });
        }
        catch (Exception ex)
        {
            return ApiResponse.ServerError($"Error getting user roles: {ex.Message}");
        }
    }

    // ============= ROLE MANAGEMENT API HANDLERS =============

    private async Task<ApiResponse> HandleListRolesAsync(ApiContext context)
    {
        try
        {
            var currentUser = context.Request.Context.HttpContext.GetCurrentUser();
            if (currentUser == null)
            {
                return ApiResponse.Unauthorized("Authentication required");
            }

            if (!await _authService!.HasPermissionAsync(currentUser.Id, "role.list"))
            {
                return ApiResponse.Forbidden("You don't have permission to list roles");
            }

            var qb = _mysql!.GetQueryBuilder<Role>("Default");
            qb.OrderBy(r => r.Name);

            var result = await qb.ExecuteAsync();

            if (!result.Success)
            {
                return ApiResponse.ServerError(result.ErrorMessage ?? "Failed to fetch roles");
            }

            var roles = result.Data!.Select(r => new
            {
                r.Id,
                r.Name,
                r.DisplayName,
                r.Description,
                r.IsSystemRole,
                r.CreatedAt,
                r.UpdatedAt
            });

            return ApiResponse.Ok(new { roles });
        }
        catch (Exception ex)
        {
            return ApiResponse.ServerError($"Error listing roles: {ex.Message}");
        }
    }

    private async Task<ApiResponse> HandleGetRoleAsync(ApiContext context)
    {
        try
        {
            var currentUser = context.Request.Context.HttpContext.GetCurrentUser();
            if (currentUser == null)
            {
                return ApiResponse.Unauthorized("Authentication required");
            }

            if (!await _authService!.HasPermissionAsync(currentUser.Id, "role.read"))
            {
                return ApiResponse.Forbidden("You don't have permission to view roles");
            }

            var roleId = context.Request.Params.GetValueOrDefault("id");
            if (string.IsNullOrWhiteSpace(roleId) || !Guid.TryParse(roleId, out var roleGuid))
            {
                return ApiResponse.BadRequest("Invalid role ID");
            }

            var repo = _mysql!.GetRepository<Role>("Default");
            var result = await repo.GetByIdAsync(roleGuid);

            if (!result.Success || result.Data == null)
            {
                return ApiResponse.NotFound("Role not found");
            }

            var role = result.Data;

            return ApiResponse.Ok(new
            {
                role = new
                {
                    role.Id,
                    role.Name,
                    role.DisplayName,
                    role.Description,
                    role.IsSystemRole,
                    role.CreatedAt,
                    role.UpdatedAt
                }
            });
        }
        catch (Exception ex)
        {
            return ApiResponse.ServerError($"Error getting role: {ex.Message}");
        }
    }

    private async Task<ApiResponse> HandleCreateRoleAsync(ApiContext context)
    {
        try
        {
            var currentUser = context.Request.Context.HttpContext.GetCurrentUser();
            if (currentUser == null)
            {
                return ApiResponse.Unauthorized("Authentication required");
            }

            // Get services from request service provider
            var authService = context.Request.Context.ServiceProvider.GetService<AuthService>();
            var mysql = context.Request.Context.ServiceProvider.GetService<MySQL2Library>();

            if (authService == null || mysql == null)
            {
                return ApiResponse.ServerError("Required services not available");
            }

            if (!await authService.HasPermissionAsync(currentUser.Id, "role.create"))
            {
                return ApiResponse.Forbidden("You don't have permission to create roles");
            }

            var request = await context.GetJsonBodyAsync<CreateRoleRequest>();
            if (request == null)
            {
                return ApiResponse.BadRequest("Invalid request body");
            }

            // Validation
            if (string.IsNullOrWhiteSpace(request.Name))
            {
                return ApiResponse.BadRequest("Role name is required");
            }

            if (string.IsNullOrWhiteSpace(request.DisplayName))
            {
                return ApiResponse.BadRequest("Display name is required");
            }

            // Check if role name already exists
            var existingRole = await mysql.GetQueryBuilder<Role>("Default")
                .Where(r => r.Name == request.Name)
                .FirstOrDefaultAsync();

            if (existingRole.Success && existingRole.Data != null)
            {
                return ApiResponse.BadRequest("Role name already exists");
            }

            var role = new Role
            {
                Id = Guid.NewGuid(),
                Name = request.Name.ToLower(),
                DisplayName = request.DisplayName,
                Description = request.Description,
                IsSystemRole = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            var repo = mysql.GetRepository<Role>("Default");
            var result = await repo.InsertAsync(role);

            if (!result.Success)
            {
                return ApiResponse.ServerError(result.ErrorMessage ?? "Failed to create role");
            }

            return ApiResponse.Ok(new { roleId = role.Id }, "Role created successfully");
        }
        catch (Exception ex)
        {
            return ApiResponse.ServerError($"Error creating role: {ex.Message}");
        }
    }

    private async Task<ApiResponse> HandleUpdateRoleAsync(ApiContext context)
    {
        try
        {
            var currentUser = context.Request.Context.HttpContext.GetCurrentUser();
            if (currentUser == null)
            {
                return ApiResponse.Unauthorized("Authentication required");
            }

            if (!await _authService!.HasPermissionAsync(currentUser.Id, "role.update"))
            {
                return ApiResponse.Forbidden("You don't have permission to update roles");
            }

            var roleId = context.Request.Params.GetValueOrDefault("id");
            if (string.IsNullOrWhiteSpace(roleId) || !Guid.TryParse(roleId, out var roleGuid))
            {
                return ApiResponse.BadRequest("Invalid role ID");
            }

            var request = await context.GetJsonBodyAsync<UpdateRoleRequest>();
            if (request == null)
            {
                return ApiResponse.BadRequest("Invalid request body");
            }

            var repo = _mysql!.GetRepository<Role>("Default");
            var roleResult = await repo.GetByIdAsync(roleGuid);

            if (!roleResult.Success || roleResult.Data == null)
            {
                return ApiResponse.NotFound("Role not found");
            }

            var role = roleResult.Data;

            // Prevent editing system roles
            if (role.IsSystemRole)
            {
                return ApiResponse.Forbidden("Cannot modify system roles");
            }

            // Update fields
            if (!string.IsNullOrWhiteSpace(request.DisplayName))
            {
                role.DisplayName = request.DisplayName;
            }

            if (request.Description != null)
            {
                role.Description = request.Description;
            }

            role.UpdatedAt = DateTime.UtcNow;

            var updateResult = await repo.UpdateAsync(role);

            if (!updateResult.Success)
            {
                return ApiResponse.ServerError(updateResult.ErrorMessage ?? "Failed to update role");
            }

            return ApiResponse.Ok(new { message = "Role updated successfully" });
        }
        catch (Exception ex)
        {
            return ApiResponse.ServerError($"Error updating role: {ex.Message}");
        }
    }

    private async Task<ApiResponse> HandleDeleteRoleAsync(ApiContext context)
    {
        try
        {
            var currentUser = context.Request.Context.HttpContext.GetCurrentUser();
            if (currentUser == null)
            {
                return ApiResponse.Unauthorized("Authentication required");
            }

            if (!await _authService!.HasPermissionAsync(currentUser.Id, "role.delete"))
            {
                return ApiResponse.Forbidden("You don't have permission to delete roles");
            }

            var roleId = context.Request.Params.GetValueOrDefault("id");
            if (string.IsNullOrWhiteSpace(roleId) || !Guid.TryParse(roleId, out var roleGuid))
            {
                return ApiResponse.BadRequest("Invalid role ID");
            }

            var repo = _mysql!.GetRepository<Role>("Default");
            var roleResult = await repo.GetByIdAsync(roleGuid);

            if (!roleResult.Success || roleResult.Data == null)
            {
                return ApiResponse.NotFound("Role not found");
            }

            var role = roleResult.Data;

            // Prevent deleting system roles
            if (role.IsSystemRole)
            {
                return ApiResponse.Forbidden("Cannot delete system roles");
            }

            var deleteResult = await repo.DeleteAsync(roleGuid);

            if (!deleteResult.Success)
            {
                return ApiResponse.ServerError(deleteResult.ErrorMessage ?? "Failed to delete role");
            }

            return ApiResponse.Ok(new { message = "Role deleted successfully" });
        }
        catch (Exception ex)
        {
            return ApiResponse.ServerError($"Error deleting role: {ex.Message}");
        }
    }

    private async Task<ApiResponse> HandleGetRolePermissionsAsync(ApiContext context)
    {
        try
        {
            var currentUser = context.Request.Context.HttpContext.GetCurrentUser();
            if (currentUser == null)
            {
                return ApiResponse.Unauthorized("Authentication required");
            }

            if (!await _authService!.HasPermissionAsync(currentUser.Id, "role.read"))
            {
                return ApiResponse.Forbidden("You don't have permission to view role permissions");
            }

            var roleId = context.Request.Params.GetValueOrDefault("id");
            if (string.IsNullOrWhiteSpace(roleId) || !Guid.TryParse(roleId, out var roleGuid))
            {
                return ApiResponse.BadRequest("Invalid role ID");
            }

            // Get permissions for role via JOIN
            var permissions = await _mysql!.QueryAsync<Permission>("Default",
                @"SELECT p.* FROM wls_permissions p
                  INNER JOIN wls_role_permissions rp ON p.id = rp.permission_id
                  WHERE rp.role_id = @RoleId
                  ORDER BY p.resource, p.action",
                new { RoleId = roleGuid });

            if (!permissions.Success)
            {
                return ApiResponse.ServerError("Failed to fetch role permissions");
            }

            return ApiResponse.Ok(new { permissions = permissions.Data });
        }
        catch (Exception ex)
        {
            return ApiResponse.ServerError($"Error getting role permissions: {ex.Message}");
        }
    }

    private async Task<ApiResponse> HandleAssignPermissionAsync(ApiContext context)
    {
        try
        {
            var currentUser = context.Request.Context.HttpContext.GetCurrentUser();
            if (currentUser == null)
            {
                return ApiResponse.Unauthorized("Authentication required");
            }

            // Get services from request service provider
            var authService = context.Request.Context.ServiceProvider.GetService<AuthService>();
            var mysql = context.Request.Context.ServiceProvider.GetService<MySQL2Library>();

            if (authService == null || mysql == null)
            {
                return ApiResponse.ServerError("Required services not available");
            }

            if (!await authService.HasPermissionAsync(currentUser.Id, "role.assign_permission"))
            {
                return ApiResponse.Forbidden("You don't have permission to assign permissions");
            }

            var roleId = context.Request.Params.GetValueOrDefault("id");
            if (string.IsNullOrWhiteSpace(roleId) || !Guid.TryParse(roleId, out var roleGuid))
            {
                return ApiResponse.BadRequest("Invalid role ID");
            }

            var request = await context.GetJsonBodyAsync<AssignPermissionRequest>();
            if (request == null || request.PermissionId == Guid.Empty)
            {
                return ApiResponse.BadRequest("Permission ID is required");
            }

            // Check if already assigned
            var existing = await mysql.GetQueryBuilder<RolePermission>("Default")
                .Where(rp => rp.RoleId == roleGuid && rp.PermissionId == request.PermissionId)
                .FirstOrDefaultAsync();

            if (existing.Success && existing.Data != null)
            {
                return ApiResponse.BadRequest("Permission already assigned to this role");
            }

            var rolePermission = new RolePermission
            {
                RoleId = roleGuid,
                PermissionId = request.PermissionId,
                CreatedAt = DateTime.UtcNow
            };

            var repo = mysql.GetRepository<RolePermission>("Default");
            var result = await repo.InsertAsync(rolePermission);

            if (!result.Success)
            {
                return ApiResponse.ServerError("Failed to assign permission");
            }

            return ApiResponse.Ok(new { message = "Permission assigned successfully" });
        }
        catch (Exception ex)
        {
            return ApiResponse.ServerError($"Error assigning permission: {ex.Message}");
        }
    }

    private async Task<ApiResponse> HandleRemovePermissionAsync(ApiContext context)
    {
        try
        {
            var currentUser = context.Request.Context.HttpContext.GetCurrentUser();
            if (currentUser == null)
            {
                return ApiResponse.Unauthorized("Authentication required");
            }

            if (!await _authService!.HasPermissionAsync(currentUser.Id, "role.remove_permission"))
            {
                return ApiResponse.Forbidden("You don't have permission to remove permissions");
            }

            var roleId = context.Request.Params.GetValueOrDefault("id");
            var permissionId = context.Request.Params.GetValueOrDefault("permissionId");

            if (string.IsNullOrWhiteSpace(roleId) || !Guid.TryParse(roleId, out var roleGuid))
            {
                return ApiResponse.BadRequest("Invalid role ID");
            }

            if (string.IsNullOrWhiteSpace(permissionId) || !Guid.TryParse(permissionId, out var permGuid))
            {
                return ApiResponse.BadRequest("Invalid permission ID");
            }

            // Delete using QueryBuilder
            var result = await _mysql!.GetQueryBuilder<RolePermission>("Default")
                .Where(rp => rp.RoleId == roleGuid && rp.PermissionId == permGuid)
                .DeleteAsync();

            if (!result.Success || result.Data == 0)
            {
                return ApiResponse.NotFound("Permission assignment not found");
            }

            return ApiResponse.Ok(new { message = "Permission removed successfully" });
        }
        catch (Exception ex)
        {
            return ApiResponse.ServerError($"Error removing permission: {ex.Message}");
        }
    }

    private async Task<ApiResponse> HandleListPermissionsAsync(ApiContext context)
    {
        try
        {
            var currentUser = context.Request.Context.HttpContext.GetCurrentUser();
            if (currentUser == null)
            {
                return ApiResponse.Unauthorized("Authentication required");
            }

            if (!await _authService!.HasPermissionAsync(currentUser.Id, "permission.list"))
            {
                return ApiResponse.Forbidden("You don't have permission to list permissions");
            }

            var qb = _mysql!.GetQueryBuilder<Permission>("Default");
            qb.OrderBy(p => p.Resource).ThenBy(p => p.Action);

            var result = await qb.ExecuteAsync();

            if (!result.Success)
            {
                return ApiResponse.ServerError(result.ErrorMessage ?? "Failed to fetch permissions");
            }

            var permissions = result.Data!.Select(p => new
            {
                p.Id,
                p.Name,
                p.Resource,
                p.Action,
                p.Description,
                p.CreatedAt
            });

            return ApiResponse.Ok(new { permissions });
        }
        catch (Exception ex)
        {
            return ApiResponse.ServerError($"Error listing permissions: {ex.Message}");
        }
    }

    // ============= LOG MANAGEMENT API HANDLERS =============

    private async Task<ApiResponse> HandleListLogsAsync(ApiContext context)
    {
        try
        {
            var currentUser = context.Request.Context.HttpContext.GetCurrentUser();
            if (currentUser == null)
            {
                return ApiResponse.Unauthorized("Authentication required");
            }

            if (!await _authService!.HasPermissionAsync(currentUser.Id, "admin.access"))
            {
                return ApiResponse.Forbidden("You don't have permission to view logs");
            }

            var query = context.Request.Context.HttpContext.Request.Query;
            var page = int.TryParse(query["page"], out var p) ? p : 1;
            var pageSize = int.TryParse(query["pageSize"], out var ps) ? ps : 50;
            var category = query["category"].ToString();
            var level = query["level"].ToString();
            var search = query["search"].ToString();
            var userId = query["userId"].ToString();
            var source = query["source"].ToString();

            var qb = _mysql!.GetQueryBuilder<WebLogic.Server.Models.Database.LogEntry>("Default");

            // Apply filters
            if (!string.IsNullOrWhiteSpace(category) && int.TryParse(category, out var cat))
            {
                qb.Where(l => (int)l.Category == cat);
            }

            if (!string.IsNullOrWhiteSpace(level) && int.TryParse(level, out var lvl))
            {
                qb.Where(l => (int)l.Level >= lvl);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                qb.Where(l => l.Message.Contains(search) ||
                             (l.Details != null && l.Details.Contains(search)));
            }

            if (!string.IsNullOrWhiteSpace(userId) && Guid.TryParse(userId, out var uid))
            {
                qb.Where(l => l.UserId == uid);
            }

            if (!string.IsNullOrWhiteSpace(source))
            {
                qb.Where(l => l.Source == source);
            }

            qb.OrderByDescending(l => l.CreatedAt);

            // Use MySQL2 paging!
            var result = await qb.ExecutePagedAsync(page, pageSize);

            if (!result.Success)
            {
                return ApiResponse.ServerError(result.ErrorMessage ?? "Failed to fetch logs");
            }

            var logs = result.Data!.Items.Select(l => new
            {
                l.Id,
                category = l.Category.ToString(),
                categoryValue = (int)l.Category,
                level = l.Level.ToString(),
                levelValue = (int)l.Level,
                l.Message,
                l.Details,
                l.UserId,
                l.Username,
                l.IpAddress,
                l.UserAgent,
                l.RequestPath,
                l.HttpMethod,
                l.StatusCode,
                l.DurationMs,
                l.Exception,
                l.Source,
                l.CreatedAt
            });

            return ApiResponse.Ok(new
            {
                logs,
                pagination = new
                {
                    page = result.Data.PageNumber,
                    pageSize = result.Data.PageSize,
                    totalItems = result.Data.TotalItems,
                    totalPages = result.Data.TotalPages,
                    hasNextPage = result.Data.PageNumber < result.Data.TotalPages,
                    hasPreviousPage = result.Data.PageNumber > 1
                }
            });
        }
        catch (Exception ex)
        {
            return ApiResponse.ServerError($"Error fetching logs: {ex.Message}");
        }
    }

    private async Task<ApiResponse> HandleLogStatsAsync(ApiContext context)
    {
        try
        {
            var currentUser = context.Request.Context.HttpContext.GetCurrentUser();
            if (currentUser == null)
            {
                return ApiResponse.Unauthorized("Authentication required");
            }

            if (!await _authService!.HasPermissionAsync(currentUser.Id, "admin.access"))
            {
                return ApiResponse.Forbidden("You don't have permission to view log statistics");
            }

            var qb = _mysql!.GetQueryBuilder<WebLogic.Server.Models.Database.LogEntry>("Default");
            var result = await qb.ExecuteAsync();

            if (!result.Success || result.Data == null)
            {
                return ApiResponse.ServerError("Failed to fetch log statistics");
            }

            var logs = result.Data.ToList();

            // Calculate stats
            var totalLogs = logs.Count;
            var last24Hours = DateTime.UtcNow.AddHours(-24);
            var recentLogs = logs.Count(l => l.CreatedAt >= last24Hours);

            var byCategory = logs.GroupBy(l => l.Category)
                .Select(g => new
                {
                    category = g.Key.ToString(),
                    categoryValue = (int)g.Key,
                    count = g.Count(),
                    errors = g.Count(l => l.Level >= WebLogic.Server.Models.Database.LogLevel.Error),
                    warnings = g.Count(l => l.Level == WebLogic.Server.Models.Database.LogLevel.Warning)
                })
                .OrderByDescending(x => x.count)
                .ToList();

            var byLevel = logs.GroupBy(l => l.Level)
                .Select(g => new
                {
                    level = g.Key.ToString(),
                    levelValue = (int)g.Key,
                    count = g.Count()
                })
                .OrderBy(x => x.levelValue)
                .ToList();

            var topSources = logs.Where(l => !string.IsNullOrEmpty(l.Source))
                .GroupBy(l => l.Source)
                .Select(g => new
                {
                    source = g.Key,
                    count = g.Count()
                })
                .OrderByDescending(x => x.count)
                .Take(10)
                .ToList();

            var recentErrors = logs
                .Where(l => l.Level >= WebLogic.Server.Models.Database.LogLevel.Error)
                .OrderByDescending(l => l.CreatedAt)
                .Take(10)
                .Select(l => new
                {
                    l.Id,
                    category = l.Category.ToString(),
                    level = l.Level.ToString(),
                    l.Message,
                    l.Source,
                    l.CreatedAt
                })
                .ToList();

            return ApiResponse.Ok(new
            {
                totalLogs,
                recentLogs,
                byCategory,
                byLevel,
                topSources,
                recentErrors
            });
        }
        catch (Exception ex)
        {
            return ApiResponse.ServerError($"Error calculating log statistics: {ex.Message}");
        }
    }

    private async Task<ApiResponse> HandleLogCleanupAsync(ApiContext context)
    {
        try
        {
            var currentUser = context.Request.Context.HttpContext.GetCurrentUser();
            if (currentUser == null)
            {
                return ApiResponse.Unauthorized("Authentication required");
            }

            if (!await _authService!.HasPermissionAsync(currentUser.Id, "admin.access"))
            {
                return ApiResponse.Forbidden("You don't have permission to clean up logs");
            }

            // Get DatabaseLogger from service provider
            var dbLogger = context.Request.Context.ServiceProvider.GetService(typeof(WebLogic.Server.Services.DatabaseLogger))
                as WebLogic.Server.Services.DatabaseLogger;

            if (dbLogger == null)
            {
                return ApiResponse.ServerError("DatabaseLogger service not available");
            }

            var deletedCount = await dbLogger.CleanupOldLogsAsync();

            return ApiResponse.Ok(new
            {
                message = $"Successfully cleaned up {deletedCount} old log entries",
                deletedCount
            });
        }
        catch (Exception ex)
        {
            return ApiResponse.ServerError($"Error cleaning up logs: {ex.Message}");
        }
    }

    // ============= HELPERS =============

    /// <summary>
    /// Ensures a default admin user exists in the database on first run
    /// </summary>
    private async Task EnsureDefaultAdminUserAsync()
    {
        try
        {
            Console.WriteLine("    [Admin] DEBUG: Entered EnsureDefaultAdminUserAsync");

            if (_mysql == null)
            {
                Console.WriteLine("    [Admin] DEBUG: _mysql is null, skipping default user creation");
                return;
            }

            Console.WriteLine("    [Admin] DEBUG: _mysql is not null");

            var userRepo = _mysql.GetRepository<User>("Default");

            // Check if any users exist
            var allUsersResult = await userRepo.GetAllAsync();
            Console.WriteLine($"    [Admin] DEBUG: allUsersResult.Success: {allUsersResult.Success}");
            if (allUsersResult.Success && allUsersResult.Data != null)
            {
                Console.WriteLine($"    [Admin] DEBUG: allUsersResult.Data.Count: {allUsersResult.Data.Count()}");
                if (allUsersResult.Data.Any())
                {
                    Console.WriteLine("    [Admin] Users already exist, skipping default admin creation");
                    return;
                }
            }

            Console.WriteLine("    [Admin] No users found - creating default admin user...");

            // Get superadmin role
            var roleRepo = _mysql.GetRepository<Role>("Default");
            var allRolesResult = await roleRepo.GetAllAsync();
            Console.WriteLine($"    [Admin] DEBUG: allRolesResult.Success: {allRolesResult.Success}");

            var superadminRole = allRolesResult.Success && allRolesResult.Data != null
                ? allRolesResult.Data.FirstOrDefault(r => r.Name == "superadmin")
                : null;

            if (superadminRole == null)
            {
                Console.WriteLine("    [Admin] ✗ Superadmin role not found - cannot create admin user");
                return;
            }

            Console.WriteLine($"    [Admin] DEBUG: superadminRole.Id: {superadminRole.Id}");

            // Create admin user directly using repository (avoids reflection issues in UserService)
            var adminUser = new User
            {
                Id = Guid.NewGuid(),
                Username = "admin",
                Email = "admin@example.com",
                PasswordHash = PasswordHasher.HashPassword("Admin123!"),
                FirstName = "System",
                LastName = "Administrator",
                IsActive = true,
                IsEmailVerified = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            var createResult = await userRepo.InsertAsync(adminUser);
            Console.WriteLine($"    [Admin] DEBUG: userRepo.InsertAsync success: {createResult.Success}");

            if (!createResult.Success)
            {
                Console.WriteLine($"    [Admin] ✗ Failed to create admin user: {createResult.ErrorMessage}");
                return;
            }

            // Assign superadmin role
            var userRoleRepo = _mysql.GetRepository<UserRole>("Default");
            var userRole = new UserRole
            {
                UserId = adminUser.Id,
                RoleId = superadminRole.Id,
                AssignedBy = Guid.Empty,
                AssignedAt = DateTime.UtcNow
            };

            var assignResult = await userRoleRepo.InsertAsync(userRole);
            Console.WriteLine($"    [Admin] DEBUG: Assigned superadmin role, success: {assignResult.Success}");

            if (assignResult.Success)
            {
                Console.WriteLine("    [Admin] ✓ Created default admin user");
                Console.WriteLine("    [Admin]   Username: admin");
                Console.WriteLine("    [Admin]   Password: Admin123!");
                Console.WriteLine("    [Admin]   Role: superadmin");
            }
            else
            {
                Console.WriteLine($"    [Admin] ⚠ Admin user created but role assignment failed: {assignResult.ErrorMessage}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"    [Admin] ✗ Error creating default admin user: {ex.Message}");
            Console.WriteLine($"    [Admin] DEBUG: {ex.StackTrace}");
        }
    }

    // ============= REQUEST/RESPONSE MODELS =============

    private class LoginRequest
    {
        [System.Text.Json.Serialization.JsonPropertyName("username")]
        public string Username { get; set; } = string.Empty;

        [System.Text.Json.Serialization.JsonPropertyName("password")]
        public string Password { get; set; } = string.Empty;
    }

    private class CreateUserRequest
    {
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public Guid? RoleId { get; set; }
    }

    private class UpdateUserRequest
    {
        public string? Email { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public bool? IsActive { get; set; }
        public bool? IsEmailVerified { get; set; }
        public string? NewPassword { get; set; }
        public bool? ClearLock { get; set; }
    }

    private class AssignRoleRequest
    {
        public Guid RoleId { get; set; }
    }

    private class CreateRoleRequest
    {
        public string Name { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string? Description { get; set; }
    }

    private class UpdateRoleRequest
    {
        public string? DisplayName { get; set; }
        public string? Description { get; set; }
    }

    private class AssignPermissionRequest
    {
        public Guid PermissionId { get; set; }
    }
}
