using Bevera.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Bevera.ViewComponents
{
    public class CategoriesMenuViewComponent : ViewComponent
    {
        private readonly ApplicationDbContext _db;

        public CategoriesMenuViewComponent(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var categories = await _db.Categories
                .Where(c => c.IsActive)
                .Include(c => c.ParentCategory)
                .OrderBy(c => c.Name)
                .ToListAsync();

            return View(categories);
        }
    }
}
