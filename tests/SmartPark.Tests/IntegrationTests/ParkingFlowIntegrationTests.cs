using Moq;
using SmartPark.Core.Interfaces;
using SmartPark.Core.Models;
using SmartPark.Core.Services;

namespace SmartPark.Tests.IntegrationTests;

public class ParkingFlowIntegrationTests
{
    private readonly ParkingSessionManager _manager;
    private readonly Mock<IDateTimeProvider> _clock = new();
    private readonly DateTime _baseTime = new(2025, 1, 1, 10, 0, 0);

    private DateTime _now;

    public ParkingFlowIntegrationTests()
    {
        _now = _baseTime;

        _clock.Setup(c => c.Now).Returns(() => _now);

        var payment = new Mock<IPaymentGateway>();
        payment.Setup(p => p.ProcessPaymentAsync(It.IsAny<string>(), It.IsAny<decimal>()))
               .ReturnsAsync(true);

        var notification = new Mock<INotificationService>();
        notification.Setup(n => n.SendReceiptAsync(It.IsAny<string>(), It.IsAny<string>()))
                    .Returns(Task.CompletedTask);

        var membership = new Mock<IMembershipService>();
        membership.Setup(m => m.GetMembershipTier(It.IsAny<string>()))
                  .Returns(MembershipTier.Guest);

        var repo = new InMemoryParkingRepository();
        var calc = new ParkingFeeCalculator();

        _manager = new ParkingSessionManager(
            calc,
            payment.Object,
            notification.Object,
            membership.Object,
            repo,
            _clock.Object
        );
    }

    private void SetTime(DateTime time) => _now = time;

    [Fact]
    public async Task FullFlow_Car_2Hours_Returns2000()
    {
        SetTime(_baseTime);

        var ticket = await _manager.CheckInAsync("CAR-001", VehicleType.Car);

        SetTime(_baseTime.AddHours(2));

        var result = await _manager.CheckOutAsync(ticket.TicketId, "123");

        Assert.Equal(2000m, result.TotalFee);
    }

    [Fact]
    public async Task FullFlow_GracePeriod_Returns0()
    {
        SetTime(_baseTime);

        var ticket = await _manager.CheckInAsync("CAR-002", VehicleType.Car);

        SetTime(_baseTime.AddMinutes(20));

        var result = await _manager.CheckOutAsync(ticket.TicketId, "123");

        Assert.Equal(0m, result.TotalFee);
    }

    [Fact]
    public async Task FullFlow_LostTicket_AddsPenalty()
    {
        SetTime(_baseTime);

        var ticket = await _manager.CheckInAsync("CAR-003", VehicleType.Car);

        SetTime(_baseTime.AddHours(1));

        var result = await _manager.CheckOutAsync(
            ticket.TicketId,
            "123",
            isLostTicket: true
        );

        Assert.Equal(21000m, result.TotalFee);
    }

    [Fact]
    public async Task FullFlow_Weekend_Adds20Percent()
    {
        SetTime(new DateTime(2025, 1, 4, 10, 0, 0)); // Saturday

        var ticket = await _manager.CheckInAsync("CAR-004", VehicleType.Car);

        SetTime(new DateTime(2025, 1, 4, 12, 0, 0));

        var result = await _manager.CheckOutAsync(ticket.TicketId, "123");

        Assert.Equal(2400m, result.TotalFee);
    }

    [Fact]
    public async Task FullFlow_NotificationFails_CheckoutStillSucceeds()
    {
        var notification = new Mock<INotificationService>();
        notification.Setup(n => n.SendReceiptAsync(It.IsAny<string>(), It.IsAny<string>()))
                    .ThrowsAsync(new Exception("fail"));

        var payment = new Mock<IPaymentGateway>();
        payment.Setup(p => p.ProcessPaymentAsync(It.IsAny<string>(), It.IsAny<decimal>()))
               .ReturnsAsync(true);

        var membership = new Mock<IMembershipService>();
        membership.Setup(m => m.GetMembershipTier(It.IsAny<string>()))
                  .Returns(MembershipTier.Guest);

        var repo = new InMemoryParkingRepository();
        var calc = new ParkingFeeCalculator();

        var manager = new ParkingSessionManager(
            calc,
            payment.Object,
            notification.Object,
            membership.Object,
            repo,
            _clock.Object
        );

        SetTime(_baseTime);

        var ticket = await manager.CheckInAsync("CAR-005", VehicleType.Car);

        SetTime(_baseTime.AddHours(2));

        var result = await manager.CheckOutAsync(ticket.TicketId, "123");

        Assert.Equal(2000m, result.TotalFee);
    }
}