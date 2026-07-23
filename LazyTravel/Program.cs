using LazyTravel.Models;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// MVC
builder.Services.AddControllersWithViews();

builder.Services.AddDbContext<LazyTravelContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// 會員管理(10 黃浚翔)使用的 EF Core Power Tools 反向工程 Context,獨立於上面的 LazyTravelContext
builder.Services.AddDbContext<LazyTravel.Models.EfModels.LazyTravelDBContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// 通知 / 操作紀錄共用 Service(暫時實作,待 14 洪欣茹 完成 /Admin/Notifications、AdminLogs 後抽換)
builder.Services.AddScoped<LazyTravel.Services.INotificationService, LazyTravel.Services.NotificationService>();
builder.Services.AddScoped<LazyTravel.Services.IAdminLogService, LazyTravel.Services.AdminLogService>();

// 會員停權 Service(暫時實作,待 10 黃浚翔 完成 /Admin/Members 後抽換)
builder.Services.AddScoped<LazyTravel.Services.IMemberModerationService, LazyTravel.Services.MemberModerationService>();

// 會員管理控制台(10 黃浚翔,/Admin/Members)
builder.Services.AddScoped<LazyTravel.Models.Services.IMemberService, LazyTravel.Models.Services.MemberService>();

// 員工管理控制台(10 黃浚翔,/Admin/Employees、/Admin/Roles)
builder.Services.AddScoped<LazyTravel.Models.Services.IEmployeeService, LazyTravel.Models.Services.EmployeeService>();

// 檢舉中心商業邏輯(藍培碩負責),Controller 只呼叫這層
builder.Services.AddScoped<LazyTravel.Services.IReportService, LazyTravel.Services.ReportService>();

// ========================================================
// 1. 註冊 Cookie 身分驗證機制
// ========================================================
builder.Services.AddAuthentication(Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationDefaults.AuthenticationScheme)
	.AddCookie(options =>
	{
		options.LoginPath = "/Admin/Auth/Login"; // 如果沒登入，會自動被踢到這個網址
		options.AccessDeniedPath = "/Admin/Auth/AccessDenied"; // 如果登入了但權限不足，會導向這裡
	});

// ========================================================
// 2. 註冊 RBAC 授權原則 (Policies)
// 這裡嚴格對應你規格書中的「權限代碼」
// ========================================================
builder.Services.AddAuthorization(options =>
{
	options.AddPolicy("RequireAuditPermission", policy => policy.RequireClaim("Permissions", "Mod_Audit"));
	options.AddPolicy("RequireContentPermission", policy => policy.RequireClaim("Permissions", "Mod_Content"));
	options.AddPolicy("RequireMemberPermission", policy => policy.RequireClaim("Permissions", "Mod_Member"));
	options.AddPolicy("RequireSecurityPermission", policy => policy.RequireClaim("Permissions", "Mod_Security"));
	options.AddPolicy("RequireFinancePermission", policy => policy.RequireClaim("Permissions", "Mod_Finance"));
	options.AddPolicy("RequireDataPermission", policy => policy.RequireClaim("Permissions", "Mod_Data"));
	options.AddPolicy("RequireSystemPermission", policy => policy.RequireClaim("Permissions", "Mod_System"));
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

// ========================================================
// 3. 啟用驗證與授權 (⚠️ 注意：這兩行必須放在 UseRouting 和 MapControllerRoute 之間)
// ========================================================
app.UseAuthentication();
app.UseAuthorization();

// Area 路由(必須排在 default 之前)
app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Dashboard}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
