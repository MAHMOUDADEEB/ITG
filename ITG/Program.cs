using System.Globalization;
using ITG;
using ITG.Constants;
using ITG.Data;
using ITG.Data.Seed;
using ITG.Models.Entities;
using ITG.Services.Ai;
using ITG.Services.Implementations;
using ITG.Services.Interfaces;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Localization;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// ---------- Hosting (online server) ----------
// Keeps the sign-in keys in App_Data, so users stay signed in when the server restarts the site
if (!builder.Environment.IsDevelopment())
{
    builder.Services.AddDataProtection()
        .PersistKeysToFileSystem(new DirectoryInfo(Path.Combine(builder.Environment.ContentRootPath, "App_Data", "keys")))
        .SetApplicationName("ITG");
}

// ---------- Database ----------
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(connectionString));

// ---------- Identity ----------
builder.Services.AddIdentity<Employee, IdentityRole>(options =>
{
    options.User.RequireUniqueEmail = true;

    options.Password.RequiredLength = 8;
    options.Password.RequireDigit = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireNonAlphanumeric = false;

    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
})
.AddEntityFrameworkStores<AppDbContext>()
.AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/AccessDenied";
});

// ---------- Localization (Arabic / English) ----------
builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");

builder.Services.Configure<RequestLocalizationOptions>(options =>
{
    var cultures = AppCultures.All.Select(c => new CultureInfo(c)).ToList();

    options.DefaultRequestCulture = new RequestCulture(AppCultures.English);
    options.SupportedCultures = cultures;
    options.SupportedUICultures = cultures;

    // The language is chosen by the user (cookie), not by the browser settings
    options.RequestCultureProviders = new List<IRequestCultureProvider>
    {
        new QueryStringRequestCultureProvider(),
        new CookieRequestCultureProvider()
    };
});

// ---------- Application services ----------
builder.Services.AddScoped<IFileStorageService, FileStorageService>();
builder.Services.AddScoped<ICareerService, CareerService>();
builder.Services.AddScoped<ICertificateService, CertificateService>();
builder.Services.AddScoped<ISkillAssessmentService, SkillAssessmentService>();
builder.Services.AddScoped<IExamService, ExamService>();
builder.Services.AddScoped<ICertificateReviewService, CertificateReviewService>();
builder.Services.AddScoped<IPromotionService, PromotionService>();
builder.Services.AddScoped<IAiChatService, AiChatService>();
builder.Services.AddScoped<IRecordsService, RecordsService>();
builder.Services.AddScoped<ISearchService, SearchService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IReadinessService, ReadinessService>();
builder.Services.AddScoped<IDevelopmentPlanService, DevelopmentPlanService>();
builder.Services.AddScoped<IAnalyticsService, AnalyticsService>();

// ---------- AI (Google Gemini) ----------
// The API key lives in User Secrets (right-click the project > Manage User Secrets)
builder.Services.Configure<GeminiOptions>(builder.Configuration.GetSection(GeminiOptions.SectionName));
builder.Services.AddHttpClient<IAiClient, GeminiClient>(http =>
{
    var seconds = builder.Configuration.GetValue<int?>($"{GeminiOptions.SectionName}:TimeoutSeconds") ?? 120;
    http.Timeout = TimeSpan.FromSeconds(seconds);
});

// Keeps the skills-assessment questions between "start" and "submit"
builder.Services.AddMemoryCache();

// ---------- MVC ----------
builder.Services.AddControllersWithViews()
    .AddDataAnnotationsLocalization(options =>
        // Validation messages and display names are translated from SharedResource
        options.DataAnnotationLocalizerProvider = (type, factory) =>
            factory.Create(typeof(SharedResource)));

var app = builder.Build();

// Seed roles, admin account, and demo data
// (demo data in Development, or online when "SeedDemoData": true is set in appsettings.Production.json)
try
{
    await DbSeeder.SeedAsync(app.Services,
        app.Environment.IsDevelopment() || app.Configuration.GetValue<bool>("SeedDemoData"));
}
catch (Exception ex)
{
    // TEMPORARY (online setup): show why the site could not start instead of a blank 500 page.
    // Remove this block once the hosted site works.
    app.Logger.LogError(ex, "The site could not start");
    app.Run(async context =>
    {
        context.Response.StatusCode = 500;
        context.Response.ContentType = "text/plain; charset=utf-8";
        await context.Response.WriteAsync("ITG could not start (setup diagnostics):\n\n" + ex);
    });
    await app.RunAsync();
    return;
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRequestLocalization();
app.UseRouting();

// Authentication must come before Authorization
app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

// Admin area route (must be before the default route)
app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Dashboard}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();