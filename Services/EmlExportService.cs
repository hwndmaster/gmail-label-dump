using System.Net;
using Genius.Atom.Infrastructure.Io;
using Genius.EmailLabelDump.Models;
using Google;
using Google.Apis.Gmail.v1;

namespace Genius.EmailLabelDump.Services;

public sealed class EmlExportService
{
    private readonly IFileService _fileService;

    public EmlExportService(IFileService fileService)
    {
        _fileService = fileService ?? throw new ArgumentNullException(nameof(fileService));
    }

    public string PrepareLabelOutputDirectory(string outputRoot, string labelName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(outputRoot);
        ArgumentException.ThrowIfNullOrWhiteSpace(labelName);

        string sanitizedLabelName = EmlFileNameService.SanitizePathSegment(labelName);
        string labelOutputDirectory = Path.Combine(outputRoot, sanitizedLabelName);
        _fileService.EnsureDirectory(labelOutputDirectory);

        return labelOutputDirectory;
    }

    public ExportResult ExportMessages(
        GmailService gmailService,
        string outputDirectory,
        IReadOnlyList<string> messageIds,
        Action? onMessageProcessed = null)
    {
        ArgumentNullException.ThrowIfNull(gmailService);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputDirectory);
        ArgumentNullException.ThrowIfNull(messageIds);

        var result = new ExportResult();

        foreach (string messageId in messageIds)
        {
            try
            {
                byte[] rawBytes = ExecuteWithRetry(() => GmailMessageService.GetRawMessageBytes(gmailService, messageId));
                string fileName = EmlFileNameService.BuildFileName(rawBytes);
                string outputPath = EmlFileNameService.BuildCollisionSafePath(outputDirectory, fileName, _fileService.FileExists);

                using Stream stream = _fileService.CreateFile(outputPath);
                stream.Write(rawBytes, 0, rawBytes.Length);

                result.ExportedCount++;
            }
            catch (Exception ex)
            {
                result.FailedCount++;
                result.Errors.Add($"{messageId}: {ex.Message}");
            }
            finally
            {
                onMessageProcessed?.Invoke();
            }
        }

        return result;
    }

    public void WriteExportLog(string outputDirectory, ExportResult result)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(outputDirectory);
        ArgumentNullException.ThrowIfNull(result);

        if (result.Errors.Count == 0)
        {
            return;
        }

        string logPath = Path.Combine(outputDirectory, "export-log.txt");
        string logBody = string.Join(Environment.NewLine, result.Errors);
        _fileService.WriteTextToFile(logPath, logBody);
    }

    private static byte[] ExecuteWithRetry(Func<byte[]> callback)
    {
        ArgumentNullException.ThrowIfNull(callback);

        const int MaxAttempts = 3;

        for (int attempt = 1; attempt <= MaxAttempts; attempt++)
        {
            try
            {
                return callback();
            }
            catch (Exception ex) when (attempt < MaxAttempts && IsTransient(ex))
            {
                int delaySeconds = 1 << (attempt - 1);
                Thread.Sleep(TimeSpan.FromSeconds(delaySeconds));
            }
        }

        throw new InvalidOperationException("Retry loop ended unexpectedly.");
    }

    private static bool IsTransient(Exception ex)
    {
        if (ex is GoogleApiException googleApiException)
        {
            HttpStatusCode code = googleApiException.HttpStatusCode;
            return code == HttpStatusCode.TooManyRequests
                || code == HttpStatusCode.RequestTimeout
                || code == HttpStatusCode.InternalServerError
                || code == HttpStatusCode.BadGateway
                || code == HttpStatusCode.ServiceUnavailable
                || code == HttpStatusCode.GatewayTimeout;
        }

        return ex is IOException or TimeoutException;
    }
}
