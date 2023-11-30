using Microsoft.EntityFrameworkCore;
using ParkingFinder.Data.Entities;

namespace ParkingFinder.Data;

public class Database : DbContext
{
    public DbSet<User> Users { get; set; }

    public DbSet<ParkingSpot> ParkingSpots { get; set; }

    public DbSet<ParkingLog> ParkingLogs { get; set; }

    public DbSet<ParkingTime> ParkingTimes { get; set; }
    
    public Database(DbContextOptions<Database> options) : base(options){}

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ParkingSpot>()
            .Property(p => p.Latitude)
            .HasPrecision(12, 8);
        
        modelBuilder.Entity<ParkingSpot>()
            .Property(p => p.Longitude)
            .HasPrecision(12, 8);
        
        base.OnModelCreating(modelBuilder);
    }
}