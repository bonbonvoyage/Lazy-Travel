using Amazon.S3;
using LazyTravel.Shared.Services;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDataProtection()
	   .UseEphemeralDataProtectionProvider();

builder.Services.AddControllersWithViews();
builder.Services.AddHttpContextAccessor();

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

// 會員大頭貼上傳：R2 帳號金鑰還沒建好之前，先用本機 wwwroot/uploads/avatars/ 代替。
// 用 Keyed Service 註冊，只有 MemberProfileService 會拿到這個 "avatar" 版本，
// 不會影響上面 VlogPost 用的那個（沒有 key，還是走 R2）。
// LocalImageStorageService 建構子要吃 webRootPath 字串（不是 IWebHostEnvironment——
// LazyTravel.Shared 這個類別庫沒有參考 ASP.NET Core 的 Hosting 套件），這裡用
// builder.Environment.WebRootPath 帶進去。
// 等組長把正式的 R2 帳號、appsettings 的 CloudflareR2 設定都填好之後，
// 把下面這個 factory 換成回傳 new R2ImageStorageService(...) 就好，
// 其他程式碼完全不用動。
builder.Services.AddKeyedScoped<LazyTravel.Shared.Services.IImageStorageService>("avatar", (sp, key) =>
	new LazyTravel.Shared.Services.LocalImageStorageService(builder.Environment.WebRootPath));

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

app.MapControllerRoute(
	name: "default",
	pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
