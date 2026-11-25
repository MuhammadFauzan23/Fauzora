using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Fauzora.Models;

namespace Fauzora.ViewModels;

public class ProductFormVm
{
    public const int MaxImages = 15;

    public int Id { get; set; }

    [Required, StringLength(160), Display(Name = "Nama produk")]
    public string Name { get; set; } = "";

    [Required, StringLength(3000), Display(Name = "Deskripsi")]
    public string Description { get; set; } = "";

    [Range(0.01, 999999999, ErrorMessage = "Harga harus lebih dari 0."), Display(Name = "Harga")]
    public decimal Price { get; set; }

    [Range(0, 1000000), Display(Name = "Stok")]
    public int Stock { get; set; }

    [Display(Name = "Upload gambar produk")]
    public List<IFormFile> NewImages { get; set; } = [];

    [Display(Name = "Gambar yang dipertahankan")]
    public List<string> RetainedImageUrls { get; set; } = [];

    [BindNever]
    public List<string> CurrentImageUrls { get; set; } = [];

    [Range(1, int.MaxValue, ErrorMessage = "Pilih kategori."), Display(Name = "Kategori")]
    public int CategoryId { get; set; }

    [Display(Name = "Toko pemilik produk")]
    public string? SellerId { get; set; }

    [Display(Name = "Aktif")]
    public bool IsActive { get; set; } = true;

    public static ProductFormVm FromProduct(Product p) => new()
    {
        Id = p.Id,
        Name = p.Name,
        Description = p.Description,
        Price = p.Price,
        Stock = p.Stock,
        CurrentImageUrls = GetImageUrls(p),
        RetainedImageUrls = GetImageUrls(p),
        CategoryId = p.CategoryId,
        SellerId = p.SellerId,
        IsActive = p.IsActive
    };

    public static List<string> GetImageUrls(Product product) =>
        (string.IsNullOrWhiteSpace(product.ImageUrl) ? [] : new[] { product.ImageUrl })
            .Concat(product.Images.OrderBy(image => image.SortOrder).Select(image => image.Url))
            .Where(url => !string.IsNullOrWhiteSpace(url))
            .Distinct(StringComparer.Ordinal)
            .ToList();
}
