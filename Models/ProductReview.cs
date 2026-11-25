using System.ComponentModel.DataAnnotations;

namespace Fauzora.Models;

public class ProductReview
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public Product? Product { get; set; }

    [Required]
    public string UserId { get; set; } = "";
    public ApplicationUser? User { get; set; }

    [Range(1, 5)]
    public int Rating { get; set; }

    [StringLength(1000)]
    public string Comment { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}