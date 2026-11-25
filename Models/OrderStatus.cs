namespace Fauzora.Models;

public static class OrderStatus
{
    // Nilai status ini menjadi kontrak antara controller, service, admin, dan database.
    public const string PaidDemo = "Paid - Demo";
    public const string Pending = "Pending";
    public const string Processing = "Processing";
    public const string Shipped = "Shipped";
    public const string Delivered = "Delivered";
    public const string Completed = "Completed";
    public const string Cancelled = "Cancelled";

    public static readonly string[] All =
        [PaidDemo, Pending, Processing, Shipped, Delivered, Completed, Cancelled];

    public static string BadgeClass(string status) => status switch
    {
        Completed or Delivered => "text-bg-success",
        Cancelled => "text-bg-danger",
        Shipped or Processing => "text-bg-primary",
        PaidDemo => "text-bg-info",
        _ => "text-bg-secondary"
    };
}
