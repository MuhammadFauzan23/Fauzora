using Fauzora.Extensions;
using Fauzora.Models;
using Fauzora.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Fauzora.Controllers;

[Authorize(Roles = "Seller")]
public class SellerController(DashboardService dashboard, OrderService orders, SecureIdService secureIds) : Controller
{
    public async Task<IActionResult> Index() => View(await dashboard.GetSellerAsync(User.GetUserId()));

    public async Task<IActionResult> Orders() => View(await orders.GetForSellerAsync(User.GetUserId()));

    public async Task<IActionResult> Returns() => View(await orders.GetReturnRequestsForSellerAsync(User.GetUserId()));

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateOrderStatus(string id, string status)
    {
        if (!secureIds.TryUnprotect(id, out var orderId)) return NotFound();
        var result = await orders.SellerUpdateStatusAsync(orderId, User.GetUserId(), status);
        TempData[result.Success ? "Success" : "Error"] = result.Message;
        return RedirectToAction(nameof(Orders));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateReturnStatus(string id, string returnStatus, string? note)
    {
        if (!secureIds.TryUnprotect(id, out var orderId)) return NotFound();
        var result = await orders.SellerUpdateReturnStatusAsync(orderId, User.GetUserId(), returnStatus, note);
        TempData[result.Success ? "Success" : "Error"] = result.Message;
        return RedirectToAction(nameof(Orders));
    }
}
