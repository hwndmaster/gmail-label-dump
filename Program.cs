using Genius.EmailLabelDump.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Spectre.Console;
using System.Text;

ConfigureConsoleForUnicode();

var configuration = new ConfigurationBuilder()
	.SetBasePath(Directory.GetCurrentDirectory())
	.AddJsonFile("appsettings.json", optional: true)
	.Build();

var services = new ServiceCollection();
services.AddSingleton<IConfiguration>(configuration);

Module.Configure(services, configuration);

services.AddSingleton<GmailAuthService>();
services.AddSingleton<EmlExportService>();
services.AddSingleton<ConsoleAppRunner>();

using ServiceProvider serviceProvider = services.BuildServiceProvider();
Module.Initialize(serviceProvider);

try
{
	await serviceProvider.GetRequiredService<ConsoleAppRunner>().RunAsync();
}
catch (Exception ex)
{
	AnsiConsole.MarkupLine($"[red]Unhandled error:[/] {Markup.Escape(ex.Message)}");
	Environment.ExitCode = 1;
}

static void ConfigureConsoleForUnicode()
{
	try
	{
		var utf8 = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
		Console.InputEncoding = utf8;
		Console.OutputEncoding = utf8;
	}
	catch
	{
		// If the host does not allow changing encodings, continue with defaults.
	}
}
