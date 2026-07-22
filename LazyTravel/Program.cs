using Microsoft.EntityFrameworkCore;
using LazyTravel.Models.EfModels; // 引入 EF Core 產生的 Context 命名空間

var builder = WebApplication.CreateBuilder(args);

// MVC
builder.Services.AddControllersWithViews();

// TODO(後續):註冊 DbContext 與 Cookie 認證
builder.Services.AddDbContext<LazyTravelDBContext>(options =>
	 options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<LazyTravel.Models.Services.IMemberService, LazyTravel.Models.Services.MemberService>();
// 加入新的員工管理 Service
builder.Services.AddScoped<LazyTravel.Models.Services.IEmployeeService, LazyTravel.Models.Services.EmployeeService>();

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
