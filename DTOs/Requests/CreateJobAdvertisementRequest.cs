namespace glint_backend.DTOs.Requests;

public class CreateJobAdvertisementRequest
{
    public required string RawText { get; set; }
    public string? Title { get; set; }
}
