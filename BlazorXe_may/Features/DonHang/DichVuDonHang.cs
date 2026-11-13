using System.Security.Claims;
using BlazorXe_Xemay.Features.Shared;
using BlazorXe_Xemay.Features.TaiKhoan;
using Microsoft.EntityFrameworkCore;
using DonHangEntity = BlazorXe_Xemay.Features.TaiKhoan.DonHang;

namespace BlazorXe_Xemay.Features.DonHang;

public sealed record KhuyenMaiDuocApDung(string TenChuongTrinh, decimal SoTienGiam, decimal TamTinh);

public sealed class DichVuDonHang(IDbContextFactory<AppDbContext> dbFactory)
{
    public async Task<KhuyenMaiDuocApDung> KiemTraMaKhuyenMaiAsync(
        IReadOnlyDictionary<int, int> gioHang,
        string? maKhuyenMai)
    {
        if (gioHang.Count == 0 || string.IsNullOrWhiteSpace(maKhuyenMai))
        {
            throw new InvalidOperationException("Hãy nhập mã khuyến mãi và thêm xe vào giỏ hàng.");
        }

        await using var db = await dbFactory.CreateDbContextAsync();
        var productIds = gioHang.Keys.ToList();
        var products = await db.XeMays.AsNoTracking()
            .Where(x => productIds.Contains(x.Id) && x.DangKinhDoanh)
            .ToListAsync();
        if (products.Count != productIds.Count || gioHang.Any(item => item.Value <= 0))
        {
            throw new InvalidOperationException("Giỏ hàng có xe không còn kinh doanh hoặc số lượng không hợp lệ.");
        }

        var subtotal = products.Sum(x => x.GiaBan * gioHang[x.Id]);
        var (promotion, discount) = await TinhKhuyenMaiAsync(db, maKhuyenMai, subtotal);
        return new KhuyenMaiDuocApDung(promotion.TenKhuyenMai, discount, subtotal);
    }

    public async Task<DonHangEntity> DatHangAsync(
        ClaimsPrincipal principal,
        IReadOnlyDictionary<int, int> gioHang,
        string nguoiNhan,
        string soDienThoai,
        string diaChi,
        string ghiChu,
        string? maKhuyenMai,
        int chiNhanhId)
    {
        var userIdValue = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdValue, out var userId) || gioHang.Count == 0)
        {
            throw new InvalidOperationException("Hãy đăng nhập và thêm xe vào giỏ hàng trước khi đặt hàng.");
        }

        if (string.IsNullOrWhiteSpace(nguoiNhan) || string.IsNullOrWhiteSpace(soDienThoai)
            || string.IsNullOrWhiteSpace(diaChi))
        {
            throw new InvalidOperationException("Vui lòng nhập đầy đủ người nhận, số điện thoại và địa chỉ giao xe.");
        }

        if (gioHang.Any(item => item.Key <= 0 || item.Value <= 0))
        {
            throw new InvalidOperationException("Giỏ hàng có thông tin số lượng không hợp lệ.");
        }

        await using var db = await dbFactory.CreateDbContextAsync();
        await using var transaction = await db.Database.BeginTransactionAsync();
        var productIds = gioHang.Keys.ToList();
        var products = await db.XeMays
            .Where(x => productIds.Contains(x.Id) && x.DangKinhDoanh)
            .ToListAsync();
        if (products.Count != productIds.Count)
        {
            throw new InvalidOperationException("Một hoặc nhiều xe trong giỏ không còn kinh doanh.");
        }

        foreach (var product in products)
        {
            var orderedQuantity = gioHang[product.Id];
            if (product.SoLuong < orderedQuantity)
            {
                throw new InvalidOperationException($"Xe {product.TenXe} hiện không đủ số lượng trong kho.");
            }
        }

        var subtotal = products.Sum(x => x.GiaBan * gioHang[x.Id]);
        var discount = string.IsNullOrWhiteSpace(maKhuyenMai)
            ? 0
            : (await TinhKhuyenMaiAsync(db, maKhuyenMai, subtotal)).SoTienGiam;
        if (!await db.ChiNhanhs.AnyAsync(x => x.Id == chiNhanhId && x.DangHoatDong))
        {
            throw new InvalidOperationException("Vui lòng chọn một chi nhánh đang hoạt động.");
        }

        foreach (var product in products)
        {
            var stock = await db.TonKhoChiNhanhs.SingleOrDefaultAsync(x =>
                x.XeMayId == product.Id && x.ChiNhanhId == chiNhanhId);
            if (stock is null || stock.SoLuong < gioHang[product.Id])
            {
                throw new InvalidOperationException($"Chi nhánh đã chọn không còn đủ số lượng xe {product.TenXe}.");
            }

            stock.SoLuong -= gioHang[product.Id];
        }

        var order = new DonHangEntity
        {
            MaDonHang = $"MH-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid():N}"[..24].ToUpperInvariant(),
            NguoiDungId = userId,
            ChiNhanhId = chiNhanhId,
            TrangThai = "ChoXacNhan",
            PhuongThucThanhToan = "COD",
            NguoiNhan = nguoiNhan.Trim(),
            SoDienThoaiNhan = soDienThoai.Trim(),
            DiaChiNhan = diaChi.Trim(),
            GhiChu = ghiChu.Trim(),
            TamTinh = subtotal,
            GiamGia = discount,
            TongTien = subtotal - discount,
            ChiTiet = products.Select(product => new ChiTietDonHang
            {
                XeMayId = product.Id,
                TenXe = product.TenXe,
                DonGia = product.GiaBan,
                SoLuong = gioHang[product.Id]
            }).ToList()
        };

        db.DonHangs.Add(order);
        db.GiaoDichThanhToans.Add(new GiaoDichThanhToan
        {
            DonHang = order,
            PhuongThuc = "COD",
            TrangThai = "ChoThuTien",
            SoTien = order.TongTien
        });

        foreach (var product in products)
        {
            var quantity = gioHang[product.Id];
            product.SoLuong -= quantity;
            db.BienDongKhos.Add(new BienDongKho
            {
                XeMayId = product.Id,
                ChiNhanhId = chiNhanhId,
                SoLuongThayDoi = -quantity,
                LoaiGiaoDich = "BanHang",
                GhiChu = $"Giữ tồn kho cho đơn {order.MaDonHang}",
                NguoiTaoId = userId
            });
        }

        db.ThongBaoNguoiDungs.Add(new ThongBaoNguoiDung
        {
            NguoiDungId = userId,
            TieuDe = "Đã tiếp nhận đơn hàng",
            NoiDung = $"Đơn {order.MaDonHang} đã được ghi nhận. Thanh toán khi nhận xe (COD)."
        });
        await db.SaveChangesAsync();
        await transaction.CommitAsync();
        return order;
    }

    private static async Task<(KhuyenMai Promotion, decimal SoTienGiam)> TinhKhuyenMaiAsync(
        AppDbContext db,
        string maKhuyenMai,
        decimal subtotal)
    {
        var normalizedCode = maKhuyenMai.Trim().ToUpperInvariant();
        var now = DateTime.UtcNow;
        var promotion = await db.KhuyenMais.SingleOrDefaultAsync(x =>
            x.MaKhuyenMai == normalizedCode && x.DangHoatDong
            && x.BatDau <= now && x.KetThuc >= now);
        if (promotion is null)
        {
            throw new InvalidOperationException("Mã khuyến mãi không hợp lệ hoặc đã hết hạn.");
        }

        if (subtotal < promotion.DonToiThieu)
        {
            throw new InvalidOperationException("Đơn hàng chưa đạt giá trị tối thiểu để sử dụng mã.");
        }

        var discount = promotion.TheoPhanTram
            ? decimal.Round(subtotal * promotion.GiaTri / 100, 0, MidpointRounding.AwayFromZero)
            : promotion.GiaTri;
        return (promotion, Math.Clamp(discount, 0, subtotal));
    }

    public async Task<List<DonHangEntity>> LayDonHangCuaNguoiDungAsync(int userId)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        return await db.DonHangs.AsNoTracking()
            .Include(x => x.ChiTiet)
            .Where(x => x.NguoiDungId == userId)
            .OrderByDescending(x => x.NgayTao)
            .ToListAsync();
    }

    public async Task<List<DonHangEntity>> LayTatCaDonHangAsync()
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        return await db.DonHangs.AsNoTracking()
            .Include(x => x.ChiTiet)
            .OrderByDescending(x => x.NgayTao)
            .ToListAsync();
    }

    public async Task HuyDonAsync(int orderId, int userId, bool staff)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        await using var transaction = await db.Database.BeginTransactionAsync();
        var order = await db.DonHangs.Include(x => x.ChiTiet)
            .SingleOrDefaultAsync(x => x.Id == orderId && (staff || x.NguoiDungId == userId))
            ?? throw new InvalidOperationException("Không tìm thấy đơn hàng hoặc bạn không có quyền truy cập.");

        if (order.TrangThai != "ChoXacNhan")
        {
            throw new InvalidOperationException("Chỉ có thể hủy đơn hàng đang chờ xác nhận.");
        }

        foreach (var item in order.ChiTiet)
        {
            var product = await db.XeMays.SingleAsync(x => x.Id == item.XeMayId);
            product.SoLuong += item.SoLuong;
            if (order.ChiNhanhId.HasValue)
            {
                var branchStock = await db.TonKhoChiNhanhs.SingleOrDefaultAsync(x =>
                    x.XeMayId == product.Id && x.ChiNhanhId == order.ChiNhanhId.Value);
                if (branchStock is null)
                {
                    db.TonKhoChiNhanhs.Add(new TonKhoChiNhanh
                    {
                        XeMayId = product.Id,
                        ChiNhanhId = order.ChiNhanhId.Value,
                        SoLuong = item.SoLuong
                    });
                }
                else
                {
                    branchStock.SoLuong += item.SoLuong;
                }

                db.BienDongKhos.Add(new BienDongKho
                {
                    XeMayId = item.XeMayId,
                    ChiNhanhId = order.ChiNhanhId.Value,
                    SoLuongThayDoi = item.SoLuong,
                    LoaiGiaoDich = "HuyDon",
                    GhiChu = $"Hoàn kho đơn {order.MaDonHang}",
                    NguoiTaoId = userId
                });
            }
        }

        order.TrangThai = "DaHuy";
        await db.SaveChangesAsync();
        await transaction.CommitAsync();
    }
}
