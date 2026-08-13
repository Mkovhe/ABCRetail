using Microsoft.AspNetCore.Mvc;
using ABCRetail.Services;

namespace ABCRetail.Controllers
{
    public class LogsController : Controller
    {
        private readonly FileStorageService _fileStorageService;

        public LogsController(FileStorageService fileStorageService)
        {
            _fileStorageService = fileStorageService;
        }

        // GET: /Logs - displays the full contents of applog.txt from Azure Files
        public async Task<IActionResult> Index()
        {
            string logContent = await _fileStorageService.ReadLogAsync();
            return View(model: logContent);
        }
    }
}