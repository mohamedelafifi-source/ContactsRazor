# Database Setup Guide for Contacts Application

## Overview
This guide explains how to save contact form data to a database using Entity Framework Core in ASP.NET Core Razor Pages.

## Database Options

### Option 1: SQLite (Recommended for Development/Simple Apps)
- **Pros**: No separate database server needed, file-based, easy setup
- **Cons**: Limited for high-concurrency production scenarios
- **Best for**: Development, testing, small applications

### Option 2: SQL Server (Recommended for Production)
- **Pros**: Full-featured, production-ready, excellent tooling
- **Cons**: Requires separate database server or SQL Server Express
- **Best for**: Production applications

## Implementation Steps

### Step 1: Install NuGet Packages

#### For SQLite:
```bash
dotnet add package Microsoft.EntityFrameworkCore.Sqlite
dotnet add package Microsoft.EntityFrameworkCore.Tools
```

#### For SQL Server:
```bash
dotnet add package Microsoft.EntityFrameworkCore.SqlServer
dotnet add package Microsoft.EntityFrameworkCore.Tools
```

### Step 2: Create the Database Model

Create a `Models` folder and add a `Contact.cs` file:

```csharp
using System.ComponentModel.DataAnnotations;

namespace ContactsRazor.Models;

public class Contact
{
    public int Id { get; set; }
    
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;
    
    [Required]
    [EmailAddress]
    [MaxLength(200)]
    public string Email { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(20)]
    public string Phone { get; set; } = string.Empty;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
```

### Step 3: Create DbContext

Create `Data/ContactsDbContext.cs`:

#### For SQLite:
```csharp
using Microsoft.EntityFrameworkCore;
using ContactsRazor.Models;

namespace ContactsRazor.Data;

public class ContactsDbContext : DbContext
{
    public ContactsDbContext(DbContextOptions<ContactsDbContext> options)
        : base(options)
    {
    }

    public DbSet<Contact> Contacts { get; set; }
}
```

#### For SQL Server:
(Same as above - DbContext is database-agnostic)

### Step 4: Configure Connection String

Add to `appsettings.json`:

#### For SQLite:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=contacts.db"
  },
  ...
}
```

#### For SQL Server:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=ContactsRazor;Trusted_Connection=True;MultipleActiveResultSets=true"
  },
  ...
}
```

### Step 5: Register DbContext in Program.cs

#### For SQLite:
```csharp
builder.Services.AddDbContext<ContactsDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));
```

#### For SQL Server:
```csharp
builder.Services.AddDbContext<ContactsDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
```

### Step 6: Create and Apply Database Migration

```bash
# Create initial migration
dotnet ef migrations add InitialCreate

# Apply migration to create database
dotnet ef database update
```

### Step 7: Update Contacts.cshtml.cs to Save to Database

```csharp
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ContactsRazor.Data;
using ContactsRazor.Models;
using System.ComponentModel.DataAnnotations;

namespace ContactsRazor.Pages;

public class ContactsModel : PageModel
{
    private readonly ContactsDbContext _context;

    public ContactsModel(ContactsDbContext context)
    {
        _context = context;
    }

    [BindProperty]
    public ContactInput Contact { get; set; } = new();

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        // Create new Contact entity from form input
        var contact = new Contact
        {
            Name = Contact.Name,
            Email = Contact.Email,
            Phone = Contact.Phone,
            CreatedAt = DateTime.UtcNow
        };

        // Add to database
        _context.Contacts.Add(contact);
        await _context.SaveChangesAsync();

        TempData["Message"] = $"Contact '{Contact.Name}' has been saved successfully!";
        return RedirectToPage("./Contacts");
    }
}

// Keep ContactInput for form binding and validation
public class ContactInput
{
    [Required(ErrorMessage = "Name is required")]
    [Display(Name = "Name")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email address")]
    [Display(Name = "Email")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Phone is required")]
    [Display(Name = "Phone")]
    [RegularExpression(@"^[0-9\-\+\(\)\s]+$", ErrorMessage = "Invalid phone number format")]
    public string Phone { get; set; } = string.Empty;
}
```

## Architecture Overview

```
User Form (Contacts.cshtml)
    ↓
ContactInput (DTO for form binding)
    ↓
ContactsModel.OnPostAsync()
    ↓
Contact Entity (Database model)
    ↓
ContactsDbContext
    ↓
Database (SQLite/SQL Server)
```

## Benefits of This Approach

1. **Separation of Concerns**: Form input model (`ContactInput`) is separate from database model (`Contact`)
2. **Validation**: Client-side and server-side validation
3. **Type Safety**: Strongly-typed models
4. **Migrations**: EF Core handles database schema changes
5. **Async Operations**: Non-blocking database operations

## Optional: Display Saved Contacts

To view all saved contacts, you could add:

```csharp
public List<Contact> AllContacts { get; set; } = new();

public async Task OnGetAsync()
{
    AllContacts = await _context.Contacts
        .OrderByDescending(c => c.CreatedAt)
        .ToListAsync();
}
```

Then in the view, display them in a table.
