using Microsoft.EntityFrameworkCore;
using Domain.Entities;

namespace Infrastructure.Persistence.Data;

public class ApplicationDbContext : DbContext
{
    public DbSet<Server> Servers { get; set; }
    public DbSet<Rental> Rentals { get; set; }
    
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) {}

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Server>()
            .HasKey(s => s.Id);

        modelBuilder.Entity<Server>()
            .Property(s => s.RowVersion)
            .IsRowVersion()
            .HasColumnType("xid");
        
        modelBuilder.Entity<Server>()
            .HasOne(s => s.CurrentRental)
            .WithOne()
            .HasForeignKey<Server>(s => s.CurrentRentalId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Rental>()
            .HasKey(r => r.Id);
        
        modelBuilder.Entity<Rental>()
            .HasOne(r => r.Server)
            .WithMany()
            .HasForeignKey(r => r.ServerId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}