using Azure.Storage.Files.Shares;
using Azure.Storage.Files.Shares.Models;

namespace ABCRetail.Services
{
    // Wraps all interaction with Azure Files. Each log event becomes its
    // own separate file (not one growing file) so individual file records
    // are visible in the file share, matching the "5 records" requirement.
    public class FileStorageService
    {
        private readonly ShareClient _shareClient;

        public FileStorageService(IConfiguration configuration)
        {
            string connectionString = configuration["AzureStorage:ConnectionString"]!;
            _shareClient = new ShareClient(connectionString, "logs");
            _shareClient.CreateIfNotExists();
        }

        // Creates a new, uniquely-named log file for this single event.
        // e.g. log_20260813_182819123.txt containing one timestamped message.
        public async Task AppendLogAsync(string message)
        {
            var rootDir = _shareClient.GetRootDirectoryClient();

            string fileName = $"log_{DateTime.UtcNow:yyyyMMdd_HHmmssfff}.txt";
            string content = $"{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} - {message}";

            byte[] bytes = System.Text.Encoding.UTF8.GetBytes(content);
            var fileClient = rootDir.GetFileClient(fileName);

            await fileClient.CreateAsync(bytes.Length);
            using var stream = new MemoryStream(bytes);
            await fileClient.UploadRangeAsync(new Azure.HttpRange(0, bytes.Length), stream);
        }

        // Returns every log file's name and content, most recent first -
        // this is what the Logs page displays as a list of "records".
        public async Task<List<(string FileName, string Content)>> GetAllLogFilesAsync()
        {
            var results = new List<(string, string)>();
            var rootDir = _shareClient.GetRootDirectoryClient();

            await foreach (ShareFileItem item in rootDir.GetFilesAndDirectoriesAsync())
            {
                if (item.IsDirectory) continue;

                var fileClient = rootDir.GetFileClient(item.Name);
                var download = await fileClient.DownloadAsync();
                using var reader = new StreamReader(download.Value.Content);
                string content = await reader.ReadToEndAsync();

                results.Add((item.Name, content));
            }

            // Most recent first, based on the timestamp encoded in the filename.
            results.Sort((a, b) => string.Compare(b.Item1, a.Item1, StringComparison.Ordinal));
            return results;
        }
    }
}