using Fauzora.Extensions;
using Fauzora.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Fauzora.Controllers;

[Authorize(Roles = "Admin")]
public class AdminController(AdminUserManagementService users, CategoryService categories) : Controller
{
    public async Task<IActionResult> Index(string? search) => View(await users.GetUsersAsync(search));

    [HttpGet]
    public async Task<IActionResult> Categories() => View(await categories.GetAllAsync());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateCategory(string? name, string? description)
    {
        var result = await categories.CreateAsync(name, description);
        TempData[result.Success ? "Success" : "Error"] = result.Message;
        return RedirectToAction(nameof(Categories));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateCategory(int id, string? name, string? description)
    {
        var result = await categories.UpdateAsync(id, name, description);
        TempData[result.Success ? "Success" : "Error"] = result.Message;
        return RedirectToAction(nameof(Categories));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteCategory(int id)
    {
        var result = await categories.DeleteAsync(id);
        TempData[result.Success ? "Success" : "Error"] = result.Message;
        return RedirectToAction(nameof(Categories));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SetRole(string userId, string? role, string? search)
    {
        var result = await users.SetRoleAsync(userId, User.GetUserId(), role);
        TempData[result.Success ? "Success" : "Error"] = result.Message;
        return RedirectToAction(nameof(Index), new { search });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public Task<IActionResult> ActivateAccount(string userId, string? search) =>
        UpdateAccountStatusAsync(userId, true, search);

    [HttpPost, ValidateAntiForgeryToken]
    public Task<IActionResult> DeactivateAccount(string userId, string? search) =>
        UpdateAccountStatusAsync(userId, false, search);

    private async Task<IActionResult> UpdateAccountStatusAsync(string userId, bool isActive, string? search)
    {
        var result = await users.SetActiveAsync(userId, User.GetUserId(), isActive);
        TempData[result.Success ? "Success" : "Error"] = result.Message;
        return RedirectToAction(nameof(Index), new { search });
    }
}
