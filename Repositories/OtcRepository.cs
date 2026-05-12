using glint_backend.Data;
using glint_backend.Models;
using glint_backend.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace glint_backend.Repositories
{
    public class OtcRepository : IOtcRepository
    {
        private readonly AppDBContext _dbContext;

        public OtcRepository(AppDBContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task CreateAsync(OneTimeCode oneTimeCode)
        {
            _dbContext.OneTimeCodes.Add(oneTimeCode);
            await _dbContext.SaveChangesAsync();
        }

        public async Task<OneTimeCode?> GetByCodeAsync(string code, OneTimeCodeType type)
        {
            return await _dbContext.OneTimeCodes.FirstOrDefaultAsync(otc => otc.Code == code && otc.Type == type);
        }

        public async Task MarkUsedAsync(OneTimeCode oneTimeCode)
        {
            oneTimeCode.IsUsed = true;
            _dbContext.Update(oneTimeCode);
            await _dbContext.SaveChangesAsync();
        }

        public async Task DeleteExpiredAsync()
        {
            var expiredOtcs = await _dbContext.OneTimeCodes.Where(otc => otc.ExpiresAt < DateTime.UtcNow).ToListAsync();
            _dbContext.OneTimeCodes.RemoveRange(expiredOtcs);
            await _dbContext.SaveChangesAsync();
        }

        public async Task DeleteAsync(Guid id)
        {
            var otc = await _dbContext.OneTimeCodes.FindAsync(id);
            if (otc != null)
            {
                _dbContext.OneTimeCodes.Remove(otc);
                await _dbContext.SaveChangesAsync();
            }
        }

        public async Task InvalidateAllAsync(Guid userId, OneTimeCodeType type)
        {
            var codes = await _dbContext.OneTimeCodes
                .Where(c => c.UserId == userId && c.Type == type && !c.IsUsed)
                .ToListAsync();
            codes.ForEach(c => c.IsUsed = true);
            await _dbContext.SaveChangesAsync();
        }
    }
}
