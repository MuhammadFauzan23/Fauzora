namespace Fauzora.ViewModels;

public record AdminDashboardVm(int Users, int Products, int Orders, decimal Revenue);

public sealed class AdminUserListVm
{
	public string Search { get; set; } = "";
	public IReadOnlyList<AdminUserRowVm> Users { get; set; } = [];
}

public sealed record AdminUserRowVm(string Id, string Name, string Email, string Role, bool IsActive);

public record SellerDashboardVm(
	int Products,
	int Orders,
	decimal Revenue,
	int UnitsSold,
	int ProcessingOrders,
	int ShippedOrders,
	int ReturnRequests,
	int PendingReturns,
	IReadOnlyList<SellerMonthlySalesVm> MonthlySales);

public record SellerMonthlySalesVm(string Label, decimal Revenue, int BarHeightPercent);
