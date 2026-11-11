using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Security.Claims;
using System.Threading.RateLimiting;
using TuTuc.Data;
using TuTuc.Services;

var builder = WebApplication.CreateBuilder(args);

// MVC: kiểm tra antiforgery cho các yêu cầu ghi và chuẩn hóa lỗi nghiệp vụ.
builder.Services.AddControllersWithViews(o =>
{
    o.Filters.Add(new Microsoft.AspNetCore.Mvc.AutoValidateAntiforgeryTokenAttribute());
    o.Filters.Add<BusinessErrorFilter>();
});
builder.Services.AddHttpContextAccessor();
builder.Services.AddDbContext<AppDbContext>(o => o.UseSqlServer(builder.Configuration.GetConnectionString("Default")));

// Đăng ký các lớp xử lý; mỗi request nhận JourneyManager và DbContext theo scope.
builder.Services.AddScoped<JourneyManager>();
builder.Services.AddSingleton<IDongHo, DongHoHeThong>();
builder.Services.AddScoped<CommunityManager>();
builder.Services.AddScoped<MarketplaceManager>();
builder.Services.AddScoped<AccountManager>();
builder.Services.AddScoped<ReferenceManager>();
builder.Services.AddScoped<PrivateDocumentStore>();
builder.Services.AddScoped<VerificationManager>();
builder.Services.AddScoped<GovernanceManager>();
builder.Services.AddScoped<TravelSearchManager>();
// Bổ sung sau A14: gợi ý đồng hành.
builder.Services.AddScoped<CompanionSuggestion>();
builder.Services.AddHostedService<GovernanceReminderService>();
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(o =>
{
    o.IdleTimeout = TimeSpan.FromHours(2);
    o.Cookie.HttpOnly = true;
    o.Cookie.IsEssential = true;
});

var dataProtection = builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(
        Path.Combine(builder.Environment.ContentRootPath, ".data", "keys")));
if (OperatingSystem.IsWindows())
    dataProtection.ProtectKeysWithDpapi();

// Cookie authentication: kiểm tra lại trạng thái tài khoản và dấu phiên ở DB.
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddCookie(o =>
{
    o.LoginPath = "/Account/Login";
    o.AccessDeniedPath = "/Account/Denied";
    o.Cookie.HttpOnly = true;
    o.Cookie.SameSite = SameSiteMode.Lax;
    o.ExpireTimeSpan = TimeSpan.FromHours(8);
    o.Events.OnValidatePrincipal = async context =>
    {
        var idText = context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier);
        var stamp = context.Principal?.FindFirstValue("session_stamp");
        if (!int.TryParse(idText, out var id))
        {
            context.RejectPrincipal();
            return;
        }

        var db = context.HttpContext.RequestServices.GetRequiredService<AppDbContext>();
        var user = await db.Accounts.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id);
        if (user == null || !user.IsActive || user.SessionStamp != stamp)
        {
            context.RejectPrincipal();
            await context.HttpContext.SignOutAsync();
        }
    };
});
builder.Services.AddAuthorization();
builder.Services.AddRateLimiter(o =>
{
    o.RejectionStatusCode = 429;
    o.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(ctx =>
        ctx.Request.Method == "POST" && ctx.Request.Path.StartsWithSegments("/Account")
            ? RateLimitPartition.GetFixedWindowLimiter(
                ctx.Connection.RemoteIpAddress?.ToString() ?? "local",
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 60,
                    Window = TimeSpan.FromMinutes(1),
                    QueueLimit = 0
                })
            : RateLimitPartition.GetNoLimiter("public"));
});

var app = builder.Build();
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    DatabaseBootstrap.Initialize(db);
    SeedData.Initialize(db, app.Configuration.GetValue<bool>("Demo:Enabled"));
    SucDataUpgrade.Apply(db, app.Environment, app.Configuration.GetValue<bool>("Demo:Enabled"));
    // Dữ liệu mẫu bổ sung sau A14 (dịch vụ ăn uống/di chuyển, chuyến đi đang diễn ra).
    DemoA14Seed.Apply(db, app.Configuration.GetValue<bool>("Demo:Enabled"));
    // Dữ liệu mẫu đầy đủ cho các điểm đến (địa điểm, nhà cung cấp, dịch vụ, nhu cầu đồng hành).
    DemoFullDataSeed.Apply(db, app.Configuration.GetValue<bool>("Demo:Enabled"));
}

if (!app.Environment.IsDevelopment())
    app.UseExceptionHandler("/Home/Error");
app.Use(async (context, next) =>
{
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    context.Response.Headers["X-Frame-Options"] = "SAMEORIGIN";
    context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    await next();
});
app.UseStatusCodePagesWithReExecute("/Home/Status", "?code={0}");
app.UseStaticFiles();
app.UseRouting();
app.UseSession();
app.UseRateLimiter();

// Xác thực danh tính trước, kiểm tra quyền truy cập sau.
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/health", async (AppDbContext db) => await db.Database.CanConnectAsync()
    ? Results.Ok(new { application = "TuTuc", status = "ready" })
    : Results.Json(new { application = "TuTuc", status = "database-unavailable" }, statusCode: 503));
app.MapControllerRoute("default", "{controller=Home}/{action=Index}/{id?}");
app.Run();
public partial class Program
{
}
