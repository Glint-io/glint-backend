using System.Threading.Tasks;

namespace glint_backend.Interfaces
{
    public interface IKeywordAnalysisService
    {
        Task<(decimal Score, string Feedback)> AnalyzeAsync(string resumeText, string jobText);
    }
}
