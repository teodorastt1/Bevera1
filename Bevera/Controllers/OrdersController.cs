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

        public async Task<IActionResult> Index(string? status, string? q, int page = 1, int pageSize = 10)
        {
            IQueryable<Order> query = _context.Orders
                .Include(o => o.Client)
                .AsNoTracking();

            if (!string.IsNullOrWhiteSpace(status))
                query = query.Where(o => o.Status == status);

            if (!string.IsNullOrWhiteSpace(q))
            {
                q = q.Trim();
                query = query.Where(o =>
                    o.Id.ToString().Contains(q) ||
                    (o.Client.Email ?? "").Contains(q) ||
                    ((o.Client.FirstName ?? "") + " " + (o.Client.LastName ?? "")).Contains(q));
            }

            query = query.OrderByDescending(o => o.ChangedAt);

            var paged = await query.ToPagedAsync(page, pageSize);

            ViewBag.Status = status;
            ViewBag.Q = q;
            ViewBag.PageSize = pageSize;

            return View(paged);
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
