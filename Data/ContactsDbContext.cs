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
