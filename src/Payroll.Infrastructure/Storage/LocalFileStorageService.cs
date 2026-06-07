using Microsoft.Extensions.Options;
using Payroll.Application.Interfaces;

namespace Payroll.Infrastructure.Storage;

/// <summary>
/// Filesystem-backed <see cref="IFileStorageService"/>. Used in k8s where a PVC is mounted
/// at <see cref="LocalStorageOptions.RootPath"/> instead of an S3/MinIO object store.
/// Object keys map directly to relative paths under the root.
/// </summary>
internal sealed class LocalFileStorageService : IFileStorageService
{
    private readonly string _root;

    public LocalFileStorageService(IOptions<LocalStorageOptions> options)
    {
        _root = Path.GetFullPath(options.Value.RootPath);
        Directory.CreateDirectory(_root);
    }

    public async Task<string> UploadAsync(string objectKey, Stream stream, string contentType, CancellationToken ct = default)
    {
        string path = ResolvePath(objectKey);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        await using FileStream file = File.Create(path);
        await stream.CopyToAsync(file, ct);
        return objectKey;
    }

    public Task DeleteAsync(string objectKey, CancellationToken ct = default)
    {
        string path = ResolvePath(objectKey);
        if (File.Exists(path))
            File.Delete(path);
        return Task.CompletedTask;
    }

    public Task<Stream> GetAsync(string objectKey, CancellationToken ct = default)
    {
        Stream stream = File.OpenRead(ResolvePath(objectKey));
        return Task.FromResult(stream);
    }

    // Map an object key to a path under the storage root, rejecting traversal outside it.
    private string ResolvePath(string objectKey)
    {
        string path = Path.GetFullPath(Path.Combine(_root, objectKey));
        if (path != _root && !path.StartsWith(_root + Path.DirectorySeparatorChar, StringComparison.Ordinal))
            throw new UnauthorizedAccessException($"Object key escapes storage root: {objectKey}");
        return path;
    }
}
