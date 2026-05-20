namespace Genius.EmailLabelDump.Models;

public sealed record GmailLabelInfo(
    string Id,
    string Name,
    string Type,
    long? MessagesTotal);
