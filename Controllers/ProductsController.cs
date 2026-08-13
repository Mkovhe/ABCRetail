using Microsoft.AspNetCore.Mvc;
using ABCRetail.Models;
using ABCRetail.Services;

namespace ABCRetail.Controllers
{
    public class ProductsController : Controller
    {
        private readonly TableStorageService _tableStorageService;
        private readonly BlobStorageService _blobStorageService;
        private readonly QueueStorageService _queueStorageService;
        private readonly FileStorageService _fileStorageService;

        public ProductsController(
            TableStorageService tableStorageService,
            BlobStorageService blobStorageService,
            QueueStorageService queueStorageService,
            FileStorageService fileStorageService)
        {
            _tableStorageService = tableStorageService;
            _blobStorageService = blobStorageService;
            _queueStorageService = queueStorageService;
            _fileStorageService = fileStorageService;
        }

        // GET: /Products
        public async Task<IActionResult> Index()
        {
            var products = await _tableStorageService.GetAllProductsAsync();
            return View(products);
        }

        // GET: /Products/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: /Products/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Product product, IFormFile? imageFile)
        {
            if (!ModelState.IsValid)
            {
                return View(product);
            }

            // If an image was uploaded, send it to Blob Storage and
            // store the resulting URL on the product record.
            if (imageFile != null && imageFile.Length > 0)
            {
                string imageUrl = await _blobStorageService.UploadImageAsync(imageFile);
                product.ImageUrl = imageUrl;

                // Demonstrates Queue Storage: notify "processing" that an image was uploaded.
                await _queueStorageService.SendMessageAsync($"imageName: {imageFile.FileName} uploaded");
            }

            await _tableStorageService.AddProductAsync(product);

            // Demonstrates Queue Storage: notify that inventory changed.
            await _queueStorageService.SendMessageAsync($"Processing new product: {product.ProductName}");

            // Demonstrates File Storage: log the action.
            await _fileStorageService.AppendLogAsync($"Product added: {product.ProductName} (Stock: {product.StockQuantity})");

            return RedirectToAction(nameof(Index));
        }

        // POST: /Products/Delete
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(string partitionKey, string rowKey)
        {
            await _tableStorageService.DeleteProductAsync(partitionKey, rowKey);
            return RedirectToAction(nameof(Index));
        }
    }
}