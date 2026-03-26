using glint_backend.Models;
using Microsoft.AspNetCore.Http;

namespace glint_backend.Interfaces;

public interface IFileValidationService
{
    /// <summary>
    /// Validates a PDF upload: size cap, MIME type, magic bytes, and malicious-content scan.
    /// On success the returned result contains the already-read file bytes so the
    /// stream does not need to be consumed a second time.
    /// </summary>
    Task<FileValidationResult> ValidatePdfAsync(IFormFile file);
}