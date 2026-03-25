namespace SmartPark.Core.Interfaces;

/// <summary>
/// External payment processing service.
/// </summary>
public interface IPaymentGateway
{
    /// <summary>
    /// Processes a payment. Returns true if successful.
    /// </summary>
    Task<bool> ProcessPaymentAsync(string ticketId, decimal amount);

    /// <summary>
    /// Refunds a payment. Returns true if successful.
    /// </summary>
    Task<bool> RefundAsync(string ticketId, decimal amount);
}
