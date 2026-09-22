using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using portofolio3.Models;
namespace portofolio3.Data
{
    public class AppDbContext : IdentityDbContext<ApplicationUser>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        public new DbSet<User> Users => Set<User>();
        public DbSet<Admin> Admins => Set<Admin>();
        public DbSet<Category> Categories => Set<Category>();
        public DbSet<Seller> Sellers => Set<Seller>();
        public DbSet<Product> Products => Set<Product>();
        public DbSet<ProductImage> ProductImages => Set<ProductImage>();
        public DbSet<Cart> Carts => Set<Cart>();
        public DbSet<CartItem> CartItems => Set<CartItem>();
        public DbSet<Order> Orders => Set<Order>();
        public DbSet<OrderItem> OrderItems => Set<OrderItem>();
        public DbSet<Review> Reviews => Set<Review>();
        public DbSet<Address> Addresses => Set<Address>();
        public DbSet<SellerProduct> SellerProducts => Set<SellerProduct>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique();

            modelBuilder.Entity<Admin>()
                .HasIndex(a => a.Email)
                .IsUnique();

            modelBuilder.Entity<Seller>()
                .HasIndex(s => s.Email)
                .IsUnique();

            modelBuilder.Entity<Category>()
                .HasIndex(c => c.Name)
                .IsUnique();

            modelBuilder.Entity<User>()
                .Property(u => u.Name)
                .HasMaxLength(100);
            modelBuilder.Entity<User>()
                .Property(u => u.Email)
                .HasMaxLength(200);
            modelBuilder.Entity<User>()
                .Property(u => u.PasswordHash)
                .HasMaxLength(200);
            modelBuilder.Entity<User>()
                .Property(u => u.Phone)
                .HasMaxLength(50);

            modelBuilder.Entity<Admin>()
                .Property(a => a.Name)
                .HasMaxLength(100);
            modelBuilder.Entity<Admin>()
                .Property(a => a.Email)
                .HasMaxLength(200);
            modelBuilder.Entity<Admin>()
                .Property(a => a.PasswordHash)
                .HasMaxLength(200);
            modelBuilder.Entity<Admin>()
                .Property(a => a.Role)
                .HasMaxLength(50);

            modelBuilder.Entity<Seller>()
                .Property(s => s.ShopName)
                .HasMaxLength(100);
            modelBuilder.Entity<Seller>()
                .Property(s => s.Email)
                .HasMaxLength(200);
            modelBuilder.Entity<Seller>()
                .Property(s => s.PasswordHash)
                .HasMaxLength(200);
            modelBuilder.Entity<Seller>()
                .Property(s => s.Phone)
                .HasMaxLength(50);

            modelBuilder.Entity<Category>()
                .Property(c => c.Name)
                .HasMaxLength(100);
            modelBuilder.Entity<Category>()
                .Property(c => c.Description)
                .HasMaxLength(500);

            modelBuilder.Entity<Product>()
                .Property(p => p.Name)
                .HasMaxLength(200);
            modelBuilder.Entity<Product>()
                .Property(p => p.Description)
                .HasMaxLength(1000);
            modelBuilder.Entity<Product>()
                .Property(p => p.Price)
                .HasPrecision(18, 2);

            modelBuilder.Entity<ProductImage>()
                .Property(i => i.ImageUrl)
                .HasMaxLength(500);

            modelBuilder.Entity<Review>()
                .Property(r => r.Comment)
                .HasMaxLength(1000);

            modelBuilder.Entity<Order>()
                .Property(o => o.Status)
                .HasMaxLength(50);
            modelBuilder.Entity<Order>()
                .Property(o => o.ShippingAddress)
                .HasMaxLength(500);
            modelBuilder.Entity<Order>()
                .Property(o => o.TotalAmount)
                .HasPrecision(18, 2);

            modelBuilder.Entity<OrderItem>()
                .Property(oi => oi.UnitPrice)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Address>()
                .Property(a => a.Street)
                .HasMaxLength(200);
            modelBuilder.Entity<Address>()
                .Property(a => a.City)
                .HasMaxLength(100);
            modelBuilder.Entity<Address>()
                .Property(a => a.Region)
                .HasMaxLength(100);
            modelBuilder.Entity<Address>()
                .Property(a => a.PostalCode)
                .HasMaxLength(20);
            modelBuilder.Entity<Address>()
                .Property(a => a.Country)
                .HasMaxLength(100);

            modelBuilder.Entity<User>()
                .HasMany(u => u.Addresses)
                .WithOne(a => a.User)
                .HasForeignKey(a => a.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<User>()
                .HasMany(u => u.Carts)
                .WithOne(c => c.User)
                .HasForeignKey(c => c.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<User>()
                .HasMany(u => u.Orders)
                .WithOne(o => o.User)
                .HasForeignKey(o => o.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<User>()
                .HasMany(u => u.Reviews)
                .WithOne(r => r.User)
                .HasForeignKey(r => r.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Category>()
                .HasMany(c => c.Products)
                .WithOne(p => p.Category)
                .HasForeignKey(p => p.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Seller>()
                .HasMany(s => s.Products)
                .WithOne(p => p.Seller)
                .HasForeignKey(p => p.SellerId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Product>()
                .HasMany(p => p.Images)
                .WithOne(i => i.Product)
                .HasForeignKey(i => i.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Product>()
                .HasMany(p => p.Reviews)
                .WithOne(r => r.Product)
                .HasForeignKey(r => r.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Cart>()
                .HasMany(c => c.Items)
                .WithOne(ci => ci.Cart)
                .HasForeignKey(ci => ci.CartId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Product>()
                .HasMany(p => p.CartItems)
                .WithOne(ci => ci.Product)
                .HasForeignKey(ci => ci.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Order>()
                .HasMany(o => o.Items)
                .WithOne(oi => oi.Order)
                .HasForeignKey(oi => oi.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Product>()
                .HasMany(p => p.OrderItems)
                .WithOne(oi => oi.Product)
                .HasForeignKey(oi => oi.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<CartItem>()
                .HasIndex(ci => new { ci.CartId, ci.ProductId })
                .IsUnique();

            modelBuilder.Entity<Review>()
                .HasIndex(r => new { r.ProductId, r.UserId })
                .IsUnique();

            modelBuilder.Entity<SellerProduct>()
                .HasKey(sp => new { sp.SellerId, sp.ProductId });
        }
    }
}