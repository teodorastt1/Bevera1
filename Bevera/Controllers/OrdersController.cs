using Bevera.Data;
using Bevera.Extensions;
using Bevera.Helpers;
using Bevera.Models;
using Bevera.Models.ViewModels;
using Bevera.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Bevera.Controllers
{
    [Authorize(Roles = "Admin,Worker")]
    public class OrdersController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly InvoiceService _invoiceService;
        private readonly UserManager<ApplicationUser> _userManager;

        public OrdersController(ApplicationDbContext context, InvoiceService invoiceService, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _invoiceService = invoiceService;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index(
    string? status,
    string? q,
    string? name,
    string? email,
    DateTime? from,
    DateTime? to,
    string? payment,
    string? sort,
    int page = 1,
    int pageSize = 10)
        {
            IQueryable<Order> query = _context.Orders
                .Include(o => o.Client)
                .AsNoTracking();

            // ===== Status =====
            if (!string.IsNullOrWhiteSpace(status))
                query = query.Where(o => o.Status == status);

            // ===== Quick search (id / email / name) =====
            if (!string.IsNullOrWhiteSpace(q))
            {
                q = q.Trim();

                query = query.Where(o =>
                    o.Id.ToString().Contains(q) ||
                    (o.Email != null && o.Email.Contains(q)) ||
                    (o.FullName != null && o.FullName.Contains(q)) ||
                    (o.Client != null && o.Client.Email.Contains(q))
                );
            }

            // ===== Name =====
            if (!string.IsNullOrWhiteSpace(name))
            {
                name = name.Trim();
                query = query.Where(o => o.FullName != null && o.FullName.Contains(name));
            }

            // ===== Email =====
            if (!string.IsNullOrWhiteSpace(email))
            {
                email = email.Trim();
                query = query.Where(o =>
                    (o.Email != null && o.Email.Contains(email)) ||
                    (o.Client != null && o.Client.Email.Contains(email))
                );
            }

            // ===== Date range =====
            if (from.HasValue)
                query = query.Where(o => o.ChangedAt >= from.Value.Date);

            if (to.HasValue)
                query = query.Where(o => o.ChangedAt < to.Value.Date.AddDays(1));

            // ===== Payment =====
            if (!string.IsNullOrWhiteSpace(payment))
                query = query.Where(o => o.PaymentStatus == payment);

            // ===== Sorting =====
            sort = string.IsNullOrWhiteSpace(sort) ? "changed_desc" : sort;

            query = sort switch
            {
                "changed_asc" => query.OrderBy(o => o.ChangedAt),
                "total_asc" => query.OrderBy(o => o.Total),
                "total_desc" => query.OrderByDescending(o => o.Total),
                "id_asc" => query.OrderBy(o => o.Id),
                "id_desc" => query.OrderByDescending(o => o.Id),
                _ => query.OrderByDescending(o => o.ChangedAt)
            };

            // ===== Paging validation =====
            if (page < 1) page = 1;
            if (pageSize < 5) pageSize = 5;

            var totalItems = await query.CountAsync();

            var items = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // ===== ViewBag (за да пазим филтрите) =====
            ViewBag.Status = status;
            ViewBag.Q = q;
            ViewBag.Name = name;
            ViewBag.Email = email;
            ViewBag.From = from;
            ViewBag.To = to;
            ViewBag.Payment = payment;
            ViewBag.Sort = sort;
            ViewBag.PageSize = pageSize;

            var vm = new PagedResult<Order>
            {
                Items = items,
                Page = page,
                PageSize = pageSize,
                TotalItems = totalItems
            };

            return View(vm);
        }

        public async Task<IActionResult> Details(int id)
        {
            var order = await _context.Orders
                .AsNoTracking()
                .Include(o => o.Client)
                .Include(o => o.Items).ThenInclude(i => i.Product)
                .Include(o => o.StatusHistory).ThenInclude(h => h.ChangedByUser)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null) return NotFound();
            return View(order);
        }

        // =========================
        // Workflow actions (Worker/Admin)
        // =========================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkPaid(int id)
        {
            var order = await _context.Orders.FirstOrDefaultAsync(o => o.Id == id);
            if (order == null) return NotFound();

            if (order.PaymentStatus != PaymentStates.Paid)
            {
                order.PaymentStatus = PaymentStates.Paid;
                order.PaidOn = DateTime.UtcNow;
                order.ChangedAt = DateTime.UtcNow;

                await AddHistory(order.Id, order.Status, "Плащане: маркирано като ПЛАТЕНО (симулация).");
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Details), new { id });
        }

        // Submitted -> Preparing (само ако е Paid)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> StartPreparing(int id)
        {
            var order = await _context.Orders.FirstOrDefaultAsync(o => o.Id == id);
            if (order == null) return NotFound();

            if (order.Status != OrderStates.Submitted)
                return RedirectToAction(nameof(Details), new { id });

            if (order.PaymentStatus != PaymentStates.Paid)
                return RedirectToAction(nameof(Details), new { id });

            order.Status = OrderStates.Preparing;
            order.ChangedAt = DateTime.UtcNow;

            await AddHistory(order.Id, OrderStates.Preparing, "Статус: подготовка/опаковане.");
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Details), new { id });
        }

        // Preparing -> ReadyForPickup
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkReadyForPickup(int id)
        {
            var order = await _context.Orders.FirstOrDefaultAsync(o => o.Id == id);
            if (order == null) return NotFound();

            if (order.Status != OrderStates.Preparing)
                return RedirectToAction(nameof(Details), new { id });

            order.Status = OrderStates.ReadyForPickup;
            order.ChangedAt = DateTime.UtcNow;

            await AddHistory(order.Id, OrderStates.ReadyForPickup, "Статус: готова за взимане/изпращане.");
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Details), new { id });
        }

        // ReadyForPickup -> Delivered
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Ship(int id)
        {
            var order = await _context.Orders.FirstOrDefaultAsync(o => o.Id == id);
            if (order == null) return NotFound();

            if (order.Status != OrderStates.ReadyForPickup)
                return RedirectToAction(nameof(Details), new { id });

            order.Status = OrderStates.Delivered;
            order.ChangedAt = DateTime.UtcNow;

            await AddHistory(order.Id, OrderStates.Delivered, "Статус: изпратена/пристигнала.");
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Details), new { id });
        }

        // Delivered -> Received
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkReceived(int id)
        {
            var order = await _context.Orders.FirstOrDefaultAsync(o => o.Id == id);
            if (order == null) return NotFound();

            if (order.Status != OrderStates.Delivered)
                return RedirectToAction(nameof(Details), new { id });

            order.Status = OrderStates.Received;
            order.ChangedAt = DateTime.UtcNow;

            await AddHistory(order.Id, OrderStates.Received, "Статус: получена.");
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Details), new { id });
        }

        // =========================
        // Invoice download
        // =========================
        public async Task<IActionResult> DownloadInvoice(int id)
        {
            var order = await _context.Orders.FirstOrDefaultAsync(o => o.Id == id);
            if (order == null) return NotFound();

            if (string.IsNullOrEmpty(order.InvoiceStoredFileName))
            {
                await _invoiceService.GenerateInvoiceAsync(order.Id);

                order = await _context.Orders.FirstOrDefaultAsync(o => o.Id == id);
                if (order == null) return NotFound();

                if (string.IsNullOrEmpty(order.InvoiceStoredFileName))
                    return StatusCode(500, "Failed to generate invoice metadata.");
            }

            var path = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "invoices", order.InvoiceStoredFileName);

            if (!System.IO.File.Exists(path))
                return NotFound("Фактурата липсва на сървъра.");

            return PhysicalFile(path, order.InvoiceContentType ?? "application/pdf", order.InvoiceFileName ?? $"Invoice_{order.Id}.pdf");
        }

        // =========================
        // Helper: history
        // =========================
        private async Task AddHistory(int orderId, string status, string? note)
        {
            var userId = _userManager.GetUserId(User) ?? "";

            _context.OrderStatusHistories.Add(new OrderStatusHistory
            {
                OrderId = orderId,
                Status = status,
                Note = note,
                ChangedAt = DateTime.UtcNow,
                ChangedByUserId = userId
            });

            await Task.CompletedTask;
        }
    }
}
