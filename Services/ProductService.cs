using Fauzora.Models;
using Fauzora.Repositories;
using Fauzora.ViewModels;

namespace Fauzora.Services;

public class ProductService(
    ProductRepository repository,
    ProductImageStorageService imageStorage,
    RichTextSanitizer richTextSanitizer,
    ILogger<ProductService> logger)
{
    // Service menjaga aturan kepemilikan produk: seller hanya boleh mengelola produk miliknya.
    public Task<List<Product>> GetCatalogAsync(string? search = null, int? categoryId = null) =>
        repository.GetActiveAsync(search, categoryId);
    public Task<List<Product>> GetNewestAsync(int take = 5) => repository.GetNewestAsync(take);
    public Task<List<Product>> GetBestSellingAsync(int take = 5) => repository.GetBestSellingAsync(take);
    public Task<Product?> GetByIdAsync(int id) => repository.GetByIdAsync(id);
    public Task<string?> GetStoreNameAsync(string sellerId) => repository.GetStoreNameAsync(sellerId);
    public Task<List<Product>> GetRelatedAsync(int productId, string sellerId, int categoryId) => repository.GetRelatedAsync(productId, sellerId, categoryId);
    public Task<List<Product>> GetRelatedFromOtherStoresAsync(int productId, string sellerId, int categoryId) => repository.GetRelatedFromOtherStoresAsync(productId, sellerId, categoryId);
    public Task<List<Product>> GetManagedAsync(string userId, int? categoryId = null) =>
        repository.GetManagedAsync(userId, categoryId);

    public async Task<ServiceResult> CreateAsync(ProductFormVm form, string actorId)
    {
        var sellerId = actorId;
        if (string.IsNullOrWhiteSpace(sellerId))
            return ServiceResult.Fail("Akun seller tidak valid.");

        var files = form.NewImages ?? [];
        var validationError = imageStorage.ValidateFiles(files, 0);
        if (validationError is not null) return ServiceResult.Fail(validationError);

        var (imageUrls, imageError) = await imageStorage.StoreAsync(files);
        if (imageError is not null) return ServiceResult.Fail(imageError);

        var product = new Product
        {
            Name = form.Name.Trim(),
            Description = richTextSanitizer.Sanitize(form.Description),
            Price = form.Price,
            Stock = form.Stock,
            CategoryId = form.CategoryId,
            IsActive = true,
            SellerId = sellerId,
            CreatedAt = DateTime.UtcNow
        };
        product.ImageUrl = imageUrls.FirstOrDefault();
        product.Images = imageUrls.Select((url, index) => new ProductImage { Url = url, SortOrder = index }).ToList();
        try
        {
            await repository.AddAsync(product);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Could not save product with uploaded images for seller {SellerId}", sellerId);
            await imageStorage.DeleteManagedAsync(imageUrls);
            return ServiceResult.Fail("Produk belum berhasil disimpan. File upload sementara sudah dibersihkan; silakan coba lagi.");
        }
        return ServiceResult.Ok("Produk berhasil ditambahkan.");
    }

    public async Task<ServiceResult> UpdateAsync(ProductFormVm form, string userId)
    {
        var existing = await repository.GetByIdAsync(form.Id);
        if (existing is null || existing.SellerId != userId)
        {
            return ServiceResult.Fail("Produk tidak ditemukan atau bukan milik toko kamu.");
        }

        var retainedUrls = form.RetainedImageUrls ?? [];
        var currentUrls = ProductFormVm.GetImageUrls(existing);
        if (retainedUrls.Count != retainedUrls.Distinct(StringComparer.Ordinal).Count() ||
            retainedUrls.Any(url => !currentUrls.Contains(url, StringComparer.Ordinal)))
            return ServiceResult.Fail("Daftar gambar yang dipertahankan tidak valid. Muat ulang halaman lalu coba kembali.");

        var files = form.NewImages ?? [];
        var validationError = imageStorage.ValidateFiles(files, retainedUrls.Count);
        if (validationError is not null) return ServiceResult.Fail(validationError);
        var (newImageUrls, imageError) = await imageStorage.StoreAsync(files);
        if (imageError is not null) return ServiceResult.Fail(imageError);

        var finalImageUrls = retainedUrls.Concat(newImageUrls).ToList();
        var removedUrls = currentUrls.Except(retainedUrls, StringComparer.Ordinal).ToList();
        var changedFields = new List<string>();
        var name = form.Name.Trim();
        var description = richTextSanitizer.Sanitize(form.Description);
        if (existing.Name != name) changedFields.Add(nameof(Product.Name));
        if (existing.Description != description) changedFields.Add(nameof(Product.Description));
        if (existing.Price != form.Price) changedFields.Add(nameof(Product.Price));
        if (existing.Stock != form.Stock) changedFields.Add(nameof(Product.Stock));
        if (existing.CategoryId != form.CategoryId) changedFields.Add(nameof(Product.CategoryId));
        if (existing.IsActive != form.IsActive) changedFields.Add(nameof(Product.IsActive));
        if (!currentUrls.SequenceEqual(finalImageUrls, StringComparer.Ordinal))
            changedFields.Add("Images");

        existing.Name = name;
        existing.Description = description;
        existing.Price = form.Price;
        existing.Stock = form.Stock;
        existing.CategoryId = form.CategoryId;
        var existingImagesByUrl = existing.Images
            .GroupBy(image => image.Url, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);
        foreach (var oldImage in existing.Images.ToList())
        {
            if (!finalImageUrls.Contains(oldImage.Url, StringComparer.Ordinal) || existingImagesByUrl[oldImage.Url] != oldImage)
            {
                repository.RemoveImage(oldImage);
                existing.Images.Remove(oldImage);
            }
        }
        for (var i = 0; i < finalImageUrls.Count; i++)
        {
            var url = finalImageUrls[i];
            var image = existing.Images.FirstOrDefault(item => item.Url == url);
            if (image is null)
            {
                image = new ProductImage { ProductId = existing.Id, Url = url };
                existing.Images.Add(image);
            }
            image.SortOrder = i;
        }
        existing.ImageUrl = finalImageUrls.FirstOrDefault();
        existing.IsActive = form.IsActive;

        int affectedEntries;
        try
        {
            affectedEntries = await repository.UpdateAsync(existing);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Could not persist product edit for product {ProductId}", form.Id);
            await imageStorage.DeleteManagedAsync(newImageUrls);
            return ServiceResult.Fail("Perubahan belum tersimpan. Gambar lama tetap dipertahankan; silakan coba lagi.");
        }
        logger.LogInformation(
            "Product edit processed. ProductId={ProductId}; ChangedFields={ChangedFields}; AffectedEntries={AffectedEntries}",
            form.Id,
            changedFields.Count == 0 ? "(none)" : string.Join(",", changedFields),
            affectedEntries);

        if (changedFields.Count > 0 && affectedEntries == 0)
        {
            logger.LogWarning("Product edit reported changed fields but EF Core saved no entries. ProductId={ProductId}", form.Id);
            await imageStorage.DeleteManagedAsync(newImageUrls);
            return ServiceResult.Fail("Perubahan produk tidak tersimpan. Silakan coba lagi atau hubungi admin.");
        }

        if (changedFields.Count == 0)
        {
            await imageStorage.DeleteManagedAsync(newImageUrls);
            return ServiceResult.Ok("Tidak ada perubahan pada produk. Pastikan perubahan yang diinginkan sudah masuk sebelum menekan Simpan.");
        }

        await imageStorage.DeleteManagedAsync(removedUrls);

        return ServiceResult.Ok("Produk berhasil diperbarui.");
    }

    public async Task<bool> DeleteAsync(int id, string userId)
    {
        var product = await repository.GetByIdAsync(id);
        if (product is null || product.SellerId != userId)
        {
            return false;
        }
        var imageUrls = ProductFormVm.GetImageUrls(product);
        await repository.DeleteAsync(product);
        await imageStorage.DeleteManagedAsync(imageUrls);
        return true;
    }
}
