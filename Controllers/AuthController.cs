using glint_backend.DTOs.Auth;
using glint_backend.Helpers;
using glint_backend.Services;
using glint_backend.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
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
        private readonly IConfiguration _configuration;
        private readonly IWebHostEnvironment _environment;

        public AuthController(
            IAuthService authService,
            ILogger<AuthController> logger,
            IConfiguration configuration,
            IWebHostEnvironment environment)
        {
            _authService = authService;
            _logger = logger;
            _configuration = configuration;
            _environment = environment;
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
                if (request.UseSessionCookies)
                    AuthCookieHelper.AppendAuthCookies(Response, result, _configuration, _environment);
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
                if (request.UseSessionCookies)
                    AuthCookieHelper.AppendAuthCookies(Response, result, _configuration, _environment);
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
        public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequest? request)
        {
            var refreshCookieName = _configuration["Auth:RefreshCookie"] ?? "glint_refresh";

            string? refreshToken = null;
            if (!string.IsNullOrWhiteSpace(request?.RefreshToken))
                refreshToken = request.RefreshToken;
            else if (Request.Cookies.TryGetValue(refreshCookieName, out var cookieToken) && !string.IsNullOrEmpty(cookieToken))
                refreshToken = cookieToken;

            if (string.IsNullOrWhiteSpace(refreshToken))
                return Unauthorized(new { error = "Invalid or expired refresh token" });

            try
            {
                var result = await _authService.RefreshAsync(new RefreshTokenRequest { RefreshToken = refreshToken });

                var suppliedBodyToken = !string.IsNullOrWhiteSpace(request?.RefreshToken);
                var usedCookieOnly = !suppliedBodyToken;
                var promote = request?.PromoteToCookieSession == true;

                if (promote || usedCookieOnly)
                    AuthCookieHelper.AppendAuthCookies(Response, result, _configuration, _environment);

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

        [HttpPost("logout")]
        [AllowAnonymous]
        public IActionResult Logout()
        {
            AuthCookieHelper.DeleteAuthCookies(Response, _configuration, _environment);
            return Ok(new { message = "Signed out" });
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
