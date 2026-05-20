using Genius.Atom.Infrastructure.Io;
using Genius.EmailLabelDump.Models;
using System.Globalization;
using Microsoft.Extensions.Configuration;
using Spectre.Console;
using Google.Apis.Gmail.v1;

namespace Genius.EmailLabelDump.Services;

public sealed class ConsoleAppRunner
{
    private readonly GmailAuthService _authService;
    private readonly EmlExportService _emlExportService;
    private readonly IFileService _fileService;
    private readonly IConfiguration _configuration;

    public ConsoleAppRunner(
        GmailAuthService authService,
        EmlExportService emlExportService,
        IFileService fileService,
        IConfiguration configuration)
    {
        _authService = authService;
        _emlExportService = emlExportService;
        _fileService = fileService;
        _configuration = configuration;
    }

    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        AnsiConsole.MarkupLine("[bold green]Email Label Dump[/]");
        AnsiConsole.MarkupLine("Reads Gmail labels and exports selected label messages as .eml files.");

        using GmailService gmailService = await _authService.CreateServiceAsync(cancellationToken);
        IReadOnlyList<GmailLabelInfo> labels = GmailLabelService.GetLabels(gmailService);

        if (labels.Count == 0)
        {
            AnsiConsole.MarkupLine("[yellow]No labels were found in this Gmail account.[/]");
            return;
        }

        GmailLabelInfo selectedLabel = PromptForLabel(labels);
        string outputRoot = ResolveOutputRoot();
        _fileService.EnsureDirectory(outputRoot);

        string labelOutputDirectory = _emlExportService.PrepareLabelOutputDirectory(outputRoot, selectedLabel.Name);

        IReadOnlyList<string> messageIds = GmailMessageService.GetMessageIdsForLabel(gmailService, selectedLabel.Id);
        if (messageIds.Count == 0)
        {
            AnsiConsole.MarkupLine($"[yellow]No messages found for label:[/] {Markup.Escape(selectedLabel.Name)}");
            AnsiConsole.MarkupLine($"Output folder created at: {Markup.Escape(labelOutputDirectory)}");
            return;
        }

        AnsiConsole.MarkupLine($"Selected label: [aqua]{Markup.Escape(selectedLabel.Name)}[/]");
        AnsiConsole.MarkupLine($"Messages to export: [aqua]{messageIds.Count}[/]");

        ExportResult? result = null;
        AnsiConsole.Progress().Start(context => {
            ProgressTask task = context.AddTask("[green]Exporting emails[/]", maxValue: messageIds.Count);
            result = _emlExportService.ExportMessages(gmailService, labelOutputDirectory, messageIds, () => task.Increment(1));
        });

        if (result is null)
        {
            throw new InvalidOperationException("Export finished unexpectedly without a result.");
        }

        _emlExportService.WriteExportLog(labelOutputDirectory, result);

        AnsiConsole.MarkupLine("[bold green]Export finished.[/]");
        AnsiConsole.MarkupLine($"Exported: [green]{result.ExportedCount}[/]");
        AnsiConsole.MarkupLine($"Failed: [red]{result.FailedCount}[/]");
        AnsiConsole.MarkupLine($"Output folder: {Markup.Escape(labelOutputDirectory)}");

        if (result.FailedCount > 0)
        {
            AnsiConsole.MarkupLine("[yellow]See export-log.txt in the output folder for failed message IDs.[/]");
        }
    }

    private static GmailLabelInfo PromptForLabel(IReadOnlyList<GmailLabelInfo> labels)
    {
        var prompt = new SelectionPrompt<GmailLabelInfo>()
            .Title("Select a Gmail label")
            .PageSize(Math.Min(labels.Count, 20))
            .UseConverter(label => {
                string countText = label.MessagesTotal?.ToString(CultureInfo.InvariantCulture) ?? "?";
                return $"{label.Name} ({countText})";
            });

        prompt.AddChoices(labels);
        return AnsiConsole.Prompt(prompt);
    }

    private string ResolveOutputRoot()
    {
        string configuredRoot = _configuration["Output:RootDirectory"] ?? "output";

        if (Path.IsPathRooted(configuredRoot))
        {
            return configuredRoot;
        }

        return Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), configuredRoot));
    }
}
