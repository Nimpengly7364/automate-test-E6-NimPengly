using SmartPark.Core.Models;
using SmartPark.Core.Services;
using FsCheck;
using FsCheck.Xunit;

namespace SmartPark.Tests;

public class ParkingFeeCalculatorTests
{
    private readonly ParkingFeeCalculator _calculator = new();

    // ────────────────────────────────────────────────────────────
    //  EXAMPLE TEST — shows the naming convention and AAA pattern.
    //  Delete or keep this; it does not count toward your grade.
    // ────────────────────────────────────────────────────────────

    // [Fact]
    // public void CalculateFee_ZeroDuration_ReturnsFree()
    // {
    //     // Arrange
    //     var checkIn = new DateTime(2026, 3, 16, 10, 0, 0);  // Monday
    //     var checkOut = checkIn; // same time = 0 duration

    //     // Act
    //     var result = _calculator.CalculateFee(VehicleType.Car, MembershipTier.Guest, checkIn, checkOut);

    //     // Assert
    //     Assert.Equal(0m, result.TotalFee);

    // }


    #region Basic Fee Calculation
    // Test basic hourly rates for each vehicle type
    // Consider using [Theory] with [InlineData] for multiple scenarios
    [Fact]
    public void CalculateFee_Motorcycle_2Hours_Returns1000()
    {
        // Arrange
        var calc = new ParkingFeeCalculator();

        var start = new DateTime(2026, 1, 1, 8, 0, 0);
        var end = start.AddHours(2);

        // Act
        var result = calc.CalculateFee(
            VehicleType.Motorcycle,
            MembershipTier.Guest,
            start,
            end
        );

        // Assert
        Assert.Equal(1000, result.TotalFee);
    }
    #endregion

    #region Grace Period
    [Fact]
    public void CalculateFee_GracePeriod_30Minutes_ReturnsFree()
    {
        var calc = new ParkingFeeCalculator();

        var checkIn = new DateTime(2026, 1, 1, 8, 0, 0);
        var checkOut = checkIn.AddMinutes(30);

        var result = calc.CalculateFee(
            VehicleType.Car,
            MembershipTier.Guest,
            checkIn,
            checkOut);

        Assert.Equal(0m, result.TotalFee);
    }
    #endregion

    #region Duration Rounding
    // Test how partial hours are rounded for billing
    [Fact]
    public void CalculateFee_31Minutes_Returns1000()
    {
        var calc = new ParkingFeeCalculator();

        var checkIn = new DateTime(2026, 1, 1, 8, 0, 0);
        var checkOut = checkIn.AddMinutes(31);

        var result = calc.CalculateFee(
            VehicleType.Car,
            MembershipTier.Guest,
            checkIn,
            checkOut);

        Assert.Equal(1000m, result.TotalFee);
    }
    #endregion

    #region Daily Cap
    // Test that fees respect maximum daily limits per vehicle type

    [Fact]
    public void CalculateFee_Car24Hours_AppliesDailyCap()
    {
        // Arrange
        var calc = new ParkingFeeCalculator();

        var checkIn = new DateTime(2026, 1, 1, 0, 0, 0);
        var checkOut = checkIn.AddHours(24);

        // Act
        var result = calc.CalculateFee(
            VehicleType.Car,
            MembershipTier.Guest,
            checkIn,
            checkOut);

        // Assert
        Assert.Equal(8000m, result.TotalFee);
    }

    #endregion


    #region Overnight Fee
    // Test the flat fee applied for sessions that extend into late hours#region Overnight Fee
    [Fact]
    public void CalculateFee_OvernightParking_Adds2000Fee()
    {
        // Arrange
        var calc = new ParkingFeeCalculator();

        var checkIn = new DateTime(2026, 1, 1, 20, 0, 0);
        var checkOut = new DateTime(2026, 1, 1, 23, 0, 0);

        // Act
        var result = calc.CalculateFee(
            VehicleType.Car,
            MembershipTier.Guest,
            checkIn,
            checkOut);

        // Assert
        Assert.Equal(5000m, result.TotalFee);
    }


    #endregion

    #region Weekend Surcharge
    // Test the percentage-based surcharge on specific days

    [Fact]
    public void CalculateFee_Weekend_Adds20PercentSurcharge()
    {
        // Arrange
        var calc = new ParkingFeeCalculator();

        var saturday = new DateTime(2026, 1, 3, 8, 0, 0);
        var checkOut = saturday.AddHours(2);

        // Act
        var result = calc.CalculateFee(
            VehicleType.Car,
            MembershipTier.Guest,
            saturday,
            checkOut);

        // Assert
        Assert.Equal(2400m, result.TotalFee);
    }

    #endregion

    #region Holiday Surcharge
    // Test holiday pricing and its interaction with weekend pricing
    [Fact]
    public void CalculateFee_HolidayParking_Adds50PercentSurcharge()
    {
        var calc = new ParkingFeeCalculator();

        var checkIn = new DateTime(2026, 1, 1, 10, 0, 0);
        var checkOut = checkIn.AddHours(2);

        var result = calc.CalculateFee(
            VehicleType.Car,
            MembershipTier.Guest,
            checkIn,
            checkOut,
            isHoliday: true);

        Assert.Equal(3000m, result.TotalFee);
    }
    #endregion

    #region Membership Discounts
    // Test discount tiers and what amounts they apply to

    [Fact]
    public void CalculateFee_GoldMember_Gets20PercentDiscount()
    {
        // Arrange
        var calc = new ParkingFeeCalculator();

        var checkIn = new DateTime(2026, 1, 1, 8, 0, 0);
        var checkOut = checkIn.AddHours(2);

        // Act
        var result = calc.CalculateFee(
            VehicleType.Car,
            MembershipTier.Gold,
            checkIn,
            checkOut);

        // Assert
        Assert.Equal(1600m, result.TotalFee);
    }

    [Fact]
    public void CalculateFee_SilverMember_Gets10PercentDiscount()
    {
        // Arrange
        var calc = new ParkingFeeCalculator();

        var checkIn = new DateTime(2026, 1, 1, 8, 0, 0);
        var checkOut = checkIn.AddHours(2);

        // Act
        var result = calc.CalculateFee(
            VehicleType.Car,
            MembershipTier.Silver,
            checkIn,
            checkOut);

        // Assert
        Assert.Equal(1800m, result.TotalFee);
    }

    [Fact]
    public void CalculateFee_PlatinumMember_Gets30PercentDiscount()
    {
        // Arrange
        var calc = new ParkingFeeCalculator();

        var checkIn = new DateTime(2026, 1, 1, 8, 0, 0);
        var checkOut = checkIn.AddHours(2);

        // Act
        var result = calc.CalculateFee(
            VehicleType.Car,
            MembershipTier.Platinum,
            checkIn,
            checkOut);

        // Assert
        Assert.Equal(1400m, result.TotalFee);
    }

    #endregion

    #region Lost Ticket
    // Test the penalty and how it interacts with other fee modifiers
    [Fact]
    public void CalculateFee_LostTicket_AddsPenalty()
    {
        var calc = new ParkingFeeCalculator();

        var checkIn = new DateTime(2026, 1, 1, 10, 0, 0);
        var checkOut = checkIn.AddHours(2);

        var result = calc.CalculateFee(
            VehicleType.Car,
            MembershipTier.Guest,
            checkIn,
            checkOut,
            isLostTicket: true);

        Assert.Equal(22000m, result.TotalFee);
    }
    #endregion

    #region Edge Cases
    // Test invalid inputs and boundary conditions
    [Fact]
    public void CalculateFee_NegativeDuration_ThrowsException()
    {
        // Arrange
        var calc = new ParkingFeeCalculator();

        var checkIn = new DateTime(2026, 1, 1, 10, 0, 0);
        var checkOut = checkIn.AddHours(-2);

        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            calc.CalculateFee(
                VehicleType.Car,
                MembershipTier.Guest,
                checkIn,
                checkOut));
    }
    #endregion

    #region Property-Based Tests
    // Write at least 5 FsCheck properties that must hold for ALL valid inputs
    // You may need custom Arbitrary<T> for generating valid DateTime pairs
    #endregion
}
