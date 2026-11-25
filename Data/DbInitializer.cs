using Fauzora.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Fauzora.Data;

public static class DbInitializer
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        // Startup: pastikan schema terbaru, role Identity, akun default, dan kategori awal tersedia.
        var db = services.GetRequiredService<AppDbContext>();
        var configuration = services.GetRequiredService<IConfiguration>();
        await db.Database.MigrateAsync();

        var roles = new[] { "Customer", "Seller", "Admin" };
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        foreach (var role in roles)
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new IdentityRole(role));

        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        // Akun Admin lokal hanya diberi role saat pertama kali dibuat; perubahan role tidak dibatalkan saat startup berikutnya.
        var adminEmail = "admin@fauzora.local";
        var adminPassword = configuration["SeedUsers:AdminPassword"];
        var admin = await userManager.FindByEmailAsync(adminEmail);
        var createdAdmin = false;
        if (admin is null)
        {
            if (string.IsNullOrWhiteSpace(adminPassword))
                throw new InvalidOperationException("SeedUsers:AdminPassword belum dikonfigurasi.");

            var candidate = new ApplicationUser { UserName = adminEmail, Email = adminEmail, FullName = "Fauzora Admin", EmailConfirmed = true, IsActive = true };
            var result = await userManager.CreateAsync(candidate, adminPassword);
            admin = result.Succeeded ? candidate : null;
            createdAdmin = result.Succeeded;
        }
        if (createdAdmin && admin is not null)
        {
            var roleResult = await userManager.AddToRoleAsync(admin, "Admin");
            if (!roleResult.Succeeded)
                throw new InvalidOperationException(string.Join("; ", roleResult.Errors.Select(error => error.Description)));
        }

        var sellerEmail = "seller@fauzora.local";
        var sellerPassword = configuration["SeedUsers:SellerPassword"];
        // Akun Seller demo dibuat idempotent agar startup berikutnya tidak membuat duplikat.
        var seller = await userManager.FindByEmailAsync(sellerEmail);
        var createdSeller = false;
        if (seller is null)
        {
            if (string.IsNullOrWhiteSpace(sellerPassword))
                throw new InvalidOperationException("SeedUsers:SellerPassword belum dikonfigurasi.");

            seller = new ApplicationUser
            {
                UserName = sellerEmail,
                Email = sellerEmail,
                FullName = "Seller Fauzora",
                City = "Jakarta",
                EmailConfirmed = true,
                IsActive = true
            };

            var result = await userManager.CreateAsync(seller, sellerPassword);
            if (!result.Succeeded)
                throw new InvalidOperationException(string.Join("; ", result.Errors.Select(error => error.Description)));
            createdSeller = true;
        }

        if (createdSeller)
        {
            var roleResult = await userManager.AddToRoleAsync(seller, "Seller");
            if (!roleResult.Succeeded)
                throw new InvalidOperationException(string.Join("; ", roleResult.Errors.Select(error => error.Description)));
        }

        if (!await db.Categories.AnyAsync())
        {
            db.Categories.AddRange(
                new Category { Name = "Electronics", Description = "Gadget and electronic products" },
                new Category { Name = "Fashion", Description = "Fashion and accessories" },
                new Category { Name = "Home & Living", Description = "Home essentials" });
            await db.SaveChangesAsync();
        }
    }
}
