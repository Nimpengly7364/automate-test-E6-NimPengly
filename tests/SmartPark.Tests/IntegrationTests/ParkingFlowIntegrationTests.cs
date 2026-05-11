using Moq;
using SmartPark.Core.Interfaces;
using SmartPark.Core.Models;
using SmartPark.Core.Services;

namespace SmartPark.Tests.IntegrationTests;

public class ParkingFlowIntegrationTests
{
    // Real system under test
    private readonly ParkingSessionManager _manager;

    // Mocked clock so we can control time in tests
    private readonly Mock<IDateTimeProvider> _clock = new();

    // Shared starting time for tests
    private readonly DateTime _baseTime = new(2025, 1, 1, 10, 0, 0);

    // Mutable current time used by the fake clock
    private DateTime _now;

    public ParkingFlowIntegrationTests()
    {
        // Initialize current time
        _now = _baseTime;

        // Whenever the app asks for current time,
        // return the value stored in _now
        _clock.Setup(c => c.Now)
              .Returns(() => _now);

        // Mock payment gateway
        // Always return successful payment
        var payment = new Mock<IPaymentGateway>();

        payment.Setup(p =>
                p.ProcessPaymentAsync(
                    It.IsAny<string>(),
                    It.IsAny<decimal>()))
               .ReturnsAsync(true);

        // Mock notification service
        // Do nothing when sending receipt
        var notification = new Mock<INotificationService>();

        notification.Setup(n =>
                n.SendReceiptAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>()))
                    .Returns(Task.CompletedTask);

        // Mock membership service
        // Every vehicle becomes Guest member
        var membership = new Mock<IMembershipService>();

        membership.Setup(m =>
                m.GetMembershipTier(It.IsAny<string>()))
                  .Returns(MembershipTier.Guest);

        // REAL repository (not mocked)
        var repo = new InMemoryParkingRepository();

        // REAL fee calculator
        var calc = new ParkingFeeCalculator();

        // Create real ParkingSessionManager
        _manager = new ParkingSessionManager(
            calc,
            payment.Object,
            notification.Object,
            membership.Object,
            repo,
            _clock.Object
        );
    }

    // Helper method to simulate time passing
    private void SetTime(DateTime time)
    {
        _now = time;
    }

    // ────────────────────────────────────────────────────────────
    // FULL FLOW — NORMAL CHECK IN + CHECK OUT
    // Verify 2-hour car parking = 2,000 KHR
    // ────────────────────────────────────────────────────────────

    [Fact]
    public async Task FullFlow_Car_2Hours_Returns2000()
    {
        // Arrange
        SetTime(_baseTime);

        // Check in vehicle
        var ticket = await _manager.CheckInAsync(
            "CAR-001",
            VehicleType.Car);

        // Simulate 2 hours later
        SetTime(_baseTime.AddHours(2));

        // Act
        var result = await _manager.CheckOutAsync(
            ticket.TicketId,
            "123");

        // Assert
        Assert.Equal(2000m, result.TotalFee);
    }

    // ────────────────────────────────────────────────────────────
    // GRACE PERIOD
    // Parking less than or equal to 30 min is free
    // ────────────────────────────────────────────────────────────

    [Fact]
    public async Task FullFlow_GracePeriod_Returns0()
    {
        // Arrange
        SetTime(_baseTime);

        var ticket = await _manager.CheckInAsync(
            "CAR-002",
            VehicleType.Car);

        // Only parked for 20 minutes
        SetTime(_baseTime.AddMinutes(20));

        // Act
        var result = await _manager.CheckOutAsync(
            ticket.TicketId,
            "123");

        // Assert
        Assert.Equal(0m, result.TotalFee);
    }

    // ────────────────────────────────────────────────────────────
    // LOST TICKET
    // Lost ticket should add 20,000 KHR penalty
    // ────────────────────────────────────────────────────────────

    [Fact]
    public async Task FullFlow_LostTicket_AddsPenalty()
    {
        // Arrange
        SetTime(_baseTime);

        var ticket = await _manager.CheckInAsync(
            "CAR-003",
            VehicleType.Car);

        // Stay for 1 hour
        SetTime(_baseTime.AddHours(1));

        // Act
        var result = await _manager.CheckOutAsync(
            ticket.TicketId,
            "123",
            isLostTicket: true
        );

        // 1 hour car = 1,000
        // Lost ticket penalty = 20,000
        // Total = 21,000
        Assert.Equal(21000m, result.TotalFee);
    }

    // ────────────────────────────────────────────────────────────
    // WEEKEND SURCHARGE
    // Saturday/Sunday adds 20%
    // ────────────────────────────────────────────────────────────

    [Fact]
    public async Task FullFlow_Weekend_Adds20Percent()
    {
        // Arrange
        // January 4, 2025 = Saturday
        SetTime(new DateTime(2025, 1, 4, 10, 0, 0));

        var ticket = await _manager.CheckInAsync(
            "CAR-004",
            VehicleType.Car);

        // 2 hours later
        SetTime(new DateTime(2025, 1, 4, 12, 0, 0));

        // Act
        var result = await _manager.CheckOutAsync(
            ticket.TicketId,
            "123");

        // Base fee = 2,000
        // Weekend surcharge = 400
        // Total = 2,400
        Assert.Equal(2400m, result.TotalFee);
    }

    // ────────────────────────────────────────────────────────────
    // GRACEFUL DEGRADATION
    // Checkout should still succeed even if
    // notification service fails
    // ────────────────────────────────────────────────────────────

    [Fact]
    public async Task FullFlow_NotificationFails_CheckoutStillSucceeds()
    {
        // Mock notification service to throw error
        var notification = new Mock<INotificationService>();

        notification.Setup(n =>
                n.SendReceiptAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>()))
                    .ThrowsAsync(new Exception("fail"));

        // Payment still succeeds
        var payment = new Mock<IPaymentGateway>();

        payment.Setup(p =>
                p.ProcessPaymentAsync(
                    It.IsAny<string>(),
                    It.IsAny<decimal>()))
               .ReturnsAsync(true);

        // Guest membership
        var membership = new Mock<IMembershipService>();

        membership.Setup(m =>
                m.GetMembershipTier(It.IsAny<string>()))
                  .Returns(MembershipTier.Guest);

        // Real repository + calculator
        var repo = new InMemoryParkingRepository();
        var calc = new ParkingFeeCalculator();

        // Create manager with failing notification service
        var manager = new ParkingSessionManager(
            calc,
            payment.Object,
            notification.Object,
            membership.Object,
            repo,
            _clock.Object
        );

        // Arrange
        SetTime(_baseTime);

        var ticket = await manager.CheckInAsync(
            "CAR-005",
            VehicleType.Car);

        SetTime(_baseTime.AddHours(2));

        // Act
        var result = await manager.CheckOutAsync(
            ticket.TicketId,
            "123");

        // Assert
        // Checkout still succeeds
        Assert.Equal(2000m, result.TotalFee);
    }

    // ────────────────────────────────────────────────────────────
    // MULTIPLE VEHICLES
    // Check in 3 vehicles and check out 1
    // Verify the others remain active
    // ────────────────────────────────────────────────────────────

    [Fact]
    public async Task FullFlow_MultipleVehicles_TwoRemainActive()
    {
        // Arrange
        SetTime(_baseTime);

        var ticket1 = await _manager.CheckInAsync(
            "CAR-101",
            VehicleType.Car);

        var ticket2 = await _manager.CheckInAsync(
            "CAR-102",
            VehicleType.Car);

        var ticket3 = await _manager.CheckInAsync(
            "CAR-103",
            VehicleType.Car);

        // Check out only first vehicle
        SetTime(_baseTime.AddHours(2));

        await _manager.CheckOutAsync(
            ticket1.TicketId,
            "123");

        // Act
        var result2 = await _manager.CheckOutAsync(
            ticket2.TicketId,
            "123");

        var result3 = await _manager.CheckOutAsync(
            ticket3.TicketId,
            "123");

        // Assert
        Assert.Equal(2000m, result2.TotalFee);
        Assert.Equal(2000m, result3.TotalFee);
    }

    // ────────────────────────────────────────────────────────────
    // DUPLICATE CHECK IN
    // Same vehicle cannot check in twice
    // ────────────────────────────────────────────────────────────

    [Fact]
    public async Task FullFlow_DuplicateCheckIn_ThrowsException()
    {
        // Arrange
        SetTime(_baseTime);

        await _manager.CheckInAsync(
            "DUP-001",
            VehicleType.Car);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _manager.CheckInAsync(
                "DUP-001",
                VehicleType.Car));
    }


    // ────────────────────────────────────────────────────────────
    // FAILED PAYMENT
    // Failed payment should NOT complete checkout
    // Ticket must remain active
    // ────────────────────────────────────────────────────────────

    [Fact]
    public async Task FullFlow_FailedPayment_TicketRemainsActive()
    {
        // Arrange
        var payment = new Mock<IPaymentGateway>();

        payment.Setup(p =>
                p.ProcessPaymentAsync(
                    It.IsAny<string>(),
                    It.IsAny<decimal>()))
               .ReturnsAsync(false);

        var notification = new Mock<INotificationService>();

        notification.Setup(n =>
                n.SendReceiptAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>()))
                    .Returns(Task.CompletedTask);

        var membership = new Mock<IMembershipService>();

        membership.Setup(m =>
                m.GetMembershipTier(It.IsAny<string>()))
                  .Returns(MembershipTier.Guest);

        var repo = new InMemoryParkingRepository();

        var manager = new ParkingSessionManager(
            new ParkingFeeCalculator(),
            payment.Object,
            notification.Object,
            membership.Object,
            repo,
            _clock.Object);

        SetTime(_baseTime);

        var ticket = await manager.CheckInAsync(
            "FAIL-001",
            VehicleType.Car);

        SetTime(_baseTime.AddHours(2));

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() =>
            manager.CheckOutAsync(
                ticket.TicketId,
                "123"));

        // Ticket should still be active
        var activeTicket = await repo.GetActiveTicketByPlateAsync("FAIL-001");

        Assert.NotNull(activeTicket);
    }

    // ────────────────────────────────────────────────────────────
    // EDGE TO EDGE
    // Overnight + Weekend + Gold Membership
    // ────────────────────────────────────────────────────────────

    [Fact]
    public async Task FullFlow_WeekendOvernightGoldMember_CalculatesCorrectFee()
    {
        // Arrange

        // Saturday 8 PM
        SetTime(new DateTime(2025, 1, 4, 20, 0, 0));

        var payment = new Mock<IPaymentGateway>();

        payment.Setup(p =>
                p.ProcessPaymentAsync(
                    It.IsAny<string>(),
                    It.IsAny<decimal>()))
               .ReturnsAsync(true);

        var notification = new Mock<INotificationService>();

        notification.Setup(n =>
                n.SendReceiptAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>()))
                    .Returns(Task.CompletedTask);

        // Gold member
        var membership = new Mock<IMembershipService>();

        membership.Setup(m =>
                m.GetMembershipTier(It.IsAny<string>()))
                  .Returns(MembershipTier.Gold);

        var repo = new InMemoryParkingRepository();

        var manager = new ParkingSessionManager(
            new ParkingFeeCalculator(),
            payment.Object,
            notification.Object,
            membership.Object,
            repo,
            _clock.Object);

        var ticket = await manager.CheckInAsync(
            "VIP-001",
            VehicleType.Car);

        // Saturday 11 PM
        SetTime(new DateTime(2025, 1, 4, 23, 0, 0));

        // Act
        var result = await manager.CheckOutAsync(
            ticket.TicketId,
            "123");

        // Calculation:
        // Base = 3,000
        // Weekend surcharge 20% = 600
        // Subtotal = 3,600
        // Gold discount 25% = 900
        // After discount = 2,700
        // Overnight fee = 2,000
        // Total = 4,700
        Assert.Equal(4700m, result.TotalFee);

    }
}

