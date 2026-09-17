using System.Text;
using System.Text.Json;
using Azure.Storage.Queues;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace ABCRetail.Functions
{
    public class TransactionMessage
    {
        public string? TransactionId { get; set; }
        public string? CustomerId { get; set; }
        public decimal Amount { get; set; }
        public string? Description { get; set; }
    }

    public class QueueStorageFunction
    {
        private readonly ILogger<QueueStorageFunction> _logger;
        private const string QueueName = "transaction-queue";

        public QueueStorageFunction(ILogger<QueueStorageFunction> logger)
        {
            _logger = logger;
        }

        // POST /api/EnqueueTransaction
        // Body (JSON): { "transactionId": "T1", "customerId": "12345", "amount": 199.99, "description": "Order #123" }
        [Function("EnqueueTransaction")]
        public async Task<IActionResult> EnqueueTransaction([HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequest req)
        {
            _logger.LogInformation("EnqueueTransaction function triggered.");

            using var reader = new StreamReader(req.Body);
            var body = await reader.ReadToEndAsync();

            var message = JsonSerializer.Deserialize<TransactionMessage>(body, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (message is null || string.IsNullOrWhiteSpace(message.TransactionId))
            {
                return new BadRequestObjectResult("Request must include a transactionId.");
            }

            var connectionString = Environment.GetEnvironmentVariable("AzureStorage__ConnectionString");
            var queueClient = new QueueClient(connectionString, QueueName);
            await queueClient.CreateIfNotExistsAsync();

            var json = JsonSerializer.Serialize(message);
            var base64Message = Convert.ToBase64String(Encoding.UTF8.GetBytes(json));
            await queueClient.SendMessageAsync(base64Message);

            return new OkObjectResult($"Message queued for transaction {message.TransactionId}");
        }

        // GET /api/DequeueTransaction
        // Reads (and removes) the next message from the queue
        [Function("DequeueTransaction")]
        public async Task<IActionResult> DequeueTransaction([HttpTrigger(AuthorizationLevel.Function, "get")] HttpRequest req)
        {
            _logger.LogInformation("DequeueTransaction function triggered.");

            var connectionString = Environment.GetEnvironmentVariable("AzureStorage__ConnectionString");
            var queueClient = new QueueClient(connectionString, QueueName);
            await queueClient.CreateIfNotExistsAsync();

            var result = await queueClient.ReceiveMessageAsync();
            if (result.Value is null)
            {
                return new OkObjectResult("Queue is empty.");
            }

            var decoded = Encoding.UTF8.GetString(Convert.FromBase64String(result.Value.MessageText));
            await queueClient.DeleteMessageAsync(result.Value.MessageId, result.Value.PopReceipt);

            return new OkObjectResult(decoded);
        }
    }
}
