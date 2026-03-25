using System.ComponentModel.DataAnnotations;

namespace SmartPark.Api.DTOs;

public class FeeEstimateRequest : IValidatableObject
{
    [Required(ErrorMessage = "Vehicle type is required.")]
    [Range(0, 2, ErrorMessage = "Vehicle type must be 0 (Motorcycle), 1 (Car), or 2 (SUV).")]
    public int VehicleType { get; set; }

    [Range(0, 3, ErrorMessage = "Membership must be 0 (Guest), 1 (Silver), 2 (Gold), or 3 (Platinum).")]
    public int Membership { get; set; } = 0;

    [Required(ErrorMessage = "Check-in time is required.")]
    public DateTime CheckIn { get; set; }

    [Required(ErrorMessage = "Check-out time is required.")]
    public DateTime CheckOut { get; set; }

    public bool IsLostTicket { get; set; } = false;
    public bool IsHoliday { get; set; } = false;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (CheckOut < CheckIn)
            yield return new ValidationResult("Check-out time must be after check-in time.", [nameof(CheckOut)]);
    }
}
