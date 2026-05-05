using SmartPark.Core.Models;

namespace SmartPark.Core.Services;

public class ParkingFeeCalculator
{
    // Rates
    private const decimal MotorcycleRate = 500m;
    private const decimal CarRate = 1000m;
    private const decimal SuvRate = 1500m;

    // Caps
    private const decimal MotorcycleCap = 4000m;
    private const decimal CarCap = 8000m;
    private const decimal SuvCap = 12000m;

    // Rules
    private const int GraceMinutes = 30;
    private const decimal OvernightFee = 2000m;
    private const int OvernightHour = 22;

    private const decimal WeekendRate = 0.20m;
    private const decimal HolidayRate = 0.50m;

    private const decimal LostTicketFee = 20000m;

    public ParkingFeeResult CalculateFee(
        VehicleType vehicleType,
        MembershipTier membership,
        DateTime checkIn,
        DateTime checkOut,
        bool isLostTicket = false,
        bool isHoliday = false)
    {
        if (checkOut <= checkIn)
            throw new ArgumentException("Check-out must be after check-in");

        var duration = checkOut - checkIn;

        if (duration.TotalMinutes <= GraceMinutes)
        {
            return new ParkingFeeResult
            {
                BaseFee = 0,
                TotalFee = isLostTicket ? LostTicketFee : 0
            };
        }

        decimal rate = vehicleType switch
        {
            VehicleType.Motorcycle => MotorcycleRate,
            VehicleType.Car => CarRate,
            VehicleType.SUV => SuvRate,
            _ => throw new ArgumentOutOfRangeException()
        };

        decimal cap = vehicleType switch
        {
            VehicleType.Motorcycle => MotorcycleCap,
            VehicleType.Car => CarCap,
            VehicleType.SUV => SuvCap,
            _ => 0
        };

        decimal hours = (decimal)Math.Ceiling((duration.TotalMinutes - GraceMinutes) / 60.0);
        decimal baseFee = Math.Min(hours * rate, cap);

        // surcharge
        decimal surcharge = 0;
        if (isHoliday)
            surcharge = baseFee * HolidayRate;
        else if (checkIn.DayOfWeek == DayOfWeek.Saturday || checkIn.DayOfWeek == DayOfWeek.Sunday)
            surcharge = baseFee * WeekendRate;

        decimal subtotal = baseFee + surcharge;

        // discount
        decimal discountRate = membership switch
        {
            MembershipTier.Silver => 0.10m,
            MembershipTier.Gold => 0.25m,
            MembershipTier.Platinum => 0.40m,
            _ => 0m
        };

        decimal discount = subtotal * discountRate;

        // overnight
        decimal overnight = checkIn.Hour < OvernightHour && checkOut.Hour >= OvernightHour
            ? OvernightFee
            : 0;

        // lost ticket
        decimal penalty = isLostTicket ? LostTicketFee : 0;

        decimal total = subtotal - discount + overnight + penalty;

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