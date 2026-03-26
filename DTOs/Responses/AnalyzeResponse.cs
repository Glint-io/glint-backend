using glint_backend.Models;

namespace glint_backend.DTOs.Responses;

public class AnalyzeResponse
{
    public Guid AnalysisId { get; set; }
    public string? Label { get; set; }
    public AnalysisStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<AnalysisResultResponse> Results { get; set; } = [];
}