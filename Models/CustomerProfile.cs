using Azure;
using Azure.Data.Tables;

namespace ABCRetail.Models
{
    // Represents a row in the "CustomerProfiles" Azure Table.
    // ITableEntity is required by the Azure.Data.Tables SDK -
    // it gives us PartitionKey, RowKey, Timestamp and ETag for free.
    public class CustomerProfile : ITableEntity
    {
        public string PartitionKey { get; set; } = "Customer";
        public string RowKey { get; set; } = Guid.NewGuid().ToString();

        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public DateTime RegisteredDate { get; set; } = DateTime.UtcNow;

        // Required by ITableEntity - Azure manages these automatically
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }
    }
}