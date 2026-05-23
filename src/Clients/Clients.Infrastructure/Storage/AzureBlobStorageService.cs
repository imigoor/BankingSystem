using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Clients.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Clients.Infrastructure.Storage;

public class AzureBlobStorageService : IBlobStorageService
{
    private readonly BlobServiceClient _blobServiceClient;
    private readonly string _containerName;
    private readonly ILogger<AzureBlobStorageService> _logger;

    public AzureBlobStorageService(
        BlobServiceClient blobServiceClient,
        IConfiguration configuration,
        ILogger<AzureBlobStorageService> logger)
    {
        _blobServiceClient = blobServiceClient;
        _containerName = configuration["AzureBlobStorage:ContainerName"]
            ?? throw new InvalidOperationException("AzureBlobStorage:ContainerName not configured.");
        _logger = logger;
    }

    public async Task<string> UploadAsync(
        Stream fileStream,
        string blobName,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);

        await containerClient.CreateIfNotExistsAsync(PublicAccessType.Blob, cancellationToken: cancellationToken);

        var blobClient = containerClient.GetBlobClient(blobName);

        var uploadOptions = new BlobUploadOptions
        {
            HttpHeaders = new BlobHttpHeaders { ContentType = contentType }
        };

        await blobClient.UploadAsync(fileStream, uploadOptions, cancellationToken);

        _logger.LogInformation("Uploaded blob {BlobName} to container {Container}", blobName, _containerName);

        return blobClient.Uri.ToString();
    }

    public async Task DeleteAsync(string blobUrl, CancellationToken cancellationToken = default)
    {
        var uri = new Uri(blobUrl);
        var blobName = uri.AbsolutePath.TrimStart('/').Replace($"{_containerName}/", string.Empty);
        var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
        var blobClient = containerClient.GetBlobClient(blobName);
        await blobClient.DeleteIfExistsAsync(cancellationToken: cancellationToken);

        _logger.LogInformation("Deleted blob {BlobName}", blobName);
    }
}
