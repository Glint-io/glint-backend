namespace glint_backend.DTOs.Responses;

public class AnalysisResultResponse
{
    public Guid Id { get; set; }
    public string Method { get; set; } = string.Empty;
    public decimal? Score { get; set; }
    public string? Feedback { get; set; }
    public DateTime? CompletedAt { get; set; }
}