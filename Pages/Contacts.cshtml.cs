using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
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

    public List<Contact> AllContacts { get; set; } = new();

    public async Task OnGetAsync()
    {
        AllContacts = await _context.Contacts
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            AllContacts = await _context.Contacts
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();
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

    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        var contact = await _context.Contacts.FindAsync(id);
        
        if (contact == null)
        {
            TempData["Error"] = "Contact not found.";
            return RedirectToPage("./Contacts");
        }

        _context.Contacts.Remove(contact);
        await _context.SaveChangesAsync();

        TempData["Message"] = $"Contact '{contact.Name}' has been deleted successfully!";
        return RedirectToPage("./Contacts");
    }
}

// ContactInput class for form binding and validation
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
