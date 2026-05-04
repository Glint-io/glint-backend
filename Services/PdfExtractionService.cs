using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;
using glint_backend.Interfaces;
using glint_backend.Models;
using Microsoft.AspNetCore.Http;
using System.Text;

namespace glint_backend.Services;

public class PdfExtractionService : IPdfExtractionService
{
    public async Task<PdfDocumentData> ExtractAsync(IFormFile file)
    {
        if (file is null) throw new ArgumentNullException(nameof(file));

        await using var ms = new MemoryStream();
        await file.CopyToAsync(ms);
        return await ExtractAsync(ms.ToArray(), file.FileName);
    }

    public Task<PdfDocumentData> ExtractAsync(byte[] pdfBytes, string? fileName = null)
    {
        if (pdfBytes is null || pdfBytes.Length == 0)
            return Task.FromResult(new PdfDocumentData
            {
                FileName = fileName ?? string.Empty,
                FileSize = 0,
                Text = string.Empty,
                PageCount = 0
            });

        try
        {
            using var doc = PdfDocument.Open(pdfBytes);

            var sb = new StringBuilder();
            foreach (Page page in doc.GetPages())
                sb.AppendLine(page.Text);

            return Task.FromResult(new PdfDocumentData
            {
                FileName = fileName ?? string.Empty,
                FileSize = pdfBytes.LongLength,
                Text = sb.ToString(),
                PageCount = doc.NumberOfPages
            });
        }
        catch (Exception)
        {
            // Not a valid PDF (e.g. seeder placeholder or corrupt file)
            return Task.FromResult(new PdfDocumentData
            {
                FileName = fileName ?? string.Empty,
                FileSize = pdfBytes.LongLength,
                Text = string.Empty,
                PageCount = 0
            });
        }
    }
}