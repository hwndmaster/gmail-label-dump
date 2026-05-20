using Google.Apis.Gmail.v1;
using Google.Apis.Gmail.v1.Data;

namespace Genius.EmailLabelDump.Services;

public static class GmailMessageService
{
    public static IReadOnlyList<string> GetMessageIdsForLabel(GmailService gmailService, string labelId)
    {
        ArgumentNullException.ThrowIfNull(gmailService);
        ArgumentException.ThrowIfNullOrWhiteSpace(labelId);

        var messageIds = new List<string>();
        string? pageToken = null;

        do
        {
            var request = gmailService.Users.Messages.List("me");
            request.LabelIds = new[] { labelId };
            request.MaxResults = 500;
            request.IncludeSpamTrash = false;
            request.Fields = "messages(id),nextPageToken,resultSizeEstimate";
            request.PageToken = pageToken;

            ListMessagesResponse response = request.Execute();
            if (response.Messages is not null)
            {
                foreach (var message in response.Messages)
                {
                    if (!string.IsNullOrWhiteSpace(message.Id))
                    {
                        messageIds.Add(message.Id);
                    }
                }
            }

            pageToken = response.NextPageToken;
        }
        while (!string.IsNullOrWhiteSpace(pageToken));

        return messageIds;
    }

    public static byte[] GetRawMessageBytes(GmailService gmailService, string messageId)
    {
        ArgumentNullException.ThrowIfNull(gmailService);
        ArgumentException.ThrowIfNullOrWhiteSpace(messageId);

        var request = gmailService.Users.Messages.Get("me", messageId);
        request.Format = UsersResource.MessagesResource.GetRequest.FormatEnum.Raw;
        request.Fields = "id,raw";

        Message message = request.Execute();
        if (string.IsNullOrWhiteSpace(message.Raw))
        {
            throw new InvalidOperationException($"Message '{messageId}' did not contain raw payload.");
        }

        return DecodeBase64Url(message.Raw);
    }

    private static byte[] DecodeBase64Url(string base64Url)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(base64Url);

        string normalized = base64Url.Replace('-', '+').Replace('_', '/');
        int requiredPadding = (4 - normalized.Length % 4) % 4;
        if (requiredPadding > 0)
        {
            normalized = normalized.PadRight(normalized.Length + requiredPadding, '=');
        }

        return Convert.FromBase64String(normalized);
    }
}
