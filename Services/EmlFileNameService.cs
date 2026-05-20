using System.Text.RegularExpressions;
using MimeKit;

namespace Genius.EmailLabelDump.Services;

public static partial class EmlFileNameService
{
    public static string SanitizePathSegment(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);

        string candidate = value.Replace('/', '_').Replace('\\', '_');
        return SanitizeFileNameCore(candidate, "Unknown Label");
    }

    public static string BuildFileName(byte[] rawMessageBytes)
    {
        ArgumentNullException.ThrowIfNull(rawMessageBytes);

        var metadata = ExtractMetadata(rawMessageBytes);
        string safeSubject = SanitizeFileNameCore(metadata.Subject, "No Subject");
        return $"{metadata.Date:yyyy.MM.dd} - {safeSubject}.eml";
    }

    public static string BuildCollisionSafePath(string directoryPath, string fileName, Func<string, bool> fileExists)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(directoryPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);
        ArgumentNullException.ThrowIfNull(fileExists);

        string extension = Path.GetExtension(fileName);
        string baseName = Path.GetFileNameWithoutExtension(fileName);

        string candidatePath = Path.Combine(directoryPath, fileName);
        if (!fileExists(candidatePath))
        {
            return candidatePath;
        }

        int index = 2;
        while (true)
        {
            string candidateName = $"{baseName} ({index}){extension}";
            candidatePath = Path.Combine(directoryPath, candidateName);

            if (!fileExists(candidatePath))
            {
                return candidatePath;
            }

            index++;
        }
    }

    private static (DateTime Date, string Subject) ExtractMetadata(byte[] rawMessageBytes)
    {
        ArgumentNullException.ThrowIfNull(rawMessageBytes);

        try
        {
            using var stream = new MemoryStream(rawMessageBytes);
            MimeMessage message = MimeMessage.Load(stream);

            DateTime date = message.Date != DateTimeOffset.MinValue
                ? message.Date.Date
                : DateTime.UtcNow.Date;

            string subject = string.IsNullOrWhiteSpace(message.Subject)
                ? "No Subject"
                : message.Subject;

            return (date, subject);
        }
        catch
        {
            return (DateTime.UtcNow.Date, "No Subject");
        }
    }

    private static string SanitizeFileNameCore(string value, string fallback)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return fallback;
        }

        char[] invalidChars = Path.GetInvalidFileNameChars();
        var buffer = value
            .Select(character => invalidChars.Contains(character) ? '_' : character)
            .ToArray();

        string sanitized = new string(buffer);
        sanitized = MultipleWhitespaceRegex().Replace(sanitized, " ").Trim().TrimEnd('.', ' ');

        if (string.IsNullOrWhiteSpace(sanitized))
        {
            return fallback;
        }

        const int MaxLength = 120;
        if (sanitized.Length > MaxLength)
        {
            sanitized = sanitized[..MaxLength].TrimEnd('.', ' ');
        }

        return string.IsNullOrWhiteSpace(sanitized) ? fallback : sanitized;
    }

    [GeneratedRegex("\\s+")]
    private static partial Regex MultipleWhitespaceRegex();
}
