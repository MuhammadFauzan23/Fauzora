using Fauzora.Models;

namespace Fauzora.ViewModels;

public class OrderAddressSnapshotVm
{
    public Order Order { get; set; } = new();
    public bool ShowCustomerAddress { get; set; }
    public bool ShowSellerAddress { get; set; }
}
