using Fauzora.Extensions;
using Fauzora.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Fauzora.Controllers;

[Authorize(Roles = "Customer")]
public class CheckoutController(OrderService service) : Controller
{
    // Checkout hanya tersedia untuk Customer dan seluruh aturan stok berada di OrderService.
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> PlaceOrder(List<int>? selectedCartItemIds)
    {
        var result = await service.CheckoutAsync(User.GetUserId(), selectedCartItemIds ?? []);
        TempData[result.Success ? "Success" : "Error"] = result.Message;
        return result.Success ? RedirectToAction("Index", "Order") : RedirectToAction("Index", "Cart");
    }
}
