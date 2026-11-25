using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace Fauzora.Models;

public class Order
{
    public int Id { get; set; }
    public string OrderNumber { get; set; } = Guid.NewGuid().ToString("N")[..12].ToUpperInvariant();
    public string CustomerId { get; set; } = "";
    public ApplicationUser? Customer { get; set; }
    // Null for legacy orders created before seller-based order splitting.
    public string? SellerId { get; set; }
    public ApplicationUser? Seller { get; set; }
    public string? SellerName { get; set; }
    public Guid? CheckoutGroupId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string Status { get; set; } = "Pending";
    public DateTime? DeliveredAt { get; set; }
    public string ReturnStatus { get; set; } = ReturnRequestStatus.None;
    public string? ReturnReason { get; set; }
    public DateTime? ReturnRequestedAt { get; set; }
    public DateTime? ReturnDecisionAt { get; set; }
    public string? ReturnAdminNote { get; set; }
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
    [Column(TypeName = "numeric(18,2)")]
    public decimal Total { get; set; }
    public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();
}
