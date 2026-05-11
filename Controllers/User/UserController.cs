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
public class UserController(
    IUserService userService,
    IResumeService resumeService,
    IJobAdvertisementService jobAdvertisementService) : ControllerBase
{
    private Guid CurrentUserId =>
        Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet("history")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetHistory([FromQuery] AnalysisHistoryRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var result = await userService.GetHistoryAsync(CurrentUserId, request);
        return Ok(result);
    }

    [HttpDelete("history")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ClearHistory(
        [FromQuery] AnalysisHistoryRange range = AnalysisHistoryRange.All)
    {
        var deleted = await userService.ClearHistoryAsync(CurrentUserId, range);
        // Return 200 OK with count since frontend expects the data for confirmation message
        return Ok(new { deleted });
    }

    [HttpGet("statistics")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetStatistics(
        [FromQuery] AnalysisHistoryRange range = AnalysisHistoryRange.All)
    {
        var result = await userService.GetStatisticsAsync(CurrentUserId, range);
        return Ok(result);
    }

    [HttpPost("resume")]
    [RequestSizeLimit(5 * 1024 * 1024)]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UploadResume(IFormFile file)
    {
        if (file is null)
            return BadRequest(new { error = "No file provided." });

        try
        {
            var result = await resumeService.UploadAsync(CurrentUserId, file);
            return StatusCode(StatusCodes.Status201Created, result);
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

    [HttpGet("resume/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetResume(Guid id)
    {
        var resume = await resumeService.GetByIdAsync(CurrentUserId, id);
        if (resume is null)
            return NotFound(new { error = "Resume not found." });

        return File(resume.FileData, "application/pdf");
    }

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
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpGet("job-advertisement")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetJobAdvertisements()
    {
        var result = await jobAdvertisementService.GetUserJobAdvertisementsAsync(CurrentUserId);
        return Ok(result);
    }

    [HttpPost("job-advertisement")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateJobAdvertisement(
        [FromBody] CreateJobAdvertisementRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.RawText))
            return BadRequest(new { error = "Job advertisement text cannot be empty." });

        try
        {
            var result = await jobAdvertisementService.CreateOrGetAsync(CurrentUserId, request);
            return StatusCode(StatusCodes.Status201Created, result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpDelete("job-advertisement/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteJobAdvertisement(Guid id)
    {
        try
        {
            await jobAdvertisementService.DeleteAsync(CurrentUserId, id);
            return NoContent();
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }

    [HttpDelete("account")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteAccount([FromBody] DeleteAccountRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            await userService.DeleteOwnAccountAsync(CurrentUserId, request.Password);
            return NoContent();
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { error = ex.Message });
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }
}