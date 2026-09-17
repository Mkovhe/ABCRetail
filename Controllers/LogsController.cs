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

        // GET: /Logs - displays every individual log file from Azure Files
        public async Task<IActionResult> Index()
        {
            var logFiles = await _fileStorageService.GetAllLogFilesAsync();
            return View(logFiles);
        }
    }
}