using LazyTravel.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// MVC
builder.Services.AddControllersWithViews();

// EF Core Power Tools 反向工程的完整版 Context(涵蓋全部資料表)。
// 舊版的 LazyTravelContext(只涵蓋 4 張表,範圍是這個的子集)已淘汰移除。
builder.Services.AddDbContext<LazyTravel.Models.EfModels.LazyTravelDBContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// 通知 / 操作紀錄共用 Service(暫時實作,待 14 洪欣茹 完成 /Admin/Notifications、AdminLogs 後抽換)
builder.Services.AddScoped<LazyTravel.Services.INotificationService, LazyTravel.Services.NotificationService>();
builder.Services.AddScoped<LazyTravel.Services.IAdminLogService, LazyTravel.Services.AdminLogService>();

// 會員停權 Service(暫時實作,待 10 黃浚翔 完成 /Admin/Members 後抽換)
builder.Services.AddScoped<IMemberModerationService, MemberModerationService>();

// 會員管理控制台(10 黃浚翔,/Admin/Members)
builder.Services.AddScoped<IMemberService, MemberService>();

// 員工管理控制台(10 黃浚翔,/Admin/Employees、/Admin/Roles)
builder.Services.AddScoped<IEmployeeService, EmployeeService>();

// 檢舉中心商業邏輯(藍培碩負責),Controller 只呼叫這層
builder.Services.AddScoped<LazyTravel.Services.IReportService, LazyTravel.Services.ReportService>();

// ========================================================
// 🌟 1. 註冊後台專屬的 Cookie 身分驗證機制 (AdminAuth)
// 確保與未來的「前台會員登入」完全隔離
// ========================================================
builder.Services.AddAuthentication("AdminAuth")
	.AddCookie("AdminAuth", options =>
	{
		options.LoginPath = "/Admin/Auth/Login"; // 沒登入的人會被踢到這裡
		options.AccessDeniedPath = "/Admin/Auth/AccessDenied"; // 登入但權限不夠會被踢到這裡
		options.Cookie.Name = "LazyTravel.Admin.Session"; // 專屬的 Cookie 名稱
	});

// ========================================================
// 🌟 2. 註冊 RBAC 授權原則 (Policies)
// 綁定最新企業級資料庫的細粒度權限 (PermissionCode)
// ========================================================
builder.Services.AddAuthorization(options =>
{
	// --- 總覽 ---
	options.AddPolicy("RequireDashboardRead", policy => policy.RequireClaim("Permission", "system:dashboard:read"));

	// --- 會員與審核中心 ---
	options.AddPolicy("RequireMemberRead", policy => policy.RequireClaim("Permission", "member:account:read"));
	options.AddPolicy("RequireReportRead", policy => policy.RequireClaim("Permission", "content:report:read"));

	// (保留操作權限供 Controller 內使用)
	options.AddPolicy("RequireMemberBlock", policy => policy.RequireClaim("Permission", "member:account:block"));
	options.AddPolicy("RequireReportAudit", policy => policy.RequireClaim("Permission", "content:report:audit"));

	// --- 社群內容管理 ---
	options.AddPolicy("RequireVlogRead", policy => policy.RequireClaim("Permission", "content:vlog:read"));
	options.AddPolicy("RequireForumRead", policy => policy.RequireClaim("Permission", "content:forum:read"));
	options.AddPolicy("RequireTravelGroupRead", policy => policy.RequireClaim("Permission", "social:travelgroup:read"));
	options.AddPolicy("RequireContentDelete", policy => policy.RequireClaim("Permission", "content:vlog:delete"));

	// --- 財務與數據 ---
	options.AddPolicy("RequirePlanRead", policy => policy.RequireClaim("Permission", "finance:plan:read"));
	options.AddPolicy("RequireSplitRead", policy => policy.RequireClaim("Permission", "finance:split:read"));
	options.AddPolicy("RequireAnalyticsRead", policy => policy.RequireClaim("Permission", "system:analytics:read"));

	// --- 系統 ---
	options.AddPolicy("RequireNotificationRead", policy => policy.RequireClaim("Permission", "system:notification:read"));

	// --- 內部管理 ---
	options.AddPolicy("RequireEmployeeRead", policy => policy.RequireClaim("Permission", "system:employee:read"));
	options.AddPolicy("RequireRoleManage", policy => policy.RequireClaim("Permission", "system:role:manage"));
	options.AddPolicy("RequireSystemManage", policy => policy.RequireClaim("Permission", "system:employee:manage"));
});

var app = builder.Build(); if (!app.Environment.IsDevelopment())
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