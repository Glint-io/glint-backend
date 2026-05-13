using glint_backend.DTOs.Auth;
using glint_backend.Services;
using glint_backend.Services.Interfaces;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace glint_backend.Controllers
{
    [ApiController]
    [Route("auth")]
    [EnableRateLimiting("auth")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(IAuthService authService, ILogger<AuthController> logger)
        {
            _authService = authService;
            _logger = logger;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] DTOs.Auth.RegisterRequest request)
        {
            try
            {
                await _authService.RegisterAsync(request);
                return StatusCode(StatusCodes.Status201Created, new { message = "Registration successful. Check your email for a verification code." });
            }
            catch (EmailDeliveryException ex)
            {
                return UnprocessableEntity(new { error = ex.Message });
            }
            catch (Exception ex) when (ex.Message == "Email already in use")
            {
                return Conflict(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during registration for {Email}", request.Email);
                return StatusCode(500, new { error = "An unexpected error occurred. Please try again." });
            }
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] DTOs.Auth.LoginRequest request)
        {
            try
            {
                var result = await _authService.LoginAsync(request);
                return Ok(result);
            }
            catch (Exception ex) when (ex.Message == "Invalid email or password")
            {
                return Unauthorized(new { error = ex.Message });
            }
            catch (Exception ex) when (ex.Message.StartsWith("Email not verified"))
            {
                return StatusCode(403, new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during login for {Email}", request.Email);
                return StatusCode(500, new { error = "An unexpected error occurred. Please try again." });
            }
        }

        [HttpPost("login/otc")]
        public async Task<IActionResult> LoginWithOtc([FromBody] OtcLoginRequest request)
        {
            try
            {
                var result = await _authService.LoginWithOtcAsync(request);
                return Ok(result);
            }
            catch (Exception ex) when (ex.Message == "Invalid or expired code")
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during OTC login");
                return StatusCode(500, new { error = "An unexpected error occurred. Please try again." });
            }
        }

        [HttpPost("verify-email")]
        public async Task<IActionResult> VerifyEmail([FromBody] VerifyEmailRequest request)
        {
            try
            {
                await _authService.VerifyEmailAsync(request);
                return Ok(new { message = "Email verified successfully." });
            }
            catch (Exception ex) when (ex.Message == "Invalid or expired code")
            {
                // 410 Gone signals to the frontend that the code existed but is no longer valid,
                // which triggers the manual re-entry fallback in VerifyEmailPage.tsx.
                return StatusCode(410, new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during email verification");
                return StatusCode(500, new { error = "An unexpected error occurred. Please try again." });
            }
        }

        [HttpPost("refresh")]
        public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequest request)
        {
            try
            {
                var result = await _authService.RefreshAsync(request);
                return Ok(result);
            }
            catch (Exception ex) when (ex.Message == "Invalid or expired refresh token")
            {
                return Unauthorized(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during token refresh");
                return StatusCode(500, new { error = "An unexpected error occurred. Please try again." });
            }
        }

        [HttpPost("resend-verification")]
        public async Task<IActionResult> ResendVerification([FromBody] ResendVerificationRequest request)
        {
            try
            {
                await _authService.ResendVerificationAsync(request);
                // Always return 200 — don't confirm whether the email is registered.
                return Ok(new { message = "If that address is registered and unverified, a new code has been sent." });
            }
            catch (EmailDeliveryException ex)
            {
                return UnprocessableEntity(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during resend-verification for {Email}", request.Email);
                return StatusCode(500, new { error = "An unexpected error occurred. Please try again." });
            }
        }

        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] DTOs.Auth.ForgotPasswordRequest request)
        {
            try
            {
                await _authService.ForgotPasswordAsync(request);
                return Ok(new { message = "If that address is registered, a reset code has been sent." });
            }
            catch (EmailDeliveryException ex)
            {
                return UnprocessableEntity(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during forgot-password for {Email}", request.Email);
                return StatusCode(500, new { error = "An unexpected error occurred." });
            }
        }

        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] DTOs.Auth.ResetPasswordRequest request)
        {
            try
            {
                await _authService.ResetPasswordAsync(request);
                return Ok(new { message = "Password reset successfully. You can now sign in." });
            }
            catch (Exception ex) when (ex.Message == "Invalid or expired code")
            {
                return StatusCode(410, new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during reset-password");
                return StatusCode(500, new { error = "An unexpected error occurred." });
            }
        }
    }
}