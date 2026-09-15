namespace Jotanunes.Infra.Storage.Settings;

public class S3Settings
{
    public const string SectionName = "S3";

    public string BucketName { get; set; } = string.Empty;
    public string Region { get; set; } = string.Empty;
    public string? AccessKey { get; set; }
    public string? SecretKey { get; set; }
    public string? ServiceUrl { get; set; }
    public bool ForcePathStyle { get; set; }
}
