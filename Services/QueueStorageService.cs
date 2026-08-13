using Azure.Storage.Queues;
using Azure.Storage.Queues.Models;

namespace ABCRetail.Services
{
    // Wraps all interaction with Azure Queue Storage for
    // order processing and inventory management messages.
    public class QueueStorageService
    {
        private readonly QueueClient _queueClient;

        public QueueStorageService(IConfiguration configuration)
        {
            string connectionString = configuration["AzureStorage:ConnectionString"]!;
            _queueClient = new QueueClient(connectionString, "orderprocessing");
            _queueClient.CreateIfNotExists();
        }

        // Sends a plain-text message onto the queue,
        // e.g. "Processing order #1023" or "imageName: shoe123.jpg uploaded".
        public async Task SendMessageAsync(string message)
        {
            await _queueClient.SendMessageAsync(message);
        }

        // Peeks at messages without removing them from the queue -
        // good for just displaying "what's currently queued" on a page.
        public async Task<List<string>> PeekMessagesAsync(int maxMessages = 10)
        {
            var results = new List<string>();
            PeekedMessage[] messages = await _queueClient.PeekMessagesAsync(maxMessages);

            foreach (var msg in messages)
            {
                results.Add(msg.MessageText);
            }
            return results;
        }

        // Actually removes the next message from the queue, simulating
        // a worker/background process picking up and completing the task.
        public async Task<string?> ReceiveAndDeleteMessageAsync()
        {
            QueueMessage[] messages = await _queueClient.ReceiveMessagesAsync(maxMessages: 1);
            if (messages.Length == 0) return null;

            var msg = messages[0];
            await _queueClient.DeleteMessageAsync(msg.MessageId, msg.PopReceipt);
            return msg.MessageText;
        }
    }
}