using System.ComponentModel.DataAnnotations;

namespace Fauzora.Models;

public class SellerApplication
{
    public int Id { get; set; }

    [Required]
    public string UserId { get; set; } = "";
    public ApplicationUser? User { get; set; }

    [Required, StringLength(120)]
    public string StoreName { get; set; } = "";

    [Required, StringLength(1000)]
    public string Description { get; set; } = "";

    [Required, StringLength(100)]
    public string City { get; set; } = "";

    [Required, StringLength(20)]
    public string Status { get; set; } = SellerApplicationStatus.Pending;

    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ReviewedAt { get; set; }

    [StringLength(500)]
    public string? AdminNote { get; set; }
}