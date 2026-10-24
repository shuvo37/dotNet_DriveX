using DriveX.DTOs.Submissions;

namespace DriveX.Services;

public interface ISubmissionService
{
    Task<SubmissionResponse> CreateAsync(SubmitCarRequest request);
}
