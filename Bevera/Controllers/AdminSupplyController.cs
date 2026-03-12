using Bevera.Data;
using Bevera.Helpers;
using Bevera.Models;
using Bevera.Models.Catalog;
using Bevera.Models.Finance;
using Bevera.Models.Inventory;
using Bevera.Models.Supply;
using Bevera.Models.ViewModels.Supply;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace Bevera.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminSupplyController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;

        public AdminSupplyController(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
        {
            _db = db;
            _userManager = userManager;
        }

        // =========================
        // DASHBOARD
        // =========================
        public async Task<IActionResult> Index()
        {
            var balance = await EnsureCompanyBalanceAsync();

            var totalIncome = await _db.FinanceTransactions
                .Where(x => x.Type == FinanceTypes.Income)
                .SumAsync(x => (decimal?)x.Amount) ?? 0m;

            var totalExpenses = await _db.FinanceTransactions
                .Where(x => x.Type == FinanceTypes.Expense)
                .SumAsync(x => (decimal?)x.Amount) ?? 0m;

            var grossProfit = await _db.OrderItems
                .Include(x => x.Product)
                .SumAsync(x => (decimal?)((x.UnitPrice - (x.Product != null ? x.Product.CostPrice : 0m)) * x.Quantity)) ?? 0m;

            var vm = new SupplyDashboardVm
            {
                CompanyBalance = balance.Balance,
                TotalIncome = totalIncome,
                TotalExpenses = totalExpenses,
                GrossProfit = grossProfit,
                DistributorsCount = await _db.Distributors.CountAsync(),
                DraftOrdersCount = await _db.PurchaseOrders.CountAsync(x => x.Status == PurchaseOrderStates.Draft),
                SubmittedOrdersCount = await _db.PurchaseOrders.CountAsync(x => x.Status == PurchaseOrderStates.Submitted),
                ReceivedOrdersCount = await _db.PurchaseOrders.CountAsync(x => x.Status == PurchaseOrderStates.Received),
                LatestOrders = await _db.PurchaseOrders
                    .Include(x => x.Distributor)
                    .OrderByDescending(x => x.CreatedAt)
                    .Take(10)
                    .Select(x => new PurchaseOrderShortVm
                    {
                        Id = x.Id,
                        DistributorName = x.Distributor.Name,
                        Status = x.Status,
                        TotalAmount = x.TotalAmount,
                        CreatedAt = x.CreatedAt
                    })
                    .ToListAsync()
            };

            return View(vm);
        }

        // =========================
        // DISTRIBUTORS
        // =========================
        public async Task<IActionResult> Distributors()
        {
            var distributors = await _db.Distributors
                .OrderBy(x => x.Name)
                .ToListAsync();

            return View(distributors);
        }

        [HttpGet]
        public IActionResult CreateDistributor()
        {
            return View(new DistributorFormVm());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateDistributor(DistributorFormVm vm)
        {
            if (!ModelState.IsValid)
                return View(vm);

            var entity = new Distributor
            {
                Name = vm.Name,
                Email = vm.Email,
                Phone = vm.Phone,
                Address = vm.Address,
                Notes = vm.Notes,
                IsActive = vm.IsActive,
                CreatedAt = DateTime.UtcNow
            };

            _db.Distributors.Add(entity);
            await _db.SaveChangesAsync();

            TempData["Success"] = "Дистрибуторът е добавен успешно.";
            return RedirectToAction(nameof(Distributors));
        }

        [HttpGet]
        public async Task<IActionResult> EditDistributor(int id)
        {
            var entity = await _db.Distributors.FindAsync(id);
            if (entity == null) return NotFound();

            var vm = new DistributorFormVm
            {
                Id = entity.Id,
                Name = entity.Name,
                Email = entity.Email,
                Phone = entity.Phone,
                Address = entity.Address,
                Notes = entity.Notes,
                IsActive = entity.IsActive
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditDistributor(DistributorFormVm vm)
        {
            if (!ModelState.IsValid)
                return View(vm);

            var entity = await _db.Distributors.FindAsync(vm.Id);
            if (entity == null) return NotFound();

            entity.Name = vm.Name;
            entity.Email = vm.Email;
            entity.Phone = vm.Phone;
            entity.Address = vm.Address;
            entity.Notes = vm.Notes;
            entity.IsActive = vm.IsActive;

            await _db.SaveChangesAsync();

            TempData["Success"] = "Дистрибуторът е редактиран успешно.";
            return RedirectToAction(nameof(Distributors));
        }

        // =========================
        // DISTRIBUTOR PRODUCTS / PRICE LIST
        // =========================
        [HttpGet]
        public async Task<IActionResult> DistributorProducts(int distributorId)
        {
            var distributor = await _db.Distributors.FindAsync(distributorId);
            if (distributor == null) return NotFound();

            ViewBag.Distributor = distributor;

            var items = await _db.DistributorProducts
                .Include(x => x.Product)
                .Where(x => x.DistributorId == distributorId)
                .OrderBy(x => x.Product.Name)
                .Select(x => new DistributorProductVm
                {
                    Id = x.Id,
                    DistributorId = x.DistributorId,
                    ProductId = x.ProductId,
                    ProductName = x.Product.Name,
                    CostPrice = x.CostPrice,
                    IsAvailable = x.IsAvailable
                })
                .ToListAsync();

            return View(items);
        }

        [HttpGet]
        public async Task<IActionResult> AddDistributorProduct(int distributorId)
        {
            var distributor = await _db.Distributors.FindAsync(distributorId);
            if (distributor == null) return NotFound();

            var usedProductIds = await _db.DistributorProducts
                .Where(x => x.DistributorId == distributorId)
                .Select(x => x.ProductId)
                .ToListAsync();

            var vm = new PurchaseOrderItemCreateVm
            {
                Products = await _db.Products
                    .Where(x => x.IsActive && !usedProductIds.Contains(x.Id))
                    .OrderBy(x => x.Name)
                    .Select(x => new SelectListItem
                    {
                        Value = x.Id.ToString(),
                        Text = x.Name
                    })
                    .ToListAsync()
            };

            ViewBag.Distributor = distributor;
            ViewBag.DistributorId = distributorId;
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddDistributorProduct(int distributorId, PurchaseOrderItemCreateVm vm)
        {
            var distributor = await _db.Distributors.FindAsync(distributorId);
            if (distributor == null) return NotFound();

            if (await _db.DistributorProducts.AnyAsync(x => x.DistributorId == distributorId && x.ProductId == vm.ProductId))
            {
                ModelState.AddModelError("", "Този продукт вече е добавен към дистрибутора.");
            }

            if (!ModelState.IsValid)
            {
                vm.Products = await _db.Products
                    .Where(x => x.IsActive)
                    .OrderBy(x => x.Name)
                    .Select(x => new SelectListItem
                    {
                        Value = x.Id.ToString(),
                        Text = x.Name
                    })
                    .ToListAsync();

                ViewBag.Distributor = distributor;
                ViewBag.DistributorId = distributorId;
                return View(vm);
            }

            var entity = new DistributorProduct
            {
                DistributorId = distributorId,
                ProductId = vm.ProductId,
                CostPrice = vm.CostPrice,
                IsAvailable = true,
                UpdatedAt = DateTime.UtcNow
            };

            _db.DistributorProducts.Add(entity);
            await _db.SaveChangesAsync();

            TempData["Success"] = "Продуктът е добавен към ценовата листа.";
            return RedirectToAction(nameof(DistributorProducts), new { distributorId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateDistributorProduct(DistributorProductVm vm)
        {
            var entity = await _db.DistributorProducts.FindAsync(vm.Id);
            if (entity == null) return NotFound();

            entity.CostPrice = vm.CostPrice;
            entity.IsAvailable = vm.IsAvailable;
            entity.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();

            TempData["Success"] = "Цената е обновена успешно.";
            return RedirectToAction(nameof(DistributorProducts), new { distributorId = entity.DistributorId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteDistributorProduct(int id)
        {
            var entity = await _db.DistributorProducts.FindAsync(id);
            if (entity == null) return NotFound();

            var distributorId = entity.DistributorId;

            _db.DistributorProducts.Remove(entity);
            await _db.SaveChangesAsync();

            TempData["Success"] = "Продуктът е премахнат от ценовата листа.";
            return RedirectToAction(nameof(DistributorProducts), new { distributorId });
        }

        // =========================
        // PURCHASE ORDERS
        // =========================
        [HttpGet]
        public async Task<IActionResult> PurchaseOrders()
        {
            var orders = await _db.PurchaseOrders
                .Include(x => x.Distributor)
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync();

            return View(orders);
        }

        [HttpGet]
        public async Task<IActionResult> CreatePurchaseOrder()
        {
            var vm = new PurchaseOrderCreateVm
            {
                Distributors = await _db.Distributors
                    .Where(x => x.IsActive)
                    .OrderBy(x => x.Name)
                    .Select(x => new SelectListItem
                    {
                        Value = x.Id.ToString(),
                        Text = x.Name
                    })
                    .ToListAsync()
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreatePurchaseOrder(PurchaseOrderCreateVm vm)
        {
            if (!ModelState.IsValid)
            {
                vm.Distributors = await _db.Distributors
                    .Where(x => x.IsActive)
                    .OrderBy(x => x.Name)
                    .Select(x => new SelectListItem
                    {
                        Value = x.Id.ToString(),
                        Text = x.Name
                    })
                    .ToListAsync();

                return View(vm);
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var entity = new PurchaseOrder
            {
                DistributorId = vm.DistributorId,
                Status = PurchaseOrderStates.Draft,
                CreatedAt = DateTime.UtcNow,
                CreatedByUserId = user.Id,
                Notes = vm.Notes,
                TotalAmount = 0m
            };

            _db.PurchaseOrders.Add(entity);
            await _db.SaveChangesAsync();

            TempData["Success"] = "Заявката е създадена успешно.";
            return RedirectToAction(nameof(PurchaseOrderDetails), new { id = entity.Id });
        }

        [HttpGet]
        public async Task<IActionResult> PurchaseOrderDetails(int id)
        {
            var entity = await _db.PurchaseOrders
                .Include(x => x.Distributor)
                .Include(x => x.Items)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (entity == null) return NotFound();

            var vm = new PurchaseOrderDetailsVm
            {
                Id = entity.Id,
                DistributorName = entity.Distributor.Name,
                Status = entity.Status,
                CreatedAt = entity.CreatedAt,
                SubmittedAt = entity.SubmittedAt,
                ReceivedAt = entity.ReceivedAt,
                TotalAmount = entity.TotalAmount,
                Notes = entity.Notes,
                Items = entity.Items
                    .OrderBy(x => x.ProductName)
                    .Select(x => new PurchaseOrderItemRowVm
                    {
                        Id = x.Id,
                        ProductId = x.ProductId,
                        ProductName = x.ProductName,
                        Quantity = x.Quantity,
                        CostPrice = x.CostPrice,
                        LineTotal = x.LineTotal
                    })
                    .ToList()
            };

            ViewBag.CanEdit = entity.Status == PurchaseOrderStates.Draft;
            ViewBag.CanSubmit = entity.Status == PurchaseOrderStates.Draft && entity.Items.Any();
            ViewBag.CanReceive = entity.Status == PurchaseOrderStates.Submitted;

            return View(vm);
        }

        [HttpGet]
        public async Task<IActionResult> AddPurchaseOrderItem(int purchaseOrderId)
        {
            var po = await _db.PurchaseOrders
                .Include(x => x.Distributor)
                .FirstOrDefaultAsync(x => x.Id == purchaseOrderId);

            if (po == null) return NotFound();

            if (po.Status != PurchaseOrderStates.Draft)
            {
                TempData["Error"] = "Можеш да добавяш редове само към чернова.";
                return RedirectToAction(nameof(PurchaseOrderDetails), new { id = purchaseOrderId });
            }

            var distributorProducts = await _db.DistributorProducts
                .Include(x => x.Product)
                .Where(x => x.DistributorId == po.DistributorId && x.IsAvailable)
                .OrderBy(x => x.Product.Name)
                .ToListAsync();

            var vm = new PurchaseOrderItemCreateVm
            {
                PurchaseOrderId = purchaseOrderId,
                Products = distributorProducts.Select(x => new SelectListItem
                {
                    Value = x.ProductId.ToString(),
                    Text = $"{x.Product.Name} - {x.CostPrice:F2} лв."
                }).ToList()
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddPurchaseOrderItem(PurchaseOrderItemCreateVm vm)
        {
            var po = await _db.PurchaseOrders
                .Include(x => x.Items)
                .FirstOrDefaultAsync(x => x.Id == vm.PurchaseOrderId);

            if (po == null) return NotFound();

            if (po.Status != PurchaseOrderStates.Draft)
            {
                TempData["Error"] = "Можеш да добавяш редове само към чернова.";
                return RedirectToAction(nameof(PurchaseOrderDetails), new { id = vm.PurchaseOrderId });
            }

            var distributorProduct = await _db.DistributorProducts
                .Include(x => x.Product)
                .FirstOrDefaultAsync(x =>
                    x.DistributorId == po.DistributorId &&
                    x.ProductId == vm.ProductId &&
                    x.IsAvailable);

            if (distributorProduct == null)
            {
                ModelState.AddModelError("", "Този продукт не е наличен при избрания дистрибутор.");
            }

            if (!ModelState.IsValid)
            {
                vm.Products = await _db.DistributorProducts
                    .Include(x => x.Product)
                    .Where(x => x.DistributorId == po.DistributorId && x.IsAvailable)
                    .OrderBy(x => x.Product.Name)
                    .Select(x => new SelectListItem
                    {
                        Value = x.ProductId.ToString(),
                        Text = $"{x.Product.Name} - {x.CostPrice:F2} лв."
                    })
                    .ToListAsync();

                return View(vm);
            }

            var product = distributorProduct!.Product;
            var lineTotal = vm.Quantity * vm.CostPrice;

            var item = new PurchaseOrderItem
            {
                PurchaseOrderId = po.Id,
                ProductId = product.Id,
                ProductName = product.Name,
                Quantity = vm.Quantity,
                CostPrice = vm.CostPrice,
                LineTotal = lineTotal
            };

            _db.PurchaseOrderItems.Add(item);
            await _db.SaveChangesAsync();

            await RecalculatePurchaseOrderTotal(po.Id);

            TempData["Success"] = "Редът е добавен успешно.";
            return RedirectToAction(nameof(PurchaseOrderDetails), new { id = po.Id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeletePurchaseOrderItem(int id)
        {
            var item = await _db.PurchaseOrderItems.FindAsync(id);
            if (item == null) return NotFound();

            var po = await _db.PurchaseOrders.FindAsync(item.PurchaseOrderId);
            if (po == null) return NotFound();

            if (po.Status != PurchaseOrderStates.Draft)
            {
                TempData["Error"] = "Можеш да триеш редове само от чернова.";
                return RedirectToAction(nameof(PurchaseOrderDetails), new { id = po.Id });
            }

            _db.PurchaseOrderItems.Remove(item);
            await _db.SaveChangesAsync();

            await RecalculatePurchaseOrderTotal(po.Id);

            TempData["Success"] = "Редът е изтрит успешно.";
            return RedirectToAction(nameof(PurchaseOrderDetails), new { id = po.Id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitPurchaseOrder(int id)
        {
            var po = await _db.PurchaseOrders
                .Include(x => x.Items)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (po == null) return NotFound();

            if (po.Status != PurchaseOrderStates.Draft)
            {
                TempData["Error"] = "Само чернова може да бъде изпратена.";
                return RedirectToAction(nameof(PurchaseOrderDetails), new { id });
            }

            if (!po.Items.Any())
            {
                TempData["Error"] = "Не можеш да изпратиш празна заявка.";
                return RedirectToAction(nameof(PurchaseOrderDetails), new { id });
            }

            po.Status = PurchaseOrderStates.Submitted;
            po.SubmittedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();

            TempData["Success"] = "Заявката е изпратена успешно.";
            return RedirectToAction(nameof(PurchaseOrderDetails), new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReceivePurchaseOrder(int id)
        {
            var po = await _db.PurchaseOrders
                .Include(x => x.Items)
                .Include(x => x.Distributor)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (po == null) return NotFound();

            if (po.Status != PurchaseOrderStates.Submitted)
            {
                TempData["Error"] = "Само изпратена заявка може да бъде получена.";
                return RedirectToAction(nameof(PurchaseOrderDetails), new { id });
            }

            var balance = await EnsureCompanyBalanceAsync();

            if (balance.Balance < po.TotalAmount)
            {
                TempData["Error"] = "Няма достатъчно фирмени средства за получаване на доставката.";
                return RedirectToAction(nameof(PurchaseOrderDetails), new { id });
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            using var transaction = await _db.Database.BeginTransactionAsync();

            try
            {
                foreach (var item in po.Items)
                {
                    var product = await _db.Products.FirstOrDefaultAsync(x => x.Id == item.ProductId);
                    if (product == null)
                        continue;

                    product.StockQty += item.Quantity;
                    product.Quantity = product.StockQty;
                    product.CostPrice = item.CostPrice;

                    _db.InventoryMovements.Add(new InventoryMovement
                    {
                        ProductId = product.Id,
                        QuantityDelta = item.Quantity,
                        Type = "IN",
                        Note = $"Получена доставка по заявка #{po.Id}",
                        CreatedAt = DateTime.UtcNow,
                        CreatedByUserId = user.Id,
                        OrderId = null
                    });
                }

                balance.Balance -= po.TotalAmount;
                balance.UpdatedAt = DateTime.UtcNow;

                _db.FinanceTransactions.Add(new FinanceTransaction
                {
                    Type = FinanceTypes.Expense,
                    Source = FinanceSources.PurchaseOrder,
                    Amount = po.TotalAmount,
                    Description = $"Разход по доставка от дистрибутор {po.Distributor.Name} по заявка #{po.Id}",
                    CreatedAt = DateTime.UtcNow,
                    PurchaseOrderId = po.Id,
                    CreatedByUserId = user.Id
                });

                po.Status = PurchaseOrderStates.Received;
                po.ReceivedAt = DateTime.UtcNow;
                po.ReceivedByUserId = user.Id;

                await _db.SaveChangesAsync();
                await transaction.CommitAsync();

                TempData["Success"] = "Доставката е получена успешно.";
            }
            catch
            {
                await transaction.RollbackAsync();
                TempData["Error"] = "Възникна проблем при получаване на доставката.";
            }

            return RedirectToAction(nameof(PurchaseOrderDetails), new { id });
        }

        // =========================
        // FINANCE
        // =========================
        public async Task<IActionResult> Finance()
        {
            var balance = await EnsureCompanyBalanceAsync();
            ViewBag.Balance = balance.Balance;

            var transactions = await _db.FinanceTransactions
                .OrderByDescending(x => x.CreatedAt)
                .Take(100)
                .ToListAsync();

            return View(transactions);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SeedBalance(decimal amount)
        {
            if (amount < 0)
            {
                TempData["Error"] = "Сумата не може да е отрицателна.";
                return RedirectToAction(nameof(Finance));
            }

            var balance = await EnsureCompanyBalanceAsync();
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            balance.Balance = amount;
            balance.UpdatedAt = DateTime.UtcNow;

            _db.FinanceTransactions.Add(new FinanceTransaction
            {
                Type = FinanceTypes.Income,
                Source = FinanceSources.Manual,
                Amount = amount,
                Description = "Начално или ръчно зададено фирмено салдо",
                CreatedAt = DateTime.UtcNow,
                CreatedByUserId = user.Id
            });

            await _db.SaveChangesAsync();

            TempData["Success"] = "Фирменият баланс е зададен успешно.";
            return RedirectToAction(nameof(Finance));
        }

        // =========================
        // HELPERS
        // =========================
        private async Task RecalculatePurchaseOrderTotal(int purchaseOrderId)
        {
            var po = await _db.PurchaseOrders
                .Include(x => x.Items)
                .FirstOrDefaultAsync(x => x.Id == purchaseOrderId);

            if (po == null) return;

            po.TotalAmount = po.Items.Sum(x => x.LineTotal);
            await _db.SaveChangesAsync();
        }

        private async Task<CompanyBalance> EnsureCompanyBalanceAsync()
        {
            var balance = await _db.CompanyBalances.FirstOrDefaultAsync();

            if (balance == null)
            {
                balance = new CompanyBalance
                {
                    Balance = 0m,
                    UpdatedAt = DateTime.UtcNow
                };

                _db.CompanyBalances.Add(balance);
                await _db.SaveChangesAsync();
            }

            return balance;
        }
    }
}