using WebLogic.Shared.Abstractions;

namespace WebLogic.Server.Services;

/// <summary>
/// Local file system storage provider
/// </summary>
public class LocalStorageProvider : IStorageProvider
{
    private readonly string _rootPath;
    private readonly string _baseUrl;

    public string ProviderName => "local";

    public LocalStorageProvider(string rootPath, string baseUrl = "/uploads")
    {
        _rootPath = Path.GetFullPath(rootPath);
        _baseUrl = baseUrl.TrimEnd('/');

        // Ensure root directory exists
        if (!Directory.Exists(_rootPath))
        {
            Directory.CreateDirectory(_rootPath);
        }
    }

    public async Task<StorageResult> StoreAsync(Stream fileStream, string fileName, StorageOptions? options = null)
    {
        try
        {
            options ??= new StorageOptions();

            var targetPath = GetTargetPath(fileName, options);
            var targetDir = Path.GetDirectoryName(targetPath);

            if (!string.IsNullOrEmpty(targetDir) && !Directory.Exists(targetDir))
            {
                Directory.CreateDirectory(targetDir);
            }

            // Check if file exists and handle accordingly
            if (File.Exists(targetPath) && !options.Overwrite)
            {
                if (options.GenerateUniqueName)
                {
                    targetPath = GenerateUniquePath(targetPath);
                }
                else
                {
                    return new StorageResult
                    {
                        Success = false,
                        Error = "File already exists and overwrite is disabled"
                    };
                }
            }

            // Write file
            await using var fileStreamOutput = new FileStream(targetPath, FileMode.Create, FileAccess.Write);
            await fileStream.CopyToAsync(fileStreamOutput);

            var fileInfo = new FileInfo(targetPath);
            var relativePath = GetRelativePath(targetPath);

            return new StorageResult
            {
                Success = true,
                FilePath = relativePath,
                Url = GetUrl(relativePath),
                FileSize = fileInfo.Length,
                Metadata = new Dictionary<string, object>
                {
                    ["created_at"] = fileInfo.CreationTimeUtc,
                    ["full_path"] = targetPath
                }
            };
        }
        catch (Exception ex)
        {
            return new StorageResult
            {
                Success = false,
                Error = $"Failed to store file: {ex.Message}"
            };
        }
    }

    public async Task<StorageResult> StoreAsync(byte[] fileData, string fileName, StorageOptions? options = null)
    {
        using var memoryStream = new MemoryStream(fileData);
        return await StoreAsync(memoryStream, fileName, options);
    }

    public async Task<StorageResult> StoreFromFileAsync(string sourceFilePath, StorageOptions? options = null)
    {
        if (!File.Exists(sourceFilePath))
        {
            return new StorageResult
            {
                Success = false,
                Error = "Source file does not exist"
            };
        }

        options ??= new StorageOptions();
        options.FileName ??= Path.GetFileName(sourceFilePath);

        await using var fileStream = new FileStream(sourceFilePath, FileMode.Open, FileAccess.Read);
        return await StoreAsync(fileStream, options.FileName, options);
    }

    public async Task<Stream?> GetStreamAsync(string filePath)
    {
        var fullPath = GetFullPath(filePath);

        if (!File.Exists(fullPath))
        {
            return null;
        }

        var memoryStream = new MemoryStream();
        await using var fileStream = new FileStream(fullPath, FileMode.Open, FileAccess.Read);
        await fileStream.CopyToAsync(memoryStream);
        memoryStream.Position = 0;

        return memoryStream;
    }

    public async Task<byte[]?> GetBytesAsync(string filePath)
    {
        var fullPath = GetFullPath(filePath);

        if (!File.Exists(fullPath))
        {
            return null;
        }

        return await File.ReadAllBytesAsync(fullPath);
    }

    public async Task<bool> DownloadAsync(string filePath, string destinationPath)
    {
        try
        {
            var fullPath = GetFullPath(filePath);

            if (!File.Exists(fullPath))
            {
                return false;
            }

            var destDir = Path.GetDirectoryName(destinationPath);
            if (!string.IsNullOrEmpty(destDir) && !Directory.Exists(destDir))
            {
                Directory.CreateDirectory(destDir);
            }

            await using var sourceStream = new FileStream(fullPath, FileMode.Open, FileAccess.Read);
            await using var destStream = new FileStream(destinationPath, FileMode.Create, FileAccess.Write);
            await sourceStream.CopyToAsync(destStream);

            return true;
        }
        catch
        {
            return false;
        }
    }

    public Task<bool> DeleteAsync(string filePath)
    {
        try
        {
            var fullPath = GetFullPath(filePath);

            if (!File.Exists(fullPath))
            {
                return Task.FromResult(false);
            }

            File.Delete(fullPath);
            return Task.FromResult(true);
        }
        catch
        {
            return Task.FromResult(false);
        }
    }

    public Task<bool> ExistsAsync(string filePath)
    {
        var fullPath = GetFullPath(filePath);
        return Task.FromResult(File.Exists(fullPath));
    }

    public Task<long?> GetFileSizeAsync(string filePath)
    {
        try
        {
            var fullPath = GetFullPath(filePath);

            if (!File.Exists(fullPath))
            {
                return Task.FromResult<long?>(null);
            }

            var fileInfo = new FileInfo(fullPath);
            return Task.FromResult<long?>(fileInfo.Length);
        }
        catch
        {
            return Task.FromResult<long?>(null);
        }
    }

    public Task<string?> GetUrlAsync(string filePath)
    {
        var fullPath = GetFullPath(filePath);

        if (!File.Exists(fullPath))
        {
            return Task.FromResult<string?>(null);
        }

        return Task.FromResult<string?>(GetUrl(filePath));
    }

    public Task<Dictionary<string, object>?> GetMetadataAsync(string filePath)
    {
        try
        {
            var fullPath = GetFullPath(filePath);

            if (!File.Exists(fullPath))
            {
                return Task.FromResult<Dictionary<string, object>?>(null);
            }

            var fileInfo = new FileInfo(fullPath);

            var metadata = new Dictionary<string, object>
            {
                ["size"] = fileInfo.Length,
                ["created_at"] = fileInfo.CreationTimeUtc,
                ["modified_at"] = fileInfo.LastWriteTimeUtc,
                ["full_path"] = fullPath,
                ["relative_path"] = filePath,
                ["extension"] = fileInfo.Extension
            };

            return Task.FromResult<Dictionary<string, object>?>(metadata);
        }
        catch
        {
            return Task.FromResult<Dictionary<string, object>?>(null);
        }
    }

    public async Task<StorageResult> CopyAsync(string sourceFilePath, string destinationFilePath)
    {
        try
        {
            var sourceFullPath = GetFullPath(sourceFilePath);

            if (!File.Exists(sourceFullPath))
            {
                return new StorageResult
                {
                    Success = false,
                    Error = "Source file does not exist"
                };
            }

            var destFullPath = GetFullPath(destinationFilePath);
            var destDir = Path.GetDirectoryName(destFullPath);

            if (!string.IsNullOrEmpty(destDir) && !Directory.Exists(destDir))
            {
                Directory.CreateDirectory(destDir);
            }

            File.Copy(sourceFullPath, destFullPath, true);

            var fileInfo = new FileInfo(destFullPath);

            return new StorageResult
            {
                Success = true,
                FilePath = destinationFilePath,
                Url = GetUrl(destinationFilePath),
                FileSize = fileInfo.Length
            };
        }
        catch (Exception ex)
        {
            return new StorageResult
            {
                Success = false,
                Error = $"Failed to copy file: {ex.Message}"
            };
        }
    }

    public async Task<StorageResult> MoveAsync(string sourceFilePath, string destinationFilePath)
    {
        try
        {
            var sourceFullPath = GetFullPath(sourceFilePath);

            if (!File.Exists(sourceFullPath))
            {
                return new StorageResult
                {
                    Success = false,
                    Error = "Source file does not exist"
                };
            }

            var destFullPath = GetFullPath(destinationFilePath);
            var destDir = Path.GetDirectoryName(destFullPath);

            if (!string.IsNullOrEmpty(destDir) && !Directory.Exists(destDir))
            {
                Directory.CreateDirectory(destDir);
            }

            File.Move(sourceFullPath, destFullPath, true);

            var fileInfo = new FileInfo(destFullPath);

            return new StorageResult
            {
                Success = true,
                FilePath = destinationFilePath,
                Url = GetUrl(destinationFilePath),
                FileSize = fileInfo.Length
            };
        }
        catch (Exception ex)
        {
            return new StorageResult
            {
                Success = false,
                Error = $"Failed to move file: {ex.Message}"
            };
        }
    }

    public Task<IEnumerable<string>> ListFilesAsync(string? directory = null, string? searchPattern = null)
    {
        try
        {
            var targetDir = string.IsNullOrEmpty(directory)
                ? _rootPath
                : Path.Combine(_rootPath, directory);

            if (!Directory.Exists(targetDir))
            {
                return Task.FromResult(Enumerable.Empty<string>());
            }

            var pattern = searchPattern ?? "*.*";
            var files = Directory.GetFiles(targetDir, pattern, SearchOption.TopDirectoryOnly);

            var relativePaths = files.Select(f => GetRelativePath(f));

            return Task.FromResult(relativePaths);
        }
        catch
        {
            return Task.FromResult(Enumerable.Empty<string>());
        }
    }

    // Helper methods

    private string GetFullPath(string relativePath)
    {
        // Normalize path separators
        relativePath = relativePath.Replace('/', Path.DirectorySeparatorChar)
                                   .Replace('\\', Path.DirectorySeparatorChar);

        // Remove leading separator if present
        if (relativePath.StartsWith(Path.DirectorySeparatorChar))
        {
            relativePath = relativePath.TrimStart(Path.DirectorySeparatorChar);
        }

        return Path.Combine(_rootPath, relativePath);
    }

    private string GetRelativePath(string fullPath)
    {
        var relativePath = Path.GetRelativePath(_rootPath, fullPath);
        // Normalize to forward slashes for web URLs
        return relativePath.Replace(Path.DirectorySeparatorChar, '/');
    }

    private string GetTargetPath(string fileName, StorageOptions options)
    {
        var targetFileName = options.FileName ?? fileName;

        if (options.GenerateUniqueName && string.IsNullOrEmpty(options.FileName))
        {
            var extension = Path.GetExtension(fileName);
            var uniqueName = $"{Guid.NewGuid():N}{extension}";
            targetFileName = uniqueName;
        }

        var targetPath = string.IsNullOrEmpty(options.Directory)
            ? Path.Combine(_rootPath, targetFileName)
            : Path.Combine(_rootPath, options.Directory, targetFileName);

        return targetPath;
    }

    private string GenerateUniquePath(string filePath)
    {
        var directory = Path.GetDirectoryName(filePath) ?? "";
        var fileNameWithoutExt = Path.GetFileNameWithoutExtension(filePath);
        var extension = Path.GetExtension(filePath);

        var counter = 1;
        var newPath = filePath;

        while (File.Exists(newPath))
        {
            var newFileName = $"{fileNameWithoutExt}_{counter}{extension}";
            newPath = Path.Combine(directory, newFileName);
            counter++;
        }

        return newPath;
    }

    private string GetUrl(string relativePath)
    {
        // Normalize to forward slashes for URLs
        var normalizedPath = relativePath.Replace(Path.DirectorySeparatorChar, '/');
        return $"{_baseUrl}/{normalizedPath}";
    }
}
