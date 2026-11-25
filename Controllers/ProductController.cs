using Fauzora.Extensions;
using Fauzora.Services;
using Fauzora.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Fauzora.Controllers;

public class ProductController(ProductService service, CategoryService categories, SecureIdService secureIds) : Controller
{
    // Katalog publik hanya membaca produk aktif; aksi pengelolaan dilindungi role Seller.
    public async Task<IActionResult> Index(string? q, int? categoryId)
    {
        var model = new CatalogVm(
            await service.GetCatalogAsync(q, categoryId),
            await categories.GetAllAsync(),
            q,
            categoryId)
        {
            NewestProducts = await service.GetNewestAsync(),
            BestSellingProducts = await service.GetBestSellingAsync()
        };
        return View(model);
    }

    public async Task<IActionResult> Details(string id)
    {
        if (!secureIds.TryUnprotect(id, out var productId)) return NotFound();
        var product = await service.GetByIdAsync(productId);
        if (product is null) return NotFound();

        ViewBag.RelatedProducts = await service.GetRelatedAsync(product.Id, product.SellerId, product.CategoryId);
        ViewBag.OtherStoreProducts = await service.GetRelatedFromOtherStoresAsync(product.Id, product.SellerId, product.CategoryId);
        ViewBag.StoreName = await service.GetStoreNameAsync(product.SellerId);
        return View(product);
    }

    [Authorize(Roles = "Seller")]
    public async Task<IActionResult> Create()
    {
        await LoadCategories();
        return View(new ProductFormVm());
    }

    [HttpPost, RequestSizeLimit(52L * 1024 * 1024), ValidateAntiForgeryToken, Authorize(Roles = "Seller")]
    public async Task<IActionResult> Create(ProductFormVm form)
    {
        // SellerId selalu diambil dari akun Seller yang login.
        await ValidateCategoryAsync(form);
        if (!ModelState.IsValid)
        {
            var validationErrors = ModelState.Values
                .SelectMany(value => value.Errors)
                .Select(error => error.ErrorMessage)
                .Where(message => !string.IsNullOrWhiteSpace(message))
                .Distinct()
                .Take(3)
                .ToArray();
            TempData["Error"] = validationErrors.Length > 0
                ? $"Produk belum disimpan. {string.Join(" ", validationErrors)}"
                : "Produk belum disimpan. Periksa kembali data yang wajib diisi.";
            await LoadCategories();
            return View(form);
        }
        var result = await service.CreateAsync(form, User.GetUserId());
        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Message);
            TempData["Error"] = result.Message;
            await LoadCategories();
            return View(form);
        }
        TempData["Success"] = result.Message;
        return RedirectToAction(nameof(MyProducts));
    }

    [Authorize(Roles = "Seller")]
    public async Task<IActionResult> MyProducts(int? categoryId)
    {
        var allCategories = await categories.GetAllAsync();
        if (categoryId is not null && !allCategories.Any(category => category.Id == categoryId))
            categoryId = null;

        ViewBag.Categories = new SelectList(allCategories, "Id", "Name", categoryId);
        ViewBag.SelectedCategoryId = categoryId;
        return View(await service.GetManagedAsync(User.GetUserId(), categoryId));
    }

    [Authorize(Roles = "Seller")]
    public async Task<IActionResult> Edit(string id)
    {
        if (!secureIds.TryUnprotect(id, out var productId)) return NotFound();
        var product = await service.GetByIdAsync(productId);
        if (product is null) return NotFound();
        if (product.SellerId != User.GetUserId()) return Forbid();

        // Route id berisi token opaque; jangan biarkan nilai ModelState `id` menimpa hidden ProductFormVm.Id.
        ModelState.Remove(nameof(ProductFormVm.Id));
        await LoadCategories();
        return View(ProductFormVm.FromProduct(product));
    }

    [HttpPost, RequestSizeLimit(52L * 1024 * 1024), ValidateAntiForgeryToken, Authorize(Roles = "Seller")]
    public async Task<IActionResult> Edit([FromForm] ProductFormVm form)
    {
        var currentProduct = await service.GetByIdAsync(form.Id);
        if (currentProduct is null) return NotFound();
        if (currentProduct.SellerId != User.GetUserId()) return Forbid();
        form.CurrentImageUrls = ProductFormVm.GetImageUrls(currentProduct);

        await ValidateCategoryAsync(form);
        if (!ModelState.IsValid)
        {
            var validationErrors = ModelState.Values
                .SelectMany(value => value.Errors)
                .Select(error => error.ErrorMessage)
                .Where(message => !string.IsNullOrWhiteSpace(message))
                .Distinct()
                .Take(3)
                .ToArray();
            TempData["Error"] = validationErrors.Length > 0
                ? $"Perubahan belum disimpan. {string.Join(" ", validationErrors)}"
                : "Perubahan belum disimpan. Periksa kembali data produk yang wajib diisi.";
            await LoadCategories();
            return View(form);
        }
        var result = await service.UpdateAsync(form, User.GetUserId());
        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Message);
            TempData["Error"] = result.Message;
            await LoadCategories();
            return View(form);
        }

        TempData["Success"] = result.Message;
        return RedirectToAction(nameof(Edit), new { id = secureIds.Protect(form.Id) });
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = "Seller")]
    public async Task<IActionResult> Delete(int id)
    {
        if (!await service.DeleteAsync(id, User.GetUserId())) return Forbid();

        TempData["Success"] = "Produk berhasil dihapus.";
        return RedirectToAction(nameof(MyProducts));
    }

    private async Task ValidateCategoryAsync(ProductFormVm form)
    {
        if (form.CategoryId > 0 && !await categories.ExistsAsync(form.CategoryId))
            ModelState.AddModelError(nameof(form.CategoryId), "Kategori tidak ditemukan.");
    }

    private async Task LoadCategories()
    {
        ViewBag.Categories = new SelectList(await categories.GetAllAsync(), "Id", "Name");
    }
}
