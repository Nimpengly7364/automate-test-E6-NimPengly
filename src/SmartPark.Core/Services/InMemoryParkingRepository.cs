using SmartPark.Core.Interfaces;
using SmartPark.Core.Models;

namespace SmartPark.Core.Services;

/// <summary>
/// Simple in-memory implementation of IParkingRepository for testing and demo purposes.
/// </summary>
public class InMemoryParkingRepository : IParkingRepository
{
    private readonly List<ParkingTicket> _tickets = new();

    public Task SaveTicketAsync(ParkingTicket ticket)
    {
        _tickets.Add(ticket);
        return Task.CompletedTask;
    }

    public Task<ParkingTicket?> GetTicketByIdAsync(string ticketId)
    {
        return Task.FromResult(_tickets.FirstOrDefault(t => t.TicketId == ticketId));
    }

    public Task<ParkingTicket?> GetActiveTicketByPlateAsync(string licensePlate)
    {
        return Task.FromResult(_tickets.FirstOrDefault(
            t => t.Vehicle.LicensePlate == licensePlate && t.IsActive));
    }

    public Task UpdateTicketAsync(ParkingTicket ticket)
    {
        // In-memory — object is already updated by reference
        return Task.CompletedTask;
    }

    public Task<IEnumerable<ParkingTicket>> GetAllActiveTicketsAsync()
    {
        return Task.FromResult<IEnumerable<ParkingTicket>>(
            _tickets.Where(t => t.IsActive).ToList());
    }
}
