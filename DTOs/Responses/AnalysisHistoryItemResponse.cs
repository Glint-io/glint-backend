namespace glint_backend.DTOs.Responses;

public class AnalysisHistoryItemResponse
{
    public Guid Id { get; set; }
    public string? Label { get; set; }
    public DateTime CreatedAt { get; set; }
    public string Status { get; set; } = string.Empty;
    public string ResumeFileName { get; set; } = string.Empty;

    /// <summary>First 200 characters of the job ad text.</summary>
    public string JobAdSnippet { get; set; } = string.Empty;

    public List<AnalysisResultResponse> Results { get; set; } = [];
}