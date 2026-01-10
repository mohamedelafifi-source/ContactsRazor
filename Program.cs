using ContactsRazor.Data;
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
builder.Services.AddScoped<BasicDataLoaderService>();

// Configure Cookie Authentication
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Login";
        options.LogoutPath = "/Logout";
        options.AccessDeniedPath = "/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = false; // Disable sliding expiration to prevent caching issues
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.Name = ".AspNetCore.Cookies"; // Explicit cookie name
        // Configure logout events to ensure cookie is properly deleted
        options.Events.OnSigningOut = async context =>
        {
            context.CookieOptions.Expires = DateTimeOffset.UtcNow.AddDays(-1);
            context.CookieOptions.MaxAge = TimeSpan.Zero; // Immediately expire
        };
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

// Ensure database is created and load BasicData.txt
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ContactsDbContext>();
    context.Database.EnsureCreated();
    
    // Load BasicData.txt automatically on startup
    var basicDataLoader = scope.ServiceProvider.GetRequiredService<BasicDataLoaderService>();
    var loadResult = await basicDataLoader.LoadBasicDataAsync();
    
    // Log results (errors will be logged, warnings can be reviewed)
    if (!loadResult.Success)
    {
        Console.WriteLine("=== BasicData.txt Loading Errors ===");
        foreach (var error in loadResult.Errors)
        {
            Console.WriteLine($"Error: {error}");
        }
    }
    
    if (loadResult.Warnings.Any())
    {
        Console.WriteLine("=== BasicData.txt Loading Warnings ===");
        foreach (var warning in loadResult.Warnings)
        {
            Console.WriteLine($"Warning: {warning}");
        }
    }
    
    if (loadResult.Success || loadResult.UsersAdded > 0 || loadResult.UsersUpdated > 0)
    {
        Console.WriteLine($"=== BasicData.txt Loaded Successfully ===");
        Console.WriteLine($"Clubs: Added {loadResult.ClubsAdded}, Updated {loadResult.ClubsUpdated}");
        Console.WriteLine($"Users: Added {loadResult.UsersAdded}, Updated {loadResult.UsersUpdated}");
    }
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

app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages();

app.Run();
