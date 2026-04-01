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

        public HomeController(ITenderService tenderService)
        {
            _tenderService = tenderService;
        }

        public async Task<IActionResult> Index(CancellationToken cancellationToken)
        {
            var tenders = await _tenderService.GetDashboardAsync(cancellationToken);
            return View(tenders);
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
