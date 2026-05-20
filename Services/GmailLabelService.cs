using Genius.EmailLabelDump.Models;
using Google.Apis.Gmail.v1;

namespace Genius.EmailLabelDump.Services;

public static class GmailLabelService
{
    public static IReadOnlyList<GmailLabelInfo> GetLabels(GmailService gmailService)
    {
        ArgumentNullException.ThrowIfNull(gmailService);

        var request = gmailService.Users.Labels.List("me");
        var response = request.Execute();

        return (response.Labels ?? [])
            .Where(label => !string.IsNullOrWhiteSpace(label.Id) && !string.IsNullOrWhiteSpace(label.Name))
            .Select(label => new GmailLabelInfo(
                label.Id!,
                label.Name!,
                label.Type ?? "user",
                label.MessagesTotal))
            .OrderBy(label => string.Equals(label.Type, "user", StringComparison.OrdinalIgnoreCase) ? 0 : 1)
            .ThenBy(label => label.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
}
