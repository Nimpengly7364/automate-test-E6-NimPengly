namespace SmartPark.Core.Interfaces;

/// <summary>
/// Abstracts system clock for testability.
/// </summary>
public interface IDateTimeProvider
{
    DateTime Now { get; }
}
