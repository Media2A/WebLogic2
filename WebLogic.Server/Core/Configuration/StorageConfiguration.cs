namespace WebLogic.Server.Core.Configuration;

/// <summary>
/// Storage configuration options
/// </summary>
public class StorageConfiguration
{
    /// <summary>
    /// Default storage provider (local, s3, azure, etc.)
    /// </summary>
    public string DefaultProvider { get; set; } = "local";

    /// <summary>
    /// Local storage options
    /// </summary>
    public LocalStorageOptions Local { get; set; } = new();

    /// <summary>
    /// S3 storage options (for future implementation)
    /// </summary>
    public S3StorageOptions? S3 { get; set; }

    /// <summary>
    /// Azure Blob storage options (for future implementation)
    /// </summary>
    public AzureBlobStorageOptions? AzureBlob { get; set; }
}

/// <summary>
/// Local file system storage options
/// </summary>
public class LocalStorageOptions
{
    /// <summary>
    /// Root directory for file storage (relative to application root or absolute path)
    /// </summary>
    public string RootPath { get; set; } = "uploads";

    /// <summary>
    /// Base URL for accessing uploaded files
    /// </summary>
    public string BaseUrl { get; set; } = "/uploads";

    /// <summary>
    /// Maximum file size in bytes (0 = unlimited)
    /// </summary>
    public long MaxFileSize { get; set; } = 10 * 1024 * 1024; // 10 MB default

    /// <summary>
    /// Allowed file extensions (empty = all allowed)
    /// </summary>
    public string[] AllowedExtensions { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Organize files by date (e.g., uploads/2025/11/02/file.jpg)
    /// </summary>
    public bool OrganizeByDate { get; set; } = false;
}

/// <summary>
/// S3 storage options (placeholder for future implementation)
/// </summary>
public class S3StorageOptions
{
    public string? AccessKey { get; set; }
    public string? SecretKey { get; set; }
    public string? BucketName { get; set; }
    public string? Region { get; set; }
    public string? BaseUrl { get; set; }
}

/// <summary>
/// Azure Blob storage options (placeholder for future implementation)
/// </summary>
public class AzureBlobStorageOptions
{
    public string? ConnectionString { get; set; }
    public string? ContainerName { get; set; }
    public string? BaseUrl { get; set; }
}
