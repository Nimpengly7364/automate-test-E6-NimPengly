namespace SmartPark.Core.Models;

/// <summary>
/// Represents a vehicle entering the parking facility.
/// </summary>
public class Vehicle
{
    public string LicensePlate { get; set; } = string.Empty;
    public VehicleType Type { get; set; }
    public MembershipTier Membership { get; set; } = MembershipTier.Guest;
}
