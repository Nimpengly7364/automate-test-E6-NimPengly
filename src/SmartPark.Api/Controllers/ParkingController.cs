using Microsoft.AspNetCore.Mvc;
using SmartPark.Api.DTOs;
using SmartPark.Core.Interfaces;
using SmartPark.Core.Models;
using SmartPark.Core.Services;

namespace SmartPark.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ParkingController : ControllerBase
{
    private readonly ParkingSessionManager _sessionManager;
    private readonly ParkingFeeCalculator _feeCalculator;
    private readonly IParkingRepository _repository;

    public ParkingController(
        ParkingSessionManager sessionManager,
        ParkingFeeCalculator feeCalculator,
        IParkingRepository repository)
    {
        _sessionManager = sessionManager;
        _feeCalculator = feeCalculator;
        _repository = repository;
    }

    /// <summary>
    /// Check in a vehicle. Validates license plate format and vehicle type.
    /// </summary>
    [HttpPost("checkin")]
    public async Task<ActionResult<ApiResponse<TicketResponse>>> CheckIn([FromBody] CheckInRequest request)
    {
        try
        {
            var ticket = await _sessionManager.CheckInAsync(
                request.LicensePlate.ToUpper(),
                (VehicleType)request.VehicleType);

            return Ok(ApiResponse<TicketResponse>.Ok(MapTicket(ticket)));
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(ApiResponse<TicketResponse>.Fail(ex.Message));
        }
    }

    /// <summary>
    /// Check out a vehicle by ticket ID. Processes payment and sends receipt.
    /// </summary>
    [HttpPost("checkout/{ticketId}")]
    public async Task<ActionResult<ApiResponse<FeeResponse>>> CheckOut(
        string ticketId,
        [FromBody] CheckOutRequest request)
    {
        try
        {
            var result = await _sessionManager.CheckOutAsync(
                ticketId.ToUpper(),
                request.PhoneNumber,
                request.IsLostTicket,
                request.IsHoliday);

            return Ok(ApiResponse<FeeResponse>.Ok(new FeeResponse
            {
                BaseFee = result.BaseFee,
                SurchargeAmount = result.SurchargeAmount,
                DiscountAmount = result.DiscountAmount,
                LostTicketPenalty = result.LostTicketPenalty,
                TotalFee = result.TotalFee,
                Breakdown = result.Breakdown
            }));
        }
        catch (KeyNotFoundException)
        {
            return NotFound(ApiResponse<FeeResponse>.Fail("Ticket not found."));
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(ApiResponse<FeeResponse>.Fail(ex.Message));
        }
        catch (Exception ex)
        {
            return StatusCode(502, ApiResponse<FeeResponse>.Fail(ex.Message));
        }
    }

    /// <summary>
    /// List all currently active (parked) tickets.
    /// </summary>
    [HttpGet("tickets")]
    public async Task<ActionResult<ApiResponse<List<TicketResponse>>>> GetActiveTickets()
    {
        var tickets = await _repository.GetAllActiveTicketsAsync();
        var response = tickets.Select(MapTicket).ToList();
        return Ok(ApiResponse<List<TicketResponse>>.Ok(response));
    }

    /// <summary>
    /// Get a single ticket by ID.
    /// </summary>
    [HttpGet("tickets/{ticketId}")]
    public async Task<ActionResult<ApiResponse<TicketResponse>>> GetTicket(string ticketId)
    {
        var ticket = await _repository.GetTicketByIdAsync(ticketId.ToUpper());
        if (ticket == null)
            return NotFound(ApiResponse<TicketResponse>.Fail("Ticket not found."));

        return Ok(ApiResponse<TicketResponse>.Ok(MapTicket(ticket)));
    }

    /// <summary>
    /// Calculate a fee estimate without processing payment.
    /// Validates that check-out is after check-in.
    /// </summary>
    [HttpPost("estimate")]
    public ActionResult<ApiResponse<FeeResponse>> Estimate([FromBody] FeeEstimateRequest request)
    {
        var result = _feeCalculator.CalculateFee(
            (VehicleType)request.VehicleType,
            (MembershipTier)request.Membership,
            request.CheckIn,
            request.CheckOut,
            request.IsLostTicket,
            request.IsHoliday);

        return Ok(ApiResponse<FeeResponse>.Ok(new FeeResponse
        {
            BaseFee = result.BaseFee,
            SurchargeAmount = result.SurchargeAmount,
            DiscountAmount = result.DiscountAmount,
            LostTicketPenalty = result.LostTicketPenalty,
            TotalFee = result.TotalFee,
            Breakdown = result.Breakdown
        }));
    }

    private static TicketResponse MapTicket(ParkingTicket ticket) => new()
    {
        TicketId = ticket.TicketId,
        LicensePlate = ticket.Vehicle.LicensePlate,
        VehicleType = ticket.Vehicle.Type.ToString(),
        Membership = ticket.Vehicle.Membership.ToString(),
        CheckInTime = ticket.CheckInTime,
        CheckOutTime = ticket.CheckOutTime,
        IsActive = ticket.IsActive
    };
}
