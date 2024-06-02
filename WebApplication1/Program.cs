using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Security.Claims;
using WebApplication1.Data;
using WebApplication1.Helpers;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();


// connection with database
builder.Services.AddDbContext<EcommerceContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("HShop"));
});

// session để lưu đơn hàng trên server
builder.Services.AddDistributedMemoryCache();

// coockie
// https://learn.microsoft.com/en-us/aspnet/core/security/authentication/cookie?view=aspnetcore-8.0
//builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
//    .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme,
//        options =>
//        {
//            options.LoginPath = "/KhachHang/DangNhap";
//            //options.LoginPath = "/Admin/AccountAdmin/LoginAdmin";
//            options.AccessDeniedPath = "/AccessDenied"; // nếu user đó ko có quyền truy cập thì sẽ chuyển tới trang AccessDenied vd như thông báo lỗi
//        });


builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
})
    .AddCookie("UserCookies",
        options =>
        {
            options.LoginPath = "/KhachHang/DangNhap";
            //options.LoginPath = "/Admin/AccountAdmin/LoginAdmin";
            options.AccessDeniedPath = "/AccessDenied"; // nếu user đó ko có quyền truy cập thì sẽ chuyển tới trang AccessDenied vd như thông báo lỗi
            options.Cookie.Name = "UserAuthCookie";
        })
    .AddCookie("AdminCookies", options =>
    {
        options.LoginPath = "/Admin/AccountAdmin/LoginAdmin";
        options.AccessDeniedPath = "/Admin/AccessDenied";
        options.Cookie.Name = "AdminAuthCookie";
    });


builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("RequireAdminRole", policy => policy.RequireClaim(ClaimTypes.Role, "Admin"));
    options.AddPolicy("RequireUserRole", policy => policy.RequireClaim(ClaimTypes.Role, "Customer"));
});


builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromDays(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// đăng kí AutoMapper
// https://docs.automapper.org/en/stable/Dependency-injection.html
builder.Services.AddAutoMapper(typeof(AutoMapperProfile));


// đăng kí PaypalClient dạng Singleton() --> chỉ có 1 instance duy nhất trong toàn ứng dụng
builder.Services.AddSingleton(x => new PaypalClient(
    builder.Configuration["PaypalOpyions:AppId"],
    builder.Configuration["PaypalOpyions:AppSecret"],
    builder.Configuration["PaypalOpyions:Mode"]

));



var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.UseSession();


app.MapControllerRoute(
  name: "areas",
  pattern: "{area:exists}/{controller=HomeAdmin}/{action=Index}/{id?}"
);

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
