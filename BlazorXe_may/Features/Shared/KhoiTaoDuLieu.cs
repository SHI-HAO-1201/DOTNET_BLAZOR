using BlazorXe_Xemay.Features.TaiKhoan;
using BlazorXe_Xemay.Features.XeMay.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace BlazorXe_Xemay.Features.Shared;

public static class KhoiTaoDuLieu
{
    private static readonly IReadOnlyDictionary<string, string> HinhAnhXeTheoMa =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["HON-SH150"] = "https://thumb.wikimedia.org/wikipedia/commons/thumb/0/06/Honda_SH_150i_2026.jpg/960px-Honda_SH_150i_2026.jpg",
            ["YAM-EX155"] = "https://yamaha-motor.com.vn/wp/wp-content/uploads/2025/04/Exciter-155VVA-STD-Red-004-1024x768.png",
            ["HON-VAR160"] = "https://thumb.wikimedia.org/wikipedia/commons/thumb/d/dd/Vario_160_2023.png/960px-Vario_160_2023.png",
            ["HON-LEAD125"] = "https://thumb.wikimedia.org/wikipedia/commons/thumb/5/57/2026_Honda_Lead_125_ABS.jpg/960px-2026_Honda_Lead_125_ABS.jpg",
            ["HON-FUT125"] = "https://thumb.wikimedia.org/wikipedia/commons/thumb/0/0e/Future_125_FI.jpg/960px-Future_125_FI.jpg",
            ["HON-CBR150"] = "https://cdn.honda.com.vn/motorbikes/October2024/eZBStod5l7EoGjGslDKa.png",
            ["HON-CBR650"] = "https://thumb.wikimedia.org/wikipedia/commons/thumb/4/47/2023_Honda_CBR650R_Standard.jpg/960px-2023_Honda_CBR650R_Standard.jpg",
            ["HON-CUB125"] = "https://cdn.honda.com.vn/motorbikes/September2026/uWAHDZTCHaGc3zCAvAvR.png",
            ["YAM-GRAND"] = "https://yamaha-motor.com.vn/wp/wp-content/uploads/2025/12/Grande_Limited_Bac-Nham_G4-1-1024x860.png",
            ["YAM-JANUS"] = "https://yamaha-motor.com.vn/wp/wp-content/uploads/2024/10/Ja-Limited-2024-Mat-Cyan-004-1024x860.png",
            ["YAM-NVX155"] = "https://yamaha-motor.com.vn/wp/wp-content/uploads/2024/02/NVX-Pearl-Blue-004-1024x860.png",
            ["YAM-FREEGO"] = "https://yamaha-motor.com.vn/wp/wp-content/uploads/2025/08/FreeGo_Sport-B4UC_Den-Do-Bong_Goc-4-1024x860.png",
            ["YAM-MT15"] = "https://yamaha-motor.com.vn/wp/wp-content/uploads/2023/06/2022_MTN155_DPBMC_IDN_MT15_BLUE-004-1024x860.png",
            ["YAM-R15V4"] = "https://yamaha-motor.com.vn/wp/wp-content/uploads/2022/09/R15-GP-004-1-1024x860.png",
            ["SUZ-BURGMAN"] = "https://thumb.wikimedia.org/wikipedia/commons/thumb/1/17/2024_Suzuki_Burgman_Street_125_GL.jpg/960px-2024_Suzuki_Burgman_Street_125_GL.jpg",
            ["SUZ-SATRIA"] = "https://d2i5n8imv7zpm3.cloudfront.net/models/XE619/FU150RF/colors/Std-Red1_1783062779290.png",
            ["PIA-LIBERTY"] = "https://thumb.wikimedia.org/wikipedia/commons/thumb/0/0b/Piaggio_Liberty_125_in_Salerno.jpg/960px-Piaggio_Liberty_125_in_Salerno.jpg",
            ["VES-PRIM125"] = "https://thumb.wikimedia.org/wikipedia/commons/thumb/6/6f/Piaggio_Vespa_Primavera_125.jpg/500px-Piaggio_Vespa_Primavera_125.jpg"
        };

    private static readonly string[] VaiTros =
    [
        "QuanTriVien", "ChuCuaHang", "BanHang", "Kho", "KeToan",
        "BaoHanh", "QuanLy", "NhanVien", "User"
    ];

    private static readonly (string Ma, string Ten, string Nhom)[] QuyenMau =
    [
        ("products.view", "Xem xe máy", "Sản phẩm"),
        ("products.manage", "Quản lý xe máy", "Sản phẩm"),
        ("inventory.view", "Xem tồn kho", "Kho"),
        ("inventory.manage", "Điều chỉnh tồn kho", "Kho"),
        ("orders.view", "Xem đơn hàng", "Đơn hàng"),
        ("orders.manage", "Xử lý đơn hàng", "Đơn hàng"),
        ("customers.view", "Xem khách hàng", "Khách hàng"),
        ("customers.manage", "Quản lý khách hàng", "Khách hàng"),
        ("finance.view", "Xem tài chính", "Tài chính"),
        ("warranty.manage", "Xử lý bảo hành", "Bảo hành"),
        ("reports.view", "Xem báo cáo", "Báo cáo"),
        ("roles.manage", "Quản lý vai trò và quyền", "Phân quyền"),
        ("settings.manage", "Quản lý cấu hình", "Cấu hình")
    ];

    public static async Task KhoiTaoAsync(IServiceProvider services)
    {
        var factory = services.GetRequiredService<IDbContextFactory<AppDbContext>>();
        await using var db = await factory.CreateDbContextAsync();
        await KhoiTaoSchemaAsync(db);

        var roleManager = services.GetRequiredService<RoleManager<IdentityRole<int>>>();
        foreach (var roleName in VaiTros)
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                var result = await roleManager.CreateAsync(new IdentityRole<int>(roleName));
                if (!result.Succeeded)
                {
                    throw new InvalidOperationException($"Không thể tạo vai trò {roleName}: {string.Join("; ", result.Errors.Select(x => x.Description))}");
                }
            }
        }

        foreach (var (ma, ten, nhom) in QuyenMau)
        {
            var permission = await db.QuyenHeThongs.SingleOrDefaultAsync(x => x.MaQuyen == ma);
            if (permission is null)
            {
                permission = new QuyenHeThong { MaQuyen = ma, TenQuyen = ten, Nhom = nhom };
                db.QuyenHeThongs.Add(permission);
                await db.SaveChangesAsync();
            }

            var adminRole = await roleManager.FindByNameAsync("QuanTriVien")
                ?? throw new InvalidOperationException("Không tìm thấy vai trò quản trị viên.");
            if (!await db.GanQuyenVaiTros.AnyAsync(x => x.VaiTroId == adminRole.Id && x.QuyenId == permission.Id))
            {
                db.GanQuyenVaiTros.Add(new GanQuyenVaiTro { VaiTroId = adminRole.Id, QuyenId = permission.Id });
            }
        }

        var rolePermissions = new Dictionary<string, string[]>
        {
            ["ChuCuaHang"] = QuyenMau.Select(x => x.Ma).ToArray(),
            ["QuanLy"] = ["products.view", "products.manage", "inventory.view", "inventory.manage",
                "orders.view", "orders.manage", "customers.view", "customers.manage", "warranty.manage",
                "reports.view", "finance.view"],
            ["BanHang"] = ["products.view", "orders.view", "orders.manage", "customers.view", "customers.manage"],
            ["Kho"] = ["products.view", "inventory.view", "inventory.manage"],
            ["KeToan"] = ["orders.view", "finance.view", "reports.view"],
            ["BaoHanh"] = ["products.view", "customers.view", "warranty.manage"],
            ["NhanVien"] = ["products.view", "orders.view"]
        };
        foreach (var (roleName, allowedPermissions) in rolePermissions)
        {
            var role = await roleManager.FindByNameAsync(roleName)
                ?? throw new InvalidOperationException($"Không tìm thấy vai trò {roleName}.");
            var permissionIds = await db.QuyenHeThongs
                .Where(x => allowedPermissions.Contains(x.MaQuyen))
                .Select(x => x.Id)
                .ToListAsync();
            var existingIds = await db.GanQuyenVaiTros
                .Where(x => x.VaiTroId == role.Id)
                .Select(x => x.QuyenId)
                .ToListAsync();
            db.GanQuyenVaiTros.AddRange(permissionIds
                .Except(existingIds)
                .Select(permissionId => new GanQuyenVaiTro
                {
                    VaiTroId = role.Id,
                    QuyenId = permissionId
                }));
        }

        if (!await db.ChiNhanhs.AnyAsync())
        {
            db.ChiNhanhs.Add(new ChiNhanh
            {
                MaChiNhanh = "MOTUHUB-01",
                TenChiNhanh = "MotoHub - Cửa hàng chính",
                DiaChi = "Việt Nam"
            });
        }

        var today = DateTime.UtcNow.Date;
        var promotionSeeds = new[]
        {
            new KhuyenMai { MaKhuyenMai = "MOTOHUB50", TenKhuyenMai = "Ưu đãi thành viên MotoHub", GiaTri = 500000, DonToiThieu = 30000000, BatDau = today, KetThuc = today.AddDays(91).AddTicks(-1) },
            new KhuyenMai { MaKhuyenMai = "RIDE3", TenKhuyenMai = "Khởi hành cùng ưu đãi 3%", GiaTri = 3, TheoPhanTram = true, DonToiThieu = 40000000, BatDau = today, KetThuc = today.AddDays(61).AddTicks(-1) },
            new KhuyenMai { MaKhuyenMai = "SHOWROOM1M", TenKhuyenMai = "Đặc quyền nhận xe tại showroom", GiaTri = 1000000, DonToiThieu = 70000000, BatDau = today, KetThuc = today.AddDays(31).AddTicks(-1) }
        };
        var promotionCodes = await db.KhuyenMais.Select(x => x.MaKhuyenMai).ToListAsync();
        db.KhuyenMais.AddRange(promotionSeeds.Where(x => !promotionCodes.Contains(x.MaKhuyenMai, StringComparer.OrdinalIgnoreCase)));

        var articleSeeds = new[]
        {
            new BaiViet { TieuDe = "Chọn xe đi phố: 5 điều nên thử trước khi quyết định", TomTat = "Tư thế ngồi, bán kính quay đầu và chiều cao yên ảnh hưởng thế nào đến trải nghiệm hằng ngày?", Loai = "TuVan", DaXuatBan = true, NoiDung = "Hãy thử chống chân khi dừng, đánh lái ở tốc độ thấp và kiểm tra tầm với tới tay lái. Nếu thường chở thêm người, hãy ngồi thử cả hai vị trí. Cuối cùng, đối chiếu dung tích cốp, trọng lượng xe và lịch bảo dưỡng với nhu cầu sử dụng thực tế." },
            new BaiViet { TieuDe = "Xe côn tay cho chuyến đi dài: chuẩn bị gì trước ngày khởi hành?", TomTat = "Danh sách kiểm tra lốp, phanh, nhông xích và hành lý gọn nhẹ cho một hành trình an toàn.", Loai = "KinhNghiem", DaXuatBan = true, NoiDung = "Trước chuyến đi, kiểm tra áp suất và độ mòn lốp, hành trình phanh, đèn chiếu sáng, dầu động cơ và độ chùng nhông xích theo hướng dẫn của nhà sản xuất. Sắp xếp hành lý cân bằng, mang giấy tờ cần thiết và nghỉ giữa chặng. Không cố chạy khi mệt hoặc thời tiết xấu." },
            new BaiViet { TieuDe = "ABS, CBS và phanh thường: hiểu nhanh trước khi chọn xe", TomTat = "Giải thích ngắn gọn các hệ thống phanh phổ biến và cách chúng hỗ trợ người lái.", Loai = "KienThuc", DaXuatBan = true, NoiDung = "ABS giúp hạn chế khóa bánh khi phanh gấp trong điều kiện phù hợp; CBS phân bổ lực phanh giữa hai bánh theo thiết kế của xe. Mỗi hệ thống có giới hạn riêng và không thay thế việc giữ khoảng cách, giảm tốc độ phù hợp hay bảo dưỡng định kỳ. Hãy đọc sách hướng dẫn đúng mẫu xe." },
            new BaiViet { TieuDe = "Chăm sóc xe sau mùa mưa", TomTat = "Làm sạch, kiểm tra và bảo quản các chi tiết thường tiếp xúc với nước mưa.", Loai = "BaoDuong", DaXuatBan = true, NoiDung = "Rửa sạch bùn đất bằng nước và dung dịch phù hợp, lau khô các đầu nối dễ đọng nước, kiểm tra phanh và lốp. Bôi trơn xích theo khuyến nghị cho xe có truyền động xích. Nếu phát hiện tiếng động, rung hoặc đèn cảnh báo bất thường, hãy đưa xe đến kỹ thuật viên." }
        };
        var articleTitles = await db.BaiViets.Select(x => x.TieuDe).ToListAsync();
        db.BaiViets.AddRange(articleSeeds.Where(x => !articleTitles.Contains(x.TieuDe, StringComparer.OrdinalIgnoreCase)));

        var announcementTitles = await db.ThongBaoNguoiDungs
            .Where(x => x.NguoiDungId == null).Select(x => x.TieuDe).ToListAsync();
        if (!announcementTitles.Contains("MotoHub: hướng dẫn áp dụng ưu đãi", StringComparer.OrdinalIgnoreCase))
        {
            db.ThongBaoNguoiDungs.Add(new ThongBaoNguoiDung
            {
                TieuDe = "MotoHub: hướng dẫn áp dụng ưu đãi",
                NoiDung = "Chọn mã còn hiệu lực tại mục Ưu đãi, nhập mã ở bước thanh toán và nhấn Áp dụng để xem số tiền được giảm trước khi xác nhận đơn."
            });
        }

        if (!await db.XeMays.AnyAsync())
        {
            db.XeMays.AddRange(
                new XeMayModel { MaXe = "HON-SH150", TenXe = "SH 150i", HangXe = "Honda", LoaiXe = "Xe tay ga", GiaNiemYet = 98000000, GiaBan = 95000000, SoLuong = 5, HinhAnh = "https://placehold.co/640x400?text=Honda+SH+150i", MoTa = "Xe tay ga Honda SH 150i.", PhanKho = 150 },
                new XeMayModel { MaXe = "YAM-EX155", TenXe = "Exciter 155 VVA", HangXe = "Yamaha", LoaiXe = "Xe côn tay", GiaNiemYet = 50000000, GiaBan = 48000000, SoLuong = 8, HinhAnh = "https://placehold.co/640x400?text=Yamaha+Exciter+155", MoTa = "Xe côn tay Yamaha Exciter 155 VVA.", PhanKho = 155 },
                new XeMayModel { MaXe = "HON-WINX", TenXe = "Winner X", HangXe = "Honda", LoaiXe = "Xe côn tay", GiaNiemYet = 44000000, GiaBan = 42000000, SoLuong = 10, HinhAnh = "https://placehold.co/640x400?text=Honda+Winner+X", MoTa = "Xe côn tay Honda Winner X.", PhanKho = 150 }
            );
        }

        var boSuuTapMau = new[]
        {
            new XeMayModel { MaXe = "HON-VISION", TenXe = "Vision Cao Cấp", HangXe = "Honda", LoaiXe = "Xe tay ga", GiaNiemYet = 34500000, GiaBan = 32900000, SoLuong = 8, PhanKho = 110, MoTa = "Nhỏ gọn, linh hoạt và tiết kiệm cho nhịp sống đô thị." },
            new XeMayModel { MaXe = "HON-AB160", TenXe = "Air Blade 160 ABS", HangXe = "Honda", LoaiXe = "Xe tay ga", GiaNiemYet = 57500000, GiaBan = 55900000, SoLuong = 5, PhanKho = 160, MoTa = "Thiết kế thể thao mạnh mẽ, trang bị phanh ABS an tâm." },
            new XeMayModel { MaXe = "HON-VAR160", TenXe = "Vario 160", HangXe = "Honda", LoaiXe = "Xe tay ga", GiaNiemYet = 55500000, GiaBan = 53900000, SoLuong = 4, PhanKho = 160, MoTa = "Phong cách hiện đại cùng động cơ 160cc linh hoạt." },
            new XeMayModel { MaXe = "HON-LEAD125", TenXe = "Lead 125 ABS", HangXe = "Honda", LoaiXe = "Xe tay ga", GiaNiemYet = 46500000, GiaBan = 44900000, SoLuong = 6, PhanKho = 125, MoTa = "Tiện dụng mỗi ngày với cốp chứa đồ rộng rãi." },
            new XeMayModel { MaXe = "HON-FUT125", TenXe = "Future 125 FI", HangXe = "Honda", LoaiXe = "Xe số", GiaNiemYet = 32500000, GiaBan = 30900000, SoLuong = 5, PhanKho = 125, MoTa = "Êm ái, bền bỉ và tiết kiệm nhiên liệu." },
            new XeMayModel { MaXe = "HON-CBR150", TenXe = "CBR150R", HangXe = "Honda", LoaiXe = "Sport", GiaNiemYet = 73500000, GiaBan = 71900000, SoLuong = 3, PhanKho = 150, MoTa = "Tinh thần đường đua trong một mẫu sportbike linh hoạt." },
            new XeMayModel { MaXe = "HON-CBR650", TenXe = "CBR650R", HangXe = "Honda", LoaiXe = "Sport", GiaNiemYet = 265000000, GiaBan = 259000000, SoLuong = 2, PhanKho = 650, MoTa = "Sportbike phân khối lớn dành cho những hành trình đáng nhớ." },
            new XeMayModel { MaXe = "HON-CUB125", TenXe = "Super Cub C125", HangXe = "Honda", LoaiXe = "Xe số", GiaNiemYet = 87500000, GiaBan = 85900000, SoLuong = 2, PhanKho = 125, MoTa = "Dấu ấn cổ điển giao hòa cùng tiện nghi hiện đại." },
            new XeMayModel { MaXe = "YAM-GRAND", TenXe = "Grande Hybrid", HangXe = "Yamaha", LoaiXe = "Xe tay ga", GiaNiemYet = 49500000, GiaBan = 47900000, SoLuong = 5, PhanKho = 125, MoTa = "Thanh lịch, nhẹ nhàng và tinh tế cho phố thị." },
            new XeMayModel { MaXe = "YAM-JANUS", TenXe = "Janus Limited", HangXe = "Yamaha", LoaiXe = "Xe tay ga", GiaNiemYet = 33500000, GiaBan = 31900000, SoLuong = 7, PhanKho = 125, MoTa = "Phong cách trẻ trung, dễ dàng đồng hành mỗi ngày." },
            new XeMayModel { MaXe = "YAM-NVX155", TenXe = "NVX 155 VVA", HangXe = "Yamaha", LoaiXe = "Xe tay ga", GiaNiemYet = 55500000, GiaBan = 53900000, SoLuong = 4, PhanKho = 155, MoTa = "Thiết kế thể thao cùng hiệu năng đầy phấn khích." },
            new XeMayModel { MaXe = "YAM-FREEGO", TenXe = "FreeGo S ABS", HangXe = "Yamaha", LoaiXe = "Xe tay ga", GiaNiemYet = 35500000, GiaBan = 33900000, SoLuong = 6, PhanKho = 125, MoTa = "Thực dụng, thoải mái và sẵn sàng cho mọi lịch trình." },
            new XeMayModel { MaXe = "YAM-MT15", TenXe = "MT-15 V2", HangXe = "Yamaha", LoaiXe = "Naked bike", GiaNiemYet = 70500000, GiaBan = 68900000, SoLuong = 3, PhanKho = 155, MoTa = "Chất naked bike cá tính trong thân hình linh hoạt." },
            new XeMayModel { MaXe = "YAM-R15V4", TenXe = "YZF-R15 V4", HangXe = "Yamaha", LoaiXe = "Sport", GiaNiemYet = 87500000, GiaBan = 85900000, SoLuong = 3, PhanKho = 155, MoTa = "Cảm hứng đường đua với dáng vẻ khí động học." },
            new XeMayModel { MaXe = "SUZ-RAIDER", TenXe = "Raider R150", HangXe = "Suzuki", LoaiXe = "Hyper Underbone", GiaNiemYet = 53500000, GiaBan = 51900000, SoLuong = 4, PhanKho = 150, MoTa = "Bứt phá cá tính với thiết kế hyper underbone mạnh mẽ." },
            new XeMayModel { MaXe = "SUZ-BURGMAN", TenXe = "Burgman Street 125", HangXe = "Suzuki", LoaiXe = "Xe tay ga", GiaNiemYet = 49500000, GiaBan = 47900000, SoLuong = 4, PhanKho = 125, MoTa = "Tư thế lái thoải mái, phong thái scooter thanh lịch." },
            new XeMayModel { MaXe = "SUZ-SATRIA", TenXe = "Satria F150", HangXe = "Suzuki", LoaiXe = "Hyper Underbone", GiaNiemYet = 53500000, GiaBan = 51900000, SoLuong = 3, PhanKho = 150, MoTa = "Mẫu xe côn tay gọn nhẹ với tinh thần thể thao." },
            new XeMayModel { MaXe = "PIA-LIBERTY", TenXe = "Liberty 125", HangXe = "Piaggio", LoaiXe = "Xe tay ga", GiaNiemYet = 57500000, GiaBan = 55900000, SoLuong = 3, PhanKho = 125, MoTa = "Phong cách Italy thanh lịch trên từng cung đường." },
            new XeMayModel { MaXe = "VES-PRIM125", TenXe = "Vespa Primavera 125", HangXe = "Vespa", LoaiXe = "Xe tay ga", GiaNiemYet = 86500000, GiaBan = 84900000, SoLuong = 2, PhanKho = 125, MoTa = "Biểu tượng thiết kế vượt thời gian của nước Ý." },
            new XeMayModel { MaXe = "VES-SPRINT150", TenXe = "Vespa Sprint 150", HangXe = "Vespa", LoaiXe = "Xe tay ga", GiaNiemYet = 102000000, GiaBan = 99500000, SoLuong = 2, PhanKho = 150, MoTa = "Chất thể thao pha nét thanh lịch đặc trưng Vespa." },
            new XeMayModel { MaXe = "PIA-MEDLEY", TenXe = "Medley S 150", HangXe = "Piaggio", LoaiXe = "Xe tay ga", GiaNiemYet = 98500000, GiaBan = 96900000, SoLuong = 2, PhanKho = 150, MoTa = "Scooter cao cấp kết hợp tiện nghi và phong cách." }
        };
        var maXeDaCo = await db.XeMays.Select(x => x.MaXe).ToListAsync();
        var xeCanThem = boSuuTapMau.Where(x => !maXeDaCo.Contains(x.MaXe, StringComparer.Ordinal)).ToList();
        if (xeCanThem.Count > 0)
        {
            db.XeMays.AddRange(xeCanThem);
        }

        await db.XeMays.ToListAsync();
        foreach (var xe in db.XeMays.Local)
        {
            if (HinhAnhXeTheoMa.TryGetValue(xe.MaXe, out var imageUrl)
                && (string.IsNullOrWhiteSpace(xe.HinhAnh)
                    || xe.HinhAnh.Contains("placehold.co", StringComparison.OrdinalIgnoreCase)))
            {
                xe.HinhAnh = imageUrl;
            }
        }

        if (!await db.CauHinhCuaHangs.AnyAsync())
        {
            db.CauHinhCuaHangs.AddRange(
                new CauHinhCuaHang { Khoa = "StoreName", GiaTri = "MotoHub" },
                new CauHinhCuaHang { Khoa = "Currency", GiaTri = "VND" },
                new CauHinhCuaHang { Khoa = "DefaultPaymentMethod", GiaTri = "COD" }
            );
        }

        await db.SaveChangesAsync();

        var defaultBranchId = await db.ChiNhanhs
            .Where(x => x.DangHoatDong)
            .Select(x => (int?)x.Id)
            .FirstOrDefaultAsync();
        if (defaultBranchId.HasValue)
        {
            var stocks = await db.TonKhoChiNhanhs.Select(x => x.XeMayId).ToListAsync();
            var products = await db.XeMays.ToListAsync();
            foreach (var product in products.Where(x => !stocks.Contains(x.Id)))
            {
                db.TonKhoChiNhanhs.Add(new TonKhoChiNhanh
                {
                    XeMayId = product.Id,
                    ChiNhanhId = defaultBranchId.Value,
                    SoLuong = product.SoLuong
                });
            }

            if (products.Any(x => !stocks.Contains(x.Id)))
            {
                await db.SaveChangesAsync();
            }
        }


    }

    private static async Task KhoiTaoSchemaAsync(AppDbContext db)
    {
        await db.Database.MigrateAsync();
    }
}
