using EMS_MVC.Data;
using EMS_MVC.Data.Seed;
using EMS_MVC.Models.Entities;
using EMS_MVC.Services.Implementations;
using EMS_MVC.Services.Interfaces;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// 1. Cấu hình Database & Entity Framework Core 10 (Trách nhiệm: Ngô Trọng Thiện)
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? "Server=localhost;Database=QLyDRL;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true";

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

// 2. Cấu hình Password Hasher an toàn (ASP.NET Core Identity PasswordHasher)
builder.Services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();

// 3. Đăng ký các dịch vụ nghiệp vụ Service Layer (Trách nhiệm: Lê Thành Hy & Thiện)
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IActivityService, ActivityService>();
builder.Services.AddScoped<IRegistrationService, RegistrationService>();
builder.Services.AddScoped<IAttendanceService, AttendanceService>();
builder.Services.AddScoped<IAdminService, AdminService>();
builder.Services.AddScoped<IProfileService, ProfileService>();

// 4. Cấu hình Cookie Authentication & Role-based Authorization (Trách nhiệm: Lê Thành Hy)
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.Cookie.Name = "EMS_MVC_AuthCookie";
        options.Cookie.HttpOnly = true;
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
    });

builder.Services.AddAuthorization();

// 5. Cấu hình MVC Controllers & Razor Views (Trách nhiệm: Lê Thành Đạt)
builder.Services.AddControllersWithViews();

var app = builder.Build();

// 6. Khởi tạo dữ liệu mẫu và tài khoản phát triển an toàn
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        var hasher = services.GetRequiredService<IPasswordHasher<User>>();
        await DbInitializer.SeedAsync(context, hasher);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Lỗi trong quá trình khởi tạo dữ liệu mẫu (DbInitializer).");
    }
}

// 7. Cấu hình HTTP request pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
