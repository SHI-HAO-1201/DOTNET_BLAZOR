# Ghi chú luồng nút và điều hướng — Blazor

Tài liệu này ghi vị trí giao diện, handler nhận thao tác và nơi handler chuyển tiếp tới. Trong Blazor, phần lớn nút **không chuyển sang file khác**: `@onclick` gọi method ngay trong cùng component `.razor`; form `EditForm` gọi callback `OnValidSubmit`. Chỉ link/`Navigation.NavigateTo` mới đổi route.

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

## Tra cứu theo tên file: file nào làm nhiệm vụ gì?

### Khung ứng dụng và điều hướng

| File | Ý nghĩa/vai trò | Khi lần theo một nút |
|---|---|---|
| `Program.cs` | Điểm khởi động: đăng ký Razor/Identity/EF Core, đăng ký service DI, cấu hình middleware và các endpoint HTTP đăng nhập/đăng ký/đăng xuất. | Tới đây khi handler gọi service/Identity, hoặc form HTML gửi `/auth/login`, `/auth/register`, `/auth/logout`. Endpoint auth được map trong file này; nghiệp vụ bán hàng thông thường không đi qua endpoint HTTP riêng. |
| `Components/App.razor` | HTML gốc và nạp các tài nguyên của ứng dụng. | Hiếm khi chứa logic nút nghiệp vụ. |
| `Components/Routes.razor` | Router ánh xạ URL sang component có `@page`; áp dụng layout mặc định. | Dùng để xác nhận URL dẫn đến file trang nào; router không thực hiện lưu dữ liệu. |
| `Components/_Imports.razor`, `Features/_Imports.razor` | Các `using`, namespace/component dùng chung cho file Razor trong từng vùng. | Nếu thiếu namespace/type hoặc directive dùng chung, kiểm tra imports. |
| `Components/Layout/UserLayout.razor` | Khung cửa hàng: menu, tìm kiếm, giỏ hàng, tài khoản, đăng xuất. | Link chỉ điều hướng; menu gọi `ToggleMenu`/`DongMenu`; tìm kiếm GET sang `/san-pham`; đăng xuất POST sang endpoint trong `Program.cs`. |
| `Components/Layout/AdminLayout.razor` | Khung admin và link module `/admin/{Module}`. | Link đổi URL nhưng nội dung module vẫn nằm trong `AdminPanel.razor`; đăng xuất dùng cùng endpoint auth. |
| `Components/Layout/AuthLayout.razor` | Khung giao diện xác thực. | Chỉ ảnh hưởng bố cục trang đăng nhập, không xử lý đăng nhập. |
| `Components/Layout/ReconnectModal.razor`, `.razor.js`, `.razor.css` | Hộp trạng thái kết nối Blazor, JavaScript nối sự kiện thử lại/tiếp tục và CSS. | Các nút reconnect/resume là luồng kết nối của framework, không phải API nghiệp vụ. |
| `Components/Layout/NavMenu.razor` | Menu điều hướng mẫu của template. | Kiểm tra xem giao diện có đang dùng menu này hay `UserLayout`; tránh nhầm với menu cửa hàng thật. |

### Các trang Razor

Tên trang thường mô tả nơi người dùng đang đứng; markup của nút và handler thường cùng nằm trong chính file `.razor` đó.

| File/vùng | Ý nghĩa và luồng chính |
|---|---|
| `Components/Pages/Trangchu.razor` | `/` và `/san-pham`: trang cửa hàng/danh sách xe, nhận query tìm kiếm/lọc, gọi `XeMayService`; nút thêm giỏ gọi `GioHangService`. |
| `Components/Pages/DangNhap.razor` | `/dang-nhap`: form đăng nhập/đăng ký gửi POST đến endpoint auth trong `Program.cs`; nút hiện mật khẩu chỉ đổi state giao diện. |
| `Components/Pages/Counter.razor`, `Weather.razor` | Trang demo/template; Counter chỉ tăng biến tại chỗ, Weather đọc dữ liệu mẫu. |
| `Features/XeMay/Pages/ChiTietXe.razor` | `/xe/{Id}`: tải xe, đánh giá; thêm giỏ/so sánh/yêu thích và gửi đánh giá. Đây là điểm bắt đầu các thao tác tại trang chi tiết. |
| `Features/XeMay/Pages/DanhSachXe.razor` | `/danh-sach-xe`: danh sách mẫu riêng; các handler thêm/sửa/xóa hiện là TODO, không nhầm với trang `/san-pham` hay CRUD thật ở AdminPanel. |
| `Features/XeMay/Pages/YeuThich.razor`, `SoSanhXe.razor` | `/yeu-thich` hiển thị/xóa yêu thích; `/so-sanh` lấy các ID từ service so sánh rồi tải thông tin xe. |
| `Features/DonHang/Pages/GioHang.razor` | `/gio-hang`: đọc giỏ từ `GioHangService`, tải chi tiết xe, cập nhật số lượng hoặc xóa dòng. |
| `Features/DonHang/Pages/ThanhToan.razor` | `/thanh-toan`: kiểm tra mã khuyến mãi và submit đặt hàng qua `DichVuDonHang`; đây là nơi bắt đầu luồng tạo đơn online. |
| `Features/DonHang/Pages/DonHangCuaToi.razor` | `/don-hang`: liệt kê đơn của user hiện tại; yêu cầu hủy gọi `DichVuDonHang`. |
| `Features/TaiKhoan/Pages/TaiKhoanCuaToi.razor` | `/tai-khoan`: cập nhật hồ sơ/mật khẩu bằng Identity, địa chỉ qua dịch vụ khách hàng; nút hiện mật khẩu là UI. |
| `Features/KhachHang/Pages/*.razor` | Các trang tin tức, ưu đãi, showroom, bộ sưu tập, hỗ trợ, bảo hành, thông báo. Form hỗ trợ/bảo hành có lưu dữ liệu; nhiều trang nội dung chỉ đọc và điều hướng. |
| `Features/Admin/Pages/AdminPanel.razor` | `/admin` và `/admin/{Module}`: trang admin đơn lớn, chọn giao diện theo tham số Module. Form/nút gọi handler tại đây, kiểm tra quyền và chuyển tiếp sang service, Identity hoặc DbContext. |

### Service, model, database và cấu hình

| File/vùng | Ý nghĩa/vai trò |
|---|---|
| `Features/XeMay/Service/XeMayService.cs` | Nghiệp vụ xe: đọc/lọc, thêm/sửa/xóa, ẩn/mở bán, điều chỉnh và chuyển kho; làm việc với EF Core. Namespace hiện dùng là `BlazorXe_Xemay.Features.XeMay.Services` (thư mục tên `Service` ở dạng số ít). |
| `Features/DonHang/GioHangService.cs` | State giỏ hàng trong scope phiên Blazor và `SoSanhXeService` (cũng được đặt chung trong file này). Hai service phát event `Changed` để các component/layout cập nhật; thao tác thêm/xóa giỏ và so sánh chưa tự ghi database. |
| `Features/DonHang/DichVuDonHang.cs` | Quy tắc mã giảm giá, tạo đơn, lấy đơn và hủy đơn; ghi các bảng đơn hàng/tồn kho qua EF Core. |
| `Features/TaiKhoan/DichVuKhachHang.cs` | Các nghiệp vụ dữ liệu dành cho khách hàng như địa chỉ và yêu cầu bảo hành. |
| `Features/Admin/DichVuQuanTri.cs` | Nghiệp vụ quản trị được tách khỏi component, ví dụ trạng thái đơn và phân quyền nhân viên. |
| `Features/Shared/DichVuPhanQuyen.cs`, `DichVuNhatKy.cs` | Kiểm tra/cấp quyền và ghi nhật ký thao tác. |
| `Features/Shared/AppDbContext.cs` | EF Core DbContext của nghiệp vụ xe/đơn/admin; ánh xạ entity thành bảng SQL. Các component/service gọi DbContext trực tiếp hoặc qua `IDbContextFactory`. |
| `Features/Shared/Migrations/*` | Phiên bản schema database do EF Core tạo; không phải nơi viết logic nút. |
| `Features/Shared/KhoiTaoDuLieu.cs` | Khởi tạo schema/dữ liệu ban đầu, role/quyền và dữ liệu cửa hàng theo mã. |
| `Features/TaiKhoan/AppUser.cs`, `MoHinhNghiepVu.cs`, `Features/XeMay/Models/XeMayModel.cs`, `Data/*.cs`, `Models/*.cs` | Kiểu dữ liệu người dùng/entity/form/view model. Model mô tả dữ liệu; handler/service mới quyết định thao tác. |
| `appsettings.json`, `appsettings.Development.json` | Cấu hình môi trường và chuỗi kết nối DB. Không đặt logic nút hoặc bí mật thật vào tài liệu. |
| `BlazorXe_Xemay.csproj` | Khai báo SDK/framework và thư viện NuGet. |
| `Data/README.md`, `Data/Use-DatabaseFirst.ps1`, `Data/Use-CodeFirst.ps1` | Hướng dẫn/script quy trình schema Database First hoặc Code First. |
| `wwwroot/*` | Tài nguyên tĩnh như CSS, JavaScript, Bootstrap, favicon. Chỉ tìm ở đây khi nút được xử lý bởi JavaScript hoặc cần style; auth fetch helper nằm ở `wwwroot/auth.js`. |

## Phân biệt API, service và chuyển trang

- **HTTP API/endpoint**: ứng dụng có các endpoint `/auth/*` map trong `Program.cs`. Khi cần kiểm tra API thật, tìm `MapGet`, `MapPost`, `MapPut`, `MapDelete`, `HttpClient`, `GetAsync`, `PostAsync`. Hiện trong mã ứng dụng không thấy các component gọi `HttpClient` để gọi API nghiệp vụ.
- **Service nội bộ**: `XeMayService`, `DichVuDonHang`, `DichVuKhachHang`, `GioHangService` là class C# được inject bằng DI. Gọi service không đồng nghĩa gọi HTTP API; trong ứng dụng này chúng chạy phía server và thao tác state/EF Core.
- **Chuyển trang**: `<NavLink>`, `<a href>`, `Navigation.NavigateTo(...)` đổi URL. Trang đích không phải “API”; nó là component khác được Router chọn.
- **Form GET**: tìm kiếm trong `UserLayout` gửi querystring đến `/san-pham`, trang đích đọc query và tải kết quả.
- **Form Blazor**: `EditForm OnValidSubmit` gọi trực tiếp method C# của component sau khi validation hợp lệ; không tự tạo REST request.

### Cách lần theo hoặc thêm một nút mới

1. Tìm nhãn nút trong markup `.razor`; xem `@onclick`, `type="submit"`, `EditForm OnValidSubmit` hoặc `@bind`.
2. Tìm tên method handler ở khối `@code` cùng file. Nếu không có handler C#, kiểm tra form HTML, JavaScript trong `wwwroot`, hoặc link route.
3. Từ handler lần theo service được inject (`@inject`) hoặc `IDbContextFactory`/`UserManager`; kiểm tra service đã đăng ký trong `Program.cs`.
4. Xác định đích: state UI, route khác, bảng database, Identity hay HTTP endpoint. Ghi rõ điểm này để không gọi nhầm mọi thao tác là API.
5. Sau khi ghi dữ liệu, kiểm tra handler có tải lại dữ liệu/thông báo kết quả và component liên quan có đăng ký/hủy event `Changed` phù hợp.
6. Nếu thật sự cần API HTTP mới, tạo endpoint ở `Program.cs` hoặc một lớp endpoint phù hợp, xác thực/ủy quyền và antiforgery theo kiểu request; chỉ dùng `HttpClient` khi gọi dịch vụ HTTP độc lập. Còn luồng Blazor Server nội bộ hiện tại thường gọi service trực tiếp.

