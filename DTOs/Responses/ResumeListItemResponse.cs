namespace glint_backend.DTOs.Responses
{
    public class ResumeListItemResponse
    {
        public Guid ResumeId { get; set; }
        public string FileName { get; set; } = string.Empty;
        public DateTime UploadedAt { get; set; }
    }
}
