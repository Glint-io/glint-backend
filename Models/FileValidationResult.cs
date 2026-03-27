namespace glint_backend.Models;

public class FileValidationResult
{
    public bool IsValid { get; private set; }
    public string? ErrorMessage { get; private set; }

    public byte[]? FileBytes { get; private set; }

    public static FileValidationResult Success(byte[] bytes) =>
        new() { IsValid = true, FileBytes = bytes };

    public static FileValidationResult Failure(string error) =>
        new() { IsValid = false, ErrorMessage = error };
}