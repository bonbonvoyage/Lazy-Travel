using Amazon.S3;
using LazyTravel.Shared.Services;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDataProtection()
	   .UseEphemeralDataProtectionProvider();

builder.Services.AddControllersWithViews();
builder.Services.AddHttpContextAccessor();

// 前台是純 JS fetch 打 POST（申請加入/退出揪團），不是傳統表單送出，
// 所以要讓 [ValidateAntiForgeryToken] 也認 Header 帶的 token，不是只認表單欄位。
builder.Services.AddAntiforgery(options => options.HeaderName = "RequestVerificationToken");

// 共享數據庫 Context
builder.Services.AddDbContext<LazyTravel.Shared.Models.EfModels.LazyTravelDBContext>(options =>
	options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// 前台所需 Services
builder.Services.AddScoped<LazyTravel.Shared.Services.INotificationService, LazyTravel.Shared.Services.NotificationService>();
builder.Services.AddScoped<LazyTravel.Shared.Services.IAdminLogService, LazyTravel.Shared.Services.AdminLogService>();
builder.Services.AddScoped<LazyTravel.Shared.Services.IMemberService, LazyTravel.Shared.Services.MemberService>();
builder.Services.AddScoped<LazyTravel.Shared.Services.IMemberModerationService, LazyTravel.Shared.Services.MemberModerationService>();
builder.Services.AddScoped<LazyTravel.Shared.Services.IReportService, LazyTravel.Shared.Services.ReportService>();
builder.Services.AddScoped<LazyTravel.Shared.Services.IReportLookupService, LazyTravel.Shared.Services.ReportLookupService>();
builder.Services.AddScoped<LazyTravel.Shared.Services.ICurrentMemberAccessor, LazyTravel.Shared.Services.CurrentMemberAccessor>();
builder.Services.AddScoped<LazyTravel.Shared.Services.IMemberProfileService, LazyTravel.Shared.Services.MemberProfileService>();
builder.Services.AddScoped<LazyTravel.Shared.Services.IContactBookService, LazyTravel.Shared.Services.ContactBookService>();

// 圖床 (Cloudflare R2)
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
builder.Services.AddScoped<LazyTravel.Shared.Services.VlogPostImageUploadService>();
builder.Services.AddScoped<LazyTravel.Shared.Services.IImageStorageService, LazyTravel.Shared.Services.R2ImageStorageService>();

// 前台登入驗證服務
builder.Services.AddScoped<IMemberAuthService, MemberAuthService>();

// 前台會員 Cookie 認證
builder.Services.AddAuthentication("MemberAuth")
	.AddCookie("MemberAuth", options =>
	{
		options.Cookie.Name = "LazyTravel.Member.Session";
		// 🌟 Vue 是獨立網域/Port 呼叫這支 API,Cookie 要能跨站帶過去,一定要 None + Secure
		options.Cookie.SameSite = SameSiteMode.None;
		options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
		options.Events.OnRedirectToLogin = context =>
		{
			context.Response.StatusCode = 401;
			return Task.CompletedTask;
		};
	});
builder.Services.AddAuthorization();

// 🌟 CORS:給 Vue 前端呼叫用。允許的網址從 appsettings.json 的 Cors:AllowedOrigins 讀,
// 之後 Vue 那邊確定實際開發網址後,只要改設定檔,不用改程式碼。
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();
builder.Services.AddCors(options =>
{
	options.AddPolicy("FrontendPolicy", policy =>
	{
		policy.WithOrigins(allowedOrigins)
			  .AllowAnyHeader()
			  .AllowAnyMethod()
			  .AllowCredentials(); // 因為要帶 Cookie,不能用 AllowAnyOrigin
	});
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
	using var scope = app.Services.CreateScope();
	var context = scope.ServiceProvider.GetRequiredService<LazyTravel.Shared.Models.EfModels.LazyTravelDBContext>();
	try
	{
		if (await context.Database.CanConnectAsync())
		{
			await VlogPostDbSeeder.SeedAsync(context);
			await TravelGroupDbSeeder.SeedAsync(context);
		}
	}
	catch (Exception ex)
	{
		app.Logger.LogWarning(ex, "灌示範資料時發生錯誤，已略過。");
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
app.UseCors("FrontendPolicy");
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
	name: "default",
	pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
