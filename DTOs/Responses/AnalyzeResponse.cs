using glint_backend.Models;

namespace glint_backend.DTOs.Responses;

// DTO response format: AnalysisId, Label, Status, CreatedAt, List of AnalysisResultResponse (Method, Score, Feedback, CompletedAt).
public class AnalyzeResponse
{
    public Guid AnalysisId { get; set; }
    public string? Label { get; set; }
    public AnalysisStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<AnalysisResultResponse> Results { get; set; } = [];
}