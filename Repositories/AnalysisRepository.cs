using glint_backend.Data;
using glint_backend.Interfaces;
using glint_backend.Models;
using Microsoft.EntityFrameworkCore;

namespace glint_backend.Repositories;

public class AnalysisRepository(AppDBContext db) : IAnalysisRepository
{
    public async Task<(IEnumerable<Analysis> Items, int TotalCount)> GetPagedByUserIdAsync(
        Guid userId, int page, int pageSize)
    {
        var query = db.Analyses
            .Where(a => a.UserId == userId)
            .Include(a => a.Results)
            .Include(a => a.Resume)
            .Include(a => a.JobAdvertisement)
            .OrderByDescending(a => a.CreatedAt);

        var total = await query.CountAsync();

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, total);
    }

    public async Task<IEnumerable<AnalysisResult>> GetResultsByUserIdAsync(Guid userId) =>
        await db.AnalysisResults
            .Where(r => r.Analysis.UserId == userId && r.Score != null)
            .Include(r => r.Analysis)
            .OrderBy(r => r.Analysis.CreatedAt)
            .ToListAsync();

    public async Task<Analysis> AddAnalysisAsync(Analysis analysis)
    {
        db.Analyses.Add(analysis);
        await db.SaveChangesAsync();
        return analysis;
    }

    public async Task<AnalysisResult> AddResultAsync(AnalysisResult result)
    {
        db.AnalysisResults.Add(result);
        await db.SaveChangesAsync();
        return result;
    }

    public async Task UpdateAnalysisAsync(Analysis analysis)
    {
        db.Analyses.Update(analysis);
        await db.SaveChangesAsync();
    }
}