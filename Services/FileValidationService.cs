using glint_backend.Interfaces;
using glint_backend.Models;
using Microsoft.AspNetCore.Http;

namespace glint_backend.Services;

public class FileValidationService : IFileValidationService
{
    // ── Configuration ────────────────────────────────────────────────────────
    private const long MaxFileSizeBytes = 5 * 1024 * 1024; // 5 MB
    private const string AllowedMimeType = "application/pdf";

    // PDF magic bytes: %PDF
    private static readonly byte[] PdfMagicBytes = [0x25, 0x50, 0x44, 0x46];

    // Patterns associated with malicious/active PDF content.
    // These flag embedded scripts, launch actions, and known obfuscation tricks.
    private static readonly string[] MaliciousPatterns =
    [
        "/JavaScript",
        "/JS",
        "/AA",
        "/OpenAction",
        "/Launch",
        "/EmbeddedFile",
        "/RichMedia",
        "/XFA",
        "eval(",
        "unescape(",
    ];

    public async Task<FileValidationResult> ValidatePdfAsync(IFormFile file)
    {
        // ── 1. Null / empty guard ─────────────────────────────────────────────
        if (file is null || file.Length == 0)
            return FileValidationResult.Failure("No file was provided.");

        // ── 2. File size ──────────────────────────────────────────────────────
        if (file.Length > MaxFileSizeBytes)
            return FileValidationResult.Failure(
                $"File exceeds the maximum allowed size of {MaxFileSizeBytes / 1024 / 1024} MB.");

        // ── 3. MIME type ──────────────────────────────────────────────────────
        if (!string.Equals(file.ContentType, AllowedMimeType, StringComparison.OrdinalIgnoreCase))
            return FileValidationResult.Failure("Only PDF files are accepted.");

        // ── 4. Read bytes once ────────────────────────────────────────────────
        byte[] fileBytes;
        using (var ms = new MemoryStream())
        {
            await file.CopyToAsync(ms);
            fileBytes = ms.ToArray();
        }

        // ── 5. Magic bytes ────────────────────────────────────────────────────
        if (!HasPdfMagicBytes(fileBytes))
            return FileValidationResult.Failure(
                "File does not appear to be a valid PDF (magic bytes mismatch).");

        // ── 6. Malicious content scan ─────────────────────────────────────────
        var maliciousHit = ScanForMaliciousContent(fileBytes);
        if (maliciousHit is not null)
            return FileValidationResult.Failure(
                $"File was rejected: potentially unsafe content detected ({maliciousHit}).");

        return FileValidationResult.Success(fileBytes);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static bool HasPdfMagicBytes(byte[] bytes)
    {
        if (bytes.Length < PdfMagicBytes.Length) return false;

        for (var i = 0; i < PdfMagicBytes.Length; i++)
            if (bytes[i] != PdfMagicBytes[i]) return false;

        return true;
    }

    private static string? ScanForMaliciousContent(byte[] bytes)
    {
        // Decode as Latin-1 so every byte maps to exactly one char -
        // avoids losing data that UTF-8 would reject.
        var content = System.Text.Encoding.Latin1.GetString(bytes);

        foreach (var pattern in MaliciousPatterns)
        {
            if (content.Contains(pattern, StringComparison.OrdinalIgnoreCase))
                return pattern;
        }

        return null;
    }
}