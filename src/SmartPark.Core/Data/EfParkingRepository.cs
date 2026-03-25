using Microsoft.EntityFrameworkCore;
using SmartPark.Core.Interfaces;
using SmartPark.Core.Models;

namespace SmartPark.Core.Data;

/// <summary>
/// EF Core implementation of IParkingRepository backed by SQLite.
/// </summary>
public class EfParkingRepository : IParkingRepository
{
    private readonly SmartParkDbContext _db;

    public EfParkingRepository(SmartParkDbContext db)
    {
        _db = db;
    }

    public async Task SaveTicketAsync(ParkingTicket ticket)
    {
        _db.ParkingTickets.Add(ticket);
        await _db.SaveChangesAsync();
    }

    public async Task<ParkingTicket?> GetTicketByIdAsync(string ticketId)
    {
        return await _db.ParkingTickets.FirstOrDefaultAsync(t => t.TicketId == ticketId);
    }

    public async Task<ParkingTicket?> GetActiveTicketByPlateAsync(string licensePlate)
    {
        return await _db.ParkingTickets.FirstOrDefaultAsync(
            t => t.Vehicle.LicensePlate == licensePlate && t.CheckOutTime == null);
    }

    public async Task UpdateTicketAsync(ParkingTicket ticket)
    {
        _db.ParkingTickets.Update(ticket);
        await _db.SaveChangesAsync();
    }

    public async Task<IEnumerable<ParkingTicket>> GetAllActiveTicketsAsync()
    {
        return await _db.ParkingTickets
            .Where(t => t.CheckOutTime == null)
            .ToListAsync();
    }
}
