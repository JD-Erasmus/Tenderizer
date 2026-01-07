using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tenderizer.Models;
using Tenderizer.Services.Interfaces;
using Tenderizer.ViewModels;

namespace Tenderizer.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        private readonly ITenderService _tenderService;
        private readonly ILogger<HomeController> _logger;

        public HomeController(ITenderService tenderService, ILogger<HomeController> logger)
        {
            _tenderService = tenderService;
            _logger = logger;
        }

        public async Task<IActionResult> Index(CancellationToken cancellationToken)
        {
            var tenders = await _tenderService.GetDashboardAsync(cancellationToken);
            return View(tenders);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
