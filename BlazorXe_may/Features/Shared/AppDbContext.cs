using BlazorXe_Xemay.Features.TaiKhoan;
using BlazorXe_Xemay.Features.XeMay.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using DonHangEntity = BlazorXe_Xemay.Features.TaiKhoan.DonHang;

namespace BlazorXe_Xemay.Features.Shared;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options)
    : IdentityDbContext<AppUser, IdentityRole<int>, int>(options)
{
    public DbSet<ChiNhanh> ChiNhanhs => Set<ChiNhanh>();
    public DbSet<DiaChiKhachHang> DiaChiKhachHangs => Set<DiaChiKhachHang>();
    public DbSet<QuyenHeThong> QuyenHeThongs => Set<QuyenHeThong>();
    public DbSet<GanQuyenVaiTro> GanQuyenVaiTros => Set<GanQuyenVaiTro>();
    public DbSet<PhamViNguoiDung> PhamViNguoiDungs => Set<PhamViNguoiDung>();
    public DbSet<XeMayModel> XeMays => Set<XeMayModel>();
    public DbSet<NhaCungCap> NhaCungCaps => Set<NhaCungCap>();
    public DbSet<BienDongKho> BienDongKhos => Set<BienDongKho>();
    public DbSet<TonKhoChiNhanh> TonKhoChiNhanhs => Set<TonKhoChiNhanh>();
    public DbSet<DonHangEntity> DonHangs => Set<DonHangEntity>();
    public DbSet<ChiTietDonHang> ChiTietDonHangs => Set<ChiTietDonHang>();
    public DbSet<GiaoDichThanhToan> GiaoDichThanhToans => Set<GiaoDichThanhToan>();
    public DbSet<KhuyenMai> KhuyenMais => Set<KhuyenMai>();
    public DbSet<YeuCauBaoHanh> YeuCauBaoHanhs => Set<YeuCauBaoHanh>();
    public DbSet<PhieuHoTro> PhieuHoTros => Set<PhieuHoTro>();
    public DbSet<DanhGiaXe> DanhGiaXes => Set<DanhGiaXe>();
    public DbSet<BaiViet> BaiViets => Set<BaiViet>();
    public DbSet<ThongBaoNguoiDung> ThongBaoNguoiDungs => Set<ThongBaoNguoiDung>();
    public DbSet<NhatKyHeThong> NhatKyHeThongs => Set<NhatKyHeThong>();
    public DbSet<CauHinhCuaHang> CauHinhCuaHangs => Set<CauHinhCuaHang>();
    public DbSet<XeYeuThich> XeYeuThichs => Set<XeYeuThich>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<ChiNhanh>().HasIndex(x => x.MaChiNhanh).IsUnique();
        builder.Entity<NhaCungCap>().HasIndex(x => x.MaNhaCungCap).IsUnique();
        builder.Entity<DonHangEntity>().HasIndex(x => x.MaDonHang).IsUnique();
        builder.Entity<QuyenHeThong>().HasIndex(x => x.MaQuyen).IsUnique();
        builder.Entity<KhuyenMai>().HasIndex(x => x.MaKhuyenMai).IsUnique();
        builder.Entity<TonKhoChiNhanh>().HasIndex(x => new { x.XeMayId, x.ChiNhanhId }).IsUnique();
        builder.Entity<GanQuyenVaiTro>().HasIndex(x => new { x.VaiTroId, x.QuyenId }).IsUnique();
        builder.Entity<PhamViNguoiDung>().HasIndex(x => new { x.NguoiDungId, x.ChiNhanhId }).IsUnique();
        builder.Entity<XeYeuThich>().HasIndex(x => new { x.NguoiDungId, x.XeMayId }).IsUnique();
        builder.Entity<XeMayModel>().Property(x => x.GiaBan).HasPrecision(18, 2);
        builder.Entity<XeMayModel>().ToTable("XeMays", table => table.UseSqlOutputClause(false));
        builder.Entity<XeMayModel>().Property(x => x.GiaNiemYet).HasPrecision(18, 2);
        builder.Entity<DonHangEntity>().Property(x => x.TongTien).HasPrecision(18, 2);
        builder.Entity<DonHangEntity>().Property(x => x.TamTinh).HasPrecision(18, 2);
        builder.Entity<DonHangEntity>().Property(x => x.GiamGia).HasPrecision(18, 2);
        builder.Entity<ChiTietDonHang>().Property(x => x.DonGia).HasPrecision(18, 2);
        builder.Entity<GiaoDichThanhToan>().Property(x => x.SoTien).HasPrecision(18, 2);
        builder.Entity<KhuyenMai>().Property(x => x.GiaTri).HasPrecision(18, 2);
        builder.Entity<KhuyenMai>().Property(x => x.DonToiThieu).HasPrecision(18, 2);
    }
}
