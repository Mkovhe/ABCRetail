using Microsoft.AspNetCore.Mvc;
using ABCRetail.Models;
using ABCRetail.Services;

namespace ABCRetail.Controllers
{
    public class CustomersController : Controller
    {
        private readonly TableStorageService _tableStorageService;

        public CustomersController(TableStorageService tableStorageService)
        {
            _tableStorageService = tableStorageService;
        }

        // GET: /Customers
        public async Task<IActionResult> Index()
        {
            var customers = await _tableStorageService.GetAllCustomersAsync();
            return View(customers);
        }

        // GET: /Customers/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: /Customers/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CustomerProfile customer)
        {
            if (!ModelState.IsValid)
            {
                return View(customer);
            }

            await _tableStorageService.AddCustomerAsync(customer);
            return RedirectToAction(nameof(Index));
        }

        // POST: /Customers/Delete
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(string partitionKey, string rowKey)
        {
            await _tableStorageService.DeleteCustomerAsync(partitionKey, rowKey);
            return RedirectToAction(nameof(Index));
        }
    }
}