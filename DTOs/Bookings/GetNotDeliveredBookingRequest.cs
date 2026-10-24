using System.ComponentModel.DataAnnotations;

namespace DriveX.DTOs.Bookings;

public class GetNotDeliveredBookingRequest
{
    [Required(ErrorMessage = "model name cannot be blank")]
    public string ModelName { get; set; } = string.Empty;
}
