namespace glint_backend.DTOs.Responses;


// DTO respones format : ID, Label, CreatedAt, Status, ResumeFileName, JobAdSnippet, List of AnalysisResultResponse (Method, Score, Feedback, CompletedAt).
public class AnalysisHistoryItemResponse
{
    public Guid Id { get; set; }
    public string? Label { get; set; }
    public DateTime CreatedAt { get; set; }
    public string Status { get; set; } = string.Empty;
    public string ResumeFileName { get; set; } = string.Empty;

    public string JobAdSnippet { get; set; } = string.Empty;

    public List<AnalysisResultResponse> Results { get; set; } = [];
}