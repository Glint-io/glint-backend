using glint_backend.DTOs.Responses;
using Microsoft.AspNetCore.Http;

namespace glint_backend.Interfaces;


// interface for managing user resumes
public interface IResumeService
{
    // Methods for CRUD operations
    Task<UploadResumeResponse> UploadAsync(Guid userId, IFormFile file);
    Task DeleteAsync(Guid userId, Guid resumeId);
}