using glint_backend.DTOs.Requests;
using glint_backend.DTOs.Responses;
using glint_backend.Exceptions;
using glint_backend.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Text.Json;

namespace glint_backend.Controllers.Public;

[ApiController]
[Route("analyze")]
public class AnalysisController(
    IAnalysisService analysisService,
    IFileValidationService fileValidator) : ControllerBase
{
    // ── Standard (waits for all 3, returns combined result) ──────────────────

    [HttpPost]
    [RequestSizeLimit(5 * 1024 * 1024)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> Analyze([FromForm] AnalyzeRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var isAuthenticated = userIdClaim is not null;

        try
        {
            if (!isAuthenticated)
            {
                if (request.Resume is null)
                    return BadRequest(new { error = "A resume PDF is required for guest analysis." });

                var validation = await fileValidator.ValidatePdfAsync(request.Resume);
                if (!validation.IsValid)
                    return BadRequest(new { error = validation.ErrorMessage });

                var guestResult = await analysisService.AnalyzeGuestAsync(
                    validation.FileBytes!, request.JobText);

                return Ok(guestResult);
            }

            var userId = Guid.Parse(userIdClaim!);

            if (request.ResumeId.HasValue && request.Resume is not null)
                return BadRequest(new { error = "Supply either a Resume file or a ResumeId, not both." });

            var result = await analysisService.AnalyzeAuthenticatedAsync(userId, request);
            return Ok(result);
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    // ── SSE streaming (emits each method result as it completes) ─────────────

    [HttpPost("stream")]
    [RequestSizeLimit(5 * 1024 * 1024)]
    public async Task AnalyzeStream([FromForm] AnalyzeRequest request)
    {
        if (!ModelState.IsValid)
        {
            Response.StatusCode = 400;
            return;
        }

        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var isAuthenticated = userIdClaim is not null;

        byte[]? pdfBytes = null;
        Guid? userId = null;

        if (!isAuthenticated)
        {
            if (request.Resume is null) { Response.StatusCode = 400; return; }

            var validation = await fileValidator.ValidatePdfAsync(request.Resume);
            if (!validation.IsValid) { Response.StatusCode = 400; return; }

            pdfBytes = validation.FileBytes!;
        }
        else
        {
            userId = Guid.Parse(userIdClaim!);

            if (request.ResumeId.HasValue && request.Resume is not null)
            {
                Response.StatusCode = 400;
                return;
            }
        }

        Response.Headers.Append("Content-Type", "text/event-stream");
        Response.Headers.Append("Cache-Control", "no-cache");
        Response.Headers.Append("X-Accel-Buffering", "no");

        var jsonOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

        try
        {
            var stream = isAuthenticated
                ? analysisService.StreamAuthenticatedAsync(userId!.Value, request, HttpContext.RequestAborted)
                : analysisService.StreamGuestAsync(pdfBytes!, request.JobText, HttpContext.RequestAborted);

            await foreach (var item in stream)
            {
                var json = JsonSerializer.Serialize(item, jsonOptions);
                await Response.WriteAsync($"data: {json}\n\n");
                await Response.Body.FlushAsync();
            }

            await Response.WriteAsync("data: [DONE]\n\n");
            await Response.Body.FlushAsync();
        }
        catch (OperationCanceledException) { }
    }
}