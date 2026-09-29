namespace StudentJobHub.Api.Services;

/// <summary>
/// Validation rules for uploaded CV/resume files (Feature #20).
/// The client-supplied content type is never trusted: the extension must be
/// on the allow-list AND the file's leading bytes must match that format.
/// </summary>
public static class ResumeFileRules
{
    public const long MaxSizeBytes = 5 * 1024 * 1024;

    public const int MaxFileNameLength = 200;

    private static readonly Dictionary<string, string> ContentTypesByExtension =
        new(StringComparer.OrdinalIgnoreCase)
        {
            [".pdf"] = "application/pdf",
            [".doc"] = "application/msword",
            [".docx"] = "application/vnd.openxmlformats-officedocument.wordprocessingml.document"
        };

    private static readonly Dictionary<string, byte[]> SignaturesByExtension =
        new(StringComparer.OrdinalIgnoreCase)
        {
            [".pdf"] = new byte[] { 0x25, 0x50, 0x44, 0x46 },                                   // %PDF
            [".docx"] = new byte[] { 0x50, 0x4B, 0x03, 0x04 },                                  // PK.. (zip)
            [".doc"] = new byte[] { 0xD0, 0xCF, 0x11, 0xE0, 0xA1, 0xB1, 0x1A, 0xE1 }            // OLE2
        };

    public const string AllowedTypesMessage = "Only PDF, DOC, and DOCX files are allowed.";

    public static string? GetContentType(string fileName)
    {
        var extension = Path.GetExtension(fileName);

        return ContentTypesByExtension.TryGetValue(extension, out var contentType)
            ? contentType
            : null;
    }

    /// <summary>
    /// Returns a safe display name (no path segments, bounded length).
    /// </summary>
    public static string SanitizeFileName(string fileName)
    {
        var name = Path.GetFileName(fileName ?? string.Empty).Trim();

        if (name.Length > MaxFileNameLength)
        {
            var extension = Path.GetExtension(name);
            var stem = Path.GetFileNameWithoutExtension(name);
            var allowedStem = Math.Max(1, MaxFileNameLength - extension.Length);
            name = stem[..Math.Min(stem.Length, allowedStem)] + extension;
        }

        return name;
    }

    public static async Task<(bool IsValid, string Error)> ValidateAsync(
        IFormFile file)
    {
        if (file.Length > MaxSizeBytes)
        {
            return (false, "Resume file must be 5 MB or smaller.");
        }

        var extension = Path.GetExtension(file.FileName);

        if (!ContentTypesByExtension.ContainsKey(extension))
        {
            return (false, AllowedTypesMessage);
        }

        var signature = SignaturesByExtension[extension];
        var header = new byte[signature.Length];

        await using var stream = file.OpenReadStream();

        var read = 0;
        while (read < header.Length)
        {
            var n = await stream.ReadAsync(header.AsMemory(read));
            if (n == 0)
            {
                break;
            }
            read += n;
        }

        if (read < header.Length || !header.AsSpan().SequenceEqual(signature))
        {
            return (false, "The uploaded file does not look like a valid " +
                           extension.TrimStart('.').ToUpperInvariant() + " document.");
        }

        return (true, string.Empty);
    }
}
