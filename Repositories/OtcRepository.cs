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
    }
}
