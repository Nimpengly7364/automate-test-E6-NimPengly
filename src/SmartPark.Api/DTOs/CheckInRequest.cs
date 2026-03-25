using System.ComponentModel.DataAnnotations;

namespace SmartPark.Api.DTOs;

public class CheckInRequest
{
    [Required(ErrorMessage = "License plate is required.")]
    [StringLength(20, MinimumLength = 2, ErrorMessage = "License plate must be 2–20 characters.")]
    [RegularExpression(@"^[A-Za-z0-9\-]+$", ErrorMessage = "License plate may only contain letters, digits, and hyphens.")]
    public string LicensePlate { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vehicle type is required.")]
    [Range(0, 2, ErrorMessage = "Vehicle type must be 0 (Motorcycle), 1 (Car), or 2 (SUV).")]
    public int VehicleType { get; set; }
}
