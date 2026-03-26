using glint_backend.DTOs.Requests;
using glint_backend.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace glint_backend.Controllers.Public;

[ApiController]
[Route("analyze")]
public class AnalysisController(IAnalysisService analysisService) : ControllerBase
{
    // ── Hardcoded until auth is implemented ───────────────────────────────────
    // Replace with: Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!)
    private static readonly Guid MockUserId = Guid.Parse("00000000-0000-0000-0000-000000000001");

    // ── POST /analyze ─────────────────────────────────────────────────────────
    // Rate limit: 5 requests/hr per user (stricter than the default 50/hr).
    // Wire up AspNetCoreRateLimit or a similar middleware and tag this endpoint
    // with the "analyze" policy in appsettings.json.
    [HttpPost]
    [RequestSizeLimit(5 * 1024 * 1024)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> Analyze([FromForm] AnalyzeRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var result = await analysisService.AnalyzeAsync(MockUserId, request);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}