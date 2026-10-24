using DriveX.DTOs;
using DriveX.DTOs.Cars;

namespace DriveX.Services;

public interface ICarService
{
    Task<CarResultResponse> AddCarAsync(AddCarRequest request);
    Task<CarResultResponse> UpdateCarAsync(long carId, UpdateCarRequest request);
    Task<List<CarResponse>> GetActiveCarsAsync();
    Task<List<CarResponse>> GetMaintenanceCarsAsync();
    Task<MessageResponse> UpdateRentalStatusAsync(UpdateCarRentalStatusRequest request);
    Task<MessageResponse> MarkAsMaintenanceAsync(long carId);
    Task<CarResultResponse> UpdateActiveStatusAsync(UpdateCarActiveStatusRequest request);
}
