using Moq;
using SmartPark.Core.Interfaces;
using SmartPark.Core.Models;
using SmartPark.Core.Services;

namespace SmartPark.Tests;

public class ParkingSessionManagerTests
{
    // ────────────────────────────────────────────────────────────
    //  SHARED SETUP — create test doubles and the system-under-test.
    //  Moq's Mock<T> creates test doubles that can act as:
    //    - Stubs: .Setup().Returns() — provide canned answers
    //    - Mocks: .Verify()         — assert interactions happened
    //  You can use a constructor, or duplicate this in each test.
    // ────────────────────────────────────────────────────────────

    private readonly Mock<IPaymentGateway> _paymentStub = new();
    private readonly Mock<INotificationService> _notificationStub = new();
    private readonly Mock<IMembershipService> _membershipStub = new();
    private readonly Mock<IParkingRepository> _repoStub = new();
    private readonly Mock<IDateTimeProvider> _dateTimeStub = new();
    private readonly ParkingFeeCalculator _feeCalculator = new();
    private readonly ParkingSessionManager _manager;

    public ParkingSessionManagerTests()
    {
        _manager = new ParkingSessionManager(
            _feeCalculator,
            _paymentStub.Object,
            _notificationStub.Object,
            _membershipStub.Object,
            _repoStub.Object,
            _dateTimeStub.Object);
    }

    // ────────────────────────────────────────────────────────────
    //  EXAMPLE TEST — shows stub setup + mock verification pattern.
    //  .Setup().Returns() = STUB behavior (canned answer)
    //  .Verify()          = MOCK behavior (interaction assertion)
    //  Delete or keep this; it does not count toward your grade.
    // ────────────────────────────────────────────────────────────

    [Fact]
    public async Task CheckInAsync_NewVehicle_LookUpMembership()
    {
        // Arrange — configure stubs (canned return values)
        _membershipStub.Setup(m => m.GetMembershipTier("PP-9999")).Returns(MembershipTier.Guest);
        _repoStub.Setup(r => r.GetActiveTicketByPlateAsync("PP-9999")).ReturnsAsync((ParkingTicket?)null);
        _dateTimeStub.Setup(d => d.Now).Returns(new DateTime(2026, 3, 16, 10, 0, 0));

        // Act
        var ticket = await _manager.CheckInAsync("PP-9999", VehicleType.Car);

        // Assert — verify as mock (was this interaction called?)
        _membershipStub.Verify(m => m.GetMembershipTier("PP-9999"), Times.Once);
        Assert.Equal("PP-9999", ticket.Vehicle.LicensePlate);
    }

    #region CheckIn — Happy Path
    // Test successful vehicle check-in and verify correct interactions
    [Fact]
    public async Task CheckInAsync_ValidVehicle_SaveTicket()
    {
        // Arrange
        _membershipStub.Setup(m => m.GetMembershipTier("2AB-1234"))
            .Returns(MembershipTier.Silver);

        _repoStub.Setup(r => r.GetActiveTicketByPlateAsync("2AB-1234"))
            .ReturnsAsync((ParkingTicket?)null);

        _dateTimeStub.Setup(d => d.Now)
            .Returns(new DateTime(2026, 3, 16, 9, 0, 0));

        // Act
        var ticket = await _manager.CheckInAsync("2AB-1234", VehicleType.Car);

        // Assert
        Assert.NotNull(ticket);
        Assert.Equal("2AB-1234", ticket.Vehicle.LicensePlate);

        _repoStub.Verify(r => r.SaveTicketAsync(It.IsAny<ParkingTicket>()), Times.Once);
    }
    #endregion

    #region CheckIn — Validation
    // Test check-in error scenarios and verify side effects
    [Fact]
    public async Task CheckInAsync_DuplicateCheckIn_ThrowException()
    {
        // Arrange
        var existingTicket = new ParkingTicket
        {
            Vehicle = new Vehicle { LicensePlate = "2AB-1234" },
        };

        _repoStub.Setup(r => r.GetActiveTicketByPlateAsync("2AB-1234"))
            .ReturnsAsync(existingTicket);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _manager.CheckInAsync("2AB-1234", VehicleType.Car));

        _repoStub.Verify(r => r.SaveTicketAsync(It.IsAny<ParkingTicket>()), Times.Never);
    }

    #endregion

    #region CheckOut — Happy Path
    // Test successful check-out with payment and notification
    [Fact]
    public async Task CheckOutAsync_ValidTicket_ProcessPaymentAndSendReceipt()
    {
        // Arrange
        var ticketId = "TK-001";

        var ticket = new ParkingTicket
        {
            Vehicle = new Vehicle
            {
                LicensePlate = "2AB-1234",
                Type = VehicleType.Car
            },

            CheckInTime = new DateTime(2026, 3, 16, 8, 0, 0),
            CheckOutTime = null
        };

        _repoStub.Setup(r => r.GetTicketByIdAsync(ticketId))
            .ReturnsAsync(ticket);

        _dateTimeStub.Setup(d => d.Now)
            .Returns(new DateTime(2026, 3, 16, 10, 0, 0));

        _paymentStub.Setup(p =>
                p.ProcessPaymentAsync(
                    It.IsAny<string>(),
                    It.IsAny<decimal>()))
            .ReturnsAsync(true);

        // Act
        var result = await _manager.CheckOutAsync(
            ticketId,
            "012345678",
            false,
            false);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.TotalFee >= 0);

        _paymentStub.Verify(p =>
                p.ProcessPaymentAsync(
                    It.IsAny<string>(),
                    It.IsAny<decimal>()),
            Times.Once);

        _repoStub.Verify(r =>
                r.UpdateTicketAsync(It.IsAny<ParkingTicket>()),
            Times.Once);

        _notificationStub.Verify(n =>
                n.SendReceiptAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>()),
            Times.Once);
    }
    #endregion

    #region CheckOut — Payment Failure
    // Test behavior when the payment step fails
    [Fact]
    public async Task CheckOutAsync_PaymentFails_ThrowException()
    {
        // Arrange
        var ticketId = "TK-002";

        var ticket = new ParkingTicket
        {
            Vehicle = new Vehicle
            {
                LicensePlate = "3CD-5678",
                Type = VehicleType.Car
            },

            CheckInTime = new DateTime(2026, 3, 16, 8, 0, 0),
            CheckOutTime = null
        };

        _repoStub.Setup(r => r.GetTicketByIdAsync(ticketId))
            .ReturnsAsync(ticket);

        _dateTimeStub.Setup(d => d.Now)
            .Returns(new DateTime(2026, 3, 16, 11, 0, 0));

        _paymentStub.Setup(p =>
                p.ProcessPaymentAsync(
                    It.IsAny<string>(),
                    It.IsAny<decimal>()))
            .ReturnsAsync(false);

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() =>
            _manager.CheckOutAsync(
                ticketId,
                "012345678",
                false,
                false));

        _repoStub.Verify(r =>
            r.UpdateTicketAsync(It.IsAny<ParkingTicket>()),
            Times.Never);

        _notificationStub.Verify(n =>
                n.SendReceiptAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>()),
            Times.Never);
    }

    #endregion

    #region CheckOut — Notification Failure
    // Test what happens when sending the receipt fails
    [Fact]
    public async Task CheckOutAsync_NotificationFails_CheckoutStillSucceeds()
    {
        // Arrange
        var ticketId = "TK-003";

        var ticket = new ParkingTicket
        {
            Vehicle = new Vehicle
            {
                LicensePlate = "4EF-9999",
                Type = VehicleType.SUV
            },

            CheckInTime = new DateTime(2026, 3, 16, 9, 0, 0),
            CheckOutTime = null
        };

        _repoStub.Setup(r => r.GetTicketByIdAsync(ticketId))
            .ReturnsAsync(ticket);

        _dateTimeStub.Setup(d => d.Now)
            .Returns(new DateTime(2026, 3, 16, 12, 0, 0));

        _paymentStub.Setup(p =>
                p.ProcessPaymentAsync(
                    It.IsAny<string>(),
                    It.IsAny<decimal>()))
            .ReturnsAsync(true);

        _notificationStub.Setup(n =>
                n.SendReceiptAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>()))
            .ThrowsAsync(new Exception("SMS failed"));

        // Act
        var result = await _manager.CheckOutAsync(
            ticketId,
            "012345678",
            false,
            false);

        // Assert
        Assert.NotNull(result);

        _paymentStub.Verify(p =>
                p.ProcessPaymentAsync(
                    It.IsAny<string>(),
                    It.IsAny<decimal>()),
            Times.Once);

        _repoStub.Verify(r =>
            r.UpdateTicketAsync(It.IsAny<ParkingTicket>()),
            Times.Once);
    }
    #endregion

    #region CheckOut — Validation
    // Test check-out error scenarios for missing or invalid tickets
    [Fact]
    public async Task CheckOutAsync_TicketNotFound_ThrowException()
    {
        // Arrange
        var ticketId = "INVALID";

        _repoStub.Setup(r => r.GetTicketByIdAsync(ticketId))
            .ReturnsAsync((ParkingTicket?)null);

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _manager.CheckOutAsync(
                ticketId,
                "012345678",
                false,
                false));

        _paymentStub.Verify(p =>
                p.ProcessPaymentAsync(
                    It.IsAny<string>(),
                    It.IsAny<decimal>()),
            Times.Never);
    }
    [Fact]
    public async Task CheckOutAsync_AlreadyCheckedOut_ThrowException()
    {
        // Arrange
        var ticketId = "TK-004";

        var ticket = new ParkingTicket
        {

            Vehicle = new Vehicle
            {
                LicensePlate = "5GH-7777",
                Type = VehicleType.Car
            },

            CheckInTime = new DateTime(2026, 3, 16, 8, 0, 0),

            CheckOutTime = new DateTime(2026, 3, 16, 10, 0, 0)
        };

        _repoStub.Setup(r => r.GetTicketByIdAsync(ticketId))
            .ReturnsAsync(ticket);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _manager.CheckOutAsync(
                ticketId,
                "012345678",
                false,
                false));

        _paymentStub.Verify(p =>
                p.ProcessPaymentAsync(
                    It.IsAny<string>(),
                    It.IsAny<decimal>()),
            Times.Never);
    }
    
    #endregion

    #region Verify Interaction Order
    // Verify that dependencies are called in the correct sequence
    #endregion
}
