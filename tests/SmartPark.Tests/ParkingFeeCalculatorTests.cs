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

    [Theory]
    [InlineData(VehicleType.Motorcycle, 2, 1000)]
    [InlineData(VehicleType.Car, 2, 2000)]
    [InlineData(VehicleType.SUV, 2, 3000)]
    public void CalculateFee_BasicHourlyRates_ReturnExpectedFee(
        VehicleType vehicleType,
        int hours,
        decimal expectedFee)
    {
        // Arrange
        var calc = new ParkingFeeCalculator();

        var checkIn = new DateTime(2026, 1, 1, 8, 0, 0);
        var checkOut = checkIn.AddHours(hours);

        // Act
        var result = calc.CalculateFee(
            vehicleType,
            MembershipTier.Guest,
            checkIn,
            checkOut);

        // Assert
        Assert.Equal(expectedFee, result.TotalFee);
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

// test silver member discount
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

// test platinum member discount
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

    // Test that fees are never negative regardless of input
    [Property]
    public void CalculateFee_TotalFee_ShouldNeverBeNegative(int hours)
    {
        // Arrange
        var calc = new ParkingFeeCalculator();

        hours = Math.Abs(hours % 24) + 1;

        var checkIn = new DateTime(2026, 1, 1, 8, 0, 0);
        var checkOut = checkIn.AddHours(hours);

        // Act
        var result = calc.CalculateFee(
            VehicleType.Car,
            MembershipTier.Guest,
            checkIn,
            checkOut);

        // Assert
        Assert.True(result.TotalFee >= 0);
    }

// Test that longer parking durations do not result in lower fees
    [Property]
    public void CalculateFee_LongerDuration_ShouldNotCostLess(int hours1, int hours2)
    {
        // Arrange
        var calc = new ParkingFeeCalculator();

        hours1 = Math.Abs(hours1 % 10) + 1;
        hours2 = hours1 + Math.Abs(hours2 % 10) + 1;

        var checkIn = new DateTime(2026, 1, 1, 8, 0, 0);

        var shorter = calc.CalculateFee(
            VehicleType.Car,
            MembershipTier.Guest,
            checkIn,
            checkIn.AddHours(hours1));

        var longer = calc.CalculateFee(
            VehicleType.Car,
            MembershipTier.Guest,
            checkIn,
            checkIn.AddHours(hours2));

        // Assert
        Assert.True(longer.TotalFee >= shorter.TotalFee);
    }

    #endregion
}
