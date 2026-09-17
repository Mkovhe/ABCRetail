using System.Text;
using System.Text.Json;
using Azure.Storage.Files.Shares;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace ABCRetail.Functions
{
    public class FileWriteRequest
    {
        public string? FileName { get; set; }
        public string? Content { get; set; }
    }

    public class FileStorageFunction
    {
        private readonly ILogger<FileStorageFunction> _logger;
        private const string ShareName = "abcretail-logs";

        public FileStorageFunction(ILogger<FileStorageFunction> logger)
        {
            _logger = logger;
        }

        // POST /api/WriteFile
        // Body (JSON): { "fileName": "log-2026-09-16.txt", "content": "Order 123 processed successfully" }
        [Function("WriteFile")]
        public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequest req)
        {
            _logger.LogInformation("WriteFile function triggered.");

            using var reader = new StreamReader(req.Body);
            var body = await reader.ReadToEndAsync();

            var fileRequest = JsonSerializer.Deserialize<FileWriteRequest>(body, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (fileRequest is null || string.IsNullOrWhiteSpace(fileRequest.FileName))
            {
                return new BadRequestObjectResult("Request must include a fileName.");
            }

            var connectionString = Environment.GetEnvironmentVariable("AzureStorage__ConnectionString");
            var shareClient = new ShareClient(connectionString, ShareName);
            await shareClient.CreateIfNotExistsAsync();

            var rootDirectory = shareClient.GetRootDirectoryClient();
            var fileClient = rootDirectory.GetFileClient(fileRequest.FileName);

            var contentBytes = Encoding.UTF8.GetBytes(fileRequest.Content ?? string.Empty);
            using var stream = new MemoryStream(contentBytes);

            await fileClient.CreateAsync(contentBytes.Length);
            await fileClient.UploadRangeAsync(new Azure.HttpRange(0, contentBytes.Length), stream);

            return new OkObjectResult($"File written: {fileRequest.FileName}");
        }
    }
}