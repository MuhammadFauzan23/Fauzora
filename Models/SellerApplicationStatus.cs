namespace Fauzora.Models;

public static class SellerApplicationStatus
{
    // Status pengajuan menentukan apakah customer dapat mengajukan ulang atau masuk dashboard seller.
    public const string Pending = "Pending";
    public const string Approved = "Approved";
    public const string Rejected = "Rejected";

    public static string Label(string status) => status switch
    {
        Approved => "Disetujui",
        Rejected => "Ditolak",
        _ => "Menunggu pemeriksaan"
    };

    public static string BadgeClass(string status) => status switch
    {
        Approved => "text-bg-success",
        Rejected => "text-bg-danger",
        _ => "text-bg-warning"
    };
}