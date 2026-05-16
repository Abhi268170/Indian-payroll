using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Options;
using Payroll.Application.Interfaces;

namespace Payroll.Infrastructure.Storage;

internal sealed class MinioFileStorageService(IAmazonS3 s3, IOptions<MinioOptions> options) : IFileStorageService
{
    private readonly string _bucket = options.Value.BucketName;

    public async Task<string> UploadAsync(string objectKey, Stream stream, string contentType, CancellationToken ct = default)
    {
        PutObjectRequest req = new()
        {
            BucketName = _bucket,
            Key = objectKey,
            InputStream = stream,
            ContentType = contentType,
            AutoCloseStream = false,
        };
        await s3.PutObjectAsync(req, ct);
        return objectKey;
    }

    public async Task DeleteAsync(string objectKey, CancellationToken ct = default) =>
        await s3.DeleteObjectAsync(_bucket, objectKey, ct);

    public async Task<Stream> GetAsync(string objectKey, CancellationToken ct = default)
    {
        GetObjectResponse resp = await s3.GetObjectAsync(_bucket, objectKey, ct);
        return resp.ResponseStream;
    }
}
