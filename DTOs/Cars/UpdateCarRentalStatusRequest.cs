using System.ComponentModel.DataAnnotations;
using DriveX.Enums;

namespace DriveX.DTOs.Cars;

public class UpdateCarRentalStatusRequest
{
    [Required(ErrorMessage = "carId cannot be null")]
    [Range(1, long.MaxValue, ErrorMessage = "carId must be positive")]
    public long? CarId { get; set; }

    [Required(ErrorMessage = "rentalStatus cannot be null")]
    public RentalStatus? RentalStatus { get; set; }
}
