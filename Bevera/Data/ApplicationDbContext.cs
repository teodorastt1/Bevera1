using Bevera.Models;
using Bevera.Models.Catalog;
using Bevera.Models.Inventory;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System.Reflection.Emit;

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


        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // ✅ fix за decimal warning-ите
            builder.Entity<Product>().Property(p => p.Price).HasPrecision(18, 2);
            builder.Entity<Product>().Property(p => p.VolumeLiters).HasPrecision(18, 3);
            builder.Entity<Product>().Property(p => p.AlcoholPercent).HasPrecision(5, 2);

            builder.Entity<CartItem>().Property(c => c.UnitPrice).HasPrecision(18, 2);
            builder.Entity<Order>().Property(o => o.Total).HasPrecision(18, 2);
            builder.Entity<OrderItem>().Property(oi => oi.UnitPrice).HasPrecision(18, 2);
            builder.Entity<OrderItem>().Property(oi => oi.LineTotal).HasPrecision(18, 2);


            // Category (Parent) -> Subcategories (self reference)
            builder.Entity<Category>()
                .HasOne(c => c.ParentCategory)
                .WithMany(c => c.SubCategories)
                .HasForeignKey(c => c.ParentCategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            // Category -> Products
            builder.Entity<Product>()
                .HasOne(p => p.Category)
                .WithMany(c => c.Products)
                .HasForeignKey(p => p.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Product>()
      .Property(p => p.DiscountPercent)
      .HasPrecision(5, 2);

            builder.Entity<Category>()
    .HasOne(c => c.ParentCategory)
    .WithMany(c => c.SubCategories)
    .HasForeignKey(c => c.ParentCategoryId)
    .OnDelete(DeleteBehavior.Restrict); // важно: да не трие каскадно без да искаш


            builder.Entity<Product>()
      .HasOne(p => p.Category)
      .WithMany(c => c.Products)
      .HasForeignKey(p => p.CategoryId)
      .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<Category>()
                .HasIndex(c => c.Name)
                .IsUnique();

            builder.Entity<Product>()
                .Property(x => x.AlcoholPercent)
                .HasPrecision(5, 2);


            // ✅ relationships
            builder.Entity<Product>()
                .HasOne(p => p.Category)
                .WithMany(c => c.Products)
                .HasForeignKey(p => p.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<ProductImage>()
                .HasOne(i => i.Product)
                .WithMany(p => p.Images)
                .HasForeignKey(i => i.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<InventoryMovement>()
                .HasOne(m => m.Product)
                .WithMany()
                .HasForeignKey(m => m.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Product>()
               .Property(p => p.Price)
               .HasPrecision(18, 2);

            builder.Entity<Product>()
                .Property(p => p.AlcoholPercent)
                .HasPrecision(5, 2);

            builder.Entity<Product>()
                .Property(p => p.VolumeLiters)
                .HasPrecision(10, 2);

            builder.Entity<CartItem>()
                .Property(c => c.UnitPrice)
                .HasPrecision(18, 2);

            builder.Entity<Order>()
                .Property(o => o.Total)
                .HasPrecision(18, 2);

            builder.Entity<OrderItem>()
                .Property(oi => oi.UnitPrice)
                .HasPrecision(18, 2);

            builder.Entity<OrderItem>()
                .Property(oi => oi.LineTotal)
                .HasPrecision(18, 2);

            // ✅ RELATIONSHIPS: да няма OrderId1 shadow FK
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

            builder.Entity<OrderItem>()
                .HasOne(i => i.Order)
                .WithMany(o => o.Items)
                .HasForeignKey(i => i.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            // ✅ Favorites: 1 user + 1 product (unique)
            builder.Entity<Favorite>()
                .HasIndex(x => new { x.UserId, x.ProductId })
                .IsUnique();

            // ✅ Reviews: позволяваме 1 review на user за product (по желание)
            builder.Entity<Review>()
                .HasIndex(x => new { x.UserId, x.ProductId })
                .IsUnique();

            // ✅ Money
            builder.Entity<Product>()
                .Property(p => p.Price)
                .HasPrecision(18, 2);

            builder.Entity<CartItem>()
                .Property(ci => ci.UnitPrice)
                .HasPrecision(18, 2);

            builder.Entity<OrderItem>()
                .Property(oi => oi.UnitPrice)
                .HasPrecision(18, 2);

            builder.Entity<OrderItem>()
                .Property(oi => oi.LineTotal)
                .HasPrecision(18, 2);

            builder.Entity<Order>()
                .Property(o => o.Total)
                .HasPrecision(18, 2);

            // ✅ Quantity / volume / percent (ако са decimal при теб)
            builder.Entity<Product>()
                .Property(p => p.VolumeLiters)
                .HasPrecision(18, 3);

            builder.Entity<Product>()
                .Property(p => p.AlcoholPercent)
                .HasPrecision(5, 2);
            // Category -> Products (1:N)
            builder.Entity<Product>()
                .HasOne(p => p.Category)
                .WithMany(c => c.Products)
                .HasForeignKey(p => p.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            // Brand -> Products (1:N) optional
            builder.Entity<Product>()
                .HasOne(p => p.Brand)
                .WithMany(b => b.Products)
                .HasForeignKey(p => p.BrandId)
                .OnDelete(DeleteBehavior.SetNull);

            // Product -> Images (1:N)
            builder.Entity<ProductImage>()
                .HasOne(i => i.Product)
                .WithMany(p => p.Images)
                .HasForeignKey(i => i.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            // CartItem -> User (N:1)
            builder.Entity<CartItem>()
                .HasOne(ci => ci.User)
                .WithMany()
                .HasForeignKey(ci => ci.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // CartItem -> Product (N:1)
            builder.Entity<CartItem>()
                .HasOne(ci => ci.Product)
                .WithMany()
                .HasForeignKey(ci => ci.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            // Order -> Client (N:1)
            builder.Entity<Order>()
                .HasOne(o => o.Client)
                .WithMany()
                .HasForeignKey(o => o.ClientId)
                .OnDelete(DeleteBehavior.Restrict);

            // Order -> Items (1:N)
            builder.Entity<OrderItem>()
                .HasOne(oi => oi.Order)
                .WithMany(o => o.Items)
                .HasForeignKey(oi => oi.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            // OrderItem -> Product (N:1)
            builder.Entity<OrderItem>()
                .HasOne(oi => oi.Product)
                .WithMany()
                .HasForeignKey(oi => oi.ProductId)
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

            // History -> ChangedByUser (N:1)
            builder.Entity<OrderStatusHistory>()
                .HasOne(h => h.ChangedByUser)
                .WithMany()
                .HasForeignKey(h => h.ChangedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            // InventoryMovement -> Product (N:1)
            builder.Entity<InventoryMovement>()
                .HasOne(m => m.Product)
                .WithMany()
                .HasForeignKey(m => m.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            // InventoryMovement -> CreatedByUser (N:1)
            builder.Entity<InventoryMovement>()
                .HasOne(m => m.CreatedByUser)
                .WithMany()
                .HasForeignKey(m => m.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            // InventoryMovement -> Order (optional) (N:1)
            builder.Entity<InventoryMovement>()
                .HasOne(m => m.Order)
                .WithMany()
                .HasForeignKey(m => m.OrderId)
                .OnDelete(DeleteBehavior.SetNull);

            

            // ==============================
            // Decimal precision (важно за SQL Server)
            // ==============================

            // Money
            builder.Entity<Product>()
                .Property(p => p.Price)
                .HasPrecision(18, 2);

            builder.Entity<CartItem>()
                .Property(ci => ci.UnitPrice)
                .HasPrecision(18, 2);

            builder.Entity<Order>()
                .Property(o => o.Total)
                .HasPrecision(18, 2);

            builder.Entity<OrderItem>()
                .Property(oi => oi.UnitPrice)
                .HasPrecision(18, 2);

            builder.Entity<OrderItem>()
                .Property(oi => oi.LineTotal)
                .HasPrecision(18, 2);

            // Alcohol % (пример: 4.50, 12.00)
            builder.Entity<Product>()
                .Property(p => p.AlcoholPercent)
                .HasPrecision(5, 2);

            // Volume liters (пример: 0.250, 1.500)
            builder.Entity<Product>()
                .Property(p => p.VolumeLiters)
                .HasPrecision(6, 3);

        }
    }
}
