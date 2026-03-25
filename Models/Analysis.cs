using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace glint_backend.Models;

public enum AnalysisStatus
{
    Pending,
    InProgress,
    Completed
}

public class Analysis
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid UserId { get; set; }

    [Required]
    public Guid ResumeId { get; set; }

    [Required]
    public Guid JobAdvertisementId { get; set; }

    [MaxLength(100)]
    public string? Label { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Required]
    public AnalysisStatus Status { get; set; } = AnalysisStatus.Pending;

    // Navigation
    [ForeignKey(nameof(UserId))]
    public User User { get; set; } = null!;

    [ForeignKey(nameof(ResumeId))]
    public Resume Resume { get; set; } = null!;

    [ForeignKey(nameof(JobAdvertisementId))]
    public JobAdvertisement JobAdvertisement { get; set; } = null!;

    public ICollection<AnalysisResult> Results { get; set; } = [];
}