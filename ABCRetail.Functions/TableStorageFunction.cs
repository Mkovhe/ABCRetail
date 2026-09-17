using System.Net;
using System.Text.Json;
using Azure;
using Azure.Data.Tables;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace ABCRetail.Functions
{
    // Simple entity used to write arbitrary key/value data into Table Storage.
    // PartitionKey groups related rows (e.g. "Customer"); RowKey is the unique id.
    public class GenericTableEntity : ITableEntity
    {
        public string PartitionKey { get; set; } = default!;
        public string RowKey { get; set; } = default!;
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }

        public string? Name { get; set; }
        public string? Details { get; set; }
    }

    public class TableStorageFunction
    {
        private readonly ILogger<TableStorageFunction> _logger;

        public TableStorageFunction(ILogger<TableStorageFunction> logger)
        {
            _logger = logger;
        }

        // POST /api/StoreTableEntity
        // Body (JSON): { "partitionKey": "Customer", "rowKey": "12345", "name": "John Doe", "details": "..." }
        [Function("StoreTableEntity")]
        public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequest req)
        {
            _logger.LogInformation("StoreTableEntity function triggered.");

            using var reader = new StreamReader(req.Body);
            var body = await reader.ReadToEndAsync();

            var entity = JsonSerializer.Deserialize<GenericTableEntity>(body, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (entity is null || string.IsNullOrWhiteSpace(entity.PartitionKey) || string.IsNullOrWhiteSpace(entity.RowKey))
            {
                return new BadRequestObjectResult("Request must include partitionKey and rowKey.");
            }

            var connectionString = Environment.GetEnvironmentVariable("AzureStorage__ConnectionString");
            var tableClient = new TableClient(connectionString, "ABCRetailData");
            await tableClient.CreateIfNotExistsAsync();

            await tableClient.UpsertEntityAsync(entity, TableUpdateMode.Replace);

            return new OkObjectResult($"Entity stored: PartitionKey={entity.PartitionKey}, RowKey={entity.RowKey}");
        }
    }
}
