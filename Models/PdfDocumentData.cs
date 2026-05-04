namespace glint_backend.Models;

public record PdfDocumentData
{
    public string FileName { get; init; } = string.Empty;
    public long FileSize { get; init; }
    public string Text { get; init; } = string.Empty;
    public int PageCount { get; init; }
}
