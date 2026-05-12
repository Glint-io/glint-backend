using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace glint_backend.Models;

public enum AnalysisMethod
{
    AI = 0,
    RuleBased = 1,
    Keyword = 2
}

public class AnalysisResult
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid AnalysisId { get; set; }

    [Required]
    public AnalysisMethod Method { get; set; }

    [Column(TypeName = "decimal(5,2)")]
    public decimal? Score { get; set; }

    public string? Feedback { get; set; }

    public DateTime? CompletedAt { get; set; }
    public DateTime? CreatedAt { get; set; } = DateTime.UtcNow;

    public Guid? JobAdvertisementId { get; set; }

    // Navigation
    [ForeignKey(nameof(AnalysisId))]
    public Analysis Analysis { get; set; } = null!;

    [ForeignKey(nameof(JobAdvertisementId))]
    public JobAdvertisement? JobAdvertisement { get; set; }
}