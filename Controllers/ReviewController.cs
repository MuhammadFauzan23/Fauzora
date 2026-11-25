using Fauzora.Services;
using Fauzora.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Fauzora.Extensions;

namespace Fauzora.Controllers;

[Authorize(Roles = "Customer")]
public class ReviewController(ReviewService service, SecureIdService secureIds) : Controller
{
    // Review diterima melalui service setelah service memastikan order Completed dan belum pernah direview.
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Submit([Bind(Prefix = "Form")] ReviewSubmitVm form)
    {
        if (!ModelState.IsValid)
        {
            TempData["Error"] = "Rating dan isi ulasan belum valid.";
            return RedirectToAction("Details", "Product", new { id = secureIds.Protect(form.ProductId), reviewError = true });
        }

        var result = await service.SubmitAsync(form, User.GetUserId());
        TempData[result.Success ? "Success" : "Error"] = result.Message;
        return RedirectToAction("Details", "Product", new { id = secureIds.Protect(form.ProductId), review = true });
    }
}