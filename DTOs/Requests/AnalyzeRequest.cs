using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace glint_backend.DTOs.Requests;

public class AnalyzeRequest
{
    [Required(ErrorMessage = "A resume PDF is required.")]
    public IFormFile Resume { get; set; } = null!;

    [Required(ErrorMessage = "Job listing text is required.")]
    [MinLength(20, ErrorMessage = "Job text must be at least 20 characters.")]
    [MaxLength(10_000, ErrorMessage = "Job text must not exceed 10 000 characters.")]
    public string JobText { get; set; } = string.Empty;

    /// <summary>Optional friendly label for the analysis (e.g. "Spotify – Senior Dev").</summary>
    [MaxLength(100)]
    public string? Label { get; set; }
}