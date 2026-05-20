using Genius.Atom.Infrastructure.Io;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Gmail.v1;
using Google.Apis.Services;
using Google.Apis.Util.Store;

namespace Genius.EmailLabelDump.Services;

public sealed class GmailAuthService
{
    private const string AppName = "email-label-dump";
    private readonly IFileService _fileService;

    public GmailAuthService(IFileService fileService)
    {
        _fileService = fileService ?? throw new ArgumentNullException(nameof(fileService));
    }

    public async Task<GmailService> CreateServiceAsync(CancellationToken cancellationToken = default)
    {
        string credentialsPath = Path.Combine(Directory.GetCurrentDirectory(), "credentials.json");
        if (!_fileService.FileExists(credentialsPath))
        {
            throw new FileNotFoundException(
                "Gmail OAuth credentials file was not found. Place credentials.json in the project root.",
                credentialsPath);
        }

        using Stream stream = _fileService.OpenRead(credentialsPath);
        var clientSecrets = (await GoogleClientSecrets
                .FromStreamAsync(stream, cancellationToken)
                .ConfigureAwait(false))
            .Secrets;
        string tokenDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "email-label-dump",
            "tokens");

        var credential = await GoogleWebAuthorizationBroker
            .AuthorizeAsync(
                clientSecrets,
                [GmailService.Scope.GmailReadonly],
                "user",
                cancellationToken,
                new FileDataStore(tokenDirectory, true))
            .ConfigureAwait(false);

        return new GmailService(new BaseClientService.Initializer {
            HttpClientInitializer = credential,
            ApplicationName = AppName
        });
    }
}
