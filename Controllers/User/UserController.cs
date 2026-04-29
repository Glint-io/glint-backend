using glint_backend.DTOs.Requests;
using glint_backend.DTOs.Responses;
using glint_backend.Exceptions;
using glint_backend.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace glint_backend.Controllers.User;

[ApiController]
[Route("user")]
[Authorize]

// User spesific endpoints for managing resume history, statistics, and saved resumes.
public class UserController(
    IUserService userService,
    IResumeService resumeService) : ControllerBase
{
    // Helper to get the current authenticated user's ID from claims. This is used in all endpoints to ensure actions are performed on the correct user's data.
    private Guid CurrentUserId =>
        Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    
    [HttpGet("history")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]

    public async Task<IActionResult> GetHistory([FromQuery] PaginationRequest pagination)
    {
        // Validate pagination parameters. If invalid, return 400 with details.
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var result = await userService.GetHistoryAsync(CurrentUserId, pagination);
        return Ok(result);
    }

    // User's overall statistics endpoint, consiting of avg score, total analyses, and trends over time.
    [HttpGet("statistics")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetStatistics()
    {
        var result = await userService.GetStatisticsAsync(CurrentUserId);
        return Ok(result);
    }

    // Upload new resume
    [HttpPost("resume")]
    [RequestSizeLimit(5 * 1024 * 1024)]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UploadResume(IFormFile file)
    {
        // Validate that a file was provided. If not, return 400 with an error message.
        if (file is null)
            return BadRequest(new { error = "No file provided." });
        // Validate that the file is a PDF and does not exceed size limits. If invalid, return 400 with details.
        try
        {
            var result = await resumeService.UploadAsync(CurrentUserId, file);
            return CreatedAtAction(null, new { id = result.ResumeId }, result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet("resume")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetResumes()
    {
        var result = await resumeService.GetAllAsync(CurrentUserId);
        return Ok(result);
    }


    // Delete a saved resume by ID. Only the owner can delete their resume. If the resume does not exist, return 404. If the resume is currently in use for an analysis, return 409 Conflict.
    [HttpDelete("resume/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DeleteResume(Guid id)
    {
        try
        {
            await resumeService.DeleteAsync(CurrentUserId, id);
            return NoContent();
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (ConflictException ex)
        {
            return Conflict(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message, inner = ex.InnerException?.Message });
        }
    }
}