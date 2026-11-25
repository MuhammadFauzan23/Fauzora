using Fauzora.Extensions;
using Fauzora.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace Fauzora.Components;

public class CartCountViewComponent(CartRepository carts) : ViewComponent
{
    public async Task<IViewComponentResult> InvokeAsync()
    {
        var user = UserClaimsPrincipal;
        var count = user.IsInRole("Customer") ? await carts.CountAsync(user.GetUserId()) : 0;
        return View(count);
    }
}
