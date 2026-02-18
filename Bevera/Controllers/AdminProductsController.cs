using Bevera.Data;
using Bevera.Models.Catalog;
using Bevera.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace Bevera.Controllers
{
    [Authorize(Roles = "Admin,Worker")]
    public class AdminProductsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;

        public AdminProductsController(ApplicationDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        // GET: /AdminProducts
        public async Task<IActionResult> Index(string? q, int? categoryId, string? stock, string? sort, int? minQty, int? maxQty, int page = 1, int pageSize = 10)
        {
            ViewBag.Q = q;
            ViewBag.CategoryId = categoryId;
            ViewBag.Stock = stock;
            ViewBag.PageSize = pageSize;

            // ✅ само subcategories за филтъра + показваме Parent → Sub
            var categories = await _context.Categories
                .AsNoTracking()
                .Include(c => c.ParentCategory)
                .Where(c => c.ParentCategoryId != null)
                .OrderBy(c => c.ParentCategory!.Name)
                .ThenBy(c => c.Name)
                .Select(c => new SelectListItem($"{c.ParentCategory!.Name} → {c.Name}", c.Id.ToString()))
                .ToListAsync();

            ViewBag.Categories = categories;

            var query = _context.Products
                .Include(p => p.Category)
                .AsNoTracking()
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(q))
                query = query.Where(p => p.Name.Contains(q));

            if (categoryId.HasValue && categoryId.Value > 0)
                query = query.Where(p => p.CategoryId == categoryId.Value);

            if (!string.IsNullOrWhiteSpace(stock) && stock == "low")
                query = query.Where(p => p.StockQty > 0 &&
                                         p.StockQty <= (p.LowStockThreshold > 0 ? p.LowStockThreshold : 10));

            // ✅ out of stock ако решиш да го ползваш
            if (!string.IsNullOrWhiteSpace(stock) && stock == "out")
                query = query.Where(p => p.StockQty <= 0);

            if (minQty.HasValue)
                query = query.Where(p => p.StockQty >= minQty.Value);

            if (maxQty.HasValue)
                query = query.Where(p => p.StockQty <= maxQty.Value);



            // sort
            query = sort switch
            {
                "name_asc" => query.OrderBy(p => p.Name),
                "name_desc" => query.OrderByDescending(p => p.Name),

                "stock_asc" => query.OrderBy(p => p.StockQty).ThenBy(p => p.Name),
                "stock_desc" => query.OrderByDescending(p => p.StockQty).ThenBy(p => p.Name),

                _ => query.OrderByDescending(p => p.Id)
            };



            var total = await query.CountAsync();

            var items = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(p => new AdminProductRowViewModel
                {
                    Id = p.Id,
                    Name = p.Name,
                    CategoryName = p.Category.Name,
                    Price = p.Price,
                    StockQty = p.StockQty,
                    LowStockThreshold = p.LowStockThreshold,
                    IsActive = p.IsActive,
                    ImagePath = p.Images.OrderByDescending(i => i.IsMain).Select(i => i.ImagePath).FirstOrDefault()
                })
                .ToListAsync();

            var model = new PagedResult<AdminProductRowViewModel>
            {
                Items = items,
                Page = page,
                PageSize = pageSize,
                TotalItems = total
            };

            ViewBag.Sort = sort ?? "";
            ViewBag.MinQty = minQty?.ToString() ?? "";
            ViewBag.MaxQty = maxQty?.ToString() ?? "";


            return View(model);
        }

        // GET: /AdminProducts/Create
        public async Task<IActionResult> Create()
        {
            var vm = new AdminProductFormViewModel
            {
                Categories = await SubCategoriesDropDown()
            };
            return View(vm);
        }

        // POST: /AdminProducts/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(AdminProductFormViewModel vm)
        {
            // ✅ защита: CategoryId трябва да е subcategory
            if (!await IsSubCategory(vm.CategoryId))
                ModelState.AddModelError(nameof(vm.CategoryId), "Продукт може да бъде добавен само към подкатегория.");

            if (!ModelState.IsValid)
            {
                vm.Categories = await SubCategoriesDropDown();
                return View(vm);
            }

            var product = new Product
            {
                Name = vm.Name.Trim(),
                Description = vm.Description?.Trim(),
                Price = vm.Price,
                DiscountPercent = vm.DiscountPercent,
                DiscountEndsAt = vm.DiscountEndsAt,
                CategoryId = vm.CategoryId,
                StockQty = vm.StockQty,
                LowStockThreshold = vm.LowStockThreshold,
                IsActive = vm.IsActive,
                CreatedAt = DateTime.UtcNow
            };

            _context.Products.Add(product);
            await _context.SaveChangesAsync();

            if (vm.ImageFile != null && vm.ImageFile.Length > 0)
            {
                var savedPath = await SaveProductImage(vm.ImageFile);

                var img = new ProductImage
                {
                    ProductId = product.Id,
                    ImagePath = savedPath,
                    IsMain = true
                };

                _context.ProductImages.Add(img);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        // GET: /AdminProducts/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var product = await _context.Products
                .Include(p => p.Images)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null) return NotFound();

            var vm = new AdminProductFormViewModel
            {
                Id = product.Id,
                Name = product.Name,
                Description = product.Description,
                Price = product.Price,
                DiscountPercent = product.DiscountPercent,
                DiscountEndsAt = product.DiscountEndsAt,
                CategoryId = product.CategoryId,
                StockQty = product.StockQty,
                LowStockThreshold = product.LowStockThreshold,
                IsActive = product.IsActive,
                ExistingImagePath = product.Images.OrderByDescending(i => i.IsMain).Select(i => i.ImagePath).FirstOrDefault(),
                Categories = await SubCategoriesDropDown()
            };

            return View(vm);
        }



        // POST: /AdminProducts/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(AdminProductFormViewModel vm)
        {
            if (!await IsSubCategory(vm.CategoryId))
                ModelState.AddModelError(nameof(vm.CategoryId), "Продукт може да бъде добавен само към подкатегория.");

            if (!ModelState.IsValid)
            {
                vm.Categories = await SubCategoriesDropDown();
                return View(vm);
            }

            var product = await _context.Products
                .Include(p => p.Images)
                .FirstOrDefaultAsync(p => p.Id == vm.Id);

            if (product == null) return NotFound();

            product.Name = vm.Name.Trim();
            product.Description = vm.Description?.Trim();
            product.Price = vm.Price;
            product.DiscountPercent = vm.DiscountPercent;
            product.DiscountEndsAt = vm.DiscountEndsAt;
            product.CategoryId = vm.CategoryId;
            product.StockQty = vm.StockQty;
            product.LowStockThreshold = vm.LowStockThreshold;
            product.IsActive = vm.IsActive;

            if (vm.ImageFile != null && vm.ImageFile.Length > 0)
            {
                var oldMain = product.Images.FirstOrDefault(i => i.IsMain);
                if (oldMain != null)
                {
                    DeletePhysicalFileIfExists(oldMain.ImagePath);
                    oldMain.IsMain = false;
                }

                var saved = await SaveProductImage(vm.ImageFile);

                var newImg = new ProductImage
                {
                    ProductId = product.Id,
                    ImagePath = saved,
                    IsMain = true
                };

                product.Images.Add(newImg);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // POST: /AdminProducts/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var product = await _context.Products
                .Include(p => p.Images)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null) return NotFound();

            foreach (var img in product.Images)
                DeletePhysicalFileIfExists(img.ImagePath);

            _context.Products.Remove(product);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        // ✅ dropdown само за подкатегории
        private async Task<List<SelectListItem>> SubCategoriesDropDown()
        {
            var subs = await _context.Categories
                .AsNoTracking()
                .Include(c => c.ParentCategory)
                .Where(c => c.IsActive && c.ParentCategoryId != null)
                .OrderBy(c => c.ParentCategory!.Name)
                .ThenBy(c => c.Name)
                .Select(c => new SelectListItem($"{c.ParentCategory!.Name} → {c.Name}", c.Id.ToString()))
                .ToListAsync();

            // placeholder
            subs.Insert(0, new SelectListItem { Value = "", Text = "— Избери подкатегория —" });

            return subs;
        }

        // ✅ истинска проверка: дали избраната категория е subcategory
        private async Task<bool> IsSubCategory(int categoryId)
        {
            return await _context.Categories
                .AsNoTracking()
                .AnyAsync(c => c.Id == categoryId && c.ParentCategoryId != null);
        }

        private async Task<string> SaveProductImage(IFormFile file)
        {
            var folder = Path.Combine(_env.WebRootPath, "images", "products");
            Directory.CreateDirectory(folder);

            var ext = Path.GetExtension(file.FileName);
            var name = $"prod_{Guid.NewGuid():N}{ext}";
            var path = Path.Combine(folder, name);

            using (var stream = new FileStream(path, FileMode.Create))
                await file.CopyToAsync(stream);

            return $"/images/products/{name}";
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
