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

app.MapControllerRoute(
	name: "default",
	pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
