using glint_backend.DTOs.Requests;
using glint_backend.Exceptions;
using glint_backend.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace glint_backend.Controllers.Public;

[ApiController]
[Route("analyze")]
public class AnalysisController(
    IAnalysisService analysisService,
    IFileValidationService fileValidator) : ControllerBase
{
    // Guest must upload a PDF
    // Authenticated User can upload a PDF  OR  supply a saved ResumeId.

    // Rate limit: wire up the "analyze" policy in appsettings.json
    //   - Guests:         5 req/hr
    //   - Authenticated: 50 req/hr
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

        // Determine if user is authenticated by checking for a NameIdentifier claim (user ID).
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var isAuthenticated = userIdClaim is not null;

        try
        {
            // If guest, must upload a PDF
            if (!isAuthenticated)
            {
                // Guests must always upload a file; ResumeId is not meaningful
                if (request.Resume is null)
                    return BadRequest(new { error = "A resume PDF is required for guest analysis." });

                var validation = await fileValidator.ValidatePdfAsync(request.Resume);
                if (!validation.IsValid)
                    return BadRequest(new { error = validation.ErrorMessage });

                var guestResult = await analysisService.AnalyzeGuestAsync(
                    validation.FileBytes!, request.JobText);

                return Ok(guestResult);
            }

            // If Authenticated, can upload a PDF OR supply a ResumeId.
            var userId = Guid.Parse(userIdClaim!);

            // Prevent ambiguity: if both a file and ResumeId are supplied, reject the request.
            if (request.ResumeId.HasValue && request.Resume is not null)
                return BadRequest(new
                {
                    error = "Supply either a Resume file or a ResumeId, not both."
                });

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
}