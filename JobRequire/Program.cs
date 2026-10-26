using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.Cookies;
using DinkToPdf;
using DinkToPdf.Contracts;
using JobRequire.Data;
using JobRequire.Services;
using JobRequire.ViewModels;
using JobRequire.Configuration;

var builder = WebApplication.CreateBuilder(args);

// =========================================
// Set DataDirectory for LocalDB .mdf file
// =========================================
var appDataPath = Path.Combine(builder.Environment.ContentRootPath, "App_Data");
if (!Directory.Exists(appDataPath))
{
    Directory.CreateDirectory(appDataPath);
}
AppDomain.CurrentDomain.SetData("DataDirectory", appDataPath);

// =========================================
// Configure Services
// =========================================

// Add MVC with Views and configure file upload limits
builder.Services.AddControllersWithViews();

// Configure file upload settings
builder.Services.Configure<FileUploadSettings>(options =>
{
    options.MaxFileSizeBytes = 10 * 1024 * 1024; // 10 MB
    options.AllowedExtensions = new[] { ".pdf", ".docx", ".doc", ".jpg", ".jpeg", ".png" };
    options.AllowedMimeTypes = new[]
    {
        "application/pdf",
        "application/msword",
        "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        "image/jpeg",
        "image/png"
    };
    options.UploadPath = "ProtectedFiles";
    options.UseProtectedFolder = true;
});

// Configure request body size for file uploads
builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = 50 * 1024 * 1024; // 50 MB max request size
});

// Add DbContext with SQL Server (replace {AppData} placeholder with actual path)
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")!
    .Replace("{AppData}", appDataPath);
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

// Add Cookie Authentication
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromDays(7);
        options.SlidingExpiration = true;
        options.Cookie.Name = "JobRecruitment.Auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    });

// Add Authorization policies
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
    options.AddPolicy("EmployerOnly", policy => policy.RequireRole("Employer", "Admin"));
    options.AddPolicy("JobSeekerOnly", policy => policy.RequireRole("JobSeeker", "Admin"));
});

// Add Session support
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Add HttpContextAccessor
builder.Services.AddHttpContextAccessor();

// Configure Email Settings for Gmail SMTP
builder.Services.Configure<JobRequire.Configuration.EmailSettings>(
    builder.Configuration.GetSection("EmailSettings"));

// Register custom services
builder.Services.AddScoped<IPasswordHasher, PasswordHasher>();
// Use GmailEmailService for real email sending, or FileEmailSender for file-based demo
builder.Services.AddScoped<IEmailSender, GmailEmailService>();
builder.Services.AddScoped<IResumeService, ResumeService>();
builder.Services.AddScoped<IResumeCompletionService, ResumeCompletionService>();
builder.Services.AddScoped<IFileUploadService, FileUploadService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IJobInterestService, JobInterestService>();
builder.Services.AddScoped<IChatService, ChatService>();
builder.Services.AddScoped<ICompanyReviewService, CompanyReviewService>();

// Register background service for auto-hiding reviews after 3 days
builder.Services.AddHostedService<ReviewAutoHideBackgroundService>();

// Register DinkToPdf converter (singleton for thread safety)
builder.Services.AddSingleton(typeof(IConverter), new SynchronizedConverter(new PdfTools()));
builder.Services.AddScoped<IPdfService, PdfService>();

var app = builder.Build();

// =========================================
// Ensure Database is Created (Auto-Migration)
// =========================================
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    dbContext.Database.EnsureCreated();
    
    // =========================================
    // DATABASE DATA RESET (ONE-TIME USE)
    // =========================================
    // UNCOMMENT THE LINE BELOW TO CLEAR ALL DATA:
    // await DatabaseResetUtility.ClearAllDataAsync(dbContext);
    // 
    // After running once, COMMENT IT BACK OUT!
    // This clears ALL rows but preserves schema.
    // =========================================
}

// =========================================
// Configure HTTP Request Pipeline
// =========================================

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}
else
{
    app.UseDeveloperExceptionPage();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// Authentication & Authorization middleware (order matters!)
app.UseAuthentication();
app.UseAuthorization();

app.UseSession();

// Configure routing
app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
