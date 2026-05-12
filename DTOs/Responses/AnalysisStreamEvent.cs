namespace glint_backend.DTOs.Responses;

// Emitted over SSE for each method that completes.
// The first event also carries the analysisId and JobTitle so the client
// can group all subsequent results under the same analysis.
public class AnalysisStreamEvent
{
    // Sent on the very first event so the client knows which analysis this belongs to
    public Guid? AnalysisId { get; set; }
    public string? JobTitle { get; set; }
    public string? JobAdvertisementNotice { get; set; }

    // The individual method result
    public AnalysisResultResponse Result { get; set; } = null!;

    // "result" | "done" — client can stop listening on "done"
    public string EventType { get; set; } = "result";
}