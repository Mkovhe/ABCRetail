using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;

namespace ABCRetail.Services
{
    // Wraps all interaction with Azure Blob Storage for product/profile images.
    public class BlobStorageService
    {
        private readonly BlobContainerClient _containerClient;

        public BlobStorageService(IConfiguration configuration)
        {
            string connectionString = configuration["AzureStorage:ConnectionString"]!;
            _containerClient = new BlobContainerClient(connectionString, "productimages");

            // Public access is disabled on this storage account (subscription policy),
            // so the container stays private and we generate SAS links per-blob instead.
            _containerClient.CreateIfNotExists(PublicAccessType.None);
        }

        // Uploads a file and returns a long-lived, read-only SAS URL,
        // which you can store alongside a Product record or display directly.
        public async Task<string> UploadImageAsync(IFormFile file)
        {
            string blobName = $"{Guid.NewGuid()}_{file.FileName}";
            var blobClient = _containerClient.GetBlobClient(blobName);

            using (var stream = file.OpenReadStream())
            {
                await blobClient.UploadAsync(stream, new BlobHttpHeaders
                {
                    ContentType = file.ContentType
                });
            }

            return GenerateReadOnlySasUrl(blobClient);
        }

        // Lists every blob currently in the container, each with a fresh SAS URL.
        public async Task<List<string>> ListImageUrlsAsync()
        {
            var urls = new List<string>();
            await foreach (var blobItem in _containerClient.GetBlobsAsync())
            {
                var blobClient = _containerClient.GetBlobClient(blobItem.Name);
                urls.Add(GenerateReadOnlySasUrl(blobClient));
            }
            return urls;
        }

        public async Task DeleteImageAsync(string blobName)
        {
            await _containerClient.DeleteBlobIfExistsAsync(blobName);
        }

        // Generates a signed URL that grants read-only access to a single
        // blob, valid for a long time - works even with public access disabled.
        private string GenerateReadOnlySasUrl(BlobClient blobClient)
        {
            if (!blobClient.CanGenerateSasUri)
            {
                // Shouldn't happen when using a full connection string with an account key,
                // but fall back to the plain (inaccessible) URI rather than crashing.
                return blobClient.Uri.ToString();
            }

            var sasBuilder = new BlobSasBuilder
            {
                BlobContainerName = _containerClient.Name,
                BlobName = blobClient.Name,
                Resource = "b",
                ExpiresOn = DateTimeOffset.UtcNow.AddYears(5)
            };
            sasBuilder.SetPermissions(BlobSasPermissions.Read);

            return blobClient.GenerateSasUri(sasBuilder).ToString();
        }
    }
}