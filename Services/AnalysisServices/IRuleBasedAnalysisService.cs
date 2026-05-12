using glint_backend.Models;
using System.Threading.Tasks;

namespace glint_backend.Interfaces
{
    public interface IRuleBasedAnalysisService
    {
        Task<(decimal Score, string Feedback)> AnalyzeAsync(PdfDocumentData pdfData, JobAdvertisement jobAdvertisement);
    }
}
