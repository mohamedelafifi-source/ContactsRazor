using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ContactsRazor.Helpers;
using ContactsRazor.Services;

namespace ContactsRazor.Pages;

[Authorize]
public class DashboardModel : PageModel
{
    private readonly FileLoaderService _fileLoader;

    public DashboardModel(FileLoaderService fileLoader)
    {
        _fileLoader = fileLoader;
    }

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostLoadClubsAsync()
    {
        // Check if user is Federation
        if (!User.IsFederation())
        {
            return Forbid();
        }

        var result = await _fileLoader.LoadClubsAsync();
        
        if (result.Success)
        {
            var message = $"Successfully loaded clubs. Added: {result.Added}, Updated: {result.Updated}";
            if (result.Warnings.Any())
            {
                message += $". Warnings: {string.Join(", ", result.Warnings)}";
            }
            TempData["Message"] = message;
        }
        else
        {
            var errorMsg = $"Error loading clubs: {string.Join("; ", result.Errors)}";
            if (result.Added > 0 || result.Updated > 0)
            {
                errorMsg = $"Partial success. Added: {result.Added}, Updated: {result.Updated}. Errors: {string.Join("; ", result.Errors)}";
            }
            TempData["Error"] = errorMsg;
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostLoadUsersAsync()
    {
        // Check if user is Federation
        if (!User.IsFederation())
        {
            return Forbid();
        }

        var result = await _fileLoader.LoadUsersAsync();
        
        if (result.Success)
        {
            var message = $"Successfully loaded users. Added: {result.Added}, Updated: {result.Updated}";
            if (result.Warnings.Any())
            {
                message += $". Warnings: {string.Join(", ", result.Warnings)}";
            }
            TempData["Message"] = message;
        }
        else
        {
            var errorMsg = $"Error loading users: {string.Join("; ", result.Errors)}";
            if (result.Added > 0 || result.Updated > 0)
            {
                errorMsg = $"Partial success. Added: {result.Added}, Updated: {result.Updated}. Errors: {string.Join("; ", result.Errors)}";
            }
            TempData["Error"] = errorMsg;
        }

        return RedirectToPage();
    }
}
