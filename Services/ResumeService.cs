using glint_backend.DTOs.Responses;
using glint_backend.Interfaces;
using glint_backend.Models;
using Microsoft.AspNetCore.Http;

namespace glint_backend.Services;

public class ResumeService(
    IResumeRepository resumeRepo,
    IFileValidationService fileValidator) : IResumeService
{
    public async Task<UploadResumeResponse> UploadAsync(Guid userId, IFormFile file)
    {
        var validation = await fileValidator.ValidatePdfAsync(file);

        if (!validation.IsValid)
            throw new InvalidOperationException(validation.ErrorMessage);

        var resume = new Resume
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            FileName = file.FileName,
            FileData = validation.FileBytes!,
            UploadedAt = DateTime.UtcNow
        };

        await resumeRepo.AddAsync(resume);

        return new UploadResumeResponse
        {
            ResumeId = resume.Id,
            FileName = resume.FileName,
            FileSizeBytes = resume.FileData.Length,
            UploadedAt = resume.UploadedAt
        };
    }
}