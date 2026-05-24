namespace Payroll.Application.Interfaces;

public interface IFileStorageService
{
    Task<string> UploadAsync(string objectKey, Stream stream, string contentType, CancellationToken ct = default);
    Task DeleteAsync(string objectKey, CancellationToken ct = default);
    Task<Stream> GetAsync(string objectKey, CancellationToken ct = default);
}
