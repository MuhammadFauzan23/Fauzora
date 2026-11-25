using System.ComponentModel.DataAnnotations;

namespace Fauzora.ViewModels;

public class SellerApplicationVm
{
    [Required, StringLength(120), Display(Name = "Nama toko")]
    public string StoreName { get; set; } = "";

    [Required, StringLength(1000), Display(Name = "Deskripsi toko")]
    public string Description { get; set; } = "";

    [Required, StringLength(100), Display(Name = "Kota toko")]
    public string City { get; set; } = "";
}

public class SellerApplicationAdminVm
{
    public int Id { get; set; }
    public string ApplicantName { get; set; } = "";
    public string ApplicantEmail { get; set; } = "";
    public string StoreName { get; set; } = "";
    public string Description { get; set; } = "";
    public string City { get; set; } = "";
    public string Status { get; set; } = "";
    public DateTime SubmittedAt { get; set; }
    public string? AdminNote { get; set; }
}