using DriveX.DTOs.Submissions;
using DriveX.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DriveX.Controllers;

[ApiController]
[Route("api/submission")]
public class SubmissionController : ControllerBase
{
    private readonly ISubmissionService _submissionService;

    public SubmissionController(ISubmissionService submissionService)
    {
        _submissionService = submissionService;
    }

    // Staff record a returned car.
    [Authorize(Roles = "ADMIN,STUFF")]
    [HttpPost]
    public async Task<ActionResult<SubmissionResponse>> SubmitCar(SubmitCarRequest request)
    {
        return Ok(await _submissionService.CreateAsync(request));
    }
}
