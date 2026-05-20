namespace Genius.EmailLabelDump.Models;

public sealed class ExportResult
{
    public int ExportedCount { get; set; }

    public int FailedCount { get; set; }

    public List<string> Errors { get; } = [];
}
