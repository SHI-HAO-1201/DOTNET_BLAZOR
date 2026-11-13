using System.Security.Claims;
using BlazorXe_Xemay.Features.Shared;
using Microsoft.EntityFrameworkCore;

namespace BlazorXe_Xemay.Features.TaiKhoan;

public sealed class DichVuKhachHang(IDbContextFactory<AppDbContext> dbFactory)
{
    public async Task<List<DiaChiKhachHang>> LayDiaChiAsync(int userId)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        return await db.DiaChiKhachHangs.AsNoTracking()
            .Where(x => x.NguoiDungId == userId)
            .OrderByDescending(x => x.MacDinh)
            .ThenBy(x => x.Id)
            .ToListAsync();
    }

    public async Task LuuDiaChiAsync(int userId, DiaChiKhachHang address)
    {
        if (string.IsNullOrWhiteSpace(address.NguoiNhan)
            || string.IsNullOrWhiteSpace(address.SoDienThoai)
            || string.IsNullOrWhiteSpace(address.DiaChi))
        {
            throw new InvalidOperationException("Người nhận, số điện thoại và địa chỉ là các trường bắt buộc.");
        }

        await using var db = await dbFactory.CreateDbContextAsync();
        address.Id = 0;
        address.NguoiDungId = userId;
        if (address.MacDinh)
        {
            var existingAddresses = await db.DiaChiKhachHangs
                .Where(x => x.NguoiDungId == userId)
                .ToListAsync();
            existingAddresses.ForEach(x => x.MacDinh = false);
        }

        db.DiaChiKhachHangs.Add(address);
        await db.SaveChangesAsync();
    }

    public async Task XoaDiaChiAsync(int userId, int addressId)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var address = await db.DiaChiKhachHangs.SingleOrDefaultAsync(x =>
            x.Id == addressId && x.NguoiDungId == userId);
        if (address is null)
        {
            throw new InvalidOperationException("Không tìm thấy địa chỉ trong sổ địa chỉ của bạn.");
        }

        db.DiaChiKhachHangs.Remove(address);
        await db.SaveChangesAsync();
    }

    public async Task GuiYeuCauBaoHanhAsync(int userId, int productId, string description)
    {
        if (string.IsNullOrWhiteSpace(description))
        {
            throw new InvalidOperationException("Vui lòng mô tả vấn đề cần bảo hành.");
        }

        await using var db = await dbFactory.CreateDbContextAsync();
        var completedOrderId = await db.DonHangs
            .Where(order => order.NguoiDungId == userId && order.TrangThai == "HoanThanh")
            .Where(order => db.ChiTietDonHangs.Any(item =>
                item.DonHangId == order.Id && item.XeMayId == productId))
            .OrderByDescending(order => order.NgayTao)
            .Select(order => (int?)order.Id)
            .FirstOrDefaultAsync();
        if (!completedOrderId.HasValue)
        {
            throw new InvalidOperationException("Chỉ xe có trong đơn hàng đã hoàn thành của bạn mới được yêu cầu bảo hành.");
        }

        db.YeuCauBaoHanhs.Add(new YeuCauBaoHanh
        {
            NguoiDungId = userId,
            DonHangId = completedOrderId.Value,
            XeMayId = productId,
            MoTa = description.Trim()
        });
        await db.SaveChangesAsync();
    }
}
