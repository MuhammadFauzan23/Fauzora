using Fauzora.Data;
using Fauzora.Models;
using Microsoft.EntityFrameworkCore;

namespace Fauzora.Repositories;

public class OrderRepository(AppDbContext db)
{
    // Repository order memusatkan query pesanan dan aturan query auto-completion di database.
    public async Task<List<Order>> GetForCustomerAsync(string userId) =>
        await db.Orders.Include(o => o.Items)
            .Where(o => o.CustomerId == userId)
            .OrderByDescending(o => o.CreatedAt).ToListAsync();

    public async Task<List<Order>> GetAllAsync() =>
        await db.Orders.Include(o => o.Items).Include(o => o.Customer)
            .OrderByDescending(o => o.CreatedAt).ToListAsync();

    public Task<List<Order>> GetForSellerAsync(string sellerId) =>
        db.Orders.Include(order => order.Items).Include(order => order.Customer)
            .Where(order => order.SellerId == sellerId)
            .OrderByDescending(order => order.CreatedAt)
            .ToListAsync();

    public Task<Order?> FindForSellerAsync(int id, string sellerId) =>
        db.Orders.Include(order => order.Items)
            .FirstOrDefaultAsync(order => order.Id == id && order.SellerId == sellerId);

    public ValueTask<Order?> FindAsync(int id) => db.Orders.FindAsync(id);

    public Task<Order?> FindForCustomerAsync(int id, string userId) =>
        db.Orders.FirstOrDefaultAsync(order => order.Id == id && order.CustomerId == userId);

    public Task<Order?> FindForCustomerWithItemsAsync(int id, string userId) =>
        db.Orders.Include(order => order.Items)
            .FirstOrDefaultAsync(order => order.Id == id && order.CustomerId == userId);

    public Task<Order?> FindWithCustomerAsync(int id) =>
        db.Orders.Include(order => order.Customer).FirstOrDefaultAsync(order => order.Id == id);

    public Task<List<Order>> GetReturnRequestsAsync() =>
        db.Orders.Include(order => order.Customer)
            .Include(order => order.Items)
            .Where(order => order.ReturnStatus != ReturnRequestStatus.None)
            .OrderByDescending(order => order.ReturnRequestedAt)
            .ToListAsync();

    public Task<List<Order>> GetReturnRequestsForSellerAsync(string sellerId) =>
        db.Orders.Include(order => order.Customer)
            .Include(order => order.Items)
            .Where(order => order.SellerId == sellerId && order.ReturnStatus != ReturnRequestStatus.None)
            .OrderByDescending(order => order.ReturnRequestedAt)
            .ToListAsync();

    public Task<int> CompleteExpiredDeliveredAsync(DateTime cutoff)
    {
        return db.Orders
            .Where(order => order.Status == OrderStatus.Delivered &&
                order.ReturnStatus == ReturnRequestStatus.None &&
                ((order.DeliveredAt != null && order.DeliveredAt <= cutoff) ||
                 (order.DeliveredAt == null && order.CreatedAt <= cutoff)))
            .ExecuteUpdateAsync(update => update.SetProperty(order => order.Status, OrderStatus.Completed));
    }

    public Task SaveAsync() => db.SaveChangesAsync();
}
