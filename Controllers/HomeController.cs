using Microsoft.AspNetCore.Mvc;

namespace Fauzora.Controllers;

public class HomeController : Controller
{
    public IActionResult Index() => RedirectToAction("Index", "Product");

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error() => View();
}
