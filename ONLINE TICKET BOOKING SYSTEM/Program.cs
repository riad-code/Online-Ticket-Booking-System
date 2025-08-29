using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;
using ONLINE_TICKET_BOOKING_SYSTEM.Data;
using ONLINE_TICKET_BOOKING_SYSTEM.Models;
using ONLINE_TICKET_BOOKING_SYSTEM.Services;
using QuestPDF.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// ---- QuestPDF ----
QuestPDF.Settings.License = LicenseType.Community;

// ---- Database ----
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseSqlServer(connectionString));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

// ---- Identity ----
builder.Services.AddDefaultIdentity<ApplicationUser>(options =>
{
    options.SignIn.RequireConfirmedAccount = false;
    options.SignIn.RequireConfirmedEmail = false;
})
.AddRoles<IdentityRole>()
.AddEntityFrameworkStores<ApplicationDbContext>();

// ---- MVC & Razor ----
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

// ---- Session ----
builder.Services.AddMemoryCache();
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(10);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// ---- Email ----
builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("EmailSettings"));
builder.Services.AddTransient<IEmailSender, EmailSender>();

// ---- Core App Services ----
builder.Services.AddScoped<ITicketPdfService, TicketPdfService>();
builder.Services.AddScoped<SSLCommerzPaymentService>();

// ---- Air Services ----
builder.Services.AddSingleton<IPnrService, PnrService>();          // PNR generator
builder.Services.AddScoped<IPricingService, PricingService>();     // Pricing logic
builder.Services.AddScoped<IAirBookingService, AirBookingService>(); // Booking workflow

var app = builder.Build();

// ---- Seed roles & admin user ----
await SeedRolesAndAdminAsync(app);

// ---- Seed Air tables ----
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await db.Database.MigrateAsync();   // apply latest migrations
    await ONLINE_TICKET_BOOKING_SYSTEM.Data.Seed.AirSeed.EnsureAirSeedAsync(db);
}

// ---- Pipeline ----
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

// ---- Routes ----
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapRazorPages();

app.Run();

// ----------------- Helpers -----------------
static async Task SeedRolesAndAdminAsync(WebApplication app)
{
    using var scope = app.Services.CreateScope();
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

    var roles = new[] { "Admin", "User" };
    foreach (var role in roles)
        if (!await roleManager.RoleExistsAsync(role))
            await roleManager.CreateAsync(new IdentityRole(role));

    var adminEmail = "admin@lib.com";
    var adminPassword = "Admin@123";

    if (await userManager.FindByEmailAsync(adminEmail) == null)
    {
        var adminUser = new ApplicationUser
        {
            UserName = adminEmail,
            Email = adminEmail,
            FirstName = "System",
            LastName = "Admin",
            ProfileImagePath = "/images/default.png",
            EmailConfirmed = true
        };
        await userManager.CreateAsync(adminUser, adminPassword);
        await userManager.AddToRoleAsync(adminUser, "Admin");
    }
}
