namespace WebLogic.Shared.Models;

/// <summary>
/// Result of extension loading operation
/// </summary>
public class ExtensionLoadResult
{
    public int TotalFound { get; set; }
    public int SuccessfullyLoaded { get; set; }
    public int Failed { get; set; }
    public List<string> LoadedExtensionIds { get; set; } = new();
    public List<ExtensionLoadError> Errors { get; set; } = new();

    public bool Success => Failed == 0;
}

/// <summary>
/// Details of an extension loading error
/// </summary>
public class ExtensionLoadError
{
    public required string ExtensionId { get; set; }
    public required string ErrorMessage { get; set; }
    public Exception? Exception { get; set; }
}
