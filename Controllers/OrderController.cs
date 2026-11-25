using Fauzora.Extensions;
using Fauzora.Models;
using Fauzora.Services;
using Fauzora.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Fauzora.Controllers;

[Authorize(Roles = "Customer")]
public class OrderController(OrderService service, SecureIdService secureIds) : Controller
{
    public async Task<IActionResult> Index() => View(await service.GetForCustomerAsync(User.GetUserId()));

    [Authorize(Roles = "Customer")]
    public async Task<IActionResult> Details(string id)
    {
        if (!secureIds.TryUnprotect(id, out var orderId)) return NotFound();
        var order = await service.GetForCustomerDetailsAsync(orderId, User.GetUserId());
        return order is null ? NotFound() : View(order);
    }

    [Authorize(Roles = "Customer")]
    public async Task<IActionResult> Return(string id)
    {
        if (!secureIds.TryUnprotect(id, out var orderId)) return NotFound();
        var order = await service.GetForCustomerDetailsAsync(orderId, User.GetUserId());
        if (order is null) return NotFound();
        if ((order.Status != OrderStatus.Delivered && order.Status != OrderStatus.Completed) || order.ReturnStatus != ReturnRequestStatus.None)
            return RedirectToAction(nameof(Details), new { id = secureIds.Protect(order.Id) });
        return View(order);
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = "Customer")]
    public async Task<IActionResult> ConfirmReceived(int id)
    {
        var result = await service.ConfirmReceivedAsync(id, User.GetUserId());
        TempData[result.Success ? "Success" : "Error"] = result.Message;
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = "Customer")]
    public async Task<IActionResult> RequestReturn(ReturnRequestVm form)
    {
        if (!ModelState.IsValid)
        {
            TempData["Error"] = "Alasan pengembalian harus diisi minimal 10 karakter.";
            return RedirectToAction(nameof(Index));
        }

        var result = await service.RequestReturnAsync(form, User.GetUserId());
        TempData[result.Success ? "Success" : "Error"] = result.Message;
        return RedirectToAction(nameof(Details), new { id = secureIds.Protect(form.OrderId) });
    }

}
