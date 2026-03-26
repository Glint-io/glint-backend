using glint_backend.Models;

namespace glint_backend.Repositories.Interfaces
{
    public interface IRefreshTokenRepository
    {
        // When a user is logged in it should create a token for JWT such that on page-refresh and or leaving they should
        // not get logged out, but rather check the refresh token to see if the same session is valid or not. Said token
        // should refresh on stuff like creating or doing anything as that resets the session expiery timer
        Task CreateAsync(RefreshToken token);
        Task<RefreshToken?> GetByTokenAsync(string token);
        Task RevokeAsync(Guid id);
    }
}
