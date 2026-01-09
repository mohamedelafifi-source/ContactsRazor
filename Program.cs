using ContactsRazor.Data;
using ContactsRazor.Models;
using ContactsRazor.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();

// Add DbContext for SQLite
builder.Services.AddDbContext<ContactsDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

// Register Services
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<FileLoaderService>();

// Configure Cookie Authentication
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Login";
        options.LogoutPath = "/Logout";
        options.AccessDeniedPath = "/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromHours(8); // Session expires after 8 hours
        options.SlidingExpiration = true; // Reset expiration on activity
        options.Cookie.HttpOnly = true; // Prevent XSS attacks
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest; // Use HTTPS in production
    });

// Add authorization - Federation is determined by ClubCode == "FEDERE"
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("FederationOnly", policy => 
        policy.RequireAssertion(context => 
            context.User.FindFirst("ClubCode")?.Value == "FEDERE"));
    
    options.AddPolicy("ClubAccess", policy => 
        policy.RequireAssertion(context => 
            context.User.HasClaim("ClubCode", "FEDERE") || 
            !string.IsNullOrEmpty(context.User.FindFirst("ClubCode")?.Value)));
});

var app = builder.Build();

// Ensure database is created
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ContactsDbContext>();
    context.Database.EnsureCreated();
    
    // Seed initial Federation user only (clubs and other users loaded from files)
    var authService = scope.ServiceProvider.GetRequiredService<AuthService>();
    await SeedDataAsync(context, authService);
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

//app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication(); // Must come before UseAuthorization
app.UseAuthorization();

app.MapRazorPages();

app.Run();

// Seed data method - Only creates Federation user if no users exist
static async Task SeedDataAsync(ContactsDbContext context, AuthService authService)
{
    // Only create Federation user if no users exist (first time setup)
    if (await context.Users.AnyAsync())
    {
        return; // Users already exist, skip seeding
    }

    // Create Federation user with username "federe" and password "Federation@2026"
    try
    {
        await authService.CreateUserAsync(
            username: "federe",
            password: "Federation@2026",
            clubCode: "FEDERE"
        );
    }
    catch
    {
        // Ignore if user already exists
    }
}
