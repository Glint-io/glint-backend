using glint_backend.Models;

namespace glint_backend.Interfaces;

// interface for managing job advertisements
public interface IJobAdvertisementRepository
{
    Task<JobAdvertisement> AddAsync(JobAdvertisement jobAd);
}