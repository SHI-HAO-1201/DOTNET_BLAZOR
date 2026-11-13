using BlazorXe_Xemay.Features.TaiKhoan;
using Microsoft.EntityFrameworkCore;

namespace BlazorXe_Xemay.Features.Shared;

public sealed class DichVuNhatKy(
    IDbContextFactory<AppDbContext> dbFactory,
    IHttpContextAccessor httpContextAccessor)
{
    public async Task GhiAsync(int? userId, string action, string entity = "", string detail = "")
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var ip = httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString() ?? string.Empty;
        db.NhatKyHeThongs.Add(new NhatKyHeThong
        {
            NguoiDungId = userId,
            HanhDong = action,
            DoiTuong = entity,
            ChiTiet = detail,
            DiaChiIp = ip
        });
        await db.SaveChangesAsync();
    }
}
