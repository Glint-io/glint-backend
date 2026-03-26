using glint_backend.Models;

namespace glint_backend.Repositories.Interfaces
{
    public interface IOtcRepository
    {
        // On a user-registration/login
        // Create code -> Send to user -> User sends code back -> Gets checked -> Gets marked used -> User logged in.
        Task CreateAsync(OneTimeCode code);
        Task<OneTimeCode?> GetByCodeAsync(string code, OneTimeCodeType type);
        Task MarkUsedAsync(OneTimeCode code);
    }
}
