using Amazon.S3;
using Amazon.S3.Model;
using Jotanunes.Application.Interfaces;
using Jotanunes.Infra.Storage.Settings;
using Microsoft.Extensions.Options;

namespace Jotanunes.Infra.Storage.Services;

public class S3DocumentStorageService(IAmazonS3 s3Client, IOptions<S3Settings> options) : IDocumentStorageService
{
    private readonly string _bucketName = options.Value.BucketName;

    public async Task UploadAsync(string key, Stream content, string contentType, CancellationToken cancellationToken = default)
    {
        var request = new PutObjectRequest
        {
            BucketName = _bucketName,
            Key = key,
            InputStream = content,
            ContentType = contentType,
            AutoCloseStream = false
        };

        await s3Client.PutObjectAsync(request, cancellationToken);
    }

    public async Task<Stream> DownloadAsync(string key, CancellationToken cancellationToken = default)
    {
        var response = await s3Client.GetObjectAsync(_bucketName, key, cancellationToken);
        return response.ResponseStream;
    }

    public async Task DeleteAsync(string key, CancellationToken cancellationToken = default)
    {
        await s3Client.DeleteObjectAsync(_bucketName, key, cancellationToken);
    }

    public async Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            await s3Client.GetObjectMetadataAsync(_bucketName, key, cancellationToken);
            return true;
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return false;
        }
    }
}
