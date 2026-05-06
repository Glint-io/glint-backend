using glint_backend.Models;

namespace glint_backend.Interfaces;

// interface for managing job advertisements
public interface IJobAdvertisementRepository
{
    Task<JobAdvertisement> AddAsync(JobAdvertisement jobAd);
    Task<IEnumerable<JobAdvertisement>> GetByUserIdAsync(Guid userId);
    Task<JobAdvertisement?> FindByRawTextAsync(Guid userId, string rawText);
    Task<int> CountActiveByUserIdAsync(Guid userId);
    Task<JobAdvertisement?> GetOldestActiveByUserIdAsync(Guid userId);
    Task<JobAdvertisement?> GetByIdAsync(Guid userId, Guid id);
    Task ArchiveAsync(JobAdvertisement jobAd);
}