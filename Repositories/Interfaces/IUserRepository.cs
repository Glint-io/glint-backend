using glint_backend.Models;

namespace glint_backend.Repositories.Interfaces
{
    public interface IUserRepository
    {
        // Users should be able to register (create), login (read), update (update), and delete (delete) their accounts.
        Task<User?> GetByEmailAsync(string email);
        Task<User?> GetByUuidAsync(Guid uuid);
        Task CreateAsync(User user);
        Task UpdateAsync(User user);
        Task DeleteAsync(Guid id);
    }
}
