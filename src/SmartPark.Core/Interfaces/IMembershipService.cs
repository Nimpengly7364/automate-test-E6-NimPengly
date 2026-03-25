using SmartPark.Core.Models;

namespace SmartPark.Core.Interfaces;

/// <summary>
/// Looks up membership information by license plate.
/// </summary>
public interface IMembershipService
{
    /// <summary>
    /// Returns the membership tier for a given license plate.
    /// Returns MembershipTier.Guest if not found.
    /// </summary>
    MembershipTier GetMembershipTier(string licensePlate);
}
