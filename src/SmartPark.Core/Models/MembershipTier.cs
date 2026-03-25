namespace SmartPark.Core.Models;

/// <summary>
/// Membership levels that determine discount percentages.
/// </summary>
public enum MembershipTier
{
    Guest,      // 0% discount
    Silver,     // 10% discount
    Gold,       // 25% discount
    Platinum    // 40% discount
}
