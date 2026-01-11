using Microsoft.EntityFrameworkCore;
using ContactsRazor.Models;

namespace ContactsRazor.Data;

public class ContactsDbContext : DbContext
{
    public ContactsDbContext(DbContextOptions<ContactsDbContext> options)
        : base(options)
    {
    }

    public DbSet<Player> Players { get; set; }
    public DbSet<Club> Clubs { get; set; }
    public DbSet<User> Users { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Club constraints
        modelBuilder.Entity<Club>()
            .HasIndex(c => c.ClubCode)
            .IsUnique();

        // User constraints
        modelBuilder.Entity<User>()
            .HasIndex(u => u.Username)
            .IsUnique();

        // Player constraints - Code and Name unique everywhere (globally)
        modelBuilder.Entity<Player>()
            .HasIndex(p => p.Code)
            .IsUnique();
        
        modelBuilder.Entity<Player>()
            .HasIndex(p => p.Name)
            .IsUnique();
        
        // Also index ClubCode for faster queries by club
        modelBuilder.Entity<Player>()
            .HasIndex(p => p.ClubCode);
    }
}
