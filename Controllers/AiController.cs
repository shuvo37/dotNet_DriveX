using DriveX.DTOs.Ai;
using DriveX.Services;
using Microsoft.AspNetCore.Mvc;

namespace DriveX.Controllers;

[ApiController]
[Route("api/ai")]
public class AiController : ControllerBase
{
    private readonly IAiAssistantService _assistant;

    public AiController(IAiAssistantService assistant)
    {
        _assistant = assistant;
    }

    // The customer's question goes in, a short answer based on the real car list comes out.
    [HttpPost("ask")]
    public async Task<ActionResult<AskResponse>> Ask(AskRequest request)
    {
        return Ok(new AskResponse(await _assistant.AskAsync(request.Question, request.History)));
    }
}
