namespace WebLogic.Shared.Abstractions;

/// <summary>
/// Represents the result of a storage operation
/// </summary>
public class StorageResult
{
    public bool Success { get; set; }
    public string? FilePath { get; set; }
    public string? Url { get; set; }
    public string? Error { get; set; }
    public long? FileSize { get; set; }
    public Dictionary<string, object>? Metadata { get; set; }
}

/// <summary>
/// Represents options for storing a file
/// </summary>
public class StorageOptions
{
    /// <summary>
    /// Target directory/path within storage (e.g., "uploads/images")
    /// </summary>
    public string? Directory { get; set; }

    /// <summary>
    /// Custom filename (if null, generates unique name)
    /// </summary>
    public string? FileName { get; set; }

    /// <summary>
    /// Whether to overwrite existing files
    /// </summary>
    public bool Overwrite { get; set; } = false;

    /// <summary>
    /// Whether to generate a unique filename
    /// </summary>
    public bool GenerateUniqueName { get; set; } = true;

    /// <summary>
    /// File access permissions (public/private)
    /// </summary>
    public bool IsPublic { get; set; } = true;

    /// <summary>
    /// Additional metadata to store with the file
    /// </summary>
    public Dictionary<string, string>? Metadata { get; set; }
}

/// <summary>
/// Storage provider interface for file management
/// </summary>
public interface IStorageProvider
{
    /// <summary>
    /// Provider name (e.g., "local", "s3", "azure")
    /// </summary>
    string ProviderName { get; }

    /// <summary>
    /// Store a file from a stream
    /// </summary>
    Task<StorageResult> StoreAsync(Stream fileStream, string fileName, StorageOptions? options = null);

    /// <summary>
    /// Store a file from byte array
    /// </summary>
    Task<StorageResult> StoreAsync(byte[] fileData, string fileName, StorageOptions? options = null);

    /// <summary>
    /// Store a file from local file path
    /// </summary>
    Task<StorageResult> StoreFromFileAsync(string sourceFilePath, StorageOptions? options = null);

    /// <summary>
    /// Retrieve a file as a stream
    /// </summary>
    Task<Stream?> GetStreamAsync(string filePath);

    /// <summary>
    /// Retrieve a file as byte array
    /// </summary>
    Task<byte[]?> GetBytesAsync(string filePath);

    /// <summary>
    /// Download a file to local file system
    /// </summary>
    Task<bool> DownloadAsync(string filePath, string destinationPath);

    /// <summary>
    /// Delete a file
    /// </summary>
    Task<bool> DeleteAsync(string filePath);

    /// <summary>
    /// Check if a file exists
    /// </summary>
    Task<bool> ExistsAsync(string filePath);

    /// <summary>
    /// Get file size in bytes
    /// </summary>
    Task<long?> GetFileSizeAsync(string filePath);

    /// <summary>
    /// Get public URL for a file
    /// </summary>
    Task<string?> GetUrlAsync(string filePath);

    /// <summary>
    /// Get file metadata
    /// </summary>
    Task<Dictionary<string, object>?> GetMetadataAsync(string filePath);

    /// <summary>
    /// Copy a file to another location
    /// </summary>
    Task<StorageResult> CopyAsync(string sourceFilePath, string destinationFilePath);

    /// <summary>
    /// Move a file to another location
    /// </summary>
    Task<StorageResult> MoveAsync(string sourceFilePath, string destinationFilePath);

    /// <summary>
    /// List files in a directory
    /// </summary>
    Task<IEnumerable<string>> ListFilesAsync(string? directory = null, string? searchPattern = null);
}
