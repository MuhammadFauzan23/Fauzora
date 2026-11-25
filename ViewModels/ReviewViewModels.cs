using System.ComponentModel.DataAnnotations;

namespace Fauzora.ViewModels;

public class ReviewSubmitVm
{
    [Required]
    public int ProductId { get; set; }

    [Range(1, 5, ErrorMessage = "Pilih rating antara 1 sampai 5.")]
    public int Rating { get; set; }

    [StringLength(1000, ErrorMessage = "Ulasan maksimal 1000 karakter.")]
    [Display(Name = "Ulasan")]
    public string Comment { get; set; } = "";
}

public record ReviewItemVm(string ReviewerName, int Rating, string Comment, DateTime CreatedAt);

public class ReviewPanelVm
{
    public int ProductId { get; set; }
    public decimal Average { get; set; }
    public int Count { get; set; }
    public int VisibleCount { get; set; }
    public int? SelectedRating { get; set; }
    public Dictionary<int, int> RatingCounts { get; set; } = [];
    public bool CanReview { get; set; }
    public bool HasReviewed { get; set; }
    public ReviewSubmitVm Form { get; set; } = new();
    public List<ReviewItemVm> Reviews { get; set; } = [];
}

public class ProfileVm
{
    [Required, StringLength(100), Display(Name = "Nama lengkap")]
    public string FullName { get; set; } = "";

    [StringLength(80), Display(Name = "Kota asal")]
    public string? City { get; set; }

    public string? CurrentImageUrl { get; set; }

    [Display(Name = "Foto profil")]
    public IFormFile? ProfileImage { get; set; }

    [StringLength(100), Display(Name = "Nama penerima/kontak")]
    public string? CustomerAddressContactName { get; set; }
    [StringLength(32), Display(Name = "Nomor telepon penerima")]
    public string? CustomerAddressPhone { get; set; }
    [StringLength(500), Display(Name = "Alamat jalan dan detail bangunan")]
    public string? CustomerAddressStreet { get; set; }
    [StringLength(100), Display(Name = "Kelurahan/desa")]
    public string? CustomerAddressVillage { get; set; }
    [StringLength(100), Display(Name = "Kecamatan")]
    public string? CustomerAddressDistrict { get; set; }
    [StringLength(100), Display(Name = "Kota/kabupaten tujuan")]
    public string? CustomerAddressCity { get; set; }
    [StringLength(100), Display(Name = "Provinsi tujuan")]
    public string? CustomerAddressProvince { get; set; }
    [StringLength(20), Display(Name = "Kode pos tujuan")]
    public string? CustomerAddressPostalCode { get; set; }
    [StringLength(500), Display(Name = "Catatan untuk kurir")]
    public string? CustomerAddressDeliveryNote { get; set; }

    [StringLength(100), Display(Name = "Nama kontak toko")]
    public string? SellerAddressContactName { get; set; }
    [StringLength(32), Display(Name = "Nomor telepon toko")]
    public string? SellerAddressPhone { get; set; }
    [StringLength(500), Display(Name = "Alamat jalan dan detail bangunan toko")]
    public string? SellerAddressStreet { get; set; }
    [StringLength(100), Display(Name = "Kelurahan/desa toko")]
    public string? SellerAddressVillage { get; set; }
    [StringLength(100), Display(Name = "Kecamatan toko")]
    public string? SellerAddressDistrict { get; set; }
    [StringLength(100), Display(Name = "Kota/kabupaten toko")]
    public string? SellerAddressCity { get; set; }
    [StringLength(100), Display(Name = "Provinsi toko")]
    public string? SellerAddressProvince { get; set; }
    [StringLength(20), Display(Name = "Kode pos toko")]
    public string? SellerAddressPostalCode { get; set; }
    [StringLength(500), Display(Name = "Catatan penjemputan")]
    public string? SellerAddressDeliveryNote { get; set; }
}

public class ReturnRequestVm
{
    [Required]
    public int OrderId { get; set; }

    [Required, StringLength(500, MinimumLength = 10, ErrorMessage = "Alasan pengembalian 10-500 karakter.")]
    [Display(Name = "Alasan pengembalian")]
    public string Reason { get; set; } = "";
}