using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace glint_backend.DTOs.Requests;


// DTO for the User to be able to request an analysis of a resume against a job listing. The user can either upload a new resume file or reference a previously uploaded resume by its ID.
public class AnalyzeRequest : IValidatableObject
{
    public IFormFile? Resume { get; set; }
    public Guid? ResumeId { get; set; }

    // Optional job title to provide additional context for analysis (e.g. "Senior Backend Engineer")
    [MaxLength(200)]
    public string? JobTitle { get; set; }

    // Error messages to make sure the JobText (Content) follow basic validation rules.
    [Required(ErrorMessage = "Job listing text is required.")]
    [MinLength(20, ErrorMessage = "Job text must be at least 20 characters.")]
    [MaxLength(10_000, ErrorMessage = "Job text must not exceed 10 000 characters.")]
    public string JobText { get; set; } = string.Empty;

    // If both Resume and ResumeId are null, return a custom validation error.
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Resume is null && ResumeId is null)
            yield return new ValidationResult(
                "Either a Resume file or a ResumeId must be provided.",
                new[] { nameof(Resume), nameof(ResumeId) });
    }
}