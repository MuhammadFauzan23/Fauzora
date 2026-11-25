using Fauzora.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Fauzora.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options)
    : IdentityDbContext<ApplicationUser>(options)
{
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<ProductImage> ProductImages => Set<ProductImage>();
    public DbSet<CartItem> CartItems => Set<CartItem>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<ProductReview> ProductReviews => Set<ProductReview>();
    public DbSet<SellerApplication> SellerApplications => Set<SellerApplication>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        // Pemetaan entity, relasi, index unik, dan aturan penghapusan database didefinisikan di sini.
        base.OnModelCreating(builder);

        builder.Entity<ApplicationUser>().ToTable("users");
        builder.Entity<IdentityRole>().ToTable("roles");
        builder.Entity<IdentityUserRole<string>>().ToTable("user_roles");
        builder.Entity<IdentityUserClaim<string>>().ToTable("user_claims");
        builder.Entity<IdentityUserLogin<string>>().ToTable("user_logins");
        builder.Entity<IdentityRoleClaim<string>>().ToTable("role_claims");
        builder.Entity<IdentityUserToken<string>>().ToTable("user_tokens");

        builder.Entity<Category>().ToTable("categories");
        builder.Entity<Product>().ToTable("products");
        builder.Entity<ProductImage>().ToTable("product_images");
        builder.Entity<CartItem>().ToTable("cart_items");
        builder.Entity<Order>().ToTable("orders");
        builder.Entity<OrderItem>().ToTable("order_items");
        builder.Entity<ProductReview>().ToTable("product_reviews", table =>
            table.HasCheckConstraint("ck_product_reviews_rating", "rating >= 1 AND rating <= 5"));
        builder.Entity<SellerApplication>().ToTable("seller_applications");

        builder.Entity<Product>()
            .HasOne(p => p.Seller)
            .WithMany()
            .HasForeignKey(p => p.SellerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Product>()
            .HasOne(p => p.Category)
            .WithMany(c => c.Products)
            .HasForeignKey(p => p.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<CartItem>()
            .HasIndex(c => new { c.UserId, c.ProductId })
            .IsUnique();

        builder.Entity<ProductImage>()
            .HasOne(i => i.Product)
            .WithMany(p => p.Images)
            .HasForeignKey(i => i.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<Order>()
            .HasMany(o => o.Items)
            .WithOne(i => i.Order)
            .HasForeignKey(i => i.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<Order>()
            .HasOne(o => o.Seller)
            .WithMany()
            .HasForeignKey(o => o.SellerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Order>()
            .HasIndex(o => new { o.SellerId, o.CreatedAt });

        builder.Entity<Order>()
            .HasIndex(o => o.CheckoutGroupId);

        builder.Entity<ProductReview>()
            .HasIndex(r => new { r.ProductId, r.UserId })
            .IsUnique();

        builder.Entity<ProductReview>()
            .HasOne(r => r.Product)
            .WithMany(p => p.Reviews)
            .HasForeignKey(r => r.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<ProductReview>()
            .HasOne(r => r.User)
            .WithMany()
            .HasForeignKey(r => r.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<SellerApplication>()
            .HasOne(application => application.User)
            .WithMany()
            .HasForeignKey(application => application.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<SellerApplication>()
            .HasIndex(application => application.UserId)
            .IsUnique();
    }
}
