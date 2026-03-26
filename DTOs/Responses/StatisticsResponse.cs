namespace glint_backend.DTOs.Responses;

// a DTO for statistics, based on the analysis history, containing total number of analyses, average score by method, and score trends over time.

public class StatisticsResponse
{
    public int TotalAnalyses { get; set; }
    public List<MethodStatistic> ByMethod { get; set; } = [];
    public List<ScoreDataPoint> ScoreOverTime { get; set; } = [];
}

public class MethodStatistic
{
    public string Method { get; set; } = string.Empty;
    public decimal? AverageScore { get; set; }
    public int Count { get; set; }
}

public class ScoreDataPoint
{
    public DateTime Date { get; set; }
    public decimal Score { get; set; }
    public string Method { get; set; } = string.Empty;
}