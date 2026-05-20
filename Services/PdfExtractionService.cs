using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;
using glint_backend.Interfaces;
using glint_backend.Models;
using Microsoft.AspNetCore.Http;
using System.IO.Compression;
using System.Text;
using System.Xml.Linq;

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
            var extension = Path.GetExtension(fileName ?? string.Empty).ToLowerInvariant();
            string text;
            int pageCount;

            if (extension == ".docx")
            {
                (text, pageCount) = ExtractDocxText(pdfBytes);
            }
            else if (extension == ".txt")
            {
                text = ExtractTextFile(pdfBytes);
                pageCount = 1;
            }
            else
            {
                using var doc = PdfDocument.Open(pdfBytes);

                var sb = new StringBuilder();
                foreach (Page page in doc.GetPages())
                    sb.AppendLine(page.Text);

                text = sb.ToString();
                pageCount = doc.NumberOfPages;
            }

            return Task.FromResult(new PdfDocumentData
            {
                FileName = fileName ?? string.Empty,
                FileSize = pdfBytes.LongLength,
                Text = text,
                PageCount = pageCount
            });
        }
        catch (Exception)
        {
            // Not a valid supported document (e.g. seeder placeholder or corrupt file)
            return Task.FromResult(new PdfDocumentData
            {
                FileName = fileName ?? string.Empty,
                FileSize = pdfBytes.LongLength,
                Text = string.Empty,
                PageCount = 0
            });
        }
    }

    private static (string Text, int PageCount) ExtractDocxText(byte[] bytes)
    {
        using var stream = new MemoryStream(bytes, writable: false);
        using var archive = new ZipArchive(stream, ZipArchiveMode.Read, leaveOpen: false);
        var textParts = new List<string>();

        foreach (var entryName in GetWordXmlEntries(archive))
        {
            var entry = archive.GetEntry(entryName);
            if (entry is null) continue;

            using var xmlStream = entry.Open();
            var fragmentText = ExtractWordXmlText(xmlStream);
            if (!string.IsNullOrWhiteSpace(fragmentText))
                textParts.Add(fragmentText);
        }

        return (string.Join(Environment.NewLine + Environment.NewLine, textParts), 1);
    }

    private static string ExtractTextFile(byte[] bytes)
    {
        if (bytes.Length == 0) return string.Empty;

        var utf8Text = ReadText(bytes, Encoding.UTF8);
        if (!utf8Text.Contains('\uFFFD'))
            return utf8Text;

        return ReadText(bytes, Encoding.Latin1);
    }

    private static string ExtractWordXmlText(Stream xmlStream)
    {
        var document = XDocument.Load(xmlStream);
        XNamespace w = "http://schemas.openxmlformats.org/wordprocessingml/2006/main";

        var paragraphs = document
            .Descendants(w + "p")
            .Select(paragraph => string.Concat(paragraph.Descendants(w + "t").Select(text => text.Value)))
            .Where(text => !string.IsNullOrWhiteSpace(text));

        return string.Join(Environment.NewLine, paragraphs);
    }

    private static IEnumerable<string> GetWordXmlEntries(ZipArchive archive)
    {
        yield return "word/document.xml";

        foreach (var entry in archive.Entries)
        {
            if (!entry.FullName.StartsWith("word/", StringComparison.OrdinalIgnoreCase))
                continue;

            if (!entry.FullName.EndsWith(".xml", StringComparison.OrdinalIgnoreCase))
                continue;

            if (entry.FullName is "word/document.xml" or "word/styles.xml" or "word/settings.xml")
                continue;

            if (entry.FullName.Contains("header", StringComparison.OrdinalIgnoreCase) ||
                entry.FullName.Contains("footer", StringComparison.OrdinalIgnoreCase) ||
                entry.FullName.Contains("footnotes", StringComparison.OrdinalIgnoreCase) ||
                entry.FullName.Contains("endnotes", StringComparison.OrdinalIgnoreCase) ||
                entry.FullName.Contains("comments", StringComparison.OrdinalIgnoreCase))
            {
                yield return entry.FullName;
            }
        }
    }

    private static string ReadText(byte[] bytes, Encoding encoding)
    {
        using var stream = new MemoryStream(bytes, writable: false);
        using var reader = new StreamReader(stream, encoding, detectEncodingFromByteOrderMarks: true, leaveOpen: false);
        return reader.ReadToEnd();
    }
}