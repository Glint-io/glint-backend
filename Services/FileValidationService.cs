using glint_backend.Interfaces;
using glint_backend.Models;
using Microsoft.AspNetCore.Http;
using System.IO.Compression;

namespace glint_backend.Services;

public class FileValidationService : IFileValidationService
{
    // ── Configuration ────────────────────────────────────────────────────────
    private const long MaxFileSizeBytes = 5 * 1024 * 1024; // 5 MB
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".pdf",
        ".docx",
        ".txt"
    };

    // PDF magic bytes: %PDF
    private static readonly byte[] PdfMagicBytes = [0x25, 0x50, 0x44, 0x46];

    // Patterns associated with malicious/active PDF content.
    // These flag embedded scripts, launch actions, and known obfuscation tricks.
    private static readonly string[] MaliciousPatterns =
    [
        "/JavaScript",
        "/JS",
        "/OpenAction",
        "/Launch",
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

        var extension = Path.GetExtension(file.FileName);
        if (!AllowedExtensions.Contains(extension))
            return FileValidationResult.Failure("Supported files are PDF, DOCX, and TXT.");

        // ── 4. Read bytes once ────────────────────────────────────────────────
        byte[] fileBytes;
        using (var ms = new MemoryStream())
        {
            await file.CopyToAsync(ms);
            fileBytes = ms.ToArray();
        }

        var validation = extension.ToLowerInvariant() switch
        {
            ".pdf" => ValidatePdfBytes(fileBytes),
            ".docx" => ValidateDocxBytes(fileBytes),
            ".txt" => ValidateTextBytes(fileBytes),
            _ => FileValidationResult.Failure("Supported files are PDF, DOCX, and TXT.")
        };

        if (!validation.IsValid)
            return validation;

        // ── 6. Malicious content scan ─────────────────────────────────────────
        if (extension.Equals(".pdf", StringComparison.OrdinalIgnoreCase))
        {
            var maliciousHit = ScanForMaliciousContent(fileBytes);
            if (maliciousHit is not null)
                return FileValidationResult.Failure(
                    $"File was rejected: potentially unsafe PDF content detected ({maliciousHit}).");
        }

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

    private static bool HasZipMagicBytes(byte[] bytes)
    {
        return bytes.Length >= 4
            && bytes[0] == 0x50
            && bytes[1] == 0x4B
            && bytes[2] == 0x03
            && bytes[3] == 0x04;
    }

    private static FileValidationResult ValidatePdfBytes(byte[] bytes)
    {
        if (!HasPdfMagicBytes(bytes))
            return FileValidationResult.Failure("File does not appear to be a valid PDF.");

        return FileValidationResult.Success(bytes);
    }

    private static FileValidationResult ValidateDocxBytes(byte[] bytes)
    {
        if (!HasZipMagicBytes(bytes))
            return FileValidationResult.Failure("File does not appear to be a valid DOCX document.");

        try
        {
            using var stream = new MemoryStream(bytes, writable: false);
            using var archive = new ZipArchive(stream, ZipArchiveMode.Read, leaveOpen: false);

            if (archive.GetEntry("[Content_Types].xml") is null || archive.GetEntry("word/document.xml") is null)
                return FileValidationResult.Failure("File does not appear to be a valid DOCX document.");
        }
        catch
        {
            return FileValidationResult.Failure("File does not appear to be a valid DOCX document.");
        }

        return FileValidationResult.Success(bytes);
    }

    private static FileValidationResult ValidateTextBytes(byte[] bytes)
    {
        if (bytes.Length == 0)
            return FileValidationResult.Failure("No file was provided.");

        return FileValidationResult.Success(bytes);
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