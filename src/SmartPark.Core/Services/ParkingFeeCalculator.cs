using SmartPark.Core.Models;

namespace SmartPark.Core.Services;

/// <summary>
/// This class is responsible for calculating parking fees
/// based on vehicle type, time, membership, and special rules.
/// </summary>
public class ParkingFeeCalculator
{
    // =========================
    // BASIC HOURLY RATES
    // =========================
    private const decimal MotorcycleRate = 500m;
    private const decimal CarRate = 1000m;
    private const decimal SuvRate = 1500m;

    // =========================
    // MAX DAILY CAPS (limit charge per day)
    // =========================
    private const decimal MotorcycleCap = 4000m;
    private const decimal CarCap = 8000m;
    private const decimal SuvCap = 12000m;

    // =========================
    // BUSINESS RULES
    // =========================
    private const int GraceMinutes = 30;          // first 30 minutes are free
    private const decimal OvernightFee = 2000m;   // fixed fee for overnight stay
    private const int OvernightHour = 22;         // 10 PM threshold

    private const decimal WeekendRate = 0.20m;    // +20% on weekends
    private const decimal HolidayRate = 0.50m;    // +50% on holidays

    private const decimal LostTicketFee = 20000m; // penalty for lost ticket

    public ParkingFeeResult CalculateFee(
        VehicleType vehicleType,
        MembershipTier membership,
        DateTime checkIn,
        DateTime checkOut,
        bool isLostTicket = false,
        bool isHoliday = false)
    {
        // Validate time: check-out must be after check-in
        if (checkOut <= checkIn)
            throw new ArgumentException("Check-out must be after check-in");

        var duration = checkOut - checkIn;

        // =========================
        // GRACE PERIOD (FREE PARKING)
        // =========================
        // if (duration.TotalMinutes <= GraceMinutes)
        // {
        //     return new ParkingFeeResult
        //     {
        //         BaseFee = 0,
        //         TotalFee = isLostTicket ? LostTicketFee : 0
        //     };
        // }

        // =========================
        // GET RATE BASED ON VEHICLE TYPE
        // =========================
        decimal rate = vehicleType switch
        {
            VehicleType.Motorcycle => MotorcycleRate,
            VehicleType.Car => CarRate,
            VehicleType.SUV => SuvRate,
            _ => throw new ArgumentOutOfRangeException()
        };

        // Maximum charge limit per vehicle type
        decimal cap = vehicleType switch
        {
            VehicleType.Motorcycle => MotorcycleCap,
            VehicleType.Car => CarCap,
            VehicleType.SUV => SuvCap,
            _ => 0
        };

        // Calculate billable hours (round up after grace period)
        decimal hours = (decimal)Math.Ceiling((duration.TotalMinutes - GraceMinutes) / 60.0);
        decimal baseFee = Math.Min(hours * rate, cap);

        // =========================
        // SURCHARGE (Weekend / Holiday)
        // =========================
        decimal surcharge = 0;

        if (isHoliday)
            surcharge = baseFee * HolidayRate; // holiday has highest priority
        else if (checkIn.DayOfWeek == DayOfWeek.Saturday || checkIn.DayOfWeek == DayOfWeek.Sunday)
            surcharge = baseFee * WeekendRate;

        decimal subtotal = baseFee + surcharge;

        // =========================
        // MEMBERSHIP DISCOUNT
        // =========================
        decimal discountRate = membership switch
        {
            MembershipTier.Silver => 0.10m,
            MembershipTier.Gold => 0.25m,
            MembershipTier.Platinum => 0.40m,
            _ => 0m
        };

        decimal discount = subtotal * discountRate;

        // =========================
        // OVERNIGHT FEE RULE
        // =========================
        decimal overnight = checkIn.Hour < OvernightHour && checkOut.Hour >= OvernightHour
            ? OvernightFee
            : 0;

        // =========================
        // LOST TICKET PENALTY
        // =========================
        decimal penalty = isLostTicket ? LostTicketFee : 0;

        // FINAL TOTAL
        decimal total = subtotal - discount + overnight + penalty;

        // Ensure fee never goes negative
        if (total < 0) total = 0;

        return new ParkingFeeResult
        {
            BaseFee = baseFee,
            SurchargeAmount = surcharge,
            DiscountAmount = discount,
            LostTicketPenalty = penalty,
            TotalFee = total,
            Breakdown = "Calculated"
        };
    }
}