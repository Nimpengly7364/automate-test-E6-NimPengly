using Microsoft.EntityFrameworkCore;
using SmartPark.Core.Models;

namespace SmartPark.Core.Data;

public class SmartParkDbContext : DbContext
{
    public DbSet<ParkingTicket> ParkingTickets => Set<ParkingTicket>();

    public SmartParkDbContext(DbContextOptions<SmartParkDbContext> options)
        : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ParkingTicket>(entity =>
        {
            entity.HasKey(t => t.TicketId);
            entity.OwnsOne(t => t.Vehicle);
        });
    }
}
