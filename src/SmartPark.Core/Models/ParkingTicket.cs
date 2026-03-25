namespace SmartPark.Core.Models;

/// <summary>
/// Represents an active or completed parking session.
/// </summary>
public class ParkingTicket
{
    public string TicketId { get; set; } = Guid.NewGuid().ToString("N")[..8].ToUpper();
    public Vehicle Vehicle { get; set; } = null!;
    public DateTime CheckInTime { get; set; }
    public DateTime? CheckOutTime { get; set; }
    public bool IsLostTicket { get; set; } = false;
    public bool IsActive => CheckOutTime == null;
}
