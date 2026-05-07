using glint_backend.Data;
using glint_backend.Interfaces;
using glint_backend.Models;
using Microsoft.EntityFrameworkCore;

namespace glint_backend.Repositories;

public class AnalysisRepository(AppDBContext db) : IAnalysisRepository
{
    public async Task<(IEnumerable<Analysis> Items, int TotalCount)> GetPagedByUserIdAsync(
        Guid userId, int page, int pageSize, DateTime? createdAtFrom = null)
    {
        // Fetch analyses for the user with pagination, including related entities like Results, Resume, and JobAdvertisement.
        var query = db.Analyses
            .Where(a => a.UserId == userId);

        if (createdAtFrom.HasValue)
            query = query.Where(a => a.CreatedAt >= createdAtFrom.Value);

        query = query
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

    // Fetch all analysis results for a user with some additional filtering
    public async Task<IEnumerable<AnalysisResult>> GetResultsByUserIdAsync(
        Guid userId, DateTime? createdAtFrom = null)
    {
        var query = db.AnalysisResults
            .Where(r => r.Analysis.UserId == userId && r.Score != null);

        if (createdAtFrom.HasValue)
            query = query.Where(r => r.Analysis.CreatedAt >= createdAtFrom.Value);

        query = query
            .Include(r => r.Analysis)
            .OrderBy(r => r.Analysis.CreatedAt);

        return await query.ToListAsync();
    }

    // Methods for adding and updating analyses and results. These are used by the service layer to persist changes to the database.
    public async Task<Analysis> AddAnalysisAsync(Analysis analysis)
    {
        db.Analyses.Add(analysis);
        await db.SaveChangesAsync();
        return analysis;
    }
    // Add a new analysis result to the database and return the saved entity with its generated ID.
    public async Task<AnalysisResult> AddResultAsync(AnalysisResult result)
    {
        db.AnalysisResults.Add(result);
        await db.SaveChangesAsync();
        return result;
    }
    // Update an existing analysis.
    public async Task UpdateAnalysisAsync(Analysis analysis)
    {
        db.Analyses.Update(analysis);
        await db.SaveChangesAsync();
    }

    public async Task DeleteByResumeIdAsync(Guid resumeId)
    {
        var analyses = await db.Analyses
            .Include(a => a.Results)
            .Where(a => a.ResumeId == resumeId)
            .ToListAsync();

        db.AnalysisResults.RemoveRange(analyses.SelectMany(a => a.Results));
        db.Analyses.RemoveRange(analyses);
        await db.SaveChangesAsync();
    }

    public async Task NullifyResumeIdAsync(Guid resumeId)
    {
        await db.Analyses
            .Where(a => a.ResumeId == resumeId)
            .ExecuteUpdateAsync(s => s.SetProperty(a => a.ResumeId, (Guid?)null));
    }

    public async Task<int> DeleteByUserIdAsync(Guid userId, DateTime? createdAtFrom = null)
    {
        var analyses = await db.Analyses
            .Where(a => a.UserId == userId)
            .Where(a => createdAtFrom == null || a.CreatedAt >= createdAtFrom)
            .Include(a => a.Results)
            .ToListAsync();

        if (analyses.Count == 0)
            return 0;

        db.Analyses.RemoveRange(analyses);
        await db.SaveChangesAsync();
        return analyses.Count;
    }
}