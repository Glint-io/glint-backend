using glint_backend.Data;
using glint_backend.Models;
using glint_backend.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace glint_backend.Repositories
{
    public class RefreshTokenRepository : IRefreshTokenRepository
    {
        private readonly AppDBContext _dbContext;

        public RefreshTokenRepository(AppDBContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task CreateAsync(RefreshToken refreshToken)
        {
            _dbContext.RefreshTokens.Add(refreshToken);
            await _dbContext.SaveChangesAsync();
        }

        public async Task<RefreshToken?> GetByTokenAsync(string token)
        {
            return await _dbContext.RefreshTokens.FirstOrDefaultAsync(x => x.Token == token);
        }

        public async Task RevokeAsync(Guid id)
        {
            var token = await _dbContext.RefreshTokens.FirstOrDefaultAsync(x => x.Id == id);
            if (token is null) return;

            token.IsRevoked = true;
            await _dbContext.SaveChangesAsync();
        }
    }
}
