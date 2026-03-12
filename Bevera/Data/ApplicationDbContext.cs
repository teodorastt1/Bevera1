using Bevera.Models;
using Bevera.Models.Catalog;
using Bevera.Models.Finance;
using Bevera.Models.Inventory;
using Bevera.Models.Supply;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Bevera.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Category> Categories { get; set; }
        public DbSet<Brand> Brands { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<ProductImage> ProductImages { get; set; }

        public DbSet<CartItem> CartItems { get; set; }

        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderItem> OrderItems { get; set; }
        public DbSet<OrderStatusHistory> OrderStatusHistories { get; set; }

        public DbSet<InventoryMovement> InventoryMovements { get; set; }

        public DbSet<Favorite> Favorites { get; set; } = default!;
        public DbSet<Review> Reviews { get; set; } = default!;
        public DbSet<AppNotification> AppNotifications { get; set; }

        // Supply
        public DbSet<Distributor> Distributors { get; set; }
        public DbSet<DistributorProduct> DistributorProducts { get; set; }
        public DbSet<PurchaseOrder> PurchaseOrders { get; set; }
        public DbSet<PurchaseOrderItem> PurchaseOrderItems { get; set; }

        // Finance
        public DbSet<CompanyBalance> CompanyBalances { get; set; }
        public DbSet<FinanceTransaction> FinanceTransactions { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // ======================
            // Decimal precision
            // ======================
            builder.Entity<Product>().Property(p => p.Price).HasPrecision(18, 2);
            builder.Entity<Product>().Property(p => p.CostPrice).HasPrecision(18, 2);
            builder.Entity<Product>().Property(p => p.VolumeLiters).HasPrecision(18, 3);
            builder.Entity<Product>().Property(p => p.AlcoholPercent).HasPrecision(5, 2);
            builder.Entity<Product>().Property(p => p.DiscountPercent).HasPrecision(5, 2);

            builder.Entity<CartItem>().Property(c => c.UnitPrice).HasPrecision(18, 2);

            builder.Entity<Order>().Property(o => o.Total).HasPrecision(18, 2);
            builder.Entity<OrderItem>().Property(oi => oi.UnitPrice).HasPrecision(18, 2);
            builder.Entity<OrderItem>().Property(oi => oi.LineTotal).HasPrecision(18, 2);

            builder.Entity<DistributorProduct>().Property(x => x.CostPrice).HasPrecision(18, 2);
            builder.Entity<PurchaseOrder>().Property(x => x.TotalAmount).HasPrecision(18, 2);
            builder.Entity<PurchaseOrderItem>().Property(x => x.CostPrice).HasPrecision(18, 2);
            builder.Entity<PurchaseOrderItem>().Property(x => x.LineTotal).HasPrecision(18, 2);

            builder.Entity<CompanyBalance>().Property(x => x.Balance).HasPrecision(18, 2);
            builder.Entity<FinanceTransaction>().Property(x => x.Amount).HasPrecision(18, 2);

            // ======================
            // Category
            // ======================
            builder.Entity<Category>()
                .HasOne(c => c.ParentCategory)
                .WithMany(c => c.SubCategories)
                .HasForeignKey(c => c.ParentCategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Category>()
                .HasIndex(c => c.Name)
                .IsUnique();

            // ======================
            // Product
            // ======================
            builder.Entity<Product>()
                .HasOne(p => p.Category)
                .WithMany(c => c.Products)
                .HasForeignKey(p => p.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Product>()
                .HasOne(p => p.Brand)
                .WithMany(b => b.Products)
                .HasForeignKey(p => p.BrandId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.Entity<ProductImage>()
                .HasOne(i => i.Product)
                .WithMany(p => p.Images)
                .HasForeignKey(i => i.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            // ======================
            // Cart
            // ======================
            builder.Entity<CartItem>()
                .HasOne(ci => ci.User)
                .WithMany()
                .HasForeignKey(ci => ci.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<CartItem>()
                .HasOne(ci => ci.Product)
                .WithMany()
                .HasForeignKey(ci => ci.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            // ======================
            // Orders
            // ======================
            builder.Entity<Order>()
                .HasOne(o => o.Client)
                .WithMany()
                .HasForeignKey(o => o.ClientId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<OrderItem>()
                .HasOne(i => i.Order)
                .WithMany(o => o.Items)
                .HasForeignKey(i => i.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<OrderItem>()
                .HasOne(i => i.Product)
                .WithMany()
                .HasForeignKey(i => i.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<OrderStatusHistory>()
                .HasOne(h => h.Order)
                .WithMany(o => o.StatusHistory)
                .HasForeignKey(h => h.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<OrderStatusHistory>()
                .HasOne(h => h.ChangedByUser)
                .WithMany()
                .HasForeignKey(h => h.ChangedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            // ======================
            // Inventory
            // ======================
            builder.Entity<InventoryMovement>()
                .HasOne(m => m.Product)
                .WithMany()
                .HasForeignKey(m => m.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<InventoryMovement>()
                .HasOne(m => m.CreatedByUser)
                .WithMany()
                .HasForeignKey(m => m.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<InventoryMovement>()
                .HasOne(m => m.Order)
                .WithMany()
                .HasForeignKey(m => m.OrderId)
                .OnDelete(DeleteBehavior.SetNull);

            // ======================
            // Favorites / Reviews
            // ======================
            builder.Entity<Favorite>()
                .HasIndex(x => new { x.UserId, x.ProductId })
                .IsUnique();

            builder.Entity<Review>()
                .HasIndex(x => new { x.UserId, x.ProductId })
                .IsUnique();

            // ======================
            // DistributorProduct
            // ======================
            builder.Entity<DistributorProduct>()
                .HasIndex(x => new { x.DistributorId, x.ProductId })
                .IsUnique();

            builder.Entity<DistributorProduct>()
                .HasOne(x => x.Distributor)
                .WithMany(d => d.DistributorProducts)
                .HasForeignKey(x => x.DistributorId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<DistributorProduct>()
                .HasOne(x => x.Product)
                .WithMany()
                .HasForeignKey(x => x.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            // ======================
            // PurchaseOrders
            // ======================
            builder.Entity<PurchaseOrder>()
                .HasOne(po => po.Distributor)
                .WithMany(d => d.PurchaseOrders)
                .HasForeignKey(po => po.DistributorId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<PurchaseOrder>()
                .HasOne(po => po.CreatedByUser)
                .WithMany()
                .HasForeignKey(po => po.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<PurchaseOrder>()
                .HasOne(po => po.ReceivedByUser)
                .WithMany()
                .HasForeignKey(po => po.ReceivedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<PurchaseOrderItem>()
                .HasOne(i => i.PurchaseOrder)
                .WithMany(po => po.Items)
                .HasForeignKey(i => i.PurchaseOrderId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<PurchaseOrderItem>()
                .HasOne(i => i.Product)
                .WithMany()
                .HasForeignKey(i => i.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            // ======================
            // Finance
            // ======================
            builder.Entity<FinanceTransaction>()
                .HasOne(x => x.CreatedByUser)
                .WithMany()
                .HasForeignKey(x => x.CreatedByUserId)
                .OnDelete(DeleteBehavior.SetNull);
        }
    }
}