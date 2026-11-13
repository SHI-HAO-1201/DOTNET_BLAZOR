using Microsoft.AspNetCore.Identity;

namespace BlazorXe_Xemay.Features.TaiKhoan;

public sealed class AppUser : IdentityUser<int>
{
    public string HoTen { get; set; } = string.Empty;
    public bool DangHoatDong { get; set; } = true;
    public int? ChiNhanhId { get; set; }
    public DateTime NgayTao { get; set; } = DateTime.UtcNow;
}
