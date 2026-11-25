using Fauzora.Data;
using Fauzora.Models;
using Microsoft.EntityFrameworkCore;

namespace Fauzora.Repositories;

public class CartRepository(AppDbContext db)
{
    // Repository cart hanya menangani query dan persistence; validasi bisnis berada di CartService.
    public async Task<List<CartItem>> GetForUserAsync(string userId) =>
        await db.CartItems.Include(x => x.Product).ThenInclude(p => p!.Category)
            .Include(x => x.Product).ThenInclude(p => p!.Seller)
            .Where(x => x.UserId == userId).ToListAsync();

    public Task<int> CountAsync(string userId) => db.CartItems.CountAsync(x => x.UserId == userId);

    public Task<CartItem?> FindByProductAsync(string userId, int productId) =>
        db.CartItems.FirstOrDefaultAsync(x => x.UserId == userId && x.ProductId == productId);

    public Task<CartItem?> FindAsync(string userId, int id) =>
        db.CartItems.Include(x => x.Product).FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId);

    public void Add(CartItem item) => db.CartItems.Add(item);

    public void Remove(CartItem item) => db.CartItems.Remove(item);

    public Task SaveAsync() => db.SaveChangesAsync();
}
