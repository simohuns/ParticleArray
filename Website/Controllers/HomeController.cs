using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace Website.Controllers
{
    public class HomeController : Controller
    {
        private readonly IMemoryCache _memoryCache;
        private readonly ILogger _logger;

        public HomeController(IMemoryCache memoryCache, ILogger<WebcamUploadController> logger)
        {
            _memoryCache = memoryCache;
            _logger = logger;
        }

        public IActionResult Index()
        {
            // TODO this need cached so we don't read the file system every time
            ViewBag.ImageUrl = WebcamUploadController.GetWebcamUploadLatestUrl();
            return View();
        }
    }
}
