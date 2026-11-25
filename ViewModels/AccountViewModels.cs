using System.ComponentModel.DataAnnotations;

namespace Fauzora.ViewModels;

public class RegisterVm
{
    [Required, StringLength(100), Display(Name = "Nama lengkap")]
    public string FullName { get; set; } = "";

    [Required, EmailAddress]
    public string Email { get; set; } = "";

    [Required, MinLength(8), DataType(DataType.Password), Display(Name = "Password")]
    public string Password { get; set; } = "";

    [Required, Compare(nameof(Password), ErrorMessage = "Konfirmasi password tidak sama."), DataType(DataType.Password), Display(Name = "Konfirmasi password")]
    public string ConfirmPassword { get; set; } = "";
}

public class LoginVm
{
    [Required, EmailAddress]
    public string Email { get; set; } = "";

    [Required, DataType(DataType.Password)]
    public string Password { get; set; } = "";

    [Display(Name = "Ingat saya")]
    public bool RememberMe { get; set; }
}

public class ForgotPasswordVm
{
    [Required, EmailAddress]
    public string Email { get; set; } = "";
}

public class ResetPasswordVm
{
    [Required]
    public string UserId { get; set; } = "";

    [Required]
    public string Token { get; set; } = "";

    [Required, MinLength(8), DataType(DataType.Password), Display(Name = "Password baru")]
    public string Password { get; set; } = "";

    [Required, Compare(nameof(Password), ErrorMessage = "Konfirmasi password tidak sama."), DataType(DataType.Password), Display(Name = "Konfirmasi password")]
    public string ConfirmPassword { get; set; } = "";
}
