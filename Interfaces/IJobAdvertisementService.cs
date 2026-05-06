using glint_backend.DTOs.Requests;
using glint_backend.DTOs.Responses;
using glint_backend.Models;

namespace glint_backend.Interfaces;

public interface IJobAdvertisementService
{
    Task<JobAdvertisementSaveResultResponse> CreateOrGetAsync(Guid userId, CreateJobAdvertisementRequest request);
    Task DeleteAsync(Guid userId, Guid id);
    Task<List<JobAdvertisementListItemResponse>> GetUserJobAdvertisementsAsync(Guid userId);
}
