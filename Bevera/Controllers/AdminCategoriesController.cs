using Bevera.Data;
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

        // =========================
        // INDEX
        // =========================
        [HttpGet]
        public async Task<IActionResult> Index(string? q, DateTime? from, DateTime? to, int page = 1, int pageSize = 6)
        {
            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 5, 50);
            q = q?.Trim();

            // Paging само върху MAIN категории
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
            totalPages = Math.Max(1, totalPages);
            if (page > totalPages) page = totalPages;

            var mains = await mainQuery
                .OrderByDescending(c => c.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var mainIds = mains.Select(m => m.Id).ToList();

            // Subcategories само за текущите mains на страницата
            var subs = await _db.Categories
                .AsNoTracking()
                .Where(c => c.ParentCategoryId != null && mainIds.Contains(c.ParentCategoryId.Value))
                .OrderBy(c => c.Name)
                .ToListAsync();

            var items = new List<Category>();
            items.AddRange(mains);
            items.AddRange(subs);

            var model = new PagedResult<Category>
            {
                Items = items,
                Page = page,
                PageSize = pageSize,
                TotalItems = totalItems
            };

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

        // =========================
        // CREATE
        // =========================
        [HttpGet]
        public async Task<IActionResult> Create(int? parentId)
        {
            var vm = new AdminCategoryFormViewModel
            {
                ParentOptions = await GetParentOptionsAsync()
            };

            // ако идваш от "+ Subcategory"
            if (parentId.HasValue)
                vm.ParentCategoryId = parentId.Value;

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(AdminCategoryFormViewModel vm)
        {
            vm.ParentOptions = await GetParentOptionsAsync();

            if (!ModelState.IsValid)
                return View(vm);

            if (!await IsValidRootParentAsync(vm.ParentCategoryId, excludeId: null))
            {
                ModelState.AddModelError(nameof(vm.ParentCategoryId), "Невалидна основна категория.");
                return View(vm);
            }

            var category = new Category
            {
                Name = vm.Name.Trim(),
                IsActive = vm.IsActive,
                ParentCategoryId = vm.ParentCategoryId,
                CreatedAt = DateTime.UtcNow
            };

            if (vm.ImageFile != null && vm.ImageFile.Length > 0)
                category.ImagePath = await SaveCategoryImageAsync(vm.ImageFile);

            _db.Categories.Add(category);
            await _db.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        // =========================
        // EDIT
        // =========================
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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(AdminCategoryFormViewModel vm)
        {
            vm.ParentOptions = await GetParentOptionsAsync(excludeId: vm.Id);

            if (!ModelState.IsValid)
                return View(vm);

            var c = await _db.Categories.FirstOrDefaultAsync(x => x.Id == vm.Id);
            if (c == null) return NotFound();

            // ✅ FIX за проблема ти:
            // Ако категорията е БИЛА подкатегория (има ParentCategoryId),
            // НЕ позволяваме да стане main само защото vm.ParentCategoryId е null.
            // (Това е точно бъгът при "Inactive" -> Save.)
            var originalParentId = c.ParentCategoryId;
            var wasSubcategory = originalParentId.HasValue;

            // Валидираме parent само ако е подаден (не-null)
            // И ако е null, но е била подкатегория -> пазим стария parent.
            int? newParentId = vm.ParentCategoryId;

            if (wasSubcategory && newParentId == null)
            {
                // Пази йерархията. Подкатегория си остава подкатегория.
                newParentId = originalParentId;
            }
            else
            {
                // Ако иска да зададе parent -> трябва да е root и да не е самата категория
                if (!await IsValidRootParentAsync(newParentId, excludeId: vm.Id))
                {
                    ModelState.AddModelError(nameof(vm.ParentCategoryId), "Невалидна основна категория.");
                    return View(vm);
                }
            }

            c.Name = vm.Name.Trim();
            c.IsActive = vm.IsActive;
            c.ParentCategoryId = newParentId;

            if (vm.ImageFile != null && vm.ImageFile.Length > 0)
            {
                DeletePhysicalFileIfExists(c.ImagePath);
                c.ImagePath = await SaveCategoryImageAsync(vm.ImageFile);
            }

            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // =========================
        // DELETE (REAL DELETE)
        // =========================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var category = await _db.Categories.FirstOrDefaultAsync(c => c.Id == id);
            if (category == null) return NotFound();

            // ако има подкатегории -> не трий
            var hasSubCategories = await _db.Categories.AnyAsync(c => c.ParentCategoryId == id);
            if (hasSubCategories)
            {
                TempData["Error"] = "Не може да се изтрие категория, която има подкатегории. Изтрий първо подкатегориите.";
                return RedirectToAction(nameof(Index));
            }

            // ако има продукти -> не трий
            var hasProducts = await _db.Products.AnyAsync(p => p.CategoryId == id);
            if (hasProducts)
            {
                TempData["Error"] = "Не може да се изтрие подкатегория, която има продукти. Премести/изтрий продуктите първо.";
                return RedirectToAction(nameof(Index));
            }

            _db.Categories.Remove(category);

            try
            {
                await _db.SaveChangesAsync();
                // ако не искаш съобщения - махни този ред:
                TempData["Success"] = "Категорията е изтрита.";
            }
            catch (DbUpdateException)
            {
                TempData["Error"] = "Категорията не може да се изтрие (има свързани данни).";
            }

            return RedirectToAction(nameof(Index));
        }

        // =========================
        // HELPERS
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

            list.Insert(0, new SelectListItem { Value = "", Text = "— Основна категория —" });
            return list;
        }

        // ParentCategoryId може да е null (значи main category)
        private async Task<bool> IsValidRootParentAsync(int? parentCategoryId, int? excludeId)
        {
            if (!parentCategoryId.HasValue) return true; // main category е ок

            // не може да е самата категория
            if (excludeId.HasValue && parentCategoryId.Value == excludeId.Value)
                return false;

            // parent трябва да е ROOT (ParentCategoryId == null)
            return await _db.Categories.AsNoTracking()
                .AnyAsync(c => c.Id == parentCategoryId.Value && c.ParentCategoryId == null);
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
    }
}