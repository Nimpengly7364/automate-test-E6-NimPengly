using System.ComponentModel.DataAnnotations;

namespace SmartPark.Api.DTOs;

public class CheckOutRequest
{
    [Required(ErrorMessage = "Phone number is required.")]
    [StringLength(20, MinimumLength = 6, ErrorMessage = "Phone number must be 6–20 characters.")]
    public string PhoneNumber { get; set; } = string.Empty;

    public bool IsLostTicket { get; set; } = false;

    public bool IsHoliday { get; set; } = false;
}
