using glint_backend.DTOs.Requests;
using glint_backend.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace glint_backend.Controllers.User;

[ApiController]
[Route("user")]
public class UserController(
    IUserService userService,
    IResumeService resumeService) : ControllerBase
{
    // ── Hardcoded until auth is implemented ───────────────────────────────────
    // Replace with: Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!)
    private static readonly Guid MockUserId = Guid.Parse("00000000-0000-0000-0000-000000000001");

    // ── GET /user/history?page=1&pageSize=10 ──────────────────────────────────
    [HttpGet("history")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetHistory([FromQuery] PaginationRequest pagination)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var result = await userService.GetHistoryAsync(MockUserId, pagination);
        return Ok(result);
    }

    // ── GET /user/statistics ──────────────────────────────────────────────────
    [HttpGet("statistics")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetStatistics()
    {
        var result = await userService.GetStatisticsAsync(MockUserId);
        return Ok(result);
    }

    // ── POST /user/resume ─────────────────────────────────────────────────────
    [HttpPost("resume")]
    [RequestSizeLimit(5 * 1024 * 1024)]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UploadResume(IFormFile file)
    {
        if (file is null)
            return BadRequest(new { error = "No file provided." });

        try
        {
            var result = await resumeService.UploadAsync(MockUserId, file);
            return CreatedAtAction(null, new { id = result.ResumeId }, result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}