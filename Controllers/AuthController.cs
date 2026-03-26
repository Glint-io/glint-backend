using glint_backend.DTOs.Auth;
using glint_backend.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace glint_backend.Controllers
{
    [ApiController]
    [Route("auth")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterRequest request)
        {
            await _authService.RegisterAsync(request);
            return StatusCode(201);
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginRequest request)
        {
            var result = await _authService.LoginAsync(request);
            return Ok(result);
        }

        [HttpPost("login/otc")]
        public async Task<IActionResult> LoginWithOtc(OtcLoginRequest request)
        {
            var result = await _authService.LoginWithOtcAsync(request);
            return Ok(result);
        }

        [HttpPost("verify-email")]
        public async Task<IActionResult> VerifyEmail(VerifyEmailRequest request)
        {
            await _authService.VerifyEmailAsync(request);
            return Ok();
        }

        [HttpPost("refresh")]
        public async Task<IActionResult> Refresh(RefreshTokenRequest request)
        {
            var result = await _authService.RefreshAsync(request);
            return Ok(result);
        }
    }
}