namespace glint_backend.DTOs.Responses;

public class UploadResumeResponse
{
    public Guid ResumeId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
    public DateTime UploadedAt { get; set; }
}