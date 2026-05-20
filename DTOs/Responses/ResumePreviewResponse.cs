namespace glint_backend.DTOs.Responses;

public record ResumePreviewResponse
{
    public string FileName { get; init; } = string.Empty;
    public string ContentType { get; init; } = "application/octet-stream";
    public string Text { get; init; } = string.Empty;
    public int PageCount { get; init; }
}