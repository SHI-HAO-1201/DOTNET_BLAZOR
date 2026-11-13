using System.ComponentModel.DataAnnotations;

namespace BlazorXe_Xemay.Features.XeMay.Models;

public sealed class XeMayModel
{
    public int Id { get; set; }

    [MaxLength(32)]
    public string MaXe { get; set; } = string.Empty;

    [Required, MaxLength(160)]
    public string TenXe { get; set; } = string.Empty;

    [Required, MaxLength(80)]
    public string HangXe { get; set; } = string.Empty;

    [MaxLength(80)]
    public string LoaiXe { get; set; } = "Xe so";

    [MaxLength(100)]
    public string PhienBan { get; set; } = string.Empty;

    [Range(typeof(decimal), "0", "999999999999")]
    public decimal GiaNiemYet { get; set; }

    [Range(typeof(decimal), "0", "999999999999")]
    public decimal GiaBan { get; set; }

    [Range(0, int.MaxValue)]
    public int SoLuong { get; set; }

    [MaxLength(1000)]
    public string HinhAnh { get; set; } = string.Empty;

    [MaxLength(4000)]
    public string MoTa { get; set; } = string.Empty;

    [MaxLength(40)]
    public string MauSac { get; set; } = string.Empty;

    public int? NamSanXuat { get; set; }

    [MaxLength(80)]
    public string NhienLieu { get; set; } = "Xang";

    public int? PhanKho { get; set; }

    [MaxLength(40)]
    public string TinhTrang { get; set; } = "Moi";

    public int BaoHanhThang { get; set; } = 12;

    public bool DangKinhDoanh { get; set; } = true;

    public DateTime NgayTao { get; set; } = DateTime.UtcNow;
}