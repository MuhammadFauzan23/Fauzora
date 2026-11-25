using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace Fauzora.Models;

public class ApplicationUser : IdentityUser
{
    public bool IsActive { get; set; } = true;
    public string FullName { get; set; } = "";
    public string? City { get; set; }
    public string? ProfileImageUrl { get; set; }
    [StringLength(100)] public string? CustomerAddressContactName { get; set; }
    [StringLength(32)] public string? CustomerAddressPhone { get; set; }
    [StringLength(500)] public string? CustomerAddressStreet { get; set; }
    [StringLength(100)] public string? CustomerAddressVillage { get; set; }
    [StringLength(100)] public string? CustomerAddressDistrict { get; set; }
    [StringLength(100)] public string? CustomerAddressCity { get; set; }
    [StringLength(100)] public string? CustomerAddressProvince { get; set; }
    [StringLength(20)] public string? CustomerAddressPostalCode { get; set; }
    [StringLength(500)] public string? CustomerAddressDeliveryNote { get; set; }
    [StringLength(100)] public string? SellerAddressContactName { get; set; }
    [StringLength(32)] public string? SellerAddressPhone { get; set; }
    [StringLength(500)] public string? SellerAddressStreet { get; set; }
    [StringLength(100)] public string? SellerAddressVillage { get; set; }
    [StringLength(100)] public string? SellerAddressDistrict { get; set; }
    [StringLength(100)] public string? SellerAddressCity { get; set; }
    [StringLength(100)] public string? SellerAddressProvince { get; set; }
    [StringLength(20)] public string? SellerAddressPostalCode { get; set; }
    [StringLength(500)] public string? SellerAddressDeliveryNote { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
