using Fauzora.Models;
using Fauzora.Repositories;
using Fauzora.ViewModels;

namespace Fauzora.Services;

public class ReviewService(ReviewRepository reviews, ProductRepository products)
{
    // Customer hanya dapat mengulas produk dari order yang sudah diterima dan satu kali per produk.
    public async Task<ReviewPanelVm> GetPanelAsync(int productId, string? userId, int? selectedRating = null)
    {
        var product = await products.GetByIdAsync(productId);
        var allItems = await reviews.GetForProductAsync(productId);
        selectedRating = selectedRating is >= 1 and <= 5 ? selectedRating : null;
        var panel = new ReviewPanelVm
        {
            ProductId = productId,
            Average = product?.RatingAverage ?? 0,
            Count = product?.RatingCount ?? allItems.Count,
            VisibleCount = selectedRating is null
                ? allItems.Count
                : allItems.Count(review => review.Rating == selectedRating.Value),
            SelectedRating = selectedRating,
            RatingCounts = allItems.GroupBy(review => review.Rating)
                .ToDictionary(group => group.Key, group => group.Count()),
            Reviews = allItems.Select(Map).ToList()
        };

        if (!string.IsNullOrWhiteSpace(userId))
        {
            panel.HasReviewed = await reviews.FindAsync(productId, userId) is not null;
            panel.CanReview = !panel.HasReviewed && await reviews.HasReceivedProductAsync(productId, userId);
        }
        panel.Form.ProductId = productId;
        return panel;
    }

    public async Task<ServiceResult> SubmitAsync(ReviewSubmitVm form, string userId)
    {
        if (form.Rating is < 1 or > 5)
            return ServiceResult.Fail("Pilih rating antara 1 sampai 5.");
        if (!await reviews.HasReceivedProductAsync(form.ProductId, userId))
            return ServiceResult.Fail("Ulasan hanya dapat dikirim setelah pesanan diterima.");

        var review = await reviews.FindAsync(form.ProductId, userId);
        if (review is not null)
            return ServiceResult.Fail("Anda sudah memberikan ulasan untuk produk ini.");

        review = new ProductReview { ProductId = form.ProductId, UserId = userId };
        reviews.Add(review);

        review.Rating = form.Rating;
        review.Comment = (form.Comment ?? "").Trim();
        review.CreatedAt = DateTime.UtcNow;
        await reviews.SaveAsync();
        await reviews.RefreshRatingAsync(form.ProductId);
        return ServiceResult.Ok("Ulasan produk berhasil disimpan.");
    }

    private static ReviewItemVm Map(ProductReview review)
    {
        var fullName = string.IsNullOrWhiteSpace(review.User?.FullName) ? "Pengguna" : review.User.FullName.Trim();
        var firstName = fullName.Split(' ', StringSplitOptions.RemoveEmptyEntries)[0];
        return new ReviewItemVm(firstName, review.Rating, review.Comment, review.CreatedAt);
    }
}