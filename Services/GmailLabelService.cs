using Genius.EmailLabelDump.Models;
using Google.Apis.Gmail.v1;

namespace Genius.EmailLabelDump.Services;

public static class GmailLabelService
{
    private static readonly object CacheLock = new();
    private static IReadOnlyList<GmailLabelInfo>? _cachedLabels;

    public static IReadOnlyList<GmailLabelInfo> GetLabels(GmailService gmailService)
    {
        ArgumentNullException.ThrowIfNull(gmailService);

        lock (CacheLock)
        {
            if (_cachedLabels is not null)
            {
                return _cachedLabels;
            }
        }

        var request = gmailService.Users.Labels.List("me");
        var response = request.Execute();

        IReadOnlyList<GmailLabelInfo> labels = (response.Labels ?? [])
            .Where(label => !string.IsNullOrWhiteSpace(label.Id) && !string.IsNullOrWhiteSpace(label.Name))
            .Select(label => new GmailLabelInfo(
                label.Id!,
                label.Name!,
                label.Type ?? "user",
                GetLabelMessageTotal(gmailService, label.Id!)))
            .OrderBy(label => string.Equals(label.Type, "user", StringComparison.OrdinalIgnoreCase) ? 0 : 1)
            .ThenBy(label => label.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();

        lock (CacheLock)
        {
            _cachedLabels = labels;
        }

        return labels;
    }

    private static long GetLabelMessageTotal(GmailService gmailService, string labelId)
    {
        var request = gmailService.Users.Labels.Get("me", labelId);
        var response = request.Execute();
        return response.MessagesTotal ?? 0;
    }
}
