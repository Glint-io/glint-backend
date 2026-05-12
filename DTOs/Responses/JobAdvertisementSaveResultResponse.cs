namespace glint_backend.DTOs.Responses;

public class JobAdvertisementSaveResultResponse
{
    public JobAdvertisementListItemResponse JobAdvertisement { get; set; } = null!;
    public string? Notice { get; set; }
    public bool OverwroteOldest { get; set; }
    public Guid? ReplacedJobAdvertisementId { get; set; }
}