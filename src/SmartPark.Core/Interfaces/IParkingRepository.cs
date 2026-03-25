using SmartPark.Core.Models;

namespace SmartPark.Core.Interfaces;

/// <summary>
/// Persists and retrieves parking tickets.
/// </summary>
public interface IParkingRepository
{
    Task SaveTicketAsync(ParkingTicket ticket);
    Task<ParkingTicket?> GetTicketByIdAsync(string ticketId);
    Task<ParkingTicket?> GetActiveTicketByPlateAsync(string licensePlate);
    Task UpdateTicketAsync(ParkingTicket ticket);
    Task<IEnumerable<ParkingTicket>> GetAllActiveTicketsAsync();
}
