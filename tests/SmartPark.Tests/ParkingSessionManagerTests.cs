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
    #endregion

    #region CheckOut — Payment Failure
    // Test behavior when the payment step fails
    #endregion

    #region CheckOut — Notification Failure
    // Test what happens when sending the receipt fails
    #endregion

    #region CheckOut — Validation
    // Test check-out error scenarios for missing or invalid tickets
    #endregion

    #region Verify Interaction Order
    // Verify that dependencies are called in the correct sequence
    #endregion
}
