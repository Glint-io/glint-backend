using glint_backend.Models;
using Microsoft.AspNetCore.Http;

namespace glint_backend.Interfaces;

public interface IFileValidationService
{
    Task<FileValidationResult> ValidatePdfAsync(IFormFile file);
}