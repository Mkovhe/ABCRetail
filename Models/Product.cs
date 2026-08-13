using Azure;
using Azure.Data.Tables;

namespace ABCRetail.Models
{
    // Represents a row in the "Products" Azure Table.
    public class Product : ITableEntity
    {
        // Grouping by category makes sense as a PartitionKey -
        // it keeps related products together for efficient queries.
        public string PartitionKey { get; set; } = "General";
        public string RowKey { get; set; } = Guid.NewGuid().ToString();

        public string ProductName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public double Price { get; set; }
        public int StockQuantity { get; set; }

        // URL pointing to the product's image in Blob Storage
        public string ImageUrl { get; set; } = string.Empty;

        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }
    }
}