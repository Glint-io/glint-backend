using glint_backend.Data;
using glint_backend.Interfaces;
using glint_backend.Models;
using Microsoft.EntityFrameworkCore;

namespace glint_backend.Repositories;

public class JobAdvertisementRepository(AppDBContext db) : IJobAdvertisementRepository
{
    // Add a new job advertisement to the database and return the saved entity with its generated ID.
    public async Task<JobAdvertisement> AddAsync(JobAdvertisement jobAd)
    {
        db.JobAdvertisements.Add(jobAd);
        await db.SaveChangesAsync();
        return jobAd;
    }

    // Get all job advertisements for a specific user, ordered by most recent first
    public async Task<IEnumerable<JobAdvertisement>> GetByUserIdAsync(Guid userId)
    {
        return await db.JobAdvertisements
            .Where(ja => ja.UserId == userId && !ja.IsArchived)
            .OrderByDescending(ja => ja.CreatedAt)
            .ToListAsync();
    }

    // Find a job advertisement by raw text content for a specific user (used for deduplication)
    public async Task<JobAdvertisement?> FindByRawTextAsync(Guid userId, string rawText)
    {
        return await db.JobAdvertisements
            .FirstOrDefaultAsync(ja => ja.UserId == userId && !ja.IsArchived && ja.RawText == rawText);
    }

    public async Task<int> CountActiveByUserIdAsync(Guid userId)
    {
        return await db.JobAdvertisements.CountAsync(ja => ja.UserId == userId && !ja.IsArchived);
    }

    public async Task<JobAdvertisement?> GetOldestActiveByUserIdAsync(Guid userId)
    {
        return await db.JobAdvertisements
            .Where(ja => ja.UserId == userId && !ja.IsArchived)
            .OrderBy(ja => ja.CreatedAt)
            .FirstOrDefaultAsync();
    }

    // Get a job advertisement by ID for a specific user (ensures ownership)
    public async Task<JobAdvertisement?> GetByIdAsync(Guid userId, Guid id)
    {
        return await db.JobAdvertisements
            .FirstOrDefaultAsync(ja => ja.Id == id && ja.UserId == userId && !ja.IsArchived);
    }

    public async Task ArchiveAsync(JobAdvertisement jobAd)
    {
        jobAd.IsArchived = true;
        await db.SaveChangesAsync();
    }
}