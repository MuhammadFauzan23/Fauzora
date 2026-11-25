using Fauzora.Data;
using Fauzora.Models;
using Microsoft.EntityFrameworkCore;

namespace Fauzora.Repositories;

public class ProductRepository(AppDbContext db)
{
    // Repository produk menyediakan query katalog, ownership seller, dan operasi CRUD EF Core.
    public IQueryable<Product> Query() => db.Products
        .Include(p => p.Category)
        .Include(p => p.Seller);

    public async Task<Product?> GetByIdAsync(int id)
    {
        var product = await Query()
            .Include(p => p.Images.OrderBy(i => i.SortOrder))
            .FirstOrDefaultAsync(p => p.Id == id);
        if (product is null) return null;

        var ratingStats = await db.ProductReviews
            .Where(review => review.ProductId == id)
            .GroupBy(review => review.ProductId)
            .Select(group => new
            {
                Average = group.Average(review => review.Rating),
                Count = group.Count()
            })
            .FirstOrDefaultAsync();

        product.RatingAverage = ratingStats is null ? 0 : (decimal)ratingStats.Average;
        product.RatingCount = ratingStats?.Count ?? 0;
        return product;
    }

    public async Task<List<Product>> GetActiveAsync(string? search = null, int? categoryId = null)
    {
        var query = Query().Where(p => p.IsActive && p.Stock > 0);
        if (categoryId is not null)
            query = query.Where(p => p.CategoryId == categoryId);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var escaped = search.Trim().Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_");
            query = query.Where(p => EF.Functions.ILike(p.Name, $"%{escaped}%"));
        }
        return await query.OrderByDescending(p => p.CreatedAt).ToListAsync();
    }

    public Task<List<Product>> GetNewestAsync(int take = 5) =>
        Query()
            .Where(product => product.IsActive && product.Stock > 0)
            .OrderByDescending(product => product.CreatedAt)
            .Take(take)
            .ToListAsync();

    public async Task<List<Product>> GetBestSellingAsync(int take = 5)
    {
        var sales = db.OrderItems
            .Where(item => item.Order != null &&
                item.Order.Status != OrderStatus.Cancelled &&
                item.Order.ReturnStatus == ReturnRequestStatus.None)
            .GroupBy(item => item.ProductId)
            .Select(group => new { ProductId = group.Key, Quantity = group.Sum(item => item.Quantity) });

        return await Query()
            .Where(product => product.IsActive && product.Stock > 0)
            .Join(sales, product => product.Id, sale => sale.ProductId, (product, sale) => new { product, sale.Quantity })
            .OrderByDescending(item => item.Quantity)
            .ThenByDescending(item => item.product.CreatedAt)
            .Take(take)
            .Select(item => item.product)
            .ToListAsync();
    }

    public Task<Product?> GetActiveByIdAsync(int id) =>
        db.Products.FirstOrDefaultAsync(p => p.Id == id && p.IsActive);

    public Task<string?> GetStoreNameAsync(string sellerId) =>
        db.SellerApplications
            .Where(application => application.UserId == sellerId && application.Status == SellerApplicationStatus.Approved)
            .Select(application => application.StoreName)
            .FirstOrDefaultAsync();

    public async Task<List<Product>> GetRelatedAsync(int productId, string sellerId, int categoryId, int take = 5) =>
        await Query()
            .Where(p => p.Id != productId && p.SellerId == sellerId && p.CategoryId == categoryId && p.IsActive && p.Stock > 0)
            .OrderByDescending(p => p.RatingAverage)
            .ThenByDescending(p => p.CreatedAt)
            .Take(take)
            .ToListAsync();

    public async Task<List<Product>> GetRelatedFromOtherStoresAsync(int productId, string sellerId, int categoryId, int take = 5) =>
        await Query()
            .Where(p => p.Id != productId && p.SellerId != sellerId && p.CategoryId == categoryId && p.IsActive && p.Stock > 0)
            .OrderByDescending(p => p.RatingAverage)
            .ThenByDescending(p => p.CreatedAt)
            .Take(take)
            .ToListAsync();

    public async Task<List<Product>> GetManagedAsync(string userId, int? categoryId = null)
    {
        var query = db.Products.Include(product => product.Category)
            .Where(product => product.SellerId == userId);
        if (categoryId is not null)
            query = query.Where(product => product.CategoryId == categoryId);

        return await query.OrderByDescending(product => product.CreatedAt).ToListAsync();
    }

    public async Task AddAsync(Product product)
    {
        db.Products.Add(product);
        await db.SaveChangesAsync();
    }

    // Entity sudah dilacak, jadi Update() tidak diperlukan dan bisa mengubah status gambar yang dihapus.
    public Task<int> UpdateAsync(Product product) => db.SaveChangesAsync();

    public void RemoveImage(ProductImage image) => db.ProductImages.Remove(image);

    public async Task DeleteAsync(Product product)
    {
        db.Products.Remove(product);
        await db.SaveChangesAsync();
    }
}
