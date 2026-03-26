using glint_backend.Models;

namespace glint_backend.Interfaces;

public interface IJobAdvertisementRepository
{
    Task<JobAdvertisement> AddAsync(JobAdvertisement jobAd);
}