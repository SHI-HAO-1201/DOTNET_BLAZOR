using System.ComponentModel.DataAnnotations;

namespace BlazorXe_Xemay.Features.TaiKhoan;

public sealed class ChiNhanh
{
    public int Id { get; set; }
    [Required, MaxLength(32)] public string MaChiNhanh { get; set; } = string.Empty;
    [Required, MaxLength(160)] public string TenChiNhanh { get; set; } = string.Empty;
    [MaxLength(320)] public string DiaChi { get; set; } = string.Empty;
    [MaxLength(32)] public string SoDienThoai { get; set; } = string.Empty;
    public bool DangHoatDong { get; set; } = true;
}

public sealed class DiaChiKhachHang
{
    public int Id { get; set; }
    public int NguoiDungId { get; set; }
    [Required, MaxLength(80)] public string NguoiNhan { get; set; } = string.Empty;
    [Required, MaxLength(32)] public string SoDienThoai { get; set; } = string.Empty;
    [Required, MaxLength(320)] public string DiaChi { get; set; } = string.Empty;
    public bool MacDinh { get; set; }
}

public sealed class QuyenHeThong
{
    public int Id { get; set; }
    [Required, MaxLength(100)] public string MaQuyen { get; set; } = string.Empty;
    [Required, MaxLength(160)] public string TenQuyen { get; set; } = string.Empty;
    [MaxLength(80)] public string Nhom { get; set; } = string.Empty;
}

public sealed class GanQuyenVaiTro
{
    public int Id { get; set; }
    public int VaiTroId { get; set; }
    public int QuyenId { get; set; }
}

public sealed class PhamViNguoiDung
{
    public int Id { get; set; }
    public int NguoiDungId { get; set; }
    public int ChiNhanhId { get; set; }
}

public sealed class NhaCungCap
{
    public int Id { get; set; }
    [Required, MaxLength(32)] public string MaNhaCungCap { get; set; } = string.Empty;
    [Required, MaxLength(160)] public string TenNhaCungCap { get; set; } = string.Empty;
    [MaxLength(160)] public string NguoiLienHe { get; set; } = string.Empty;
    [MaxLength(32)] public string SoDienThoai { get; set; } = string.Empty;
    [MaxLength(320)] public string Email { get; set; } = string.Empty;
    [MaxLength(320)] public string DiaChi { get; set; } = string.Empty;
    public bool DangHoatDong { get; set; } = true;
}

public sealed class BienDongKho
{
    public int Id { get; set; }
    public int XeMayId { get; set; }
    public int ChiNhanhId { get; set; }
    public int? NhaCungCapId { get; set; }
    public int SoLuongThayDoi { get; set; }
    [Required, MaxLength(40)] public string LoaiGiaoDich { get; set; } = string.Empty;
    [MaxLength(320)] public string GhiChu { get; set; } = string.Empty;
    public DateTime NgayTao { get; set; } = DateTime.UtcNow;
    public int? NguoiTaoId { get; set; }
}

public sealed class TonKhoChiNhanh
{
    public int Id { get; set; }
    public int XeMayId { get; set; }
    public int ChiNhanhId { get; set; }
    public int SoLuong { get; set; }
}

public sealed class DonHang
{
    public int Id { get; set; }
    [Required, MaxLength(32)] public string MaDonHang { get; set; } = string.Empty;
    public int NguoiDungId { get; set; }
    public int? ChiNhanhId { get; set; }
    [Required, MaxLength(40)] public string TrangThai { get; set; } = "ChoXacNhan";
    [Required, MaxLength(40)] public string PhuongThucThanhToan { get; set; } = "COD";
    [MaxLength(80)] public string NguoiNhan { get; set; } = string.Empty;
    [MaxLength(32)] public string SoDienThoaiNhan { get; set; } = string.Empty;
    [MaxLength(320)] public string DiaChiNhan { get; set; } = string.Empty;
    [MaxLength(500)] public string GhiChu { get; set; } = string.Empty;
    public decimal TamTinh { get; set; }
    public decimal GiamGia { get; set; }
    public decimal TongTien { get; set; }
    public DateTime NgayTao { get; set; } = DateTime.UtcNow;
    public List<ChiTietDonHang> ChiTiet { get; set; } = [];
}

public sealed class ChiTietDonHang
{
    public int Id { get; set; }
    public int DonHangId { get; set; }
    public int XeMayId { get; set; }
    [Required, MaxLength(160)] public string TenXe { get; set; } = string.Empty;
    public decimal DonGia { get; set; }
    public int SoLuong { get; set; }
}

public sealed class GiaoDichThanhToan
{
    public int Id { get; set; }
    public int DonHangId { get; set; }
    public DonHang? DonHang { get; set; }
    [Required, MaxLength(40)] public string PhuongThuc { get; set; } = "COD";
    [Required, MaxLength(40)] public string TrangThai { get; set; } = "ChoThuTien";
    public decimal SoTien { get; set; }
    public DateTime NgayTao { get; set; } = DateTime.UtcNow;
    public DateTime? NgayThanhToan { get; set; }
}

public sealed class KhuyenMai
{
    public int Id { get; set; }
    [Required, MaxLength(40)] public string MaKhuyenMai { get; set; } = string.Empty;
    [Required, MaxLength(160)] public string TenKhuyenMai { get; set; } = string.Empty;
    public decimal GiaTri { get; set; }
    public bool TheoPhanTram { get; set; }
    public decimal DonToiThieu { get; set; }
    public DateTime BatDau { get; set; }
    public DateTime KetThuc { get; set; }
    public bool DangHoatDong { get; set; } = true;
}

public sealed class YeuCauBaoHanh
{
    public int Id { get; set; }
    public int NguoiDungId { get; set; }
    public int? DonHangId { get; set; }
    public int? XeMayId { get; set; }
    [Required, MaxLength(500)] public string MoTa { get; set; } = string.Empty;
    [Required, MaxLength(40)] public string TrangThai { get; set; } = "MoiTiepNhan";
    public DateTime NgayTao { get; set; } = DateTime.UtcNow;
}

public sealed class PhieuHoTro
{
    public int Id { get; set; }
    public int? NguoiDungId { get; set; }
    [Required, MaxLength(160)] public string TieuDe { get; set; } = string.Empty;
    [Required, MaxLength(2000)] public string NoiDung { get; set; } = string.Empty;
    [Required, MaxLength(40)] public string TrangThai { get; set; } = "Moi";
    public DateTime NgayTao { get; set; } = DateTime.UtcNow;
}

public sealed class DanhGiaXe
{
    public int Id { get; set; }
    public int XeMayId { get; set; }
    public int NguoiDungId { get; set; }
    [Range(1, 5)] public int SoSao { get; set; }
    [MaxLength(1000)] public string NoiDung { get; set; } = string.Empty;
    public DateTime NgayTao { get; set; } = DateTime.UtcNow;
}

public sealed class BaiViet
{
    public int Id { get; set; }
    [Required, MaxLength(180)] public string TieuDe { get; set; } = string.Empty;
    [MaxLength(400)] public string TomTat { get; set; } = string.Empty;
    [MaxLength(10000)] public string NoiDung { get; set; } = string.Empty;
    [MaxLength(40)] public string Loai { get; set; } = "TinTuc";
    public bool DaXuatBan { get; set; }
    public DateTime NgayTao { get; set; } = DateTime.UtcNow;
}

public sealed class ThongBaoNguoiDung
{
    public int Id { get; set; }
    public int? NguoiDungId { get; set; }
    [Required, MaxLength(160)] public string TieuDe { get; set; } = string.Empty;
    [MaxLength(1000)] public string NoiDung { get; set; } = string.Empty;
    public bool DaDoc { get; set; }
    public DateTime NgayTao { get; set; } = DateTime.UtcNow;
}

public sealed class NhatKyHeThong
{
    public long Id { get; set; }
    public int? NguoiDungId { get; set; }
    [Required, MaxLength(80)] public string HanhDong { get; set; } = string.Empty;
    [MaxLength(160)] public string DoiTuong { get; set; } = string.Empty;
    [MaxLength(1000)] public string ChiTiet { get; set; } = string.Empty;
    [MaxLength(64)] public string DiaChiIp { get; set; } = string.Empty;
    public DateTime NgayTao { get; set; } = DateTime.UtcNow;
}

public sealed class CauHinhCuaHang
{
    [Key, MaxLength(100)] public string Khoa { get; set; } = string.Empty;
    [MaxLength(1000)] public string GiaTri { get; set; } = string.Empty;
}

public sealed class XeYeuThich
{
    public int Id { get; set; }
    public int NguoiDungId { get; set; }
    public int XeMayId { get; set; }
    public DateTime NgayTao { get; set; } = DateTime.UtcNow;
}
