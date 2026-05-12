using Microsoft.AspNetCore.Http;

namespace glint_backend.Interfaces;

public interface IPdfExtractionService
{
    // Extracts text and metadata from an IFormFile PDF
    Task<Models.PdfDocumentData> ExtractAsync(IFormFile file);

    // Extracts text and metadata from raw PDF bytes. Optionally provide a file name.
    Task<Models.PdfDocumentData> ExtractAsync(byte[] pdfBytes, string? fileName = null);
}
