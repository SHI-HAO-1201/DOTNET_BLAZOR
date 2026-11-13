# Ghi chú luồng nút và điều hướng — Blazor

Tài liệu này ghi vị trí giao diện, handler nhận thao tác và nơi handler chuyển tiếp tới. Trong Blazor, phần lớn nút **không chuyển sang file khác**: `@onclick` gọi method ngay trong cùng component `.razor`; form `EditForm` gọi callback `OnValidSubmit`. Chỉ link/`Navigation.NavigateTo` mới đổi route.

## Điều hướng chung

- `Components/Layout/NavMenu.razor`, `Components/Layout/UserLayout.razor`, `Components/Layout/AdminLayout.razor`: các link điều hướng tới route khai báo bằng `@page` trong các component bên dưới.
- `UserLayout.razor`: nút mở menu gọi `ToggleMenu`; khi chọn mục gọi `DongMenu`; ô tìm kiếm submit GET tới `/san-pham?tuKhoa=...`; nút đăng xuất POST `/auth/logout` (endpoint trong `Program.cs`). Link hồ sơ/đơn/yêu thích/bảo hành/thông báo đi tới các route ở bảng trên.
- `AdminLayout.razor`: link menu đặt module qua `/admin/{Module}`; nút đăng xuất POST tới endpoint auth tương ứng trong `Program.cs`.
- `Components/Routes.razor`: router chọn component theo URL; không phải nơi xử lý nghiệp vụ nút.
- Route `/danh-sach-xe`, `/xe/{Id}`, `/yeu-thich`, `/so-sanh`: `Features/XeMay/Pages/`.
- Route `/gio-hang`, `/thanh-toan`, `/don-hang`: `Features/DonHang/Pages/`.
- Route `/tai-khoan`: `Features/TaiKhoan/Pages/TaiKhoanCuaToi.razor`.
- Route `/admin` và `/admin/{Module}`: `Features/Admin/Pages/AdminPanel.razor`; module được chọn bởi tham số URL, không phải component khác.
- Route `/dang-nhap`: `Components/Pages/DangNhap.razor`; nút đăng nhập/đăng ký gửi POST tới `/auth/login` và `/auth/register` trong `Program.cs`. Nút hiện/ẩn mật khẩu chỉ đổi biến trong component.
- Các trang khách hàng `/tin-tuc`, `/khuyen-mai`, `/he-thong-cua-hang`, `/bo-suu-tap`, `/ho-tro`, `/bao-hanh`, `/thong-bao`: `Features/KhachHang/Pages/`.
- Nút thêm giỏ trên trang chủ gọi `ThemGioHang` trong `Components/Pages/Trangchu.razor` → `GioHangService`; link Khám phá cuộn trong trang, còn link mẫu xe mở `/xe/{id}`.
- Nút demo `Click me` gọi `IncrementCount` trong `Components/Pages/Counter.razor`. Nút reconnect/resume trong `Components/Layout/ReconnectModal.razor` do framework Blazor quản lý.
- Route `/san-pham` cũng được khai báo trong `Components/Pages/Trangchu.razor`; đó là trang storefront liệt kê xe. Route `/danh-sach-xe` là component `Features/XeMay/Pages/DanhSachXe.razor`, một trang danh sách/admin mẫu có nút CRUD TODO. Hai route này khác nhau.

## Nút khai báo ở đâu, chức năng nằm ở đâu?

Blazor thường khai báo nút trực tiếp trong markup `.razor`, rồi gắn method bằng `@onclick` hoặc callback form. Không có method riêng “tạo nút” cho mọi nút.

| Nút/chức năng | Nơi khai báo nút/ô | Nơi chứa logic |
|---|---|---|
| Tìm xe trong header | `Components/Layout/UserLayout.razor`, input `name="tuKhoa"` và nút submit | Submit GET tới `/san-pham`; `Components/Pages/Trangchu.razor` nhận `[SupplyParameterFromQuery]`, `OnParametersSetAsync()` gọi `XeMayService.LayDanhSachXeAsync(...)`; lọc truy vấn SQL ở `Features/XeMay/Service/XeMayService.cs` |
| Tìm/lọc/sắp xếp xe trên trang `/san-pham` | Form trong `Components/Pages/Trangchu.razor` | Form submit GET; `Trangchu.OnParametersSetAsync()` gọi `XeMayService`, method `LayDanhSachXeAsync` thực hiện lọc/sắp xếp |
| Thêm xe vào giỏ từ trang chủ | Nút trong vòng `@foreach` ở `Components/Pages/Trangchu.razor` | `ThemGioHang()` trong cùng file → `Features/DonHang/GioHangService.cs` |
| Thêm vào giỏ/mua ngay/so sánh ở trang chi tiết | `Features/XeMay/Pages/ChiTietXe.razor` | `ThemVaoGio`, `MuaNgay`, `ThemSoSanh` trong cùng file; dùng `GioHangService` hoặc `SoSanhXeService` |
| Xóa xe trong giỏ | `Features/DonHang/Pages/GioHang.razor` | `Xoa()` trong cùng file → `GioHangService`; số lượng thay đổi qua handler `CapNhat()` |
| Tìm kiếm admin | Không có ô tìm kiếm trong các module admin hiện tại | Admin tải/filter dữ liệu trực tiếp trong `Features/Admin/Pages/AdminPanel.razor` và service; không có hàm tìm kiếm riêng |

### Ví dụ: bấm “Tìm xe” trên Blazor

`UserLayout.razor` (hoặc form lọc trong `Trangchu.razor`) → trình duyệt GET `/san-pham?tuKhoa=...` → Router trong `Components/Routes.razor` chọn `Components/Pages/Trangchu.razor` → `OnParametersSetAsync()` đọc query → `XeMayService.LayDanhSachXeAsync()` trong `Features/XeMay/Service/XeMayService.cs` truy vấn `AppDbContext` → component render kết quả. Chức năng tìm theo tên/hãng/mã xe nằm trong service; nút submit chỉ gửi query, không có `@onclick` riêng.

## Xe và mua hàng

| Nút/thao tác | Handler hoặc đích | File tiếp theo |
|---|---|---|
| Sửa / xóa trong danh sách xe | `SuaXe`, `XoaXe` | `Features/XeMay/Pages/DanhSachXe.razor`; hiện handler là khung TODO, chưa lưu hoặc xóa trong database |
| Thêm xe mới | `ThemXeMoi` | `Features/XeMay/Pages/DanhSachXe.razor`; hiện handler là khung TODO, chưa tạo xe |
| Thêm vào giỏ | `ThemVaoGio` → `GioHangService` | `Features/XeMay/Pages/ChiTietXe.razor` → `Features/DonHang/GioHangService.cs` |
| Mua ngay | `MuaNgay` → điều hướng `/gio-hang` | `Features/XeMay/Pages/ChiTietXe.razor` → `Features/DonHang/Pages/GioHang.razor` |
| So sánh xe | `ThemSoSanh` → route `/so-sanh` | `Features/XeMay/Pages/ChiTietXe.razor` → `Features/XeMay/Pages/SoSanhXe.razor` |
| Thêm yêu thích | `ThemYeuThichAsync` → database | `Features/XeMay/Pages/ChiTietXe.razor` → `Features/Shared/AppDbContext.cs` qua EF |
| Gửi đánh giá | `GuiDanhGiaAsync` → database | `Features/XeMay/Pages/ChiTietXe.razor` → `AppDbContext` |
| Xóa xe khỏi yêu thích / so sánh | `XoaAsync` / `Xoa` | `Features/XeMay/Pages/YeuThich.razor`, `SoSanhXe.razor` |
| Xóa dòng khỏi giỏ | `Xoa` → `GioHangService` | `Features/DonHang/Pages/GioHang.razor` → `Features/DonHang/GioHangService.cs` |
| Tiến hành đặt hàng | link `/thanh-toan` | `Features/DonHang/Pages/ThanhToan.razor` |
| Đăng nhập để đặt hàng | link login kèm `returnUrl` | Trang đăng nhập khai báo trong `Components/Pages/DangNhap.razor` |
| Áp dụng mã giảm giá | `ApDungMaKhuyenMaiAsync` → `DichVuDonHang` | `Features/DonHang/Pages/ThanhToan.razor` → `Features/DonHang/DichVuDonHang.cs` |
| Đặt hàng | `DatHangAsync` → `DichVuDonHang.DatHangAsync` → lưu đơn/chi tiết/thanh toán và cập nhật tồn | `Features/DonHang/Pages/ThanhToan.razor` → `Features/DonHang/DichVuDonHang.cs` → `Features/Shared/AppDbContext.cs` |
| Theo dõi đơn | link `/don-hang` | `Features/DonHang/Pages/DonHangCuaToi.razor` |
| Yêu cầu hủy đơn | `HuyDonAsync` → `DichVuDonHang.HuyDonAsync` | `DonHangCuaToi.razor` → `Features/DonHang/DichVuDonHang.cs` |

## Tài khoản và hỗ trợ khách hàng

- **Lưu hồ sơ / đổi mật khẩu / thêm địa chỉ / xóa địa chỉ:** `LuuHoSoAsync`, `DoiMatKhauAsync`, `ThemDiaChiAsync`, `XoaDiaChiAsync` trong `Features/TaiKhoan/Pages/TaiKhoanCuaToi.razor`; nghiệp vụ dữ liệu đi qua `Features/TaiKhoan/DichVuKhachHang.cs` hoặc Identity.
- **Hiện/ẩn mật khẩu:** các nút chỉ đổi biến `ShowCurrentPassword`, `ShowNewPassword`, `ShowConfirmPassword` trong cùng component; không chuyển file.
- **Gửi yêu cầu bảo hành:** `GuiYeuCauAsync` trong `Features/KhachHang/Pages/BaoHanhCuaToi.razor` → `DichVuKhachHang.GuiYeuCauBaoHanhAsync`.
- **Gửi yêu cầu hỗ trợ:** `GuiYeuCauAsync` trong `Features/KhachHang/Pages/HoTro.razor` → xử lý trong component và `AppDbContext`.
- Link xem xe, ưu đãi, cửa hàng, bài viết, thông báo chỉ điều hướng tới route tương ứng trong `Features/KhachHang/Pages/` hoặc `Features/XeMay/Pages/`.

## Admin

Các thao tác dưới đây đều bắt đầu trong `Features/Admin/Pages/AdminPanel.razor`. `ThucHienAsync` kiểm tra quyền; thao tác dữ liệu sau đó dùng service hoặc `DbFactory`/`AppDbContext`.

| Nút/form | Handler | Tiếp tục tới |
|---|---|---|
| Lưu xe | `LuuXeAsync` | `XeMayService` → `Features/XeMay/Service/XeMayService.cs` |
| Làm mới form xe | `DatLaiFormXe` | cùng component |
| Sửa xe | `ChonXe` | cùng component; nạp xe vào form |
| Ẩn / mở bán | `NgungKinhDoanhAsync` / `MoKinhDoanhAsync` | component → `XeMayService` |
| Ghi nhận biến động kho | `CapNhatKhoAsync` | component → `XeMayService` / database |
| Chuyển kho | `ChuyenKhoAsync` | component → `XeMayService.ChuyenKhoAsync` |
| Tạo đơn tại quầy | `TaoDonTaiQuayAsync` | component → `AppDbContext`; tạo đơn bán trực tiếp và thanh toán tại quầy |
| Lưu/ẩn khách hàng | `DoiTrangThaiNguoiDungAsync`; xem chi tiết `XemKhachHangAsync` | component → `UserManager`/database |
| Lưu nhà cung cấp | `ThemNhaCungCapAsync` | component → `AppDbContext` |
| Tạo/sửa khuyến mãi | `LuuKhuyenMaiAsync`; sửa form `SuaKhuyenMai` | component → `AppDbContext` |
| Hủy sửa khuyến mãi / xóa khuyến mãi | `DatLaiKhuyenMai` / `XoaKhuyenMaiAsync` | cùng component → database khi xóa |
| Cập nhật đơn đặt online | `CapNhatDonAsync` → `DichVuQuanTri.CapNhatTrangThaiDonHangAsync` | `Features/Admin/DichVuQuanTri.cs` → `AppDbContext` |
| Hủy đơn bởi nhân viên | `HuyDonNhanVienAsync` → `DichVuDonHang.HuyDonAsync` | `Features/DonHang/DichVuDonHang.cs` |
| Hoàn tất bảo hành / đóng hỗ trợ | `CapNhatBaoHanhAsync` / `CapNhatHoTroAsync` | component → `AppDbContext` |
| Lưu bài viết / đăng-ẩn bài | `ThemBaiVietAsync` / `DoiTrangThaiBaiVietAsync` | component → `AppDbContext` |
| Gán/thu hồi vai trò, phạm vi chi nhánh | `GanVaiTroAsync` / `GanChiNhanhAsync` | component → `DichVuQuanTri` hoặc `DichVuPhanQuyen` |
| Lưu chi nhánh / gửi thông báo / lưu cấu hình | `ThemChiNhanhAsync` / `GuiThongBaoAsync` / `LuuCauHinhAsync` | component → `AppDbContext` |
| Sửa cấu hình trong bảng | `ChonCauHinh` | cùng component, đưa dữ liệu lên form |

## Khi chạy `dotnet watch`: thứ tự khởi động

Chạy lệnh từ thư mục `BlazorXe_may`:

```powershell
dotnet watch
```

1. `dotnet watch` build và theo dõi project; entry point top-level đầu tiên được chạy là `Program.cs`.
2. `Program.cs` đăng ký SQL Server `Features.Shared.AppDbContext`, Razor Components/Interactive Server, Identity và các service nghiệp vụ.
3. Sau `builder.Build()`, `KhoiTaoDuLieu.KhoiTaoAsync()` (trong `Features/Shared/KhoiTaoDuLieu.cs`) chạy migration, tạo/cập nhật dữ liệu khởi tạo và role cần thiết.
4. `Program.cs` cấu hình middleware và map endpoint auth; cuối cùng map Razor Components bằng `MapRazorComponents<App>().AddInteractiveServerRenderMode()`.
5. Browser tải tài liệu từ `Components/App.razor`; component này đặt `<Routes @rendermode="InteractiveServer" />`.
6. `Components/Routes.razor` tìm component có `@page` khớp URL, chọn layout (`UserLayout` hoặc `AdminLayout`) rồi render trang. Sau đó các event `@onclick` chạy trong kết nối Interactive Server.

`dotnet watch` không chạy một trang `.razor` cố định đầu tiên. `Program.cs` khởi động server; URL mở trong browser xác định component đầu tiên.

## Ví dụ: bấm “Bộ sưu tập” và xem xe

1. Trong header `Components/Layout/UserLayout.razor`, mục **Bộ sưu tập** trỏ tới `/bo-suu-tap`.
2. Router trong `Components/Routes.razor` khớp `@page "/bo-suu-tap"` và hiển thị `Features/KhachHang/Pages/BoSuuTap.razor` bên trong `UserLayout`.
3. Đây là trang bài viết/editorial về các mẫu xe đua và thiết kế. Nút **Xem xe đang kinh doanh** trỏ tới `/san-pham`.
4. `/san-pham` khớp route thứ hai trong `Components/Pages/Trangchu.razor` (component này cũng xử lý `/`). Trang đọc danh sách xe qua `XeMayService` và hiển thị các thẻ xe.
5. Bấm tên/ảnh xe trong `Trangchu.razor` → `/xe/{id}` → `Features/XeMay/Pages/ChiTietXe.razor`; bấm **Thêm vào giỏ** gọi `ThemVaoGio()` → `GioHangService`.
6. Bấm **Giỏ hàng** trên `UserLayout` → `/gio-hang` → `Features/DonHang/Pages/GioHang.razor`; tiếp tục đặt hàng → `/thanh-toan` → `Features/DonHang/Pages/ThanhToan.razor` → `DichVuDonHang`.

Lưu ý: `/danh-sach-xe` là route riêng cho `Features/XeMay/Pages/DanhSachXe.razor`, không phải trang bộ sưu tập storefront; các nút thêm/sửa/xóa ở đó hiện là TODO.
| Đổi quyền checkbox | `CapNhatQuyenAsync` | component → `DichVuPhanQuyen` |

