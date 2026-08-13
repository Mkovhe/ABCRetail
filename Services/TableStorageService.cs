using Azure;
using Azure.Data.Tables;
using ABCRetail.Models;

namespace ABCRetail.Services
{
    // Wraps all interaction with Azure Table Storage for both
    // the CustomerProfiles and Products tables.
    public class TableStorageService
    {
        private readonly TableClient _customerTable;
        private readonly TableClient _productTable;

        public TableStorageService(IConfiguration configuration)
        {
            string connectionString = configuration["AzureStorage:ConnectionString"]!;

            _customerTable = new TableClient(connectionString, "CustomerProfiles");
            _productTable = new TableClient(connectionString, "Products");

            // Safe to call even if the table already exists - does nothing in that case.
            _customerTable.CreateIfNotExists();
            _productTable.CreateIfNotExists();
        }

        // ---------- Customers ----------

        public async Task AddCustomerAsync(CustomerProfile customer)
        {
            await _customerTable.AddEntityAsync(customer);
        }

        public async Task<List<CustomerProfile>> GetAllCustomersAsync()
        {
            var results = new List<CustomerProfile>();
            await foreach (var entity in _customerTable.QueryAsync<CustomerProfile>())
            {
                results.Add(entity);
            }
            return results;
        }

        public async Task DeleteCustomerAsync(string partitionKey, string rowKey)
        {
            await _customerTable.DeleteEntityAsync(partitionKey, rowKey);
        }

        // ---------- Products ----------

        public async Task AddProductAsync(Product product)
        {
            await _productTable.AddEntityAsync(product);
        }

        public async Task<List<Product>> GetAllProductsAsync()
        {
            var results = new List<Product>();
            await foreach (var entity in _productTable.QueryAsync<Product>())
            {
                results.Add(entity);
            }
            return results;
        }

        public async Task DeleteProductAsync(string partitionKey, string rowKey)
        {
            await _productTable.DeleteEntityAsync(partitionKey, rowKey);
        }
    }
}