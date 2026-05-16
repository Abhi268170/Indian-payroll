namespace Payroll.Infrastructure.Storage;

internal sealed class MinioOptions
{
    public string Endpoint { get; set; } = "minio:9000";
    public string AccessKey { get; set; } = string.Empty;
    public string SecretKey { get; set; } = string.Empty;
    public string BucketName { get; set; } = "payroll";
    public bool UseHttps { get; set; } = false;
}
