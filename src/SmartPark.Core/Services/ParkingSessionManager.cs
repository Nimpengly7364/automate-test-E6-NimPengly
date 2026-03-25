using SmartPark.Core.Interfaces;
using SmartPark.Core.Models;

namespace SmartPark.Core.Services;

/// <summary>
/// Orchestrates the full parking flow (check-in, check-out, payment).
/// Primary target for mocking tests.
/// </summary>
public class ParkingSessionManager
{
    private readonly ParkingFeeCalculator _feeCalculator;
    private readonly IPaymentGateway _paymentGateway;
    private readonly INotificationService _notificationService;
    private readonly IMembershipService _membershipService;
    private readonly IParkingRepository _repository;
    private readonly IDateTimeProvider _dateTimeProvider;

    public ParkingSessionManager(
        ParkingFeeCalculator feeCalculator,
        IPaymentGateway paymentGateway,
        INotificationService notificationService,
        IMembershipService membershipService,
        IParkingRepository repository,
        IDateTimeProvider dateTimeProvider)
    {
        _feeCalculator = feeCalculator;
        _paymentGateway = paymentGateway;
        _notificationService = notificationService;
        _membershipService = membershipService;
        _repository = repository;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<ParkingTicket> CheckInAsync(string licensePlate, VehicleType vehicleType)
    {
        // 1. Look up membership tier
        var membership = _membershipService.GetMembershipTier(licensePlate);

        // 2. Check for duplicate active ticket
        var existingTicket = await _repository.GetActiveTicketByPlateAsync(licensePlate);
        if (existingTicket != null)
            throw new InvalidOperationException("Vehicle already checked in.");

        // 3. Create parking ticket
        var ticket = new ParkingTicket
        {
            Vehicle = new Vehicle
            {
                LicensePlate = licensePlate,
                Type = vehicleType,
                Membership = membership
            },
            CheckInTime = _dateTimeProvider.Now
        };

        // 4. Save to repository
        await _repository.SaveTicketAsync(ticket);

        // 5. Return the ticket
        return ticket;
    }

    public async Task<ParkingFeeResult> CheckOutAsync(
        string ticketId,
        string phoneNumber,
        bool isLostTicket = false,
        bool isHoliday = false)
    {
        // 1. Retrieve ticket
        var ticket = await _repository.GetTicketByIdAsync(ticketId);

        // 2. Validate ticket exists
        if (ticket == null)
            throw new KeyNotFoundException("Ticket not found.");

        // 3. Validate ticket is still active
        if (!ticket.IsActive)
            throw new InvalidOperationException("Ticket already processed.");

        // 4. Capture check-out time (don't mutate ticket yet — payment may fail)
        var checkOutTime = _dateTimeProvider.Now;

        // 5. Calculate fee
        var feeResult = _feeCalculator.CalculateFee(
            ticket.Vehicle.Type,
            ticket.Vehicle.Membership,
            ticket.CheckInTime,
            checkOutTime,
            isLostTicket,
            isHoliday);

        // 6. Process payment (fail fast — ticket stays active on failure)
        var paymentSuccess = await _paymentGateway.ProcessPaymentAsync(ticketId, feeResult.TotalFee);
        if (!paymentSuccess)
            throw new Exception("Payment failed. Please try again.");

        // 7. Commit state changes only after successful payment
        ticket.CheckOutTime = checkOutTime;
        ticket.IsLostTicket = isLostTicket;

        // 8. Update ticket in repository
        await _repository.UpdateTicketAsync(ticket);

        // 9. Send receipt (swallow notification errors — deliberate design decision)
        try
        {
            await _notificationService.SendReceiptAsync(phoneNumber, feeResult.Breakdown);
        }
        catch
        {
            // Notification failure should not affect checkout success.
            // Payment is already processed and ticket is updated.
        }

        // 10. Return fee result
        return feeResult;
    }
}
