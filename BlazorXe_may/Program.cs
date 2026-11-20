using BlazorXe_Xemay.Components;
using BlazorXe_Xemay.Features.Admin;
using BlazorXe_Xemay.Features.DonHang;
using BlazorXe_Xemay.Features.Shared;
using BlazorXe_Xemay.Features.TaiKhoan;
using BlazorXe_Xemay.Features.XeMay.Services;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.EventLog;
using System.Text;

var builder = WebApplication.CreateBuilder(args);
if (OperatingSystem.IsWindows())
{
    builder.Logging.AddFilter<EventLogLoggerProvider>(_ => false);
}

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException(
        "Add ConnectionStrings:DefaultConnection to appsettings.json.");

builder.Services.AddDbContextFactory<AppDbContext>(options => options.UseSqlServer(connectionString));
builder.Services.AddHttpContextAccessor();
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddAntiforgery();

builder.Services
    .AddIdentity<AppUser, IdentityRole<int>>(options =>
    {
        options.SignIn.RequireConfirmedAccount = false;
        options.Password.RequiredLength = 10;
        options.Password.RequireDigit = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireUppercase = true;
        options.Password.RequireNonAlphanumeric = true;
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
        options.User.RequireUniqueEmail = true;
    })
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.Name = "MotoHub.Auth";
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
        ? CookieSecurePolicy.SameAsRequest
        : CookieSecurePolicy.Always;
    options.LoginPath = "/dang-nhap";
    options.AccessDeniedPath = "/tu-choi-truy-cap";
});

builder.Services.AddScoped<XeMayService>();
builder.Services.AddScoped<DichVuPhanQuyen>();
builder.Services.AddScoped<DichVuNhatKy>();
builder.Services.AddScoped<DichVuDonHang>();
builder.Services.AddScoped<DichVuKhachHang>();
builder.Services.AddScoped<GioHangService>();
builder.Services.AddScoped<SoSanhXeService>();
builder.Services.AddScoped<DichVuQuanTri>();

var app = builder.Build();

if (args.Contains("--create-admin", StringComparer.OrdinalIgnoreCase))
{
    Environment.ExitCode = await CreateAdminAsync(app.Services);
    await app.DisposeAsync();
    return;
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

await using (var scope = app.Services.CreateAsyncScope())
{
    await KhoiTaoDuLieu.KhoiTaoAsync(scope.ServiceProvider);
}

app.MapGet("/auth/csrf", (HttpContext context, IAntiforgery antiforgery) =>
{
    context.Response.Headers.CacheControl = "no-store";
    var tokens = antiforgery.GetAndStoreTokens(context);
    return Results.Json(new { requestToken = tokens.RequestToken });
}).AllowAnonymous();

app.MapPost("/auth/login", async (
    HttpContext context,
    SignInManager<AppUser> signInManager,
    UserManager<AppUser> userManager,
    IAntiforgery antiforgery) =>
{
    try
    {
        await antiforgery.ValidateRequestAsync(context);
    }
    catch (AntiforgeryValidationException)
    {
        return Results.BadRequest("Phiên đăng nhập đã hết hạn. Hãy tải lại trang và thử lại.");
    }

    var form = await context.Request.ReadFormAsync();
    var email = form["email"].ToString().Trim();
    var password = form["password"].ToString();
    var user = await userManager.FindByEmailAsync(email);
    if (user is null || !user.DangHoatDong)
    {
        return Results.Redirect("/dang-nhap?error=invalid");
    }

    var result = await signInManager.PasswordSignInAsync(
        user, password, isPersistent: form["remember"] == "on", lockoutOnFailure: true);

    if (!result.Succeeded)
    {
        return Results.Redirect(result.IsLockedOut
            ? "/dang-nhap?error=locked"
            : "/dang-nhap?error=invalid");
    }

    var administrativeRoles = new HashSet<string>(StringComparer.Ordinal)
    {
        "QuanTriVien", "ChuCuaHang", "QuanLy", "BanHang", "Kho", "KeToan", "BaoHanh", "NhanVien"
    };
    var userRoles = await userManager.GetRolesAsync(user);
    var defaultReturnUrl = userRoles.Any(administrativeRoles.Contains) ? "/admin/dashboard" : "/";
    return Results.LocalRedirect(GetLocalReturnUrl(form["returnUrl"].ToString(), defaultReturnUrl));
}).AllowAnonymous();

app.MapPost("/auth/register", async (
    HttpContext context,
    UserManager<AppUser> userManager,
    SignInManager<AppUser> signInManager,
    IAntiforgery antiforgery) =>
{
    try
    {
        await antiforgery.ValidateRequestAsync(context);
    }
    catch (AntiforgeryValidationException)
    {
        return Results.BadRequest("Phiên đăng ký đã hết hạn. Hãy tải lại trang và thử lại.");
    }

    var form = await context.Request.ReadFormAsync();
    var email = form["email"].ToString().Trim();
    var fullName = form["fullName"].ToString().Trim();
    var password = form["password"].ToString();
    if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(fullName))
    {
        return Results.Redirect("/dang-nhap?error=registration");
    }

    var user = new AppUser
    {
        UserName = email,
        Email = email,
        HoTen = fullName,
        EmailConfirmed = false
    };
    var creation = await userManager.CreateAsync(user, password);
    if (!creation.Succeeded)
    {
        return Results.Redirect("/dang-nhap?error=registration");
    }

    var roleResult = await userManager.AddToRoleAsync(user, "User");
    if (!roleResult.Succeeded)
    {
        await userManager.DeleteAsync(user);
        return Results.Problem("Không thể gán quyền khách hàng. Vui lòng thử lại sau.");
    }

    await signInManager.SignInAsync(user, isPersistent: false);
    return Results.LocalRedirect("/");
}).AllowAnonymous();

app.MapPost("/auth/logout", async (
    HttpContext context,
    SignInManager<AppUser> signInManager,
    IAntiforgery antiforgery) =>
{
    try
    {
        await antiforgery.ValidateRequestAsync(context);
    }
    catch (AntiforgeryValidationException)
    {
        return Results.BadRequest("Phiên đăng xuất đã hết hạn. Hãy tải lại trang.");
    }

    await signInManager.SignOutAsync();
    return Results.LocalRedirect("/");
}).RequireAuthorization();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();

static string GetLocalReturnUrl(string? candidate, string fallback)
{
    return !string.IsNullOrWhiteSpace(candidate)
        && candidate.StartsWith('/') && !candidate.StartsWith("//")
        && !candidate.StartsWith("/\\")
        ? candidate
        : fallback;
}

static async Task<int> CreateAdminAsync(IServiceProvider services)
{
    try
    {
        await using var scope = services.CreateAsyncScope();
        var scopedServices = scope.ServiceProvider;
        await KhoiTaoDuLieu.KhoiTaoAsync(scopedServices);

        var roleManager = scopedServices.GetRequiredService<RoleManager<IdentityRole<int>>>();
        const string adminRoleName = "QuanTriVien";
        if (!await roleManager.RoleExistsAsync(adminRoleName))
        {
            var roleResult = await roleManager.CreateAsync(new IdentityRole<int>(adminRoleName));
            if (!roleResult.Succeeded)
            {
                Console.Error.WriteLine(string.Join(Environment.NewLine, roleResult.Errors.Select(error => error.Description)));
                return 1;
            }
        }

        Console.Write("Email admin: ");
        var email = Console.ReadLine()?.Trim();
        if (string.IsNullOrWhiteSpace(email))
        {
            Console.Error.WriteLine("Email không được để trống.");
            return 1;
        }

        var userManager = scopedServices.GetRequiredService<UserManager<AppUser>>();
        if (await userManager.FindByEmailAsync(email) is not null)
        {
            Console.Error.WriteLine("Email này đã có tài khoản. Lệnh không tự đổi tài khoản hiện có thành admin.");
            return 1;
        }

        Console.Write("Họ tên admin: ");
        var fullName = Console.ReadLine()?.Trim();
        if (string.IsNullOrWhiteSpace(fullName))
        {
            Console.Error.WriteLine("Họ tên không được để trống.");
            return 1;
        }

        var password = ReadConsolePassword("Mật khẩu admin: ");
        var confirmPassword = ReadConsolePassword("Nhập lại mật khẩu: ");
        if (password != confirmPassword)
        {
            Console.Error.WriteLine("Hai mật khẩu không khớp.");
            return 1;
        }

        var user = new AppUser
        {
            UserName = email,
            Email = email,
            HoTen = fullName,
            EmailConfirmed = true
        };
        var createResult = await userManager.CreateAsync(user, password);
        if (!createResult.Succeeded)
        {
            Console.Error.WriteLine(string.Join(Environment.NewLine, createResult.Errors.Select(error => error.Description)));
            return 1;
        }

        var assignRoleResult = await userManager.AddToRoleAsync(user, adminRoleName);
        if (!assignRoleResult.Succeeded)
        {
            Console.Error.WriteLine("Đã tạo tài khoản nhưng chưa gán được vai trò admin:");
            Console.Error.WriteLine(string.Join(Environment.NewLine, assignRoleResult.Errors.Select(error => error.Description)));
            return 1;
        }

        Console.WriteLine($"Đã tạo tài khoản admin: {email}");
        return 0;
    }
    catch (Exception exception)
    {
        Console.Error.WriteLine($"Không tạo được tài khoản admin: {exception.Message}");
        return 1;
    }
}

static string ReadConsolePassword(string prompt)
{
    Console.Write(prompt);
    var password = new StringBuilder();
    while (true)
    {
        var key = Console.ReadKey(intercept: true);
        if (key.Key == ConsoleKey.Enter)
        {
            Console.WriteLine();
            return password.ToString();
        }

        if (key.Key == ConsoleKey.Backspace)
        {
            if (password.Length > 0)
            {
                password.Length--;
            }
            continue;
        }

        if (!char.IsControl(key.KeyChar))
        {
            password.Append(key.KeyChar);
        }
    }
}
