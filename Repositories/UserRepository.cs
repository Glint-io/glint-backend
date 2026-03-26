using glint_backend.Data;
using glint_backend.Models;
using glint_backend.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace glint_backend.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly AppDBContext _context;

        public UserRepository(AppDBContext context)
        {
            _context = context;
        }

        public async Task<User?> GetByEmailAsync(string email)
        {
            return await _context.Users.FirstOrDefaultAsync(x => x.Email == email);
        }
        public async Task<User?> GetByUuidAsync(Guid uuid)
        {
            return await _context.Users.FirstOrDefaultAsync(x => x.Id == uuid);
        }

        public async Task CreateAsync(User user)
        {
            _context.Users.Add(user);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(User user)
        {
            _context.Users.Update(user);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(Guid uuid)
        {
            var user = await _context.Users.FirstOrDefaultAsync(x => x.Id == uuid);
            if (user is null) return;

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();
        }
    }
}
