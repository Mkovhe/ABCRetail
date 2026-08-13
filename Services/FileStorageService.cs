using Azure.Storage.Files.Shares;

namespace ABCRetail.Services
{
    // Wraps all interaction with Azure Files for application log files.
    public class FileStorageService
    {
        private readonly ShareClient _shareClient;
        private const string LogFileName = "applog.txt";

        public FileStorageService(IConfiguration configuration)
        {
            string connectionString = configuration["AzureStorage:ConnectionString"]!;
            _shareClient = new ShareClient(connectionString, "logs");
            _shareClient.CreateIfNotExists();

            // Make sure the log file itself exists (0 bytes to start).
            var rootDir = _shareClient.GetRootDirectoryClient();
            var fileClient = rootDir.GetFileClient(LogFileName);
            if (!fileClient.Exists())
            {
                fileClient.Create(0);
            }
        }

        // Appends a single timestamped line to the log file.
        // Azure Files doesn't support simple "append" the way a local
        // file does, so we read the existing content, add the new line,
        // and re-upload the whole file. Fine for a project of this scale.
        public async Task AppendLogAsync(string message)
        {
            var rootDir = _shareClient.GetRootDirectoryClient();
            var fileClient = rootDir.GetFileClient(LogFileName);

            string existingContent = "";
            if (await fileClient.ExistsAsync())
            {
                var download = await fileClient.DownloadAsync();
                using var reader = new StreamReader(download.Value.Content);
                existingContent = await reader.ReadToEndAsync();
            }

            string newLine = $"{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} - {message}{Environment.NewLine}";
            string updatedContent = existingContent + newLine;

            byte[] bytes = System.Text.Encoding.UTF8.GetBytes(updatedContent);
            await fileClient.CreateAsync(bytes.Length);

            using var stream = new MemoryStream(bytes);
            await fileClient.UploadRangeAsync(new Azure.HttpRange(0, bytes.Length), stream);
        }

        // Reads back the full log file content for display on a page.
        public async Task<string> ReadLogAsync()
        {
            var rootDir = _shareClient.GetRootDirectoryClient();
            var fileClient = rootDir.GetFileClient(LogFileName);

            if (!await fileClient.ExistsAsync()) return string.Empty;

            var download = await fileClient.DownloadAsync();
            using var reader = new StreamReader(download.Value.Content);
            return await reader.ReadToEndAsync();
        }
    }
}