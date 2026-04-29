using glint_backend.DTOs.Auth;
using glint_backend.Models;
using glint_backend.Repositories.Interfaces;
using glint_backend.Services.Interfaces;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace glint_backend.Services
{
    public class AuthService : IAuthService
    {
        private readonly IUserRepository _users;
        private readonly IOtcRepository _otcs;
        private readonly IRefreshTokenRepository _tokens;
        private readonly IConfiguration _config;
        private readonly IEmailService _email;

        public AuthService(IUserRepository users, IOtcRepository otcs, IRefreshTokenRepository tokens, IConfiguration config, IEmailService email)
        {
            _users = users;
            _otcs = otcs;
            _tokens = tokens;
            _config = config;
            _email = email;
        }

        public async Task RegisterAsync(RegisterRequest request)
        {
            var existing = await _users.GetByEmailAsync(request.Email);
            if (existing is not null)
                throw new Exception("Email already in use");

            var user = new User
            {
                Id = Guid.NewGuid(),
                Email = request.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                IsEmailVerified = false,
                CreatedAt = DateTime.UtcNow
            };

            await _users.CreateAsync(user);

            var otc = new OneTimeCode
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                Code = GenerateOtcCode(),
                Type = OneTimeCodeType.EmailVerification,
                ExpiresAt = DateTime.UtcNow.AddMinutes(15),
                IsUsed = false
            };

            await _otcs.CreateAsync(otc);

            var (subject, html, plain) = EmailTemplates.BuildVerificationEmail(
                otc.Code,
                _config["Frontend:BaseUrl"]!
            );

            try
            {
                await _email.SendAsync(user.Email, subject, html, plain);
            }
            catch (EmailDeliveryException)
            {
                // The email address is invalid or unreachable. Roll back the newly
                // created user and OTC so the address is not left as a ghost account
                // that can never be verified.
                await _otcs.DeleteAsync(otc.Id);
                await _users.DeleteAsync(user.Id);

                // Re-throw so the controller can return a 422 with the user-facing message.
                throw;
            }
        }

        private static string GenerateOtcCode()
        {
            // Upper bound is exclusive, so 1_000_000 is required to include 999999.
            return Random.Shared.Next(100_000, 1_000_000).ToString();
        }

        public async Task<AuthResponse> LoginAsync(LoginRequest request)
        {
            var user = await _users.GetByEmailAsync(request.Email);
            if (user is null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
                throw new Exception("Invalid email or password");
            if (!user.IsEmailVerified)
                throw new Exception("Email not verified, please check your spam and inbox for verification code");

            var accessToken = GenerateJwt(user);

            var refreshToken = new RefreshToken
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                Token = Guid.NewGuid().ToString(),
                ExpiresAt = DateTime.UtcNow.AddDays(7)
            };

            await _tokens.CreateAsync(refreshToken);

            return new AuthResponse
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken.Token
            };
        }

        private string GenerateJwt(User user)
        {
            var key = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));

            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Email, user.Email)
            };

            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"],
                audience: _config["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(int.Parse(_config["Jwt:ExpiryMinutes"]!)),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public async Task<AuthResponse> LoginWithOtcAsync(OtcLoginRequest request)
        {
            var otc = await _otcs.GetByCodeAsync(request.Code, OneTimeCodeType.Login);
            if (otc is null || otc.IsUsed || otc.ExpiresAt < DateTime.UtcNow)
                throw new Exception("Invalid or expired code");

            await _otcs.MarkUsedAsync(otc);

            var user = await _users.GetByUuidAsync(otc.UserId);
            if (user is null)
                throw new Exception("User not found");

            var accessToken = GenerateJwt(user);

            var refreshToken = new RefreshToken
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                Token = Guid.NewGuid().ToString(),
                ExpiresAt = DateTime.UtcNow.AddDays(7)
            };

            await _tokens.CreateAsync(refreshToken);

            return new AuthResponse
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken.Token
            };
        }

        public async Task VerifyEmailAsync(VerifyEmailRequest request)
        {
            var otc = await _otcs.GetByCodeAsync(request.Code, OneTimeCodeType.EmailVerification);
            if (otc is null || otc.IsUsed || otc.ExpiresAt < DateTime.UtcNow)
                throw new Exception("Invalid or expired code");

            await _otcs.MarkUsedAsync(otc);

            var user = await _users.GetByUuidAsync(otc.UserId);
            if (user is null)
                throw new Exception("User not found");

            user.IsEmailVerified = true;
            await _users.UpdateAsync(user);
        }

        public async Task<AuthResponse> RefreshAsync(RefreshTokenRequest request)
        {
            var existing = await _tokens.GetByTokenAsync(request.RefreshToken);
            if (existing is null || existing.IsRevoked || existing.ExpiresAt < DateTime.UtcNow)
                throw new Exception("Invalid or expired refresh token");

            await _tokens.RevokeAsync(existing.Id);

            var user = await _users.GetByUuidAsync(existing.UserId);
            if (user is null)
                throw new Exception("User not found");

            var accessToken = GenerateJwt(user);

            var newRefreshToken = new RefreshToken
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                Token = Guid.NewGuid().ToString(),
                ExpiresAt = DateTime.UtcNow.AddDays(7)
            };

            await _tokens.CreateAsync(newRefreshToken);

            return new AuthResponse
            {
                AccessToken = accessToken,
                RefreshToken = newRefreshToken.Token
            };
        }

        public async Task ResendVerificationAsync(ResendVerificationRequest request)
        {
            var user = await _users.GetByEmailAsync(request.Email);

            // Don't leak whether an account exists — always return success to the caller.
            // Only actually send if the user exists AND is unverified.
            if (user is null || user.IsEmailVerified)
                return;

            // Invalidate any existing unused codes for this user so old links stop working.
            await _otcs.InvalidateAllAsync(user.Id, OneTimeCodeType.EmailVerification);

            var otc = new OneTimeCode
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                Code = GenerateOtcCode(),
                Type = OneTimeCodeType.EmailVerification,
                ExpiresAt = DateTime.UtcNow.AddMinutes(15),
                IsUsed = false
            };

            await _otcs.CreateAsync(otc);

            var (subject, html, plain) = EmailTemplates.BuildVerificationEmail(
                otc.Code,
                _config["Frontend:BaseUrl"]!
            );

            await _email.SendAsync(user.Email, subject, html, plain);
        }
    }
}