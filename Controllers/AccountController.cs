using Fauzora.Extensions;
using Fauzora.Models;
using Fauzora.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Fauzora.Controllers;

public class AccountController(
    UserManager<ApplicationUser> userManager,
    SignInManager<ApplicationUser> signInManager,
    IWebHostEnvironment environment) : Controller
{
    // Registrasi publik selalu dimulai sebagai Customer; role tambahan diatur langsung oleh Admin.
    [HttpGet]
    public IActionResult Register() => View();

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterVm model)
    {
        if (!ModelState.IsValid) return View(model);
        var user = new ApplicationUser { UserName = model.Email, Email = model.Email, FullName = model.FullName, IsActive = true };
        var result = await userManager.CreateAsync(user, model.Password);
        if (result.Succeeded)
        {
            await userManager.AddToRoleAsync(user, "Customer");
            // Setelah daftar, user langsung login memakai authentication cookie persistent.
            await signInManager.SignInAsync(user, isPersistent: true);
            return RedirectToAction("Index", "Home");
        }
        foreach (var error in result.Errors) ModelState.AddModelError("", error.Description);
        return View(model);
    }

    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        ViewBag.ReturnUrl = returnUrl;
        return View();
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginVm model, string? returnUrl = null)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }
        var account = await userManager.FindByEmailAsync(model.Email);
        if (account is not null && !account.IsActive)
        {
            ModelState.AddModelError("", "Email atau password tidak valid.");
            return View(model);
        }
        if (account is not null && account.LockoutEnabled)
        {
            var unlockResult = await userManager.SetLockoutEndDateAsync(account, null);
            if (!unlockResult.Succeeded)
            {
                foreach (var error in unlockResult.Errors) ModelState.AddModelError("", error.Description);
                return View(model);
            }

            var resetFailedCountResult = await userManager.ResetAccessFailedCountAsync(account);
            if (!resetFailedCountResult.Succeeded)
            {
                foreach (var error in resetFailedCountResult.Errors) ModelState.AddModelError("", error.Description);
                return View(model);
            }

            var disableLockoutResult = await userManager.SetLockoutEnabledAsync(account, false);
            if (!disableLockoutResult.Succeeded)
            {
                foreach (var error in disableLockoutResult.Errors) ModelState.AddModelError("", error.Description);
                return View(model);
            }
        }

        var result = await signInManager.PasswordSignInAsync(model.Email, model.Password, model.RememberMe, lockoutOnFailure: false);
        if (result.Succeeded)
        {
            // User belum terisi di request ini, jadi role dibaca langsung dari database, bukan session.
            var user = account ?? await userManager.FindByEmailAsync(model.Email);
            var roles = user is null ? [] : await userManager.GetRolesAsync(user);
            if (roles.Contains("Admin"))
            {
                return RedirectToAction("Index", "Admin");
            }
            if (roles.Contains("Seller"))
            {
                return RedirectToAction("Index", "Seller");
            }
            // Return URL hanya untuk role umum; seller/admin selalu masuk dashboard perannya.
            if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl)) return LocalRedirect(returnUrl);
            return RedirectToAction("Index", "Home");
        }
        ModelState.AddModelError("", "Email atau password tidak valid.");
        return View(model);
    }

    [HttpGet]
    public IActionResult ForgotPassword() => View();

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordVm model)
    {
        if (!ModelState.IsValid) return View(model);

        var user = await userManager.FindByEmailAsync(model.Email);
        // Project ini belum memiliki alur verifikasi email, jadi akun terdaftar tetap dapat reset password.
        if (user is not null)
        {
            var token = await userManager.GeneratePasswordResetTokenAsync(user);
            var link = Url.Action(nameof(ResetPassword), "Account", new
            {
                userId = user.Id,
                token
            }, Request.Scheme);
            TempData["ResetLink"] = link;
        }

        // Pesan yang sama mencegah orang menebak apakah email terdaftar.
        return RedirectToAction(nameof(ForgotPasswordConfirmation));
    }

    [HttpGet]
    public IActionResult ForgotPasswordConfirmation() => View();

    [HttpGet]
    public IActionResult ResetPassword(string? userId, string? token)
    {
        if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(token))
            return RedirectToAction(nameof(ForgotPasswordConfirmation));

        return View(new ResetPasswordVm { UserId = userId, Token = token });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(ResetPasswordVm model)
    {
        if (!ModelState.IsValid) return View(model);

        var user = await userManager.FindByIdAsync(model.UserId);
        if (user is null)
        {
            ModelState.AddModelError("", "Link reset password tidak valid atau sudah kedaluwarsa.");
            return View(model);
        }

        var result = await userManager.ResetPasswordAsync(user, model.Token, model.Password);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors) ModelState.AddModelError("", error.Description);
            return View(model);
        }

        TempData["Success"] = "Password berhasil diubah. Silakan login dengan password baru.";
        return RedirectToAction(nameof(Login));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await signInManager.SignOutAsync();
        return RedirectToAction("Index", "Home");
    }

    [Authorize]
    [HttpGet]
    public async Task<IActionResult> Profile()
    {
        var user = await userManager.GetUserAsync(User);
        if (user is null) return Challenge();
        return View(ToProfileVm(user));
    }

    [Authorize]
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Profile(ProfileVm model)
    {
        var user = await userManager.GetUserAsync(User);
        if (user is null) return Challenge();
        model.CurrentImageUrl = user.ProfileImageUrl;
        if (!ModelState.IsValid) return View(model);

        var oldImageUrl = user.ProfileImageUrl;
        string? newImageUrl = null;
        if (model.ProfileImage is not null)
        {
            var extension = Path.GetExtension(model.ProfileImage.FileName).ToLowerInvariant();
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp" };
            if (!allowedExtensions.Contains(extension))
            {
                ModelState.AddModelError(nameof(model.ProfileImage), "Foto harus berformat JPG, PNG, atau WEBP.");
                return View(model);
            }
            if (model.ProfileImage.Length > 2 * 1024 * 1024)
            {
                ModelState.AddModelError(nameof(model.ProfileImage), "Ukuran foto maksimal 2 MB.");
                return View(model);
            }

            var uploadDirectory = Path.Combine(environment.WebRootPath, "uploads", "profiles");
            Directory.CreateDirectory(uploadDirectory);
            var fileName = $"{Guid.NewGuid():N}{extension}";
            var filePath = Path.Combine(uploadDirectory, fileName);
            await using (var stream = System.IO.File.Create(filePath))
            {
                await model.ProfileImage.CopyToAsync(stream);
            }
            newImageUrl = $"/uploads/profiles/{fileName}";
            user.ProfileImageUrl = newImageUrl;
        }

        user.FullName = model.FullName.Trim();
        if (User.IsInRole("Seller"))
            user.City = string.IsNullOrWhiteSpace(model.City) ? null : model.City.Trim();

        if (User.IsInRole("Customer"))
        {
            user.CustomerAddressContactName = CleanAddressValue(model.CustomerAddressContactName);
            user.CustomerAddressPhone = CleanAddressValue(model.CustomerAddressPhone);
            user.CustomerAddressStreet = CleanAddressValue(model.CustomerAddressStreet);
            user.CustomerAddressVillage = CleanAddressValue(model.CustomerAddressVillage);
            user.CustomerAddressDistrict = CleanAddressValue(model.CustomerAddressDistrict);
            user.CustomerAddressCity = CleanAddressValue(model.CustomerAddressCity);
            user.CustomerAddressProvince = CleanAddressValue(model.CustomerAddressProvince);
            user.CustomerAddressPostalCode = CleanAddressValue(model.CustomerAddressPostalCode);
            user.CustomerAddressDeliveryNote = CleanAddressValue(model.CustomerAddressDeliveryNote);
        }

        if (User.IsInRole("Seller"))
        {
            user.SellerAddressContactName = CleanAddressValue(model.SellerAddressContactName);
            user.SellerAddressPhone = CleanAddressValue(model.SellerAddressPhone);
            user.SellerAddressStreet = CleanAddressValue(model.SellerAddressStreet);
            user.SellerAddressVillage = CleanAddressValue(model.SellerAddressVillage);
            user.SellerAddressDistrict = CleanAddressValue(model.SellerAddressDistrict);
            user.SellerAddressCity = CleanAddressValue(model.SellerAddressCity);
            user.SellerAddressProvince = CleanAddressValue(model.SellerAddressProvince);
            user.SellerAddressPostalCode = CleanAddressValue(model.SellerAddressPostalCode);
            user.SellerAddressDeliveryNote = CleanAddressValue(model.SellerAddressDeliveryNote);
        }

        var result = await userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            if (newImageUrl is not null) DeleteProfileImage(newImageUrl);
            foreach (var error in result.Errors) ModelState.AddModelError("", error.Description);
            model.CurrentImageUrl = oldImageUrl;
            return View(model);
        }

        if (oldImageUrl is not null && newImageUrl is not null) DeleteProfileImage(oldImageUrl);

        await signInManager.RefreshSignInAsync(user);
        TempData["Success"] = "Profil berhasil diperbarui.";
        return RedirectToAction(nameof(Profile));
    }

    public IActionResult AccessDenied() => View();

    private static string? CleanAddressValue(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static ProfileVm ToProfileVm(ApplicationUser user) => new()
    {
        FullName = user.FullName,
        City = user.City,
        CurrentImageUrl = user.ProfileImageUrl,
        CustomerAddressContactName = user.CustomerAddressContactName,
        CustomerAddressPhone = user.CustomerAddressPhone,
        CustomerAddressStreet = user.CustomerAddressStreet,
        CustomerAddressVillage = user.CustomerAddressVillage,
        CustomerAddressDistrict = user.CustomerAddressDistrict,
        CustomerAddressCity = user.CustomerAddressCity,
        CustomerAddressProvince = user.CustomerAddressProvince,
        CustomerAddressPostalCode = user.CustomerAddressPostalCode,
        CustomerAddressDeliveryNote = user.CustomerAddressDeliveryNote,
        SellerAddressContactName = user.SellerAddressContactName,
        SellerAddressPhone = user.SellerAddressPhone,
        SellerAddressStreet = user.SellerAddressStreet,
        SellerAddressVillage = user.SellerAddressVillage,
        SellerAddressDistrict = user.SellerAddressDistrict,
        SellerAddressCity = user.SellerAddressCity,
        SellerAddressProvince = user.SellerAddressProvince,
        SellerAddressPostalCode = user.SellerAddressPostalCode,
        SellerAddressDeliveryNote = user.SellerAddressDeliveryNote
    };

    private void DeleteProfileImage(string imageUrl)
    {
        var uploadDirectory = Path.GetFullPath(Path.Combine(environment.WebRootPath, "uploads", "profiles"));
        var relativePath = imageUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
        var filePath = Path.GetFullPath(Path.Combine(environment.WebRootPath, relativePath));
        if (filePath.StartsWith(uploadDirectory + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) && System.IO.File.Exists(filePath))
        {
            System.IO.File.Delete(filePath);
        }
    }
}
