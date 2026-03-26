using glint_backend.DTOs.Requests;
using glint_backend.DTOs.Responses;

namespace glint_backend.Interfaces;

public interface IAnalysisService
{
    // For guests: must upload a PDF file and provide job text. Returns an AnalysisResponse with status "Processing".
    Task<AnalyzeResponse> AnalyzeGuestAsync(byte[] pdfBytes, string jobText);
    // For authenticated users: can either upload a PDF file or reference a previously uploaded resume by its ID.
    Task<AnalyzeResponse> AnalyzeAuthenticatedAsync(Guid userId, AnalyzeRequest request);
}