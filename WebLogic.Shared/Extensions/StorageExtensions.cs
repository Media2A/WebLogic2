using Microsoft.Extensions.DependencyInjection;
using WebLogic.Shared.Abstractions;
using WebLogic.Shared.Models;

namespace WebLogic.Shared.Extensions;

/// <summary>
/// Extension methods for storage operations on RequestContext
/// </summary>
public static class StorageExtensions
{
    /// <summary>
    /// Get the storage provider from services
    /// </summary>
    public static IStorageProvider GetStorage(this RequestContext context)
    {
        var storage = context.ServiceProvider.GetService<IStorageProvider>();
        if (storage == null)
        {
            throw new InvalidOperationException("Storage provider not registered. Please configure storage in your DI container.");
        }

        return storage;
    }

    /// <summary>
    /// Store a file from stream
    /// </summary>
    public static async Task<StorageResult> StoreFileAsync(
        this RequestContext context,
        Stream fileStream,
        string fileName,
        StorageOptions? options = null)
    {
        var storage = context.GetStorage();
        return await storage.StoreAsync(fileStream, fileName, options);
    }

    /// <summary>
    /// Store a file from byte array
    /// </summary>
    public static async Task<StorageResult> StoreFileAsync(
        this RequestContext context,
        byte[] fileData,
        string fileName,
        StorageOptions? options = null)
    {
        var storage = context.GetStorage();
        return await storage.StoreAsync(fileData, fileName, options);
    }

    /// <summary>
    /// Get a file as stream
    /// </summary>
    public static async Task<Stream?> GetFileStreamAsync(
        this RequestContext context,
        string filePath)
    {
        var storage = context.GetStorage();
        return await storage.GetStreamAsync(filePath);
    }

    /// <summary>
    /// Get a file as byte array
    /// </summary>
    public static async Task<byte[]?> GetFileBytesAsync(
        this RequestContext context,
        string filePath)
    {
        var storage = context.GetStorage();
        return await storage.GetBytesAsync(filePath);
    }

    /// <summary>
    /// Delete a file
    /// </summary>
    public static async Task<bool> DeleteFileAsync(
        this RequestContext context,
        string filePath)
    {
        var storage = context.GetStorage();
        return await storage.DeleteAsync(filePath);
    }

    /// <summary>
    /// Check if a file exists
    /// </summary>
    public static async Task<bool> FileExistsAsync(
        this RequestContext context,
        string filePath)
    {
        var storage = context.GetStorage();
        return await storage.ExistsAsync(filePath);
    }

    /// <summary>
    /// Get file URL
    /// </summary>
    public static async Task<string?> GetFileUrlAsync(
        this RequestContext context,
        string filePath)
    {
        var storage = context.GetStorage();
        return await storage.GetUrlAsync(filePath);
    }

    /// <summary>
    /// Get file metadata
    /// </summary>
    public static async Task<Dictionary<string, object>?> GetFileMetadataAsync(
        this RequestContext context,
        string filePath)
    {
        var storage = context.GetStorage();
        return await storage.GetMetadataAsync(filePath);
    }

    /// <summary>
    /// List files in a directory
    /// </summary>
    public static async Task<IEnumerable<string>> ListFilesAsync(
        this RequestContext context,
        string? directory = null,
        string? searchPattern = null)
    {
        var storage = context.GetStorage();
        return await storage.ListFilesAsync(directory, searchPattern);
    }

    /// <summary>
    /// Store uploaded file from request
    /// </summary>
    public static async Task<StorageResult> StoreUploadAsync(
        this RequestContext context,
        string formFieldName = "file",
        StorageOptions? options = null)
    {
        // This is a placeholder for future multipart form handling
        // Will need to implement file upload handling in the routing system
        throw new NotImplementedException("File upload handling will be implemented in a future phase");
    }
}
