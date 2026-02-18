using System.Diagnostics;
using Bevera.Data;
using Bevera.Helpers;
using Bevera.Models;
using Bevera.Models.ViewModels;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Bevera.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ApplicationDbContext _db;

        public HomeController(ILogger<HomeController> logger, ApplicationDbContext db)
        {
            _logger = logger;
            _db = db;
        }

        public async Task<IActionResult> Index()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                if (User.IsInRole("Admin"))
                    return RedirectToAction("Index", "Admin");

                if (User.IsInRole("Worker"))
                    return RedirectToAction("Index", "Worker");
            }

            // Public home: show "Най-продавани" (hits) + "Най-нови".
            // Treat anything except Draft/Cancelled as a "real" order for hit stats.

            var bestSellerProductIds = await _db.OrderItems
                .AsNoTracking()
                .Where(oi => oi.Order.Status != OrderStates.Draft && oi.Order.Status != OrderStates.Cancelled)
                .GroupBy(oi => oi.ProductId)
                .Select(g => new { ProductId = g.Key, Qty = g.Sum(x => x.Quantity) })
                .OrderByDescending(x => x.Qty)
                .ThenByDescending(x => x.ProductId)
                .Take(10)
                .Select(x => x.ProductId)
                .ToListAsync();

            var bestSellers = await _db.Products
                .AsNoTracking()
                .Where(p => p.IsActive && bestSellerProductIds.Contains(p.Id))
                .Include(p => p.Images)
                .ToListAsync();

            // keep ordering as in ids list
            var bestOrdered = bestSellerProductIds
                .Select(id => bestSellers.FirstOrDefault(p => p.Id == id))
                .Where(p => p != null)!
                .ToList();

            var newest = await _db.Products
                .AsNoTracking()
                .Where(p => p.IsActive)
                .OrderByDescending(p => p.CreatedAt)
                .ThenByDescending(p => p.Id)
                .Include(p => p.Images)
                .Take(10)
                .ToListAsync();

            var vm = new HomeIndexVm
            {
                BestSellers = bestOrdered.Select(p => HomeProductCardVm.From(p!, isHit: true)).ToList(),
                Newest = newest.Select(p => HomeProductCardVm.From(p, isHit: false)).ToList()
            };

            return View(vm);
        }


        // Cookie policy / consent
        public IActionResult Cookies()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AcceptCookies()
        {
            var consent = HttpContext.Features.Get<ITrackingConsentFeature>();
            consent?.GrantConsent();

            TempData["FlashMessage"] = "Бисквитките са приети.";
            TempData["FlashType"] = "success";

            // go back where user was
            var referer = Request.Headers.Referer.ToString();
            return !string.IsNullOrWhiteSpace(referer) ? Redirect(referer) : RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult RejectCookies()
        {
            var consent = HttpContext.Features.Get<ITrackingConsentFeature>();
            consent?.WithdrawConsent();

            TempData["FlashMessage"] = "Запазихме избора ти. Ще използваме само задължителните бисквитки.";
            TempData["FlashType"] = "info";

            var referer = Request.Headers.Referer.ToString();
            return !string.IsNullOrWhiteSpace(referer) ? Redirect(referer) : RedirectToAction(nameof(Index));
        }

       

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
