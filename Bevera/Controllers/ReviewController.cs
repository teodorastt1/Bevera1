using Bevera.Data;
using Bevera.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

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

    [HttpPost]
    public async Task<IActionResult> Create(int productId, int rating, string comment)
    {
        var user = await _userManager.GetUserAsync(User);

        var review = new Review
        {
            ProductId = productId,
            UserId = user.Id,
            Rating = rating,
            Comment = comment
        };

        _db.Reviews.Add(review);
        await _db.SaveChangesAsync();

        return RedirectToAction("Product", "Client", new { id = productId });
    }
}