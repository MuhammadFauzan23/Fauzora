using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Fauzora.Models;

public class Product
{
    public int Id { get; set; }

    [Required, StringLength(160)]
    public string Name { get; set; } = "";

    [Required, StringLength(3000)]
    public string Description { get; set; } = "";

    [Column(TypeName = "numeric(18,2)")]
    [Range(0.01, 999999999)]
    public decimal Price { get; set; }

    [Range(0, 1000000)]
    public int Stock { get; set; }

    [StringLength(500)]
    public string? ImageUrl { get; set; }

    public int CategoryId { get; set; }
    public Category? Category { get; set; }

    public string SellerId { get; set; } = "";
    public ApplicationUser? Seller { get; set; }

    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column(TypeName = "numeric(3,2)")]
    public decimal RatingAverage { get; set; }
    public int RatingCount { get; set; }

    public ICollection<ProductImage> Images { get; set; } = new List<ProductImage>();
    public ICollection<ProductReview> Reviews { get; set; } = new List<ProductReview>();
}
