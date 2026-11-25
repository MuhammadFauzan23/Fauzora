namespace Fauzora.Models;

public static class ReturnRequestStatus
{
    // Return berjalan terpisah dari status pengiriman agar Delivered tidak otomatis selesai saat return aktif.
    public const string None = "None";
    public const string Requested = "Requested";
    public const string Approved = "Approved";
    public const string Rejected = "Rejected";

    public static string Label(string status) => status switch
    {
        Requested => "Menunggu pemeriksaan",
        Approved => "Disetujui",
        Rejected => "Ditolak",
        _ => "Belum diajukan"
    };

    public static string BadgeClass(string status) => status switch
    {
        Requested => "text-bg-warning",
        Approved => "text-bg-success",
        Rejected => "text-bg-danger",
        _ => "text-bg-secondary"
    };
}