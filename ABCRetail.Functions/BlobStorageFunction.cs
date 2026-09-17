using Azure.Storage.Blobs;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace ABCRetail.Functions
{
    public class BlobStorageFunction
    {
        private readonly ILogger<BlobStorageFunction> _logger;

        public BlobStorageFunction(ILogger<BlobStorageFunction> logger)
        {
            _logger = logger;
        }

        // POST /api/UploadBlob
        // Send as multipart/form-data with a single file field named "file"
        [Function("UploadBlob")]
        public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequest req)
        {
            _logger.LogInformation("UploadBlob function triggered.");

            if (!req.HasFormContentType || req.Form.Files.Count == 0)
            {
                return new BadRequestObjectResult("Request must be multipart/form-data with a file field named 'file'.");
            }

            var file = req.Form.Files["file"] ?? req.Form.Files[0];

            var connectionString = Environment.GetEnvironmentVariable("AzureStorage__ConnectionString");
            var containerClient = new BlobContainerClient(connectionString, "product-images");
            await containerClient.CreateIfNotExistsAsync();

            var blobName = $"{Guid.NewGuid()}_{file.FileName}";
            var blobClient = containerClient.GetBlobClient(blobName);

            await using (var stream = file.OpenReadStream())
            {
                await blobClient.UploadAsync(stream, overwrite: true);
            }

            return new OkObjectResult($"Blob uploaded: {blobClient.Uri}");
        }
    }
}
