using Fauzora.Models;

namespace Fauzora.ViewModels;

public record CatalogVm(List<Product> Products, List<Category> Categories, string? Query, int? CategoryId)
{
	public List<Product> NewestProducts { get; init; } = [];
	public List<Product> BestSellingProducts { get; init; } = [];
}
