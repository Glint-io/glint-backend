using glint_backend.DTOs.Requests;
using glint_backend.DTOs.Responses;
using glint_backend.Exceptions;
using glint_backend.Interfaces;
using glint_backend.Models;

namespace glint_backend.Services;

public class JobAdvertisementService(IJobAdvertisementRepository jobAdRepo) : IJobAdvertisementService
{
    private const int MaxSavedJobAdvertisements = 5;

    public async Task<JobAdvertisementSaveResultResponse> CreateOrGetAsync(Guid userId, CreateJobAdvertisementRequest request)
    {
        // Check if user already has uploaded this exact job advertisement (deduplication)
        var existing = await jobAdRepo.FindByRawTextAsync(userId, request.RawText);
        
        if (existing != null)
        {
            // Return existing job advertisement if found
            return new JobAdvertisementSaveResultResponse
            {
                JobAdvertisement = Map(existing)
            };
        }

        var activeCount = await jobAdRepo.CountActiveByUserIdAsync(userId);
        Guid? replacedId = null;
        string? notice = null;

        if (activeCount >= MaxSavedJobAdvertisements)
        {
            var oldest = await jobAdRepo.GetOldestActiveByUserIdAsync(userId)
                ?? throw new InvalidOperationException("Unable to find a job advertisement to replace.");

            replacedId = oldest.Id;
            notice = "Saved job ads are capped at 5. The oldest saved job advertisement was replaced.";
            await jobAdRepo.ArchiveAsync(oldest);
        }

        // Create new job advertisement if not found
        var jobAd = new JobAdvertisement
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Title = request.Title,
            RawText = request.RawText,
            CreatedAt = DateTime.UtcNow
        };

        await jobAdRepo.AddAsync(jobAd);

        return new JobAdvertisementSaveResultResponse
        {
            JobAdvertisement = Map(jobAd),
            Notice = notice,
            OverwroteOldest = replacedId.HasValue,
            ReplacedJobAdvertisementId = replacedId
        };
    }

    public async Task DeleteAsync(Guid userId, Guid id)
    {
        var jobAd = await jobAdRepo.GetByIdAsync(userId, id)
            ?? throw new NotFoundException("Job advertisement not found.");

        await jobAdRepo.ArchiveAsync(jobAd);
    }

    public async Task<List<JobAdvertisementListItemResponse>> GetUserJobAdvertisementsAsync(Guid userId)
    {
        var jobAds = await jobAdRepo.GetByUserIdAsync(userId);
        
        return jobAds
            .Select(Map)
            .ToList();
    }

    private static JobAdvertisementListItemResponse Map(JobAdvertisement jobAd) => new()
    {
        Id = jobAd.Id,
        Title = jobAd.Title,
        RawText = jobAd.RawText,
        CreatedAt = jobAd.CreatedAt
    };
}
