using Amazon.S3;
using LazyTravel.Services;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// ========================================================
// 強制每次重啟專案就清除所有登入狀態
// ========================================================
builder.Services.AddDataProtection()
	   .UseEphemeralDataProtectionProvider();

// MVC
builder.Services.AddControllersWithViews();

// 讓 Service 層能拿到目前這次請求的 HttpContext,藉此取得真實來源 IP
builder.Services.AddHttpContextAccessor();

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

// 檢舉「類型/類別/狀態」中文對照,查資料庫的 ReportTargetTypes/ReportReasonCategories/ReportStatuses
builder.Services.AddScoped<LazyTravel.Services.IReportLookupService, LazyTravel.Services.ReportLookupService>();

// 圖床(Cloudflare R2, S3 相容 API):Vlog 新增圖片與檢舉證據都走這裡,舊圖維持存在本機。
builder.Services.AddSingleton<IAmazonS3>(sp =>
{
	var config = sp.GetRequiredService<IConfiguration>();
	var r2Config = new AmazonS3Config
	{
		ServiceURL = config["CloudflareR2:ServiceUrl"],
		ForcePathStyle = true,
		AuthenticationRegion = "auto",
	};
	return new AmazonS3Client(config["CloudflareR2:AccessKey"], config["CloudflareR2:SecretKey"], r2Config);
});
builder.Services.AddScoped<LazyTravel.Services.VlogPostImageUploadService>();
builder.Services.AddScoped<LazyTravel.Services.IImageStorageService, LazyTravel.Services.R2ImageStorageService>();

// ========================================================
// 🌟 1. 註冊後台專屬的 Cookie 身分驗證機制 (AdminAuth)
// 確保與未來的「前台會員登入」完全隔離
// ========================================================
builder.Services.AddAuthentication("AdminAuth") // 給這個通道一個名字叫 AdminAuth
	.AddCookie("AdminAuth", options =>
	{
		// 1. 指定登入頁面的路徑 (當未登入卻硬闖 [Authorize] 的頁面時，會被踢到這裡)
		options.LoginPath = "/Admin/Auth/Login";

		// 2. 指定權限不足的導向頁面 (有登入，但缺少特定 Policy 權限時會被踢到這裡)
		options.AccessDeniedPath = "/Admin/Auth/AccessDenied";

		// 3. 儲存在瀏覽器裡的 Cookie 名稱
		options.Cookie.Name = "LazyTravel.Admin.Session";

		// (選擇性，但強烈建議) 針對 AJAX 請求的特殊處理
		// 如果前端是用 AJAX (例如你的解碼按鈕) 呼叫 API 且憑證過期，
		// 不要回傳整個登入畫面的 HTML，而是回傳 401 狀態碼讓前端 JavaScript 處理。
		options.Events.OnRedirectToLogin = context =>
		{
			if (context.Request.Headers["X-Requested-With"] == "XMLHttpRequest" ||
				context.Request.Path.StartsWithSegments("/api"))
			{
				context.Response.StatusCode = 401; // 回傳 401 Unauthorized
				return Task.CompletedTask;
			}
			context.Response.Redirect(context.RedirectUri); // 一般網頁請求則照常跳轉
			return Task.CompletedTask;
		};
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

	// --- 社群內容管理 (Vlog) ---
	// 閱讀權限 (小編與主管共用)
	options.AddPolicy("RequireVlogRead", policy => policy.RequireClaim("Permission", "content:vlog:read"));
	options.AddPolicy("RequireVlogAudit", policy => policy.RequireClaim("Permission", "content:vlog:audit"));

	// 小編專屬權限
	options.AddPolicy("RequireVlogCreate", policy => policy.RequireClaim("Permission", "content:vlog:create"));
	options.AddPolicy("RequireVlogUpdate", policy => policy.RequireClaim("Permission", "content:vlog:update"));
	options.AddPolicy("RequireVlogSubmit", policy => policy.RequireClaim("Permission", "content:vlog:submit"));
	options.AddPolicy("RequireContentDelete", policy => policy.RequireClaim("Permission", "content:vlog:delete"));
	options.AddPolicy("RequireVlogRestore", policy => policy.RequireClaim("Permission", "content:vlog:restore"));

	// 主管專屬權限
	options.AddPolicy("RequireVlogPublish", policy => policy.RequireClaim("Permission", "content:vlog:publish"));
	options.AddPolicy("RequireVlogReturn", policy => policy.RequireClaim("Permission", "content:vlog:return"));

	options.AddPolicy("RequireForumRead", policy => policy.RequireClaim("Permission", "content:forum:read"));
	options.AddPolicy("RequireTravelGroupRead", policy => policy.RequireClaim("Permission", "social:travelgroup:read"));

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

var app = builder.Build();

// 開發環境專用：VlogPosts 資料表是空的時候，把示範資料塞進去（只塞 VlogPosts/ItineraryNodes/
// PostInteractions 這三張表，不動 Members 等其他組員負責的表）。已經有資料就不會重複塞。
if (app.Environment.IsDevelopment())
{
	using var scope = app.Services.CreateScope();
	var context = scope.ServiceProvider.GetRequiredService<LazyTravel.Models.EfModels.LazyTravelDBContext>();

	// 換一台電腦(教室/別人的機器)時常常還沒建好 appsettings.Development.json，
	// 這段以前會直接拋 SqlException 讓整個服務起不來，連純靜態的前台首頁都看不到。
	// 改成連不上就跳過灌資料，讓 app 照常啟動，並在主控台留下明確訊息。
	try
	{
		if (await context.Database.CanConnectAsync())
		{
			await VlogPostDbSeeder.SeedAsync(context);
		}
		else
		{
			app.Logger.LogWarning(
				"資料庫連不上，已略過示範資料。請確認 LazyTravel/appsettings.Development.json 的 Server= " +
				"是否為這台電腦實際的 SQL Server 執行個體名稱。前台靜態頁仍可瀏覽，需要撈資料的頁面會失敗。");
		}
	}
	catch (Exception ex)
	{
		app.Logger.LogWarning(ex, "灌示範資料時發生錯誤，已略過，不影響服務啟動。");
	}
}

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
