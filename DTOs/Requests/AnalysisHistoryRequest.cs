namespace glint_backend.DTOs.Requests;

public enum AnalysisHistoryRange
{
    All,
    Today,
    Last7Days,
    Last30Days,
    Last365Days
}

public class AnalysisHistoryRequest : PaginationRequest
{
    public AnalysisHistoryRange Range { get; set; } = AnalysisHistoryRange.All;
}