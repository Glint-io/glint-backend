namespace glint_backend.DTOs.Responses;

// Response when a resume is uploaded, containing the resume ID, file name, file size, and upload timestamp.
public class UploadResumeResponse
{
    public Guid ResumeId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
    public DateTime UploadedAt { get; set; }
}