using Microsoft.AspNetCore.Mvc;
using ABCRetail.Services;

namespace ABCRetail.Controllers
{
    public class OrdersController : Controller
    {
        private readonly QueueStorageService _queueStorageService;
        private readonly FileStorageService _fileStorageService;

        public OrdersController(QueueStorageService queueStorageService, FileStorageService fileStorageService)
        {
            _queueStorageService = queueStorageService;
            _fileStorageService = fileStorageService;
        }

        // GET: /Orders - shows currently queued messages
        public async Task<IActionResult> Index()
        {
            var messages = await _queueStorageService.PeekMessagesAsync(20);
            return View(messages);
        }

        // POST: /Orders/Send - manually push a new order message onto the queue
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Send(string orderDetails)
        {
            if (!string.IsNullOrWhiteSpace(orderDetails))
            {
                await _queueStorageService.SendMessageAsync(orderDetails);
                await _fileStorageService.AppendLogAsync($"Order message queued: {orderDetails}");
            }
            return RedirectToAction(nameof(Index));
        }

        // POST: /Orders/Process - simulates a worker picking up and completing the next message
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Process()
        {
            var processed = await _queueStorageService.ReceiveAndDeleteMessageAsync();
            if (processed != null)
            {
                await _fileStorageService.AppendLogAsync($"Order message processed and removed: {processed}");
            }
            return RedirectToAction(nameof(Index));
        }
    }
}