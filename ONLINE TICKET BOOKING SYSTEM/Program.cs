using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ONLINE_TICKET_BOOKING_SYSTEM.Data;
using ONLINE_TICKET_BOOKING_SYSTEM.Services;
using Microsoft.AspNetCore.Identity.UI.Services;
using QuestPDF.Infrastructure;   // ⬅️ add this

var builder = WebApplication.CreateBuilder(args);

// ⬇️ Configure QuestPDF license ONCE at startup (Community is free if you qualify)
QuestPDF.Settings.License = LicenseType.Community;
// If you have a Professional license, use:
// QuestPDF.Settings.License = LicenseType.Professional;
// QuestPDF.Settings.LicenseKey = builder.Configuration["QuestPdf:LicenseKey"];

//  Add Database Connection
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddDatabaseDeveloperPageExceptionFilter();

// Configure Identity with ApplicationUser and Roles
builder.Services.AddDefaultIdentity<ApplicationUser>(options =>
{
    options.SignIn.RequireConfirmedAccount = false;
    options.SignIn.RequireConfirmedEmail = false; // Disable email confirmation for now
})
.AddRoles<IdentityRole>() // Add Role Support
.AddEntityFrameworkStores<ApplicationDbContext>();

//  Add MVC & Razor Pages
builder.Services.AddControllersWithViews();

//  Add Session
builder.Services.AddMemoryCache();
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(10);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

//  Email Settings & Email Sender Service
builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("EmailSettings"));
builder.Services.AddTransient<IEmailSender, EmailSender>();

// ✅ Register your PDF service (scoped)
builder.Services.AddScoped<ITicketPdfService, TicketPdfService>();

var app = builder.Build();

// Seed Roles and Admin User
await SeedRolesAndAdminAsync(app);

//  Configure Middleware Pipeline
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

app.UseSession(); // Session must be before Authentication
app.UseAuthentication(); // Identity Authentication
app.UseAuthorization();

//  Default Route
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapRazorPages();

app.Run();


//  Seed Roles and Admin User Method
static async Task SeedRolesAndAdminAsync(WebApplication app)
{
    using var scope = app.Services.CreateScope();
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

    var roles = new[] { "Admin", "User" };

    //  Create roles if not exist
    foreach (var role in roles)
    {
        if (!await roleManager.RoleExistsAsync(role))
        {
            await roleManager.CreateAsync(new IdentityRole(role));
        }
    }

    //  Create Admin User
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
