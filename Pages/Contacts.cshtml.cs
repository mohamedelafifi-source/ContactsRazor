using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;

namespace ContactsRazor.Pages;

public class ContactsModel : PageModel
{
    [BindProperty]
    public ContactInput Contact { get; set; } = new();

    public void OnGet()
    {
    }

    public IActionResult OnPost()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        // Here you can process the contact data
        // For now, we'll just redirect to show success
        TempData["Message"] = $"Contact '{Contact.Name}' has been saved successfully!";
        return RedirectToPage("./Contacts");
    }
}

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
