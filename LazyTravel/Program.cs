using LazyTravel.Models;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// MVC
builder.Services.AddControllersWithViews();

// TODO(後續):註冊 DbContext 與 Cookie 認證
// builder.Services.AddDbContext<AppDbContext>(options =>
//     options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddDbContext<LazyTravelContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// 通知 / 操作紀錄共用 Service(暫時實作,待 14 洪欣茹 完成 /Admin/Notifications、AdminLogs 後抽換)
builder.Services.AddScoped<LazyTravel.Services.INotificationService, LazyTravel.Services.NotificationService>();
builder.Services.AddScoped<LazyTravel.Services.IAdminLogService, LazyTravel.Services.AdminLogService>();

// 會員停權 Service(暫時實作,待 10 黃浚翔 完成 /Admin/Members 後抽換)
builder.Services.AddScoped<LazyTravel.Services.IMemberModerationService, LazyTravel.Services.MemberModerationService>();

// 檢舉中心商業邏輯(藍培碩負責),Controller 只呼叫這層
builder.Services.AddScoped<LazyTravel.Services.IReportService, LazyTravel.Services.ReportService>();

var app = builder.Build();


if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();

// Area 路由(必須排在 default 之前)
app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Dashboard}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
