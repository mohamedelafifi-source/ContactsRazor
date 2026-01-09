using BCrypt.Net;
using ContactsRazor.Data;
using ContactsRazor.Models;
using Microsoft.EntityFrameworkCore;

namespace ContactsRazor.Services;

public class AuthService
{
    private readonly ContactsDbContext _context;
    private const int BcryptWorkFactor = 12; // Good balance of security and performance

    public AuthService(ContactsDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Hash a password using BCrypt
    /// </summary>
    public static string HashPassword(string password)
    {
        return BCrypt.Net.BCrypt.HashPassword(password, BcryptWorkFactor);
    }

    /// <summary>
    /// Verify a password against a hash
    /// </summary>
    public static bool VerifyPassword(string password, string hash)
    {
        return BCrypt.Net.BCrypt.Verify(password, hash);
    }

    /// <summary>
    /// Authenticate a user by username and password
    /// </summary>
    public async Task<User?> AuthenticateAsync(string username, string password)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Username == username);

        if (user == null)
        {
            return null;
        }

        if (!VerifyPassword(password, user.PasswordHash))
        {
            return null;
        }

        return user;
    }

    /// <summary>
    /// Create a new user (for seeding/admin purposes)
    /// </summary>
    public async Task<User> CreateUserAsync(string username, string password, string clubCode)
    {
        // Validate that club exists if not Federation
        if (clubCode != "FEDR")
        {
            var clubExists = await _context.Clubs.AnyAsync(c => c.ClubCode == clubCode);
            if (!clubExists)
            {
                throw new ArgumentException($"Club with code '{clubCode}' does not exist.");
            }
        }

        var user = new User
        {
            Username = username,
            PasswordHash = HashPassword(password),
            ClubCode = clubCode
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        return user;
    }
}
