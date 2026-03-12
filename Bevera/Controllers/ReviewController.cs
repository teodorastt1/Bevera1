using Bevera.Data;
using Bevera.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Bevera.Controllers
{
    [Authorize]
    public class ReviewsController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;

        public ReviewsController(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
        {
            _db = db;
            _userManager = userManager;
        }

        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var reviews = await _db.Reviews
                .Include(r => r.Product)
                .Include(r => r.User)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            return View(reviews);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(int productId, int rating, string? comment)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Challenge();
            }

            if (rating < 1 || rating > 5)
            {
                TempData["Error"] = "Невалидна оценка.";
                return RedirectToAction("Product", "Client", new { id = productId });
            }

            var alreadyReviewed = await _db.Reviews
                .AnyAsync(r => r.ProductId == productId && r.UserId == user.Id);

            if (alreadyReviewed)
            {
                TempData["Error"] = "Вече имаш изпратено ревю за този продукт.";
                return RedirectToAction("Product", "Client", new { id = productId });
            }

            var review = new Review
            {
                ProductId = productId,
                UserId = user.Id,
                Rating = rating,
                Comment = string.IsNullOrWhiteSpace(comment) ? null : comment.Trim(),
                IsApproved = true,
                ApprovedAt = DateTime.UtcNow
            };

            _db.Reviews.Add(review);
            await _db.SaveChangesAsync();

            TempData["Success"] = "Ревюто беше добавено успешно.";
            return RedirectToAction("Product", "Client", new { id = productId });
        }
    }
}
