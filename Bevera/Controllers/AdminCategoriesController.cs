using Bevera.Data;
using Bevera.Extensions;
using Bevera.Models.Catalog;
using Bevera.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace Bevera.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminCategoriesController : Controller
    {
        private readonly ApplicationDbContext _db;

        public AdminCategoriesController(ApplicationDbContext db)
        {
            _db = db;
        }

        // GET: /AdminCategories
        public async Task<IActionResult> Index(string? q, DateTime? from, DateTime? to, int page = 1, int pageSize = 6)
        {
            if (page < 1) page = 1;
            if (pageSize < 5) pageSize = 5;
            if (pageSize > 50) pageSize = 50;

            q = q?.Trim();

            // 1) Paging върху MAIN категориите
            IQueryable<Category> mainQuery = _db.Categories
                .AsNoTracking()
                .Where(c => c.ParentCategoryId == null);

            if (!string.IsNullOrWhiteSpace(q))
                mainQuery = mainQuery.Where(c => c.Name.Contains(q));

            if (from.HasValue)
                mainQuery = mainQuery.Where(c => c.CreatedAt >= from.Value.Date);

            if (to.HasValue)
            {
                var end = to.Value.Date.AddDays(1);
                mainQuery = mainQuery.Where(c => c.CreatedAt < end);
            }

            var totalItems = await mainQuery.CountAsync();
            var totalPages = (int)Math.Ceiling((double)totalItems / pageSize);
            if (totalPages < 1) totalPages = 1;
            if (page > totalPages) page = totalPages;

            var mains = await mainQuery
                .OrderByDescending(c => c.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var mainIds = mains.Select(m => m.Id).ToList();

            // 2) Subcategories само за тези mains (в същата страница)
            var subs = await _db.Categories
                .AsNoTracking()
                .Where(c => c.ParentCategoryId != null && mainIds.Contains(c.ParentCategoryId.Value))
                .OrderBy(c => c.Name)
                .ToListAsync();

            // 3) Комбинираме (за View-то) -> mains първо, после subs
            var items = new List<Category>();
            items.AddRange(mains);
            items.AddRange(subs);

            // 4) PagedResult<Category> (вашият модел)
            var model = new PagedResult<Category>
            {
                Items = items,
                Page = page,
                PageSize = pageSize,
                TotalItems = totalItems
            };

            // 5) Pager за partial-а
            ViewBag.Pager = new PaginationViewModel
            {
                Page = page,
                TotalPages = model.TotalPages,
                Action = "Index",
                Controller = "AdminCategories",
                RouteValues = new Dictionary<string, string?>
                {
                    ["q"] = q,
                    ["from"] = from?.ToString("yyyy-MM-dd"),
                    ["to"] = to?.ToString("yyyy-MM-dd"),
                    ["pageSize"] = pageSize.ToString()
                }
            };

            ViewBag.Q = q ?? "";
            ViewBag.PageSize = pageSize;
            ViewBag.From = from?.ToString("yyyy-MM-dd");
            ViewBag.To = to?.ToString("yyyy-MM-dd");

            return View(model);
        }



        [HttpGet]
        public async Task<IActionResult> Create(int? parentId)
        {
            var parents = await _db.Categories
                .Where(c => c.ParentCategoryId == null)
                .OrderBy(c => c.Name)
                .ToListAsync();

            var vm = new AdminCategoryFormViewModel
            {
                ParentOptions = parents.Select(p => new SelectListItem
                {
                    Value = p.Id.ToString(),
                    Text = p.Name
                }).ToList()
            };

            vm.ParentOptions.Insert(0, new SelectListItem { Value = "", Text = "— Основна категория —" });

            // ✅ ако идваш от бутона "+ Subcategory"
            if (parentId.HasValue)
                vm.ParentCategoryId = parentId.Value;

            return View(vm);
        }


        // POST: /AdminCategories/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(AdminCategoryFormViewModel vm)
        {
            // винаги презареждай dropdown-а
            vm.ParentOptions = await GetParentOptionsAsync();

            if (!ModelState.IsValid)
                return View(vm);

            // ако е избран parent -> трябва да е root категория (ParentCategoryId == null)
            if (vm.ParentCategoryId.HasValue)
            {
                var parent = await _db.Categories.FirstOrDefaultAsync(c => c.Id == vm.ParentCategoryId.Value);
                if (parent == null)
                {
                    ModelState.AddModelError(nameof(vm.ParentCategoryId), "Невалидна основна категория.");
                    return View(vm);
                }

                if (parent.ParentCategoryId != null)
                {
                    ModelState.AddModelError(nameof(vm.ParentCategoryId), "Може да избираш само основна категория за Parent.");
                    return View(vm);
                }
            }

            var category = new Category
            {
                Name = vm.Name.Trim(),
                IsActive = vm.IsActive,
                ParentCategoryId = vm.ParentCategoryId,
                CreatedAt = DateTime.UtcNow
            };

            // ✅ реално качване на снимка
            if (vm.ImageFile != null && vm.ImageFile.Length > 0)
            {
                category.ImagePath = await SaveCategoryImageAsync(vm.ImageFile);
            }

            _db.Categories.Add(category);
            await _db.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        // GET: /AdminCategories/Edit/5
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var c = await _db.Categories.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
            if (c == null) return NotFound();

            var vm = new AdminCategoryFormViewModel
            {
                Id = c.Id,
                Name = c.Name,
                IsActive = c.IsActive,
                ExistingImagePath = c.ImagePath,
                ParentCategoryId = c.ParentCategoryId,
                ParentOptions = await GetParentOptionsAsync(excludeId: c.Id)
            };

            return View(vm);
        }

        // POST: /AdminCategories/Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(AdminCategoryFormViewModel vm)
        {
            vm.ParentOptions = await GetParentOptionsAsync(excludeId: vm.Id);

            if (!ModelState.IsValid)
                return View(vm);

            var c = await _db.Categories.FirstOrDefaultAsync(x => x.Id == vm.Id);
            if (c == null) return NotFound();

            // validate parent rule
            if (vm.ParentCategoryId.HasValue)
            {
                var parent = await _db.Categories.FirstOrDefaultAsync(x => x.Id == vm.ParentCategoryId.Value);
                if (parent == null)
                {
                    ModelState.AddModelError(nameof(vm.ParentCategoryId), "Невалидна основна категория.");
                    return View(vm);
                }
                if (parent.ParentCategoryId != null)
                {
                    ModelState.AddModelError(nameof(vm.ParentCategoryId), "Може да избираш само основна категория за Parent.");
                    return View(vm);
                }
                if (parent.Id == vm.Id)
                {
                    ModelState.AddModelError(nameof(vm.ParentCategoryId), "Категорията не може да е parent на самата себе си.");
                    return View(vm);
                }
            }

            c.Name = vm.Name.Trim();
            c.IsActive = vm.IsActive;
            c.ParentCategoryId = vm.ParentCategoryId;

            if (vm.ImageFile != null && vm.ImageFile.Length > 0)
            {
                DeletePhysicalFileIfExists(c.ImagePath);
                c.ImagePath = await SaveCategoryImageAsync(vm.ImageFile);
            }

            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // =========================
        // Helpers
        // =========================
        private async Task<List<SelectListItem>> GetParentOptionsAsync(int? excludeId = null)
        {
            var query = _db.Categories
                .AsNoTracking()
                .Where(c => c.ParentCategoryId == null);

            if (excludeId.HasValue)
                query = query.Where(c => c.Id != excludeId.Value);

            var parents = await query
                .OrderBy(c => c.Name)
                .ToListAsync();

            var list = parents.Select(p => new SelectListItem
            {
                Value = p.Id.ToString(),
                Text = p.Name
            }).ToList();

            // ✅ само един placeholder
            list.Insert(0, new SelectListItem { Value = "", Text = "— Основна категория —" });

            return list;
        }

        private async Task<string> SaveCategoryImageAsync(IFormFile file)
        {
            if (file.Length > 5 * 1024 * 1024)
                throw new Exception("Файлът е твърде голям (макс 5 MB).");

            var allowedExt = new[] { ".png", ".jpg", ".jpeg", ".webp" };
            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!allowedExt.Contains(ext))
                throw new Exception("Непозволен тип файл! (png/jpg/webp)");

            var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "categories");
            Directory.CreateDirectory(uploadsFolder);

            var stored = $"{Guid.NewGuid():N}{ext}";
            var fullPath = Path.Combine(uploadsFolder, stored);

            using var stream = new FileStream(fullPath, FileMode.Create);
            await file.CopyToAsync(stream);

            return $"/uploads/categories/{stored}";
        }

        private void DeletePhysicalFileIfExists(string? publicPath)
        {
            if (string.IsNullOrWhiteSpace(publicPath)) return;

            var relative = publicPath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
            var fullPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", relative);

            if (System.IO.File.Exists(fullPath))
                System.IO.File.Delete(fullPath);
        }



    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var category = await _db.Categories
            .FirstOrDefaultAsync(c => c.Id == id);

        if (category == null)
            return NotFound();

        // 1) ако има подкатегории -> НЕ трий
        var hasSubCategories = await _db.Categories.AnyAsync(c => c.ParentCategoryId == id);
        if (hasSubCategories)
        {
            TempData["Error"] = "Не може да се изтрие категория, която има подкатегории. Изтрий първо подкатегориите.";
            return RedirectToAction(nameof(Index));
        }

        // 2) ако има продукти -> НЕ трий
        var hasProducts = await _db.Products.AnyAsync(p => p.CategoryId == id);
        if (hasProducts)
        {
            TempData["Error"] = "Не може да се изтрие подкатегория, която има продукти. Премести/изтрий продуктите първо.";
            return RedirectToAction(nameof(Index));
        }

        // 3) Реално изтриване
        _db.Categories.Remove(category);

        try
        {
            await _db.SaveChangesAsync();
            TempData["Success"] = "Категорията е изтрита.";
        }
        catch (DbUpdateException)
        {
            // ако има FK зависимост, която сме изпуснали
            TempData["Error"] = "Категорията не може да се изтрие (има свързани данни).";
        }

        return RedirectToAction(nameof(Index));
    }
}
}
