namespace Payroll.Infrastructure.Storage;

internal sealed class LocalStorageOptions
{
    /// <summary>Filesystem root for stored objects. In k8s this is a mounted PVC path.</summary>
    public string RootPath { get; set; } = "/data/storage";
}
