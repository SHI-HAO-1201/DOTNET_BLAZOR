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

## Luồng chi tiết: từ thao tác đến nơi dữ liệu đi

Đọc mỗi luồng theo thứ tự **markup/nút → event hoặc HTTP endpoint → handler → service/Identity → `AppDbContext` hoặc trạng thái trong bộ nhớ → giao diện/route kế tiếp**. Tên file trong các chuỗi dưới đây tính từ thư mục `BlazorXe_may`.

### 1. Đăng ký, đăng nhập và đăng xuất

**Đăng ký khách hàng**

```text
Components/Pages/DangNhap.razor
  form action="/auth/register" method="post"
  + input fullName, email, password
  → wwwroot/auth.js bắt sự kiện submit
  → GET /auth/csrf để xin request token và cookie antiforgery
  → gắn token vào form rồi POST /auth/register
  → endpoint MapPost("/auth/register") trong Program.cs
  → UserManager<AppUser>.CreateAsync(user, password)
  → UserManager<AppUser>.AddToRoleAsync(user, "User")
  → SignInManager<AppUser>.SignInAsync(...)
  → chuyển về /
```

`CreateAsync` tạo người dùng trong các bảng Identity (`AspNetUsers` và các bảng liên quan); mật khẩu được Identity băm trước khi lưu. Tài khoản đăng ký nhận vai trò `User`, không phải `QuanTriVien`. Nếu tạo user được nhưng gán role thất bại, endpoint xóa user vừa tạo rồi trả lỗi. Password policy được cấu hình trong `Program.cs`: tối thiểu 10 ký tự, có số, chữ thường, chữ hoa và ký tự đặc biệt.

**Đăng nhập** đi theo `DangNhap.razor` → `auth.js`/`GET /auth/csrf` → `POST /auth/login` trong `Program.cs` → `UserManager.FindByEmailAsync` → kiểm tra `DangHoatDong` → `SignInManager.PasswordSignInAsync` → tạo cookie đăng nhập. Nếu URL có `returnUrl`, endpoint chỉ chuyển tới URL nội bộ hợp lệ; nếu không, nhân viên được chuyển tới `/admin/dashboard`, khách hàng về `/`.

**Đăng xuất**: form `/auth/logout` nằm ở `UserLayout.razor` và `AdminLayout.razor` → `auth.js` lấy token chống giả mạo → `MapPost("/auth/logout")` trong `Program.cs` → `SignInManager.SignOutAsync()` xóa phiên đăng nhập → chuyển về `/`.

Các nút hiện/ẩn mật khẩu trong `DangNhap.razor` chỉ đổi biến `ShowLoginPassword`/`ShowRegisterPassword` để đổi `type` input; chúng không gửi request và không ghi dữ liệu.

### 2. Mở route và chọn layout

```text
URL người dùng mở
  → Components/Routes.razor (Router)
  → component có @page khớp URL
  → layout được áp dụng (UserLayout hoặc AdminLayout)
  → component tải dữ liệu trong OnInitializedAsync/OnParametersSetAsync
```

Các link trong layout chỉ điều hướng. Phần nghiệp vụ thường nằm trong component của route; bấm nút `@onclick` thì Blazor gọi handler ngay trong file `.razor` đó, rồi handler mới gọi service hoặc database. Ví dụ `/admin/san-pham` vẫn là `AdminPanel.razor`; giá trị `{Module}` chọn tab, không làm Router mở một component admin khác.

### 3. Trang danh sách, tìm và xem chi tiết xe

**Tìm từ thanh header**: input `tuKhoa` và form GET ở `Components/Layout/UserLayout.razor` → trình duyệt mở `/san-pham?tuKhoa=...` → route `/san-pham` trong `Components/Pages/Trangchu.razor` → `[SupplyParameterFromQuery]` nhận từ khóa → `OnParametersSetAsync()` → `XeMayService.LayDanhSachXeAsync(...)` (`Features/XeMay/Service/XeMayService.cs`) → truy vấn `AppDbContext.XeMays`/dữ liệu tồn kho → trả danh sách cho `Trangchu.razor` render.

Form lọc/sắp xếp trên chính trang `/san-pham` cũng GET về route này với các query parameter. Submit không có handler `@onclick` riêng; thay đổi query khiến component nhận parameter mới và tải lại danh sách qua cùng service.

**Mở xe**: link ảnh/tên xe trong `Trangchu.razor` → `/xe/{id}` → `Features/XeMay/Pages/ChiTietXe.razor` → `OnParametersSetAsync()` đọc `Id` → `XeMayService.LayTheoIdAsync(id)` → `AppDbContext.XeMays` → render thông tin xe. Không tìm thấy id thì component hiển thị trạng thái không có xe.

### 4. Giỏ hàng, so sánh và yêu thích

- **Thêm vào giỏ từ trang chủ**: `Trangchu.razor` → `ThemGioHang(id)` → `GioHangService.Them(id)` trong `Features/DonHang/GioHangService.cs`.
- **Thêm vào giỏ từ chi tiết**: `ChiTietXe.razor` → `ThemVaoGio()` → cùng `GioHangService.Them(id)`.
- **Mua ngay**: `ChiTietXe.razor` → handler thêm xe vào `GioHangService` → điều hướng `/gio-hang`; thao tác mua ngay chưa tạo đơn ở bước này.
- **Giỏ hàng hiển thị**: `GioHang.razor` đăng ký sự kiện `GioHangService.Changed` → đọc cặp `xeId/số lượng` trong service → lần lượt gọi `XeMayService.LayTheoIdAsync` để lấy tên/giá/tình trạng xe → render dòng giỏ. `CapNhat` đổi số lượng, `Xoa` bỏ xe; service phát `Changed` để component tải lại. Giỏ hiện được giữ trong bộ nhớ của scope Blazor, không phải bảng database.
- **So sánh**: `ChiTietXe.razor` → `ThemSoSanh` → `SoSanhXeService.Them(id)` (cùng file `GioHangService.cs`) → điều hướng `/so-sanh` → `SoSanhXe.razor` lấy các id và tải thông tin qua `XeMayService`. Danh sách so sánh cũng giữ trong bộ nhớ, giới hạn tối đa 4 xe.
- **Yêu thích**: `ChiTietXe.razor` → `ThemYeuThichAsync` → lấy user đang đăng nhập qua `AuthenticationStateProvider`/`UserManager` → thêm hoặc cập nhật bản ghi `XeYeuThich` trong `AppDbContext` → `YeuThich.razor` tải các xe đã lưu. Nút bỏ yêu thích ở trang `/yeu-thich` gọi `XoaAsync(id)` và xóa bản ghi tương ứng.
- **Đánh giá xe**: `EditForm` trong `ChiTietXe.razor` → `GuiDanhGiaAsync` khi form hợp lệ → tạo `DanhGiaXe` trong `AppDbContext` → tải/render lại danh sách đánh giá.

### 5. Đặt hàng và theo dõi đơn

```text
UserLayout/GioHang.razor
  → link /thanh-toan
  → Features/DonHang/Pages/ThanhToan.razor
  → OnInitializedAsync tải chi nhánh và user hiện tại
  → EditForm OnValidSubmit="DatHangAsync"
  → Features/DonHang/DichVuDonHang.DatHangAsync(...)
  → kiểm tra giỏ, xe, tồn kho, người nhận/chi nhánh/khuyến mãi
  → transaction ghi DonHangs + ChiTietDonHangs + GiaoDichThanhToans
    và cập nhật XeMays/TonKhoChiNhanhs/BienDongKhos
  → trả đơn cho component, xóa GioHangService, hiển thị mã đơn
```

Nếu người dùng chưa đăng nhập, `ThanhToan.razor` điều hướng tới `/dang-nhap?returnUrl=%2Fthanh-toan`; sau đăng nhập endpoint đưa người dùng trở lại checkout. Nút áp dụng mã gọi riêng `ApDungMaKhuyenMaiAsync()` → `DichVuDonHang.KiemTraMaKhuyenMaiAsync()` để kiểm tra trước; khi tạo đơn, service kiểm tra lại để không dựa vào kết quả cũ từ trình duyệt.

**Xem đơn của tôi**: link `/don-hang` → `DonHangCuaToi.razor` → lấy user hiện tại → `DichVuDonHang.LayDonHangCuaNguoiDungAsync(userId)` → truy vấn `DonHangs` kèm chi tiết. **Yêu cầu hủy**: nút gọi `HuyDonAsync(id)` → `DichVuDonHang.HuyDonAsync(id, userId, staff: false)` → chỉ cho hủy đơn đang chờ xác nhận → transaction cập nhật trạng thái và hoàn lại số lượng tồn.

### 6. Hồ sơ và yêu cầu hỗ trợ

- **Hồ sơ/mật khẩu/địa chỉ**: route `/tai-khoan` → `TaiKhoanCuaToi.razor` lấy user trong `OnInitializedAsync`. Form hồ sơ gọi `LuuHoSoAsync` → `UserManager.UpdateAsync`; form đổi mật khẩu gọi `DoiMatKhauAsync` → `UserManager.ChangePasswordAsync`; thêm/xóa địa chỉ gọi `ThemDiaChiAsync`/`XoaDiaChiAsync` → `DichVuKhachHang` hoặc `AppDbContext` → render kết quả tại cùng trang.
- **Hỗ trợ**: route `/ho-tro` → `Features/KhachHang/Pages/HoTro.razor` → form `OnValidSubmit="GuiYeuCauAsync"` → lưu `PhieuHoTro` trong `AppDbContext` (gắn user nếu đã đăng nhập).
- **Bảo hành**: route `/bao-hanh` → `BaoHanhCuaToi.razor` → `GuiYeuCauAsync` → `DichVuKhachHang.GuiYeuCauBaoHanhAsync` → lưu `YeuCauBaoHanh`; danh sách yêu cầu được tải theo user hiện tại.
- **Thông báo của tôi**: route `/thong-bao` → `ThongBaoCuaToi.razor` → truy vấn `ThongBaoNguoiDungs` theo user qua `AppDbContext`.
- **Tin tức/khuyến mãi/showroom/bộ sưu tập**: link từ `UserLayout.razor` → route tương ứng (`TinTuc.razor`, `KhuyenMai.razor`, `CuaHang.razor`, `BoSuuTap.razor`). Đây là các trang đọc/render dữ liệu; link trên trang có thể dẫn tiếp về `/san-pham` hoặc `/xe/{id}`.

### 7. Luồng trong trang quản trị

`AdminLayout.razor` tạo link `/admin/{Module}` → Router chọn `AdminPanel.razor` → `OnParametersSetAsync()` đọc module và gọi tải dữ liệu → component kiểm tra role/quyền → handler được gọi khi submit form hoặc click. Nhiều thao tác đi qua `ThucHienAsync(requiredPermission, operation)`: hàm này xác minh quyền, chạy operation và đặt thông báo thành công/lỗi; phần đọc lại danh sách hoặc ghi nhật ký thường được gọi cụ thể bên trong operation/handler. Mỗi module vẫn là một vùng giao diện trong cùng `AdminPanel.razor`, không phải mỗi tab là một file/page riêng.

| Thao tác trong `AdminPanel.razor` | Chuỗi gọi tiếp theo | Đích dữ liệu |
|---|---|---|
| Lưu/tạo xe | `EditForm` → `LuuXeAsync` → `XeMayService.ThemXeAsync` hoặc `CapNhatXeAsync` | `XeMays`; thêm/cập nhật tồn theo branch nếu cần |
| Sửa xe trong bảng | click → `ChonXe(xe)` | Chỉ đổ dữ liệu vào `XeForm`; chưa ghi DB cho tới khi bấm Lưu |
| Ẩn/mở bán | click → `NgungKinhDoanhAsync`/`MoKinhDoanhAsync` → `XeMayService` | Cập nhật trạng thái xe trong `XeMays` |
| Điều chỉnh/chuyển kho | form → `CapNhatKhoAsync`/`ChuyenKhoAsync` → `XeMayService` | `TonKhoChiNhanhs`, ghi lịch sử `BienDongKhos` |
| Tạo đơn tại quầy | form → `TaoDonTaiQuayAsync` | Tạo `DonHangs`, `ChiTietDonHangs`, thanh toán; cập nhật tồn trong `AppDbContext` |
| Khóa/mở user | click → `DoiTrangThaiNguoiDungAsync` | `UserManager` cập nhật `AppUser.DangHoatDong` |
| Nhà cung cấp/khuyến mãi | form → `ThemNhaCungCapAsync`/`LuuKhuyenMaiAsync`; xóa → `XoaKhuyenMaiAsync` | Các DbSet `NhaCungCaps`/`KhuyenMais` |
| Cập nhật trạng thái đơn online | click → `CapNhatDonAsync` → `DichVuQuanTri.CapNhatTrangThaiDonHangAsync` | `DonHangs` qua service |
| Hủy đơn nhân viên | click → `HuyDonNhanVienAsync` → `DichVuDonHang.HuyDonAsync(..., staff: true)` | Trạng thái đơn và hoàn tồn theo transaction |
| Hoàn tất bảo hành/đóng hỗ trợ | click → `CapNhatBaoHanhAsync`/`CapNhatHoTroAsync` | `YeuCauBaoHanhs`/`PhieuHoTros` |
| Bài viết đăng/ẩn | form hoặc click → `ThemBaiVietAsync`/`DoiTrangThaiBaiVietAsync` | `BaiViets` |
| Gán role/phạm vi chi nhánh | click → `GanVaiTroAsync`/`GanChiNhanhAsync` → `DichVuQuanTri` hoặc `DichVuPhanQuyen` | Bảng Identity role và `PhamViNguoiDungs` |
| Quyền checkbox | change → `CapNhatQuyenAsync` → `DichVuPhanQuyen` | `GanQuyenVaiTros` |
| Chi nhánh/thông báo/cấu hình | form → `ThemChiNhanhAsync`/`GuiThongBaoAsync`/`LuuCauHinhAsync` | `ChiNhanhs`/`ThongBaoNguoiDungs`/`CauHinhCuaHangs` |

### 8. Các nút không đi tới nghiệp vụ hoặc chưa hoàn tất

- `Counter.razor` → `IncrementCount()` chỉ tăng biến đếm mẫu.
- `ReconnectModal.razor` dùng cơ chế kết nối lại của Blazor; không gọi service nghiệp vụ.
- `DanhSachXe.razor` có các handler `ThemXeMoi`, `SuaXe`, `XoaXe` dạng TODO/khung mẫu; không đi tới `XeMayService` và chưa ghi/xóa database. Không nhầm route này với storefront `/san-pham`.
- Toggle menu, đóng menu, hiện/ẩn mật khẩu, chọn xe vào form, reset form là trạng thái UI cục bộ; chúng chỉ đi tới database nếu có handler lưu riêng được mô tả ở trên.

