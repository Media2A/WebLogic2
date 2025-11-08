using CodeLogic.Abstractions;

namespace WebLogic.Modules.Admin;

/// <summary>
/// Manifest for the Admin module
/// </summary>
public class AdminManifest : ILibraryManifest
{
    public string Id => "weblogic.modules.admin";
    public string Name => "WebLogic Admin Module";
    public string Version => "1.0.0";
    public string Author => "WebLogic";
    public string Description => "Administration panel for user, role, and permission management";
    public IReadOnlyList<LibraryDependency> Dependencies { get; } = new[]
    {
        new LibraryDependency { Id = "cl.mysql2", MinVersion = "2.0.0" }
    };
    public IReadOnlyList<string> Tags { get; } = new[] { "admin", "auth", "users", "management" };
}
