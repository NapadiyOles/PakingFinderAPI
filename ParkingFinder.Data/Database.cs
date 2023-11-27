using Microsoft.EntityFrameworkCore;
using ParkingFinder.Data.Entities;

namespace ParkingFinder.Data;

public class Database : DbContext
{
    public DbSet<User> Users { get; set; }
    
    public Database(DbContextOptions<Database> options) : base(options){}
}