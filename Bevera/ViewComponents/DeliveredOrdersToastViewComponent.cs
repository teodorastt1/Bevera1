using Bevera.Data;
using Bevera.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Bevera.ViewComponents
{
    public class DeliveredOrdersToastViewComponent : ViewComponent
    {
        private readonly ApplicationDbContext _db;

        public DeliveredOrdersToastViewComponent(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            if (User?.Identity?.IsAuthenticated != true)
                return Content(string.Empty);

            // only for clients
            if (User.IsInRole("Admin") || User.IsInRole("Worker"))
                return Content(string.Empty);

            var userId = UserClaimsPrincipal.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId))
                return Content(string.Empty);

            var deliveredCount = await _db.Orders
                .AsNoTracking()
                .Where(o => o.ClientId == userId)
                .Where(o => o.Status == OrderStates.Delivered)
                .CountAsync();

            return View("Default", deliveredCount);
        }
    }
}
