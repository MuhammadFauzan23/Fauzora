using System.ComponentModel.DataAnnotations;

namespace Fauzora.Models;

public class ProductImage
{
    public int Id { get; set; }

    public int ProductId { get; set; }
    public Product? Product { get; set; }

    [Required, StringLength(500)]
    public string Url { get; set; } = "";

    public int SortOrder { get; set; }
}
