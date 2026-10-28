using DriveX.DTOs.Ai;
using DriveX.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DriveX.Controllers;

[ApiController]
[Route("api/ai")]
public class AiAnalystController : ControllerBase
{
    private readonly IAiAnalystService _analyst;

    public AiAnalystController(IAiAnalystService analyst)
    {
        _analyst = analyst;
    }

    // ADMIN only: the analyst reads bookings and revenue.
    [Authorize(Roles = "ADMIN")]
    [HttpPost("analyst")]
    public async Task<ActionResult<AnalystResponse>> Ask(AskRequest request)
    {
        return Ok(await _analyst.AskAsync(request.Question, request.History));
    }
}
