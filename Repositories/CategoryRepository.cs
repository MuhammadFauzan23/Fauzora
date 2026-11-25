using Fauzora.Data;
using Fauzora.Models;
using Microsoft.EntityFrameworkCore;

namespace Fauzora.Repositories;

public class CategoryRepository(AppDbContext db)
{
    // Repository kategori menangani query kategori dan pemeriksaan relasi dengan produk.
    public Task<List<Category>> GetAllAsync() =>
        db.Categories.OrderBy(c => c.Name).ToListAsync();

    public Task<bool> ExistsAsync(int id) => db.Categories.AnyAsync(c => c.Id == id);

    public Task<bool> NameExistsAsync(string name, int? exceptId = null) =>
        db.Categories.AnyAsync(category => category.Name == name && (!exceptId.HasValue || category.Id != exceptId.Value));

    public Task<Category?> GetByIdAsync(int id) =>
        db.Categories.FirstOrDefaultAsync(category => category.Id == id);

    public Task<Category?> GetWithProductsAsync(int id) =>
        db.Categories.Include(c => c.Products).FirstOrDefaultAsync(c => c.Id == id);

    public async Task AddAsync(Category category)
    {
        db.Categories.Add(category);
        await db.SaveChangesAsync();
    }

    public async Task UpdateAsync(Category category)
    {
        db.Categories.Update(category);
        await db.SaveChangesAsync();
    }

    public async Task DeleteAsync(Category category)
    {
        db.Categories.Remove(category);
        await db.SaveChangesAsync();
    }
}
