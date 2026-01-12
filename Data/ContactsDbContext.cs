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
    public DbSet<ResultSet> ResultSets { get; set; }
    public DbSet<ResultEntry> ResultEntries { get; set; }

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

        // ResultSet constraints
        modelBuilder.Entity<ResultSet>()
            .HasIndex(rs => new { rs.ClubCode, rs.VenueClubCode, rs.Date });

        // ResultSet relationships
        modelBuilder.Entity<ResultSet>()
            .HasOne(rs => rs.Club)
            .WithMany()
            .HasForeignKey(rs => rs.ClubCode)
            .HasPrincipalKey(c => c.ClubCode)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ResultSet>()
            .HasOne(rs => rs.VenueClub)
            .WithMany()
            .HasForeignKey(rs => rs.VenueClubCode)
            .HasPrincipalKey(c => c.ClubCode)
            .OnDelete(DeleteBehavior.Restrict);

        // ResultEntry constraints - prevent duplicate player in same result set
        modelBuilder.Entity<ResultEntry>()
            .HasIndex(re => new { re.ResultSetId, re.PlayerId })
            .IsUnique();

        // ResultEntry relationships
        modelBuilder.Entity<ResultEntry>()
            .HasOne(re => re.ResultSet)
            .WithMany(rs => rs.ResultEntries)
            .HasForeignKey(re => re.ResultSetId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ResultEntry>()
            .HasOne(re => re.Player)
            .WithMany()
            .HasForeignKey(re => re.PlayerId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
