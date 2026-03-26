using glint_backend.DTOs.Responses;
using glint_backend.Exceptions;
using glint_backend.Interfaces;
using glint_backend.Models;
using Microsoft.AspNetCore.Http;

namespace glint_backend.Services;

public class ResumeService(
    IResumeRepository resumeRepo,
    IFileValidationService fileValidator) : IResumeService
{
    private const int MaxResumesPerUser = 3;

    public async Task<UploadResumeResponse> UploadAsync(Guid userId, IFormFile file)
    {
        // Guard: Validate the uploaded file is a PDF and meets size requirements. If invalid, throw an exception with details.
        var validation = await fileValidator.ValidatePdfAsync(file);
        if (!validation.IsValid)
            throw new InvalidOperationException(validation.ErrorMessage);

        // Guard: Enforce a maximum number of saved resumes per user. If the user has reached the limit, throw an exception with instructions.
        var count = await resumeRepo.CountByUserIdAsync(userId);
        if (count >= MaxResumesPerUser)
            throw new InvalidOperationException(
                $"You may only have {MaxResumesPerUser} saved resumes. Please delete one before uploading a new one.");

        // If validation passes, save the resume to the database and return a response with the new resume's details.
        var resume = new Resume
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            FileName = file.FileName,
            FileData = validation.FileBytes!,
            UploadedAt = DateTime.UtcNow
        };
        
        // awaits the method
        await resumeRepo.AddAsync(resume);

        // Return the details of the uploaded resume, including its ID, original file name, size in bytes, and upload timestamp.
        return new UploadResumeResponse
        {
            ResumeId = resume.Id,
            FileName = resume.FileName,
            FileSizeBytes = resume.FileData.Length,
            UploadedAt = resume.UploadedAt
        };
    }

    public async Task DeleteAsync(Guid userId, Guid resumeId)
    {
        var resume = await resumeRepo.GetByIdAsync(resumeId)
            ?? throw new NotFoundException("Resume not found.");

        // Ownership check
        if (resume.UserId != userId)
            throw new NotFoundException("Resume not found.");

        // Guard: block deletion if the resume is linked to any analyses
        if (resume.Analyses.Count > 0)
            throw new ConflictException(
                "This resume is linked to one or more analyses and cannot be deleted.");

        await resumeRepo.DeleteAsync(resume);
    }
}