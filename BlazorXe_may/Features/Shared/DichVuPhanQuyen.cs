using System.Security.Claims;
using BlazorXe_Xemay.Features.TaiKhoan;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace BlazorXe_Xemay.Features.Shared;

public sealed class DichVuPhanQuyen(
    IDbContextFactory<AppDbContext> dbFactory,
    UserManager<AppUser> userManager)
{
    public async Task<bool> CoQuyenAsync(ClaimsPrincipal principal, string maQuyen)
    {
        if (principal.Identity?.IsAuthenticated != true)
        {
            return false;
        }

        if (principal.IsInRole("QuanTriVien"))
        {
            return true;
        }

        var user = await userManager.GetUserAsync(principal);
        if (user is null || !user.DangHoatDong)
        {
            return false;
        }

        var roleNames = await userManager.GetRolesAsync(user);
        if (roleNames.Count == 0)
        {
            return false;
        }

        await using var db = await dbFactory.CreateDbContextAsync();
        var roleIds = await db.Roles
            .Where(x => roleNames.Contains(x.Name!))
            .Select(x => x.Id)
            .ToListAsync();

        return await db.GanQuyenVaiTros
            .Join(db.QuyenHeThongs, assignment => assignment.QuyenId, permission => permission.Id,
                (assignment, permission) => new { assignment.VaiTroId, permission.MaQuyen })
            .AnyAsync(x => roleIds.Contains(x.VaiTroId) && x.MaQuyen == maQuyen);
    }

    public async Task YeuCauQuyenAsync(ClaimsPrincipal principal, string maQuyen)
    {
        if (!await CoQuyenAsync(principal, maQuyen))
        {
            throw new UnauthorizedAccessException("Tài khoản không được cấp quyền thực hiện thao tác này.");
        }
    }

    public Task<bool> CoPhamViToanHeThongAsync(ClaimsPrincipal principal)
    {
        var isGlobal = principal.Identity?.IsAuthenticated == true
            && (principal.IsInRole("QuanTriVien") || principal.IsInRole("ChuCuaHang"));
        return Task.FromResult(isGlobal);
    }

    public async Task<List<int>> LayChiNhanhDuocCapAsync(ClaimsPrincipal principal)
    {
        if (await CoPhamViToanHeThongAsync(principal))
        {
            await using var allBranchesDb = await dbFactory.CreateDbContextAsync();
            return await allBranchesDb.ChiNhanhs.Where(x => x.DangHoatDong)
                .Select(x => x.Id).ToListAsync();
        }

        var user = await userManager.GetUserAsync(principal);
        if (user is null || !user.DangHoatDong)
        {
            return [];
        }

        await using var db = await dbFactory.CreateDbContextAsync();
        var branchIds = await db.PhamViNguoiDungs
            .Where(x => x.NguoiDungId == user.Id)
            .Join(db.ChiNhanhs.Where(x => x.DangHoatDong), scope => scope.ChiNhanhId, branch => branch.Id,
                (scope, branch) => scope.ChiNhanhId)
            .ToListAsync();
        if (user.ChiNhanhId.HasValue)
        {
            if (await db.ChiNhanhs.AnyAsync(x => x.Id == user.ChiNhanhId.Value && x.DangHoatDong))
            {
                branchIds.Add(user.ChiNhanhId.Value);
            }
        }

        return branchIds.Distinct().ToList();
    }

    public async Task<List<QuyenHeThong>> LayDanhSachQuyenAsync()
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        return await db.QuyenHeThongs.AsNoTracking().OrderBy(x => x.Nhom).ThenBy(x => x.TenQuyen).ToListAsync();
    }

    public async Task GanQuyenAsync(int roleId, int permissionId, bool allowed)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var roleExists = await db.Roles.AnyAsync(x => x.Id == roleId);
        var permissionExists = await db.QuyenHeThongs.AnyAsync(x => x.Id == permissionId);
        if (!roleExists || !permissionExists)
        {
            throw new InvalidOperationException("Vai trò hoặc quyền không tồn tại.");
        }

        var assignment = await db.GanQuyenVaiTros
            .SingleOrDefaultAsync(x => x.VaiTroId == roleId && x.QuyenId == permissionId);
        if (allowed && assignment is null)
        {
            db.GanQuyenVaiTros.Add(new GanQuyenVaiTro { VaiTroId = roleId, QuyenId = permissionId });
        }
        else if (!allowed && assignment is not null)
        {
            db.GanQuyenVaiTros.Remove(assignment);
        }

        await db.SaveChangesAsync();
    }

    public async Task GanPhamViChiNhanhAsync(int userId, int branchId, bool allowed)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        if (!await db.Users.AnyAsync(x => x.Id == userId)
            || !await db.ChiNhanhs.AnyAsync(x => x.Id == branchId && x.DangHoatDong))
        {
            throw new InvalidOperationException("Tài khoản hoặc chi nhánh không tồn tại.");
        }

        var assignment = await db.PhamViNguoiDungs.SingleOrDefaultAsync(x =>
            x.NguoiDungId == userId && x.ChiNhanhId == branchId);
        if (allowed && assignment is null)
        {
            db.PhamViNguoiDungs.Add(new PhamViNguoiDung { NguoiDungId = userId, ChiNhanhId = branchId });
        }
        else if (!allowed && assignment is not null)
        {
            db.PhamViNguoiDungs.Remove(assignment);
        }

        await db.SaveChangesAsync();
    }
}
