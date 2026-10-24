using DriveX.DTOs.Payments;

namespace DriveX.Services;

public interface IPaymentService
{
    Task<PaymentResultResponse> CreateAsync(CreatePaymentRequest request, long callerUserId, bool callerIsStaff);
}
