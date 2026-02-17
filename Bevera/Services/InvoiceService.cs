using Bevera.Data;
using Bevera.Models;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Bevera.Services
{
    public class InvoiceService
    {
        private readonly ApplicationDbContext _db;

        public InvoiceService(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task GenerateInvoiceAsync(int orderId)
        {
            var order = await _db.Orders
                .Include(o => o.Items)
                    .ThenInclude(i => i.Product)
                .AsNoTracking()
                .FirstOrDefaultAsync(o => o.Id == orderId);

            if (order == null) throw new Exception("Order not found.");

            var folder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "invoices");
            Directory.CreateDirectory(folder);

            var invoiceFileName = $"Invoice_{order.Id}.pdf";
            var stored = $"{Guid.NewGuid()}.pdf";
            var path = Path.Combine(folder, stored);

            // PDF документ
            var doc = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Margin(30);

                    page.Header().Text($"INVOICE #{order.Id}")
                        .FontSize(20).SemiBold();

                    page.Content().Column(col =>
                    {
                        col.Spacing(10);

                        col.Item().Text($"Date: {DateTime.UtcNow:dd.MM.yyyy HH:mm}");
                        col.Item().Text($"Customer: {order.FullName}");
                        col.Item().Text($"Email: {order.Email}");
                        if (!string.IsNullOrWhiteSpace(order.Phone))
                            col.Item().Text($"Phone: {order.Phone}");
                        if (!string.IsNullOrWhiteSpace(order.Address))
                            col.Item().Text($"Address: {order.Address}");

                        col.Item().LineHorizontal(1);

                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(4);
                                columns.RelativeColumn(2);
                                columns.RelativeColumn(2);
                                columns.RelativeColumn(2);
                            });

                            table.Header(header =>
                            {
                                header.Cell().Text("Product").SemiBold();
                                header.Cell().Text("Unit Price").SemiBold();
                                header.Cell().Text("Qty").SemiBold();
                                header.Cell().Text("Total").SemiBold();
                            });

                            foreach (var item in order.Items)
                            {
                                table.Cell().Text(item.ProductName ?? item.Product?.Name ?? "Item");
                                table.Cell().Text($"{item.UnitPrice:F2} лв.");
                                table.Cell().Text(item.Quantity.ToString());
                                table.Cell().Text($"{item.LineTotal:F2} лв.");
                            }
                        });

                        col.Item().LineHorizontal(1);
                        col.Item().AlignRight().Text($"Grand Total: {order.Total:F2} лв.").FontSize(14).SemiBold();
                    });

                    page.Footer().AlignCenter().Text("Bevera - Thank you!").FontSize(10);
                });
            });

            doc.GeneratePdf(path);

            var fi = new FileInfo(path);

            // запис метаданни в DB (тук трябва tracked entity)
            var trackedOrder = await _db.Orders.FirstAsync(o => o.Id == orderId);
            trackedOrder.InvoiceFileName = invoiceFileName;
            trackedOrder.InvoiceStoredFileName = stored;
            trackedOrder.InvoiceContentType = "application/pdf";
            trackedOrder.InvoiceFileSize = fi.Length;
            trackedOrder.InvoiceCreatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();
        }
    }
}
