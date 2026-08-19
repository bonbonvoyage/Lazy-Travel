using Amazon.S3;
using LazyTravel.Services;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDataProtection()
	   .UseEphemeralDataProtectionProvider();

builder.Services.AddControllersWithViews();
builder.Services.AddHttpContextAccessor();

// 共享數據庫 Context
builder.Services.AddDbContext<LazyTravel.Models.EfModels.LazyTravelDBContext>(options =>
	options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// 前台所需 Services
builder.Services.AddScoped<LazyTravel.Services.INotificationService, LazyTravel.Services.NotificationService>();
builder.Services.AddScoped<LazyTravel.Services.IAdminLogService, LazyTravel.Services.AdminLogService>();
builder.Services.AddScoped<LazyTravel.Services.IMemberService, LazyTravel.Services.MemberService>();
builder.Services.AddScoped<LazyTravel.Services.IMemberModerationService, LazyTravel.Services.MemberModerationService>();
builder.Services.AddScoped<LazyTravel.Services.IReportService, LazyTravel.Services.ReportService>();
builder.Services.AddScoped<LazyTravel.Services.IReportLookupService, LazyTravel.Services.ReportLookupService>();

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
builder.Services.AddScoped<LazyTravel.Services.VlogPostImageUploadService>();
builder.Services.AddScoped<LazyTravel.Services.IImageStorageService, LazyTravel.Services.R2ImageStorageService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
	using var scope = app.Services.CreateScope();
	var context = scope.ServiceProvider.GetRequiredService<LazyTravel.Models.EfModels.LazyTravelDBContext>();
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
