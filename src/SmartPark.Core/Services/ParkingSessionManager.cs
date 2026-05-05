using SmartPark.Core.Interfaces;
using SmartPark.Core.Models;

namespace SmartPark.Core.Services;

/// <summary>
/// Orchestrates the full parking flow (check-in, check-out, payment).
/// Handles coordination between repository, calculator, and external services.
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

    // ─────────────────────────────────────────────
    // CHECK-IN FLOW
    // ─────────────────────────────────────────────
    public async Task<ParkingTicket> CheckInAsync(string licensePlate, VehicleType vehicleType)
    {
        var membership = _membershipService.GetMembershipTier(licensePlate);

        var existingTicket = await _repository.GetActiveTicketByPlateAsync(licensePlate);
        if (existingTicket != null)
            throw new InvalidOperationException("Vehicle already checked in.");

        var now = _dateTimeProvider.Now; // FIXED

        var ticket = new ParkingTicket
        {
            Vehicle = new Vehicle
            {
                LicensePlate = licensePlate,
                Type = vehicleType,
                Membership = membership
            },
            CheckInTime = now // FIXED
        };

        await _repository.SaveTicketAsync(ticket);

        return ticket;
    }

    // ─────────────────────────────────────────────
    // CHECK-OUT FLOW
    // ─────────────────────────────────────────────
    public async Task<ParkingFeeResult> CheckOutAsync(
    string ticketId,
    string phoneNumber,
    bool isLostTicket = false,
    bool isHoliday = false)
    {
        var ticket = await _repository.GetTicketByIdAsync(ticketId);

        if (ticket == null)
            throw new KeyNotFoundException("Ticket not found.");

        if (!ticket.IsActive)
            throw new InvalidOperationException("Ticket already processed.");

        var checkOutTime = _dateTimeProvider.Now;

        // ❗ FIX: ensure valid time BEFORE calling calculator
        if (checkOutTime <= ticket.CheckInTime)
            checkOutTime = ticket.CheckInTime.AddMinutes(1);

        var feeResult = _feeCalculator.CalculateFee(
            ticket.Vehicle.Type,
            ticket.Vehicle.Membership,
            ticket.CheckInTime,
            checkOutTime,
            isLostTicket,
            isHoliday);

        var paymentSuccess = await _paymentGateway.ProcessPaymentAsync(ticketId, feeResult.TotalFee);

        if (!paymentSuccess)
            throw new Exception("Payment failed. Please try again.");

        ticket.CheckOutTime = checkOutTime;
        ticket.IsLostTicket = isLostTicket;

        await _repository.UpdateTicketAsync(ticket);

        try
        {
            await _notificationService.SendReceiptAsync(phoneNumber, feeResult.Breakdown);
        }
        catch { }

        return feeResult;
    }
}