using Fauzora.Models;
using Fauzora.Repositories;

namespace Fauzora.Services;

public class CategoryService(CategoryRepository repository)
{
    // Kategori divalidasi unik dan tidak boleh dihapus selama masih dipakai produk.
    public Task<List<Category>> GetAllAsync() => repository.GetAllAsync();
    public Task<bool> ExistsAsync(int id) => repository.ExistsAsync(id);

    public async Task<ServiceResult> CreateAsync(string? name, string? description)
    {
        name = name?.Trim();
        description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();

        if (string.IsNullOrEmpty(name)) return ServiceResult.Fail("Nama kategori wajib diisi.");
        if (name.Length > 80 || description?.Length > 300)
            return ServiceResult.Fail("Nama maksimal 80 karakter dan deskripsi maksimal 300 karakter.");
        if (await repository.NameExistsAsync(name)) return ServiceResult.Fail("Nama kategori sudah ada.");

        await repository.AddAsync(new Category { Name = name, Description = description });
        return ServiceResult.Ok("Kategori ditambahkan.");
    }

    public async Task<ServiceResult> UpdateAsync(int id, string? name, string? description)
    {
        var category = await repository.GetByIdAsync(id);
        if (category is null) return ServiceResult.Fail("Kategori tidak ditemukan.");

        name = name?.Trim();
        description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();

        if (string.IsNullOrEmpty(name)) return ServiceResult.Fail("Nama kategori wajib diisi.");
        if (name.Length > 80 || description?.Length > 300)
            return ServiceResult.Fail("Nama maksimal 80 karakter dan deskripsi maksimal 300 karakter.");
        if (await repository.NameExistsAsync(name, id)) return ServiceResult.Fail("Nama kategori sudah ada.");

        category.Name = name;
        category.Description = description;
        await repository.UpdateAsync(category);
        return ServiceResult.Ok("Kategori diperbarui.");
    }

    public async Task<ServiceResult> DeleteAsync(int id)
    {
        var category = await repository.GetWithProductsAsync(id);
        if (category is null) return ServiceResult.Fail("Kategori tidak ditemukan.");
        if (category.Products.Count > 0) return ServiceResult.Fail("Kategori masih dipakai oleh produk.");

        await repository.DeleteAsync(category);
        return ServiceResult.Ok("Kategori dihapus.");
    }
}
