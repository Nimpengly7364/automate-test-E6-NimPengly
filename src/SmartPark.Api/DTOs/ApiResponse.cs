namespace SmartPark.Api.DTOs;

public class ApiResponse<T>
{
    public bool Success { get; set; }
    public T? Data { get; set; }
    public string? Error { get; set; }

    public static ApiResponse<T> Ok(T data) => new() { Success = true, Data = data };
    public static ApiResponse<T> Fail(string error) => new() { Success = false, Error = error };
}

public class TicketResponse
{
    public string TicketId { get; set; } = string.Empty;
    public string LicensePlate { get; set; } = string.Empty;
    public string VehicleType { get; set; } = string.Empty;
    public string Membership { get; set; } = string.Empty;
    public DateTime CheckInTime { get; set; }
    public DateTime? CheckOutTime { get; set; }
    public bool IsActive { get; set; }
}

public class FeeResponse
{
    public decimal BaseFee { get; set; }
    public decimal SurchargeAmount { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal LostTicketPenalty { get; set; }
    public decimal TotalFee { get; set; }
    public string Breakdown { get; set; } = string.Empty;
}
