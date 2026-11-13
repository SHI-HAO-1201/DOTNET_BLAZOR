using BlazorXe_Xemay.Features.Shared;
using BlazorXe_Xemay.Features.TaiKhoan;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using DonHangEntity = BlazorXe_Xemay.Features.TaiKhoan.DonHang;

namespace BlazorXe_Xemay.Features.Admin;

public sealed record DashboardThongKe(
    int SoXe,
    int DonChoXacNhan,
    int KhachHang,
    int YeuCauBaoHanh,
    decimal DoanhThu,
    IReadOnlyList<DonHangEntity> DonHangMoiNhat);

public sealed class DichVuQuanTri(
    IDbContextFactory<AppDbContext> dbFactory,
    UserManager<AppUser> userManager,
    RoleManager<IdentityRole<int>> roleManager,
    DichVuPhanQuyen permissionService)
{
    public async Task<DashboardThongKe> LayThongKeAsync(IReadOnlyCollection<int>? branchIds = null)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var orders = db.DonHangs.AsQueryable();
        if (branchIds is not null)
        {
            orders = orders.Where(x => branchIds.Contains(x.ChiNhanhId ?? 0));
        }

        var completedOrders = orders.Where(x => x.TrangThai == "HoanThanh");
        var customerCount = branchIds is null
            ? await userManager.Users.CountAsync()
            : await orders.Select(x => x.NguoiDungId).Distinct().CountAsync();
        var warrantyRequests = db.YeuCauBaoHanhs.AsQueryable();
        if (branchIds is not null)
        {
            warrantyRequests = warrantyRequests.Where(x => x.DonHangId.HasValue
                && orders.Any(order => order.Id == x.DonHangId.Value));
        }

        var productCount = branchIds is null
            ? await db.XeMays.CountAsync(x => x.DangKinhDoanh)
            : await db.TonKhoChiNhanhs
                .Where(x => branchIds.Contains(x.ChiNhanhId) && x.SoLuong > 0)
                .Select(x => x.XeMayId).Distinct()
                .Join(db.XeMays.Where(x => x.DangKinhDoanh), id => id, product => product.Id, (id, _) => id)
                .CountAsync();

        return new DashboardThongKe(
            productCount,
            await orders.CountAsync(x => x.TrangThai == "ChoXacNhan"),
            customerCount,
            await warrantyRequests.CountAsync(x => x.TrangThai != "HoanThanh"),
            await completedOrders.SumAsync(x => (decimal?)x.TongTien) ?? 0,
            await orders.AsNoTracking().Include(x => x.ChiTiet)
                .OrderByDescending(x => x.NgayTao).Take(10).ToListAsync());
    }

    public async Task<List<DonHangEntity>> LayDonHangAsync(IReadOnlyCollection<int>? branchIds = null)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var orders = db.DonHangs.AsQueryable();
        if (branchIds is not null)
        {
            orders = orders.Where(x => branchIds.Contains(x.ChiNhanhId ?? 0));
        }

        return await orders.AsNoTracking().Include(x => x.ChiTiet)
            .OrderByDescending(x => x.NgayTao).ToListAsync();
    }

    public async Task CapNhatTrangThaiDonHangAsync(int orderId, string status)
    {
        var validStatuses = new HashSet<string>(StringComparer.Ordinal)
        {
            "ChoXacNhan", "ChoThanhToan", "DangXuLy", "DangGiao", "HoanThanh", "DaHuy", "DoiTra"
        };
        if (!validStatuses.Contains(status))
        {
            throw new InvalidOperationException("Trạng thái đơn hàng không hợp lệ.");
        }

        await using var db = await dbFactory.CreateDbContextAsync();
        var order = await db.DonHangs.SingleOrDefaultAsync(x => x.Id == orderId)
            ?? throw new InvalidOperationException("Không tìm thấy đơn hàng.");
        if (order.TrangThai == "DaHuy" || order.TrangThai == "HoanThanh")
        {
            throw new InvalidOperationException("Đơn hàng đã kết thúc, không thể thay đổi trạng thái.");
        }

        var transitions = new Dictionary<string, string[]>
        {
            ["ChoXacNhan"] = ["DangXuLy", "DaHuy"],
            ["ChoThanhToan"] = ["DangXuLy", "DaHuy"],
            ["DangXuLy"] = ["DangGiao", "DaHuy"],
            ["DangGiao"] = ["HoanThanh", "DoiTra"],
            ["DoiTra"] = ["DangXuLy", "DaHuy"]
        };
        if (!transitions.TryGetValue(order.TrangThai, out var allowed)
            || !allowed.Contains(status, StringComparer.Ordinal))
        {
            throw new InvalidOperationException("Trạng thái tiếp theo không phù hợp với quy trình hiện tại.");
        }

        if (status == "DaHuy")
        {
            throw new InvalidOperationException("Dùng thao tác hủy đơn để hoàn tồn kho an toàn.");
        }

        order.TrangThai = status;
        if (status == "HoanThanh")
        {
            var payment = await db.GiaoDichThanhToans.SingleOrDefaultAsync(x => x.DonHangId == orderId);
            if (payment is not null)
            {
                payment.TrangThai = "DaThuTien";
                payment.NgayThanhToan = DateTime.UtcNow;
            }
        }

        await db.SaveChangesAsync();
    }

    public async Task<List<AppUser>> LayNhanSuAsync()
    {
        var users = await userManager.Users.AsNoTracking().OrderBy(x => x.Email).ToListAsync();
        return users;
    }

    public async Task GanVaiTroAsync(int operatorId, int userId, string roleName, bool assign)
    {
        var role = await roleManager.FindByNameAsync(roleName);
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (role is null || user is null)
        {
            throw new InvalidOperationException("Không tìm thấy tài khoản hoặc vai trò.");
        }

        if (user.Id == operatorId)
        {
            throw new InvalidOperationException("Không thể thay đổi vai trò của chính tài khoản đang đăng nhập.");
        }

        if (!assign && roleName == "QuanTriVien")
        {
            var admins = await userManager.GetUsersInRoleAsync(roleName);
            if (admins.Count <= 1)
            {
                throw new InvalidOperationException("Hệ thống phải duy trì ít nhất một quản trị viên.");
            }
        }

        var result = assign
            ? await userManager.AddToRoleAsync(user, roleName)
            : await userManager.RemoveFromRoleAsync(user, roleName);
        if (!result.Succeeded)
        {
            throw new InvalidOperationException(string.Join("; ", result.Errors.Select(x => x.Description)));
        }
    }

    public async Task<List<IdentityRole<int>>> LayVaiTrosAsync()
    {
        return await roleManager.Roles.AsNoTracking().OrderBy(x => x.Name).ToListAsync();
    }

    public Task GanQuyenAsync(int roleId, int permissionId, bool allowed) =>
        permissionService.GanQuyenAsync(roleId, permissionId, allowed);
}
