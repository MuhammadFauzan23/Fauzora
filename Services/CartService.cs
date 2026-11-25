using Fauzora.Models;
using Fauzora.Repositories;

namespace Fauzora.Services;

public class CartService(CartRepository carts, ProductRepository products)
{
    // Service cart memvalidasi produk aktif dan stok sebelum repository menyimpan perubahan.
    public Task<List<CartItem>> GetCartAsync(string userId) => carts.GetForUserAsync(userId);

    public async Task<ServiceResult> AddAsync(string userId, int productId, int quantity)
    {
        quantity = Math.Max(quantity, 1);
        var product = await products.GetActiveByIdAsync(productId);
        if (product is null || product.Stock < quantity)
            return ServiceResult.Fail("Produk tidak tersedia atau stok tidak cukup.");

        var item = await carts.FindByProductAsync(userId, productId);
        if (item is null)
        {
            carts.Add(new CartItem { UserId = userId, ProductId = productId, Quantity = quantity });
        }
        else if (item.Quantity + quantity <= product.Stock)
        {
            item.Quantity += quantity;
        }
        else
        {
            return ServiceResult.Fail("Jumlah melebihi stok.");
        }

        await carts.SaveAsync();
        return ServiceResult.Ok("Produk ditambahkan ke keranjang.");
    }

    public async Task<ServiceResult> UpdateAsync(string userId, int id, int quantity)
    {
        var item = await carts.FindAsync(userId, id);
        if (item is null) return ServiceResult.Fail("Item keranjang tidak ditemukan.");

        if (quantity <= 0)
            carts.Remove(item);
        else if (item.Product is not null && quantity <= item.Product.Stock)
            item.Quantity = quantity;
        else
            return ServiceResult.Fail("Jumlah melebihi stok.");

        await carts.SaveAsync();
        return ServiceResult.Ok();
    }

    public async Task RemoveAsync(string userId, int id)
    {
        var item = await carts.FindAsync(userId, id);
        if (item is null) return;
        carts.Remove(item);
        await carts.SaveAsync();
    }
}
