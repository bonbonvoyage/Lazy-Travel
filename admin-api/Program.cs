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

// 通知 / 操作紀錄 Service
builder.Services.AddScoped<LazyTravel.Services.INotificationService, LazyTravel.Services.NotificationService>();
builder.Services.AddScoped<LazyTravel.Services.IAdminLogService, LazyTravel.Services.AdminLogService>();

// 會員管理
builder.Services.AddScoped<IMemberModerationService, MemberModerationService>();
builder.Services.AddScoped<IMemberService, MemberService>();

// 員工與角色管理
builder.Services.AddScoped<IEmployeeService, EmployeeService>();

// 報告 / 檢舉管理
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

// 後台認證
builder.Services.AddAuthentication("AdminAuth")
	.AddCookie("AdminAuth", options =>
	{
		options.LoginPath = "/Admin/Auth/Login";
		options.AccessDeniedPath = "/Admin/Auth/AccessDenied";
		options.Cookie.Name = "LazyTravel.Admin.Session";
		options.Events.OnRedirectToLogin = context =>
		{
			if (context.Request.Headers["X-Requested-With"] == "XMLHttpRequest" ||
				context.Request.Path.StartsWithSegments("/api"))
			{
				context.Response.StatusCode = 401;
				return Task.CompletedTask;
			}
			context.Response.Redirect(context.RedirectUri);
			return Task.CompletedTask;
		};
	});

// 授權 Policies
builder.Services.AddAuthorization(options =>
{
	options.AddPolicy("RequireDashboardRead", policy => policy.RequireClaim("Permission", "system:dashboard:read"));
	options.AddPolicy("RequireMemberRead", policy => policy.RequireClaim("Permission", "member:account:read"));
	options.AddPolicy("RequireReportRead", policy => policy.RequireClaim("Permission", "content:report:read"));
	options.AddPolicy("RequireMemberBlock", policy => policy.RequireClaim("Permission", "member:account:block"));
	options.AddPolicy("RequireReportAudit", policy => policy.RequireClaim("Permission", "content:report:audit"));
	options.AddPolicy("RequireVlogRead", policy => policy.RequireClaim("Permission", "content:vlog:read"));
	options.AddPolicy("RequireVlogAudit", policy => policy.RequireClaim("Permission", "content:vlog:audit"));
	options.AddPolicy("RequireVlogCreate", policy => policy.RequireClaim("Permission", "content:vlog:create"));
	options.AddPolicy("RequireVlogUpdate", policy => policy.RequireClaim("Permission", "content:vlog:update"));
	options.AddPolicy("RequireVlogSubmit", policy => policy.RequireClaim("Permission", "content:vlog:submit"));
	options.AddPolicy("RequireContentDelete", policy => policy.RequireClaim("Permission", "content:vlog:delete"));
	options.AddPolicy("RequireVlogRestore", policy => policy.RequireClaim("Permission", "content:vlog:restore"));
	options.AddPolicy("RequireVlogPublish", policy => policy.RequireClaim("Permission", "content:vlog:publish"));
	options.AddPolicy("RequireVlogReturn", policy => policy.RequireClaim("Permission", "content:vlog:return"));
	options.AddPolicy("RequireForumRead", policy => policy.RequireClaim("Permission", "content:forum:read"));
	options.AddPolicy("RequireTravelGroupRead", policy => policy.RequireClaim("Permission", "social:travelgroup:read"));
	options.AddPolicy("RequirePlanRead", policy => policy.RequireClaim("Permission", "finance:plan:read"));
	options.AddPolicy("RequireSplitRead", policy => policy.RequireClaim("Permission", "finance:split:read"));
	options.AddPolicy("RequireAnalyticsRead", policy => policy.RequireClaim("Permission", "system:analytics:read"));
	options.AddPolicy("RequireNotificationRead", policy => policy.RequireClaim("Permission", "system:notification:read"));
	options.AddPolicy("RequireEmployeeRead", policy => policy.RequireClaim("Permission", "system:employee:read"));
	options.AddPolicy("RequireRoleManage", policy => policy.RequireClaim("Permission", "system:role:manage"));
	options.AddPolicy("RequireSystemManage", policy => policy.RequireClaim("Permission", "system:employee:manage"));
});

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
	app.UseExceptionHandler("/Admin/Error");
	app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/", () => Results.Redirect("/Admin/Dashboard"));

// Area 路由
app.MapControllerRoute(
	name: "areas",
	pattern: "{area:exists}/{controller=Dashboard}/{action=Index}/{id?}");

app.MapControllerRoute(
	name: "default",
	pattern: "{controller=Dashboard}/{action=Index}/{id?}");

app.Run();
