using Fauzora.Data;
using Fauzora.Models;
using Fauzora.Repositories;
using Fauzora.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace Fauzora.Services;

public class OrderService(AppDbContext db, OrderRepository orders)
{
    // Service pesanan mengatur checkout, status pengiriman, konfirmasi penerimaan, review, dan return.
    public async Task<List<Order>> GetForCustomerAsync(string userId)
    {
        await CompleteExpiredDeliveredAsync();
        return await orders.GetForCustomerAsync(userId);
    }

    public async Task<List<Order>> GetAllAsync()
    {
        await CompleteExpiredDeliveredAsync();
        return await orders.GetAllAsync();
    }

    public Task<List<Order>> GetForSellerAsync(string sellerId) => orders.GetForSellerAsync(sellerId);
    public Task<Order?> GetForSellerAsync(int id, string sellerId) => orders.FindForSellerAsync(id, sellerId);
    public Task<List<Order>> GetReturnRequestsForSellerAsync(string sellerId) => orders.GetReturnRequestsForSellerAsync(sellerId);

    public async Task<ServiceResult> SellerUpdateStatusAsync(int id, string sellerId, string status)
    {
        var order = await orders.FindForSellerAsync(id, sellerId);
        if (order is null) return ServiceResult.Fail("Pesanan tidak ditemukan.");
        if (!OrderStatus.All.Contains(status))
            return ServiceResult.Fail("Status pesanan tidak valid.");

        order.Status = status;
        if (status == OrderStatus.Delivered)
            order.DeliveredAt ??= DateTime.UtcNow;
        await orders.SaveAsync();
        return ServiceResult.Ok(status == OrderStatus.Delivered
            ? "Status pesanan diperbarui. Customer dapat mengonfirmasi penerimaan atau mengajukan pengembalian."
            : "Status pesanan berhasil diperbarui.");
    }

    public async Task<ServiceResult> SellerUpdateReturnStatusAsync(int id, string sellerId, string returnStatus, string? note)
    {
        var order = await orders.FindForSellerAsync(id, sellerId);
        if (order is null) return ServiceResult.Fail("Pesanan tidak ditemukan.");
        if (order.ReturnStatus != ReturnRequestStatus.Requested)
            return ServiceResult.Fail("Pengajuan ini sudah diproses atau belum diajukan.");
        if (returnStatus != ReturnRequestStatus.Approved && returnStatus != ReturnRequestStatus.Rejected)
            return ServiceResult.Fail("Status pengembalian tidak valid.");
        if (note?.Length > 500)
            return ServiceResult.Fail("Catatan keputusan maksimal 500 karakter.");

        order.ReturnStatus = returnStatus;
        order.ReturnAdminNote = string.IsNullOrWhiteSpace(note) ? null : note.Trim();
        order.ReturnDecisionAt = DateTime.UtcNow;
        await orders.SaveAsync();
        return ServiceResult.Ok(returnStatus == ReturnRequestStatus.Approved
            ? "Pengembalian disetujui. Refund tetap diproses manual oleh admin."
            : "Pengajuan pengembalian ditolak.");
    }

    public Task<Order?> GetForCustomerDetailsAsync(int id, string userId) => orders.FindForCustomerWithItemsAsync(id, userId);
    public Task<List<Order>> GetReturnRequestsAsync() => orders.GetReturnRequestsAsync();

    public Task<int> CompleteExpiredDeliveredAsync() =>
        orders.CompleteExpiredDeliveredAsync(DateTime.UtcNow.AddDays(-7));

    public async Task<ServiceResult> ConfirmReceivedAsync(int id, string userId)
    {
        // Delivered -> Completed hanya boleh dilakukan customer pemilik pesanan.
        var order = await orders.FindForCustomerAsync(id, userId);
        if (order is null) return ServiceResult.Fail("Pesanan tidak ditemukan.");
        if (order.Status != OrderStatus.Delivered)
            return ServiceResult.Fail("Pesanan belum dapat dikonfirmasi sebagai diterima.");

        order.Status = OrderStatus.Completed;
        await orders.SaveAsync();
        return ServiceResult.Ok("Pesanan dikonfirmasi sudah diterima. Silakan berikan review produk.");
    }

    public async Task<ServiceResult> RequestReturnAsync(ReturnRequestVm form, string userId)
    {
        // Return dapat dimulai saat Delivered, sebelum customer memilih Pesanan Diterima.
        var order = await orders.FindForCustomerAsync(form.OrderId, userId);
        if (order is null) return ServiceResult.Fail("Pesanan tidak ditemukan.");
        if (order.Status != OrderStatus.Delivered && order.Status != OrderStatus.Completed)
            return ServiceResult.Fail("Pengembalian hanya dapat diajukan setelah pesanan sampai.");
        if (order.ReturnStatus != ReturnRequestStatus.None)
            return ServiceResult.Fail("Pengajuan pengembalian untuk pesanan ini sudah ada.");

        order.ReturnStatus = ReturnRequestStatus.Requested;
        order.ReturnReason = form.Reason.Trim();
        order.ReturnRequestedAt = DateTime.UtcNow;
        await orders.SaveAsync();
        return ServiceResult.Ok("Pengajuan pengembalian berhasil dikirim dan sedang diperiksa.");
    }

    public async Task<ServiceResult> DecideReturnAsync(int id, bool approved, string? adminNote)
    {
        // Admin mengubah Requested menjadi Approved/Rejected dan menyimpan catatan keputusan.
        var order = await orders.FindWithCustomerAsync(id);
        if (order is null) return ServiceResult.Fail("Pesanan tidak ditemukan.");
        if (order.ReturnStatus != ReturnRequestStatus.Requested)
            return ServiceResult.Fail("Pengajuan ini sudah diproses atau belum diajukan.");

        order.ReturnStatus = approved ? ReturnRequestStatus.Approved : ReturnRequestStatus.Rejected;
        order.ReturnAdminNote = string.IsNullOrWhiteSpace(adminNote) ? null : adminNote.Trim();
        order.ReturnDecisionAt = DateTime.UtcNow;
        await orders.SaveAsync();
        return ServiceResult.Ok(approved
            ? "Pengembalian disetujui. Admin dapat memproses refund secara manual."
            : "Pengajuan pengembalian ditolak.");
    }

    public async Task<ServiceResult> UpdateStatusAsync(int id, string status)
    {
        // Hanya status yang ada di OrderStatus.All yang boleh masuk database.
        if (!OrderStatus.All.Contains(status)) return ServiceResult.Fail("Status tidak valid.");
        var order = await orders.FindAsync(id);
        if (order is null) return ServiceResult.Fail("Pesanan tidak ditemukan.");

        order.Status = status;
        if (status == OrderStatus.Delivered && order.DeliveredAt is null)
            order.DeliveredAt = DateTime.UtcNow;
        await orders.SaveAsync();
        return ServiceResult.Ok("Status pesanan diperbarui.");
    }

    public async Task<ServiceResult> CheckoutAsync(string userId, IReadOnlyCollection<int> selectedCartItemIds)
    {
        // Checkout hanya memproses item terpilih yang memang dimiliki user; item lain tetap di cart.
        // Validasi, pengurangan stok, pembuatan order, dan penghapusan item terpilih atomik.
        // Transaksi otomatis di-rollback saat dispose jika return sebelum CommitAsync.
        var selectedIds = selectedCartItemIds.Distinct().ToArray();
        if (selectedIds.Length == 0)
            return ServiceResult.Fail("Pilih minimal satu barang untuk checkout.");

        await using var tx = await db.Database.BeginTransactionAsync();
        var cart = await db.CartItems.Include(x => x.Product).ThenInclude(product => product!.Seller)
            .Where(x => x.UserId == userId && selectedIds.Contains(x.Id)).ToListAsync();

        if (cart.Count != selectedIds.Length)
            return ServiceResult.Fail("Pilihan barang tidak valid. Muat ulang keranjang dan coba lagi.");
        if (cart.Any(x => x.Product is null || !x.Product.IsActive || x.Quantity <= 0 || x.Product.Stock < x.Quantity))
            return ServiceResult.Fail("Ada produk yang stoknya tidak mencukupi.");
        if (cart.Any(x => string.IsNullOrWhiteSpace(x.Product!.SellerId) || x.Product.Seller is null))
            return ServiceResult.Fail("Ada produk yang tidak memiliki seller valid. Hubungi admin.");

        var customer = await db.Users.FirstOrDefaultAsync(user => user.Id == userId);
        if (customer is null)
            return ServiceResult.Fail("Akun customer tidak ditemukan. Silakan login kembali.");
        if (!HasCustomerAddress(customer))
            return ServiceResult.Fail("Lengkapi alamat pengiriman di halaman Profil sebelum checkout.");

        var sellerIds = cart.Select(item => item.Product!.SellerId).Distinct().ToArray();
        var sellers = cart.Select(item => item.Product!.Seller!).DistinctBy(seller => seller.Id).ToDictionary(seller => seller.Id);
        var sellerWithoutAddress = sellers.Values.FirstOrDefault(seller => !HasSellerAddress(seller));
        if (sellerWithoutAddress is not null)
            return ServiceResult.Fail($"Toko {sellerWithoutAddress.FullName} belum melengkapi alamat asal pengiriman. Checkout tidak dapat dilanjutkan.");

        var storeNames = await db.Users
            .Where(user => sellerIds.Contains(user.Id))
            .Select(user => new
            {
                user.Id,
                StoreName = db.SellerApplications.Where(application => application.UserId == user.Id && application.Status == SellerApplicationStatus.Approved)
                    .Select(application => application.StoreName).FirstOrDefault() ?? user.FullName
            })
            .ToDictionaryAsync(user => user.Id, user => user.StoreName);
        var checkoutGroupId = Guid.NewGuid();
        var groupedOrders = cart.GroupBy(item => item.Product!.SellerId).Select(group =>
        {
            var seller = group.First().Product!.Seller!;
            return new Order
            {
                CustomerId = userId,
                SellerId = seller.Id,
                SellerName = storeNames.GetValueOrDefault(seller.Id, seller.FullName),
                CheckoutGroupId = checkoutGroupId,
                CustomerAddressContactName = customer.CustomerAddressContactName,
                CustomerAddressPhone = customer.CustomerAddressPhone,
                CustomerAddressStreet = customer.CustomerAddressStreet,
                CustomerAddressVillage = customer.CustomerAddressVillage,
                CustomerAddressDistrict = customer.CustomerAddressDistrict,
                CustomerAddressCity = customer.CustomerAddressCity,
                CustomerAddressProvince = customer.CustomerAddressProvince,
                CustomerAddressPostalCode = customer.CustomerAddressPostalCode,
                CustomerAddressDeliveryNote = customer.CustomerAddressDeliveryNote,
                SellerAddressContactName = seller.SellerAddressContactName,
                SellerAddressPhone = seller.SellerAddressPhone,
                SellerAddressStreet = seller.SellerAddressStreet,
                SellerAddressVillage = seller.SellerAddressVillage,
                SellerAddressDistrict = seller.SellerAddressDistrict,
                SellerAddressCity = seller.SellerAddressCity,
                SellerAddressProvince = seller.SellerAddressProvince,
                SellerAddressPostalCode = seller.SellerAddressPostalCode,
                SellerAddressDeliveryNote = seller.SellerAddressDeliveryNote,
                // Checkout pembayaran demo dianggap sudah dibayar; seller dapat langsung memproses order.
                Status = OrderStatus.Processing
            };
        }).ToDictionary(order => order.SellerId!);

        foreach (var item in cart)
        {
            var product = item.Product!;
            var quantity = item.Quantity;

            // Pengurangan stok bersyarat di database agar dua checkout bersamaan tidak membuat stok minus.
            var affected = await db.Products
                .Where(p => p.Id == product.Id && p.IsActive && p.Stock >= quantity)
                .ExecuteUpdateAsync(s => s.SetProperty(p => p.Stock, p => p.Stock - quantity));
            if (affected == 0)
                return ServiceResult.Fail("Ada produk yang stoknya tidak mencukupi.");

            var order = groupedOrders[product.SellerId];
            order.Items.Add(new OrderItem
            {
                ProductId = product.Id,
                ProductName = product.Name,
                Quantity = item.Quantity,
                UnitPrice = product.Price
            });
            order.Total += product.Price * item.Quantity;
        }

        db.Orders.AddRange(groupedOrders.Values);
        db.CartItems.RemoveRange(cart);
        await db.SaveChangesAsync();
        await tx.CommitAsync();
        var orderNumbers = string.Join(", ", groupedOrders.Values.Select(order => order.OrderNumber));
        return ServiceResult.Ok($"Pesanan {orderNumbers} berhasil dibuat. Pembayaran masih mode demo.");
    }

    private static bool HasCustomerAddress(ApplicationUser user) =>
        HasCompleteAddress(user.CustomerAddressContactName, user.CustomerAddressPhone, user.CustomerAddressStreet,
            user.CustomerAddressVillage, user.CustomerAddressDistrict, user.CustomerAddressCity,
            user.CustomerAddressProvince, user.CustomerAddressPostalCode);

    private static bool HasSellerAddress(ApplicationUser user) =>
        HasCompleteAddress(user.SellerAddressContactName, user.SellerAddressPhone, user.SellerAddressStreet,
            user.SellerAddressVillage, user.SellerAddressDistrict, user.SellerAddressCity,
            user.SellerAddressProvince, user.SellerAddressPostalCode);

    private static bool HasCompleteAddress(params string?[] fields) =>
        fields.All(field => !string.IsNullOrWhiteSpace(field));
}
