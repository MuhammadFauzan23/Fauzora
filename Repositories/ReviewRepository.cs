using Fauzora.Data;
using Fauzora.Models;
using Microsoft.EntityFrameworkCore;

namespace Fauzora.Repositories;

public class ReviewRepository(AppDbContext db)
{
    // Repository review membaca review dan memeriksa apakah customer benar-benar menerima produk.
    public Task<List<ProductReview>> GetForProductAsync(int productId) =>
        db.ProductReviews
            .Include(review => review.User)
            .Where(review => review.ProductId == productId)
            .OrderByDescending(review => review.CreatedAt)
            .ToListAsync();

    public Task<ProductReview?> FindAsync(int productId, string userId) =>
        db.ProductReviews.FirstOrDefaultAsync(review =>
            review.ProductId == productId && review.UserId == userId);

    public Task<bool> HasReceivedProductAsync(int productId, string userId) =>
        db.Orders.AnyAsync(order =>
            order.CustomerId == userId &&
            order.Status == OrderStatus.Completed &&
            order.Items.Any(item => item.ProductId == productId));

    public async Task RefreshRatingAsync(int productId)
    {
        var stats = await db.ProductReviews
            .Where(review => review.ProductId == productId)
            .GroupBy(review => review.ProductId)
            .Select(group => new { Average = group.Average(review => review.Rating), Count = group.Count() })
            .FirstOrDefaultAsync();

        await db.Products
            .Where(product => product.Id == productId)
            .ExecuteUpdateAsync(update => update
                .SetProperty(product => product.RatingAverage, stats == null ? 0 : (decimal)stats.Average)
                .SetProperty(product => product.RatingCount, stats == null ? 0 : stats.Count));
    }

    public Task SaveAsync() => db.SaveChangesAsync();
    public void Add(ProductReview review) => db.ProductReviews.Add(review);
}