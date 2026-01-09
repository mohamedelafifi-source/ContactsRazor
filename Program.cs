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

// Register AuthService
builder.Services.AddScoped<AuthService>();

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

// Add authorization - Federation is determined by ClubCode == "FEDR"
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("FederationOnly", policy => 
        policy.RequireAssertion(context => 
            context.User.FindFirst("ClubCode")?.Value == "FEDR"));
    
    options.AddPolicy("ClubAccess", policy => 
        policy.RequireAssertion(context => 
            context.User.HasClaim("ClubCode", "FEDR") || 
            !string.IsNullOrEmpty(context.User.FindFirst("ClubCode")?.Value)));
});

var app = builder.Build();

// Ensure database is created and seeded
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ContactsDbContext>();
    context.Database.EnsureCreated();
    
    // Seed initial data if database is empty
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

// Seed data method
static async Task SeedDataAsync(ContactsDbContext context, AuthService authService)
{
    // Check if clubs already exist
    if (await context.Clubs.AnyAsync())
    {
        return; // Database already seeded
    }

    // Create Federation club
    var federation = new Club
    {
        ClubCode = "FEDR",
        ClubId = "000000",
        LongName = "Golf Federation",
        NumberOfPlayers = 0,
        IsActive = true,
        CreatedAt = DateTime.UtcNow
    };
    context.Clubs.Add(federation);

    // Create 10 clubs
    for (int i = 1; i <= 10; i++)
    {
        var club = new Club
        {
            ClubCode = $"CLB{i:D1}",
            ClubId = $"{i:D6}",
            LongName = $"Golf Club {i}",
            NumberOfPlayers = 0,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        context.Clubs.Add(club);
    }

    await context.SaveChangesAsync();

    // Create Federation user (ClubCode = "FEDR")
    await authService.CreateUserAsync(
        username: "federation",
        password: "Federation@2024", // Change this password!
        clubCode: "FEDR"
    );

    // Create a club captain user for each club
    for (int i = 1; i <= 10; i++)
    {
        await authService.CreateUserAsync(
            username: $"club{i}_captain",
            password: $"Club{i}@2024", // Change these passwords!
            clubCode: $"CLB{i}"
        );
    }
}
