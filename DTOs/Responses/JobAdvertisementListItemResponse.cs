namespace glint_backend.DTOs.Responses;

public class JobAdvertisementListItemResponse
{
    public Guid Id { get; set; }
    public string? Title { get; set; }
    public string RawText { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
}
