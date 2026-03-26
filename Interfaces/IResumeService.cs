using glint_backend.DTOs.Responses;
using Microsoft.AspNetCore.Http;

namespace glint_backend.Interfaces;

public interface IResumeService
{
    Task<UploadResumeResponse> UploadAsync(Guid userId, IFormFile file);
}