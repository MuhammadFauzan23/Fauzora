using Fauzora.Data;
using Fauzora.Models;
using Fauzora.ViewModels;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace Fauzora.Services;

public class DashboardService(AppDbContext db)
{
    // Dashboard membaca agregat database untuk ringkasan admin dan seller, tanpa mengubah data.
    public async Task<AdminDashboardVm> GetAdminAsync()
    {
        var users = await db.Users.CountAsync();
        var products = await db.Products.CountAsync();
        var orders = await db.Orders.CountAsync();
        var revenue = await db.Orders.Where(o => o.Status != Models.OrderStatus.Cancelled)
            .SumAsync(o => (decimal?)o.Total) ?? 0;
        return new AdminDashboardVm(users, products, orders, revenue);
    }

    public async Task<SellerDashboardVm> GetSellerAsync(string sellerId)
    {
        var products = await db.Products.CountAsync(p => p.SellerId == sellerId);
        var validOrders = db.Orders.Where(order => order.SellerId == sellerId &&
            order.Status != OrderStatus.Cancelled &&
            order.ReturnStatus != ReturnRequestStatus.Approved);

        var orders = await validOrders.CountAsync();
        var revenue = await validOrders.SumAsync(order => (decimal?)order.Total) ?? 0m;
        var processingOrders = await validOrders.CountAsync(order => order.Status == OrderStatus.Processing);
        var shippedOrders = await validOrders.CountAsync(order => order.Status == OrderStatus.Shipped);
        var sellerReturns = db.Orders.Where(order => order.SellerId == sellerId &&
            order.ReturnStatus != ReturnRequestStatus.None);
        var returnRequests = await sellerReturns.CountAsync();
        var pendingReturns = await sellerReturns.CountAsync(order => order.ReturnStatus == ReturnRequestStatus.Requested);
        var unitsSold = await db.OrderItems
            .Where(item => item.Order != null && item.Order.SellerId == sellerId &&
                item.Order.Status != OrderStatus.Cancelled &&
                item.Order.ReturnStatus != ReturnRequestStatus.Approved)
            .SumAsync(item => (int?)item.Quantity) ?? 0;

        var now = DateTime.UtcNow;
        var firstMonth = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc).AddMonths(-5);
        var recentSales = await validOrders
            .Where(order => order.CreatedAt >= firstMonth)
            .Select(order => new { order.CreatedAt, order.Total })
            .ToListAsync();

        var monthlyTotals = recentSales
            .GroupBy(order => new { order.CreatedAt.Year, order.CreatedAt.Month })
            .ToDictionary(group => (group.Key.Year, group.Key.Month), group => group.Sum(order => order.Total));
        var monthlyValues = Enumerable.Range(0, 6)
            .Select(offset => firstMonth.AddMonths(offset))
            .Select(month => monthlyTotals.GetValueOrDefault((month.Year, month.Month)))
            .ToArray();
        var maxMonthlyRevenue = monthlyValues.DefaultIfEmpty(0m).Max();
        var culture = CultureInfo.GetCultureInfo("id-ID");
        var monthlySales = Enumerable.Range(0, 6)
            .Select(offset => firstMonth.AddMonths(offset))
            .Select((month, index) => new SellerMonthlySalesVm(
                culture.DateTimeFormat.GetAbbreviatedMonthName(month.Month),
                monthlyValues[index],
                maxMonthlyRevenue == 0 ? 0 : Math.Max(8, (int)Math.Round(monthlyValues[index] / maxMonthlyRevenue * 100))))
            .ToList();

        return new SellerDashboardVm(products, orders, revenue, unitsSold, processingOrders, shippedOrders,
            returnRequests, pendingReturns, monthlySales);
    }
}
