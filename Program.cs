using Laundry.Data;
using Laundry.Services;
using Laundry.Patterns.Singleton;
using Laundry.Hubs;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.Cookies;

var builder = WebApplication.CreateBuilder(args);

AppConfigManager.Instance.Initialize(builder.Configuration);

// Add services to container
builder.Services.AddControllersWithViews();

// Add DbContext
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Add authentication
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Home/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromDays(7);
        options.SlidingExpiration = true;
    });

// Add HttpContextAccessor and Session
builder.Services.AddHttpContextAccessor();
builder.Services.AddSession();

// Add SignalR
builder.Services.AddSignalR();

// Register services
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<OrderService>();
builder.Services.AddScoped<NotificationService>();
builder.Services.AddScoped<LoyaltyService>();
builder.Services.AddScoped<SubscriptionService>();
builder.Services.AddScoped<SubscriptionPlanService>(); // NEW: Register SubscriptionPlanService
builder.Services.AddScoped<RouteOptimizationService>();
builder.Services.AddScoped<PaymentService>();

var app = builder.Build();

// ====== EXECUTE IDENTITY-SAFE DATABASE INITIALIZATION SEEDS ======
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<ApplicationDbContext>();

        // This safely verifies your local model metadata columns match SQL server maps
        context.Database.EnsureCreated();

        // Run our validation check logic to insert users safely
        ApplicationDbContext.SeedDatabasePool(context);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An issue occurred during startup database seeding.");
    }
}

// Configure pipeline
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
app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

// Map SignalR hubs
app.MapHub<NotificationHub>("/notificationHub");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Initialize database
using (var scope = app.Services.CreateScope())
{
    try
    {
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await dbContext.Database.EnsureCreatedAsync();
        OrderLogger.Instance.LogEvent("Database initialized successfully", EventLevel.Info);
    }
    catch (Exception ex)
    {
        OrderLogger.Instance.LogEvent($"Database initialization error: {ex.Message}", EventLevel.Error);
    }
}

OrderLogger.Instance.LogEvent($"Application started at {DateTime.Now:yyyy-MM-dd HH:mm:ss}", EventLevel.Info);

app.Run();