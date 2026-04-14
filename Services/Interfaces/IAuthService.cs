using glint_backend.DTOs.Auth;

namespace glint_backend.Services.Interfaces
{
    public interface IAuthService
    {
        Task RegisterAsync(RegisterRequest request);
        Task<AuthResponse> LoginAsync(LoginRequest request);
        Task<AuthResponse> LoginWithOtcAsync(OtcLoginRequest request);
        Task VerifyEmailAsync(VerifyEmailRequest request);
        Task<AuthResponse> RefreshAsync(RefreshTokenRequest request);
        Task ResendVerificationAsync(ResendVerificationRequest request);
    }
}
