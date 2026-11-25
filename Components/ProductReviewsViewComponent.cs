using Fauzora.Extensions;
using Fauzora.Services;
using Microsoft.AspNetCore.Mvc;

namespace Fauzora.Components;

public class ProductReviewsViewComponent(ReviewService service) : ViewComponent
{
    public Task<IViewComponentResult> InvokeAsync(int productId)
    {
        var userId = HttpContext.User.Identity?.IsAuthenticated == true
            ? HttpContext.User.GetUserId()
            : null;
        var ratingValue = HttpContext.Request.Query["rating"].ToString();
        int? selectedRating = int.TryParse(ratingValue, out var rating) ? rating : null;
        return InvokeCoreAsync(productId, userId, selectedRating);
    }

    private async Task<IViewComponentResult> InvokeCoreAsync(int productId, string? userId, int? selectedRating) =>
        View(await service.GetPanelAsync(productId, userId, selectedRating));
}