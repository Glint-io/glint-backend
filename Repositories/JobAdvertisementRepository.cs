using glint_backend.Data;
using glint_backend.Interfaces;
using glint_backend.Models;

namespace glint_backend.Repositories;

public class JobAdvertisementRepository(AppDBContext db) : IJobAdvertisementRepository
{
    public async Task<JobAdvertisement> AddAsync(JobAdvertisement jobAd)
    {
        db.JobAdvertisements.Add(jobAd);
        await db.SaveChangesAsync();
        return jobAd;
    }
}