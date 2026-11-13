using BlazorXe_Xemay.Features.Shared;
using BlazorXe_Xemay.Features.XeMay.Models;
using Microsoft.EntityFrameworkCore;

namespace BlazorXe_Xemay.Features.XeMay.Services;

public sealed class XeMayService(IDbContextFactory<AppDbContext> dbFactory)
{
    public async Task<List<XeMayModel>> LayDanhSachXeAsync(
        string? tuKhoa = null,
        string? hangXe = null,
        decimal? giaTu = null,
        decimal? giaDen = null,
        string? sapXep = null)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var query = db.XeMays.AsNoTracking().Where(x => x.DangKinhDoanh);

        if (!string.IsNullOrWhiteSpace(tuKhoa))
        {
            var keyword = tuKhoa.Trim();
            query = query.Where(x => x.TenXe.Contains(keyword) || x.HangXe.Contains(keyword) || x.MaXe.Contains(keyword));
        }

        if (!string.IsNullOrWhiteSpace(hangXe))
        {
            query = query.Where(x => x.HangXe == hangXe);
        }

        if (giaTu.HasValue)
        {
            query = query.Where(x => x.GiaBan >= giaTu.Value);
        }

        if (giaDen.HasValue)
        {
            query = query.Where(x => x.GiaBan <= giaDen.Value);
        }

        query = sapXep switch
        {
            "gia-tang" => query.OrderBy(x => x.GiaBan),
            "gia-giam" => query.OrderByDescending(x => x.GiaBan),
            "ten" => query.OrderBy(x => x.TenXe),
            _ => query.OrderByDescending(x => x.NgayTao)
        };

        return await query.ToListAsync();
    }

    public async Task<List<XeMayModel>> LayTatCaXeAsync()
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        return await db.XeMays.AsNoTracking().OrderBy(x => x.TenXe).ToListAsync();
    }

    public async Task<XeMayModel?> LayTheoIdAsync(int id)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        return await db.XeMays.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id);
    }

    public async Task ThemXeAsync(XeMayModel xe)
    {
        KiemTraXe(xe);
        await using var db = await dbFactory.CreateDbContextAsync();
        if (string.IsNullOrWhiteSpace(xe.MaXe))
        {
            xe.MaXe = $"XE-{Guid.NewGuid():N}"[..11].ToUpperInvariant();
        }

        xe.NgayTao = DateTime.UtcNow;
        db.XeMays.Add(xe);
        await db.SaveChangesAsync();
        if (xe.SoLuong > 0)
        {
            var branchId = await db.ChiNhanhs.Where(x => x.DangHoatDong)
                .Select(x => (int?)x.Id).FirstOrDefaultAsync();
            if (branchId.HasValue)
            {
                db.TonKhoChiNhanhs.Add(new BlazorXe_Xemay.Features.TaiKhoan.TonKhoChiNhanh
                {
                    XeMayId = xe.Id,
                    ChiNhanhId = branchId.Value,
                    SoLuong = xe.SoLuong
                });
                await db.SaveChangesAsync();
            }
        }
    }

    public async Task CapNhatXeAsync(XeMayModel xe)
    {
        KiemTraXe(xe);
        await using var db = await dbFactory.CreateDbContextAsync();
        var existing = await db.XeMays.SingleOrDefaultAsync(x => x.Id == xe.Id)
            ?? throw new InvalidOperationException("Không tìm thấy xe máy cần cập nhật.");

        existing.MaXe = xe.MaXe;
        existing.TenXe = xe.TenXe;
        existing.HangXe = xe.HangXe;
        existing.LoaiXe = xe.LoaiXe;
        existing.PhienBan = xe.PhienBan;
        existing.GiaNiemYet = xe.GiaNiemYet;
        existing.GiaBan = xe.GiaBan;
        existing.HinhAnh = xe.HinhAnh;
        existing.MoTa = xe.MoTa;
        existing.MauSac = xe.MauSac;
        existing.NamSanXuat = xe.NamSanXuat;
        existing.NhienLieu = xe.NhienLieu;
        existing.PhanKho = xe.PhanKho;
        existing.TinhTrang = xe.TinhTrang;
        existing.BaoHanhThang = xe.BaoHanhThang;
        existing.DangKinhDoanh = xe.DangKinhDoanh;
        await db.SaveChangesAsync();
    }

    public async Task XoaXeAsync(int id)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var xe = await db.XeMays.SingleOrDefaultAsync(x => x.Id == id)
            ?? throw new InvalidOperationException("Không tìm thấy xe máy cần ngừng kinh doanh.");
        xe.DangKinhDoanh = false;
        await db.SaveChangesAsync();
    }

    public async Task MoKinhDoanhAsync(int id)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var xe = await db.XeMays.SingleOrDefaultAsync(x => x.Id == id)
            ?? throw new InvalidOperationException("Không tìm thấy xe máy cần mở bán.");
        xe.DangKinhDoanh = true;
        await db.SaveChangesAsync();
    }

    public async Task DieuChinhKhoAsync(
        int xeId,
        int chiNhanhId,
        int thayDoi,
        string loaiGiaoDich,
        string ghiChu,
        int? nguoiTaoId,
        int? nhaCungCapId = null)
    {
        if (thayDoi == 0)
        {
            throw new ArgumentOutOfRangeException(nameof(thayDoi), "Số lượng thay đổi phải khác 0.");
        }

        await using var db = await dbFactory.CreateDbContextAsync();
        await using var transaction = await db.Database.BeginTransactionAsync();
        var xe = await db.XeMays.SingleOrDefaultAsync(x => x.Id == xeId)
            ?? throw new InvalidOperationException("Không tìm thấy xe máy.");
        if (!await db.ChiNhanhs.AnyAsync(x => x.Id == chiNhanhId && x.DangHoatDong))
        {
            throw new InvalidOperationException("Không tìm thấy chi nhánh đang hoạt động.");
        }

        if (nhaCungCapId.HasValue && !await db.NhaCungCaps.AnyAsync(x =>
                x.Id == nhaCungCapId.Value && x.DangHoatDong))
        {
            throw new InvalidOperationException("Không tìm thấy nhà cung cấp đang hoạt động.");
        }

        var nextQuantity = xe.SoLuong + thayDoi;
        if (nextQuantity < 0)
        {
            throw new InvalidOperationException("Tồn kho không đủ để ghi nhận giao dịch xuất.");
        }

        var branchStock = await db.TonKhoChiNhanhs
            .SingleOrDefaultAsync(x => x.XeMayId == xeId && x.ChiNhanhId == chiNhanhId);
        var branchQuantity = (branchStock?.SoLuong ?? 0) + thayDoi;
        if (branchQuantity < 0)
        {
            throw new InvalidOperationException("Chi nhánh không có đủ tồn kho cho giao dịch xuất.");
        }

        xe.SoLuong = nextQuantity;
        if (branchStock is null)
        {
            branchStock = new BlazorXe_Xemay.Features.TaiKhoan.TonKhoChiNhanh
            {
                XeMayId = xeId,
                ChiNhanhId = chiNhanhId,
                SoLuong = branchQuantity
            };
            db.TonKhoChiNhanhs.Add(branchStock);
        }
        else
        {
            branchStock.SoLuong = branchQuantity;
        }
        db.BienDongKhos.Add(new BlazorXe_Xemay.Features.TaiKhoan.BienDongKho
        {
            XeMayId = xeId,
            ChiNhanhId = chiNhanhId,
            SoLuongThayDoi = thayDoi,
            LoaiGiaoDich = loaiGiaoDich,
            GhiChu = ghiChu,
            NhaCungCapId = nhaCungCapId,
            NguoiTaoId = nguoiTaoId
        });
        await db.SaveChangesAsync();
        await transaction.CommitAsync();
    }

    public async Task ChuyenKhoAsync(int xeId, int tuChiNhanhId, int denChiNhanhId, int soLuong, int? nguoiTaoId)
    {
        if (soLuong <= 0 || tuChiNhanhId == denChiNhanhId)
        {
            throw new InvalidOperationException("Chọn hai chi nhánh khác nhau và số lượng lớn hơn 0.");
        }

        await using var db = await dbFactory.CreateDbContextAsync();
        await using var transaction = await db.Database.BeginTransactionAsync();
        var product = await db.XeMays.SingleOrDefaultAsync(x => x.Id == xeId && x.DangKinhDoanh)
            ?? throw new InvalidOperationException("Không tìm thấy xe đang kinh doanh.");
        if (await db.ChiNhanhs.CountAsync(x => x.DangHoatDong
                && (x.Id == tuChiNhanhId || x.Id == denChiNhanhId)) != 2)
        {
            throw new InvalidOperationException("Chi nhánh nguồn hoặc đích không hoạt động.");
        }

        var source = await db.TonKhoChiNhanhs.SingleOrDefaultAsync(x =>
            x.XeMayId == xeId && x.ChiNhanhId == tuChiNhanhId);
        if (source is null || source.SoLuong < soLuong)
        {
            throw new InvalidOperationException("Chi nhánh nguồn không có đủ xe để chuyển.");
        }

        var destination = await db.TonKhoChiNhanhs.SingleOrDefaultAsync(x =>
            x.XeMayId == xeId && x.ChiNhanhId == denChiNhanhId);
        source.SoLuong -= soLuong;
        if (destination is null)
        {
            destination = new BlazorXe_Xemay.Features.TaiKhoan.TonKhoChiNhanh
            {
                XeMayId = xeId,
                ChiNhanhId = denChiNhanhId,
                SoLuong = soLuong
            };
            db.TonKhoChiNhanhs.Add(destination);
        }
        else
        {
            destination.SoLuong += soLuong;
        }

        db.BienDongKhos.AddRange(
            new BlazorXe_Xemay.Features.TaiKhoan.BienDongKho
            {
                XeMayId = xeId, ChiNhanhId = tuChiNhanhId,
                SoLuongThayDoi = -soLuong, LoaiGiaoDich = "ChuyenKho",
                GhiChu = $"Chuyển đến chi nhánh {denChiNhanhId}", NguoiTaoId = nguoiTaoId
            },
            new BlazorXe_Xemay.Features.TaiKhoan.BienDongKho
            {
                XeMayId = xeId, ChiNhanhId = denChiNhanhId,
                SoLuongThayDoi = soLuong, LoaiGiaoDich = "ChuyenKho",
                GhiChu = $"Nhận từ chi nhánh {tuChiNhanhId}", NguoiTaoId = nguoiTaoId
            });
        await db.SaveChangesAsync();
        await transaction.CommitAsync();
    }

    private static void KiemTraXe(XeMayModel xe)
    {
        if (string.IsNullOrWhiteSpace(xe.TenXe) || string.IsNullOrWhiteSpace(xe.HangXe))
        {
            throw new InvalidOperationException("Tên xe và hãng xe là bắt buộc.");
        }

        if (xe.GiaBan < 0 || xe.GiaNiemYet < 0 || xe.SoLuong < 0)
        {
            throw new InvalidOperationException("Giá bán, giá niêm yết và số lượng không được âm.");
        }
    }
}
