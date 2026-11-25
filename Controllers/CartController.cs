using Fauzora.Extensions;
using Fauzora.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Fauzora.Controllers;

[Authorize(Roles = "Customer")]
public class CartController(CartService service) : Controller
{
    // Semua operasi cart memakai UserId dari login agar cart antar customer tidak tercampur.
    public async Task<IActionResult> Index() => View(await service.GetCartAsync(User.GetUserId()));

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Add(int productId, int quantity = 1)
    {
        var result = await service.AddAsync(User.GetUserId(), productId, quantity);
        if (!result.Success)
        {
            TempData["Error"] = result.Message;
            return RedirectToAction("Details", "Product", new { id = productId });
        }
        TempData["Success"] = result.Message;
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(int id, int quantity)
    {
        var result = await service.UpdateAsync(User.GetUserId(), id, quantity);
        if (!result.Success) TempData["Error"] = result.Message;
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Remove(int id)
    {
        await service.RemoveAsync(User.GetUserId(), id);
        return RedirectToAction(nameof(Index));
    }
}
