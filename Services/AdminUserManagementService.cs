using System.Data;
using Fauzora.Data;
using Fauzora.Models;
using Fauzora.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Fauzora.Services;

public sealed class AdminUserManagementService(
    AppDbContext db,
    UserManager<ApplicationUser> users,
    RoleManager<IdentityRole> roles,
    ILogger<AdminUserManagementService> logger)
{
    private static readonly string[] ManagedRoles = ["Admin", "Seller", "Customer"];

    public async Task<AdminUserListVm> GetUsersAsync(string? search)
    {
        var normalizedSearch = search?.Trim();
        var query = db.Users.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(normalizedSearch))
        {
            var pattern = $"%{EscapeLike(normalizedSearch)}%";
            query = query.Where(user => EF.Functions.ILike(user.Email ?? "", pattern, "\\") ||
                                        EF.Functions.ILike(user.FullName, pattern, "\\"));
        }

        var accountRows = await query
            .OrderBy(user => user.Email)
            .Select(user => new { user.Id, user.FullName, user.Email, user.IsActive })
            .Take(250)
            .ToListAsync();
        var accountIds = accountRows.Select(account => account.Id).ToArray();
        var roleRows = await (from membership in db.UserRoles.AsNoTracking()
                              join identityRole in db.Roles.AsNoTracking() on membership.RoleId equals identityRole.Id
                              where accountIds.Contains(membership.UserId)
                              select new { membership.UserId, identityRole.Name })
            .ToListAsync();
        var rolesByUser = roleRows
            .GroupBy(row => row.UserId)
            .ToDictionary(group => group.Key, group => group.Select(row => row.Name).ToHashSet(StringComparer.Ordinal));
        var result = new List<AdminUserRowVm>(accountRows.Count);
        foreach (var account in accountRows)
        {
            var accountRoles = rolesByUser.GetValueOrDefault(account.Id) ?? [];
            var role = ManagedRoles.FirstOrDefault(accountRoles.Contains) ?? "-";
            result.Add(new AdminUserRowVm(account.Id, account.FullName, account.Email ?? "-", role, account.IsActive));
        }

        return new AdminUserListVm { Search = normalizedSearch ?? "", Users = result };
    }

    public async Task<ServiceResult> SetRoleAsync(string targetUserId, string actorUserId, string? requestedRole)
    {
        var role = string.IsNullOrWhiteSpace(requestedRole) ? null : requestedRole.Trim();
        if (role is not null && !ManagedRoles.Contains(role, StringComparer.Ordinal))
            return ServiceResult.Fail("Role yang dipilih tidak valid.");
        if (targetUserId == actorUserId)
            return ServiceResult.Fail("Admin tidak dapat mengubah role akunnya sendiri.");

        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        try
        {
            var target = await users.FindByIdAsync(targetUserId);
            if (target is null) return ServiceResult.Fail("Akun tidak ditemukan.");

            var currentRoles = await users.GetRolesAsync(target);
            var isAdmin = currentRoles.Contains("Admin", StringComparer.Ordinal);
            if (isAdmin && role != "Admin")
            {
                var adminCount = await db.UserRoles.Join(db.Roles,
                        userRole => userRole.RoleId,
                        identityRole => identityRole.Id,
                        (userRole, identityRole) => new { userRole.UserId, identityRole.Name })
                    .CountAsync(item => item.Name == "Admin");
                if (adminCount <= 1)
                    return ServiceResult.Fail("Role Admin tidak dapat dihapus dari Admin terakhir.");
            }

            foreach (var currentRole in currentRoles.Where(currentRole => ManagedRoles.Contains(currentRole, StringComparer.Ordinal)))
            {
                if (currentRole == role) continue;
                var removeResult = await users.RemoveFromRoleAsync(target, currentRole);
                if (!removeResult.Succeeded) return FailIdentity("Role lama gagal dilepas.", removeResult.Errors);
            }

            if (role is not null && !currentRoles.Contains(role, StringComparer.Ordinal))
            {
                if (!await roles.RoleExistsAsync(role))
                    return ServiceResult.Fail("Role tujuan tidak tersedia.");
                var addResult = await users.AddToRoleAsync(target, role);
                if (!addResult.Succeeded) return FailIdentity("Role baru gagal diberikan.", addResult.Errors);
            }

            if (role == "Admin" && !target.IsActive)
                target.IsActive = true;
            target.SecurityStamp = Guid.NewGuid().ToString("N");
            var update = await users.UpdateAsync(target);
            if (!update.Succeeded) return FailIdentity("Perubahan akun gagal disimpan.", update.Errors);

            await transaction.CommitAsync();
            return ServiceResult.Ok("Role akun berhasil diperbarui.");
        }
        catch (Exception ex) when (ex is DbUpdateException or PostgresException)
        {
            await transaction.RollbackAsync();
            logger.LogWarning(ex, "Role change transaction failed for user {UserId}", targetUserId);
            return ServiceResult.Fail("Perubahan role bertabrakan dengan perubahan lain. Muat ulang halaman lalu coba kembali.");
        }
    }

    public async Task<ServiceResult> SetActiveAsync(string targetUserId, string actorUserId, bool isActive)
    {
        if (targetUserId == actorUserId)
            return ServiceResult.Fail("Admin tidak dapat menonaktifkan atau mengubah status akunnya sendiri.");

        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        try
        {
            var target = await users.FindByIdAsync(targetUserId);
            if (target is null) return ServiceResult.Fail("Akun tidak ditemukan.");
            var currentRoles = await users.GetRolesAsync(target);
            if (currentRoles.Contains("Admin", StringComparer.Ordinal) && !isActive)
                return ServiceResult.Fail("Akun Admin tidak dapat dinonaktifkan dari halaman ini.");

            target.IsActive = isActive;
            target.SecurityStamp = Guid.NewGuid().ToString("N");
            var update = await users.UpdateAsync(target);
            if (!update.Succeeded) return FailIdentity("Status akun gagal diperbarui.", update.Errors);
            await transaction.CommitAsync();
            return ServiceResult.Ok(isActive ? "Akun berhasil diaktifkan." : "Akun berhasil dinonaktifkan.");
        }
        catch (Exception ex) when (ex is DbUpdateException or PostgresException)
        {
            await transaction.RollbackAsync();
            logger.LogWarning(ex, "Account status change transaction failed for user {UserId}", targetUserId);
            return ServiceResult.Fail("Perubahan status bertabrakan dengan perubahan lain. Muat ulang halaman lalu coba kembali.");
        }
    }

    private static ServiceResult FailIdentity(string message, IEnumerable<IdentityError> errors) =>
        ServiceResult.Fail($"{message} {string.Join(" ", errors.Select(error => error.Description))}");

    private static string EscapeLike(string value) => value.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_");
}
