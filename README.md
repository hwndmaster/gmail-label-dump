# Email Label Dump

Console app for exporting Gmail messages by label into `.eml` files.

## Prerequisites

- .NET SDK 10
- Local Atom source checkout at `C:\Dev\Src\atom\` (preferred) or access to `Genius.Atom.*` packages
- Google Cloud OAuth desktop credentials JSON saved as `credentials.json` in this folder

## Setup

1. Restore local tools:

```bash
dotnet tool restore
```

1. Restore packages:

```bash
dotnet restore
```

1. Build:

```bash
dotnet build
```

## Run

```bash
dotnet run
```

On first run, the app opens your browser for Google OAuth consent with `gmail.readonly` scope.

Exports are written to `./output/{label_name}/` as `{DATE} - {SUBJECT}.eml`.

## Troubleshooting

### Cyrillic or emoji appear as question marks in the console

- This app sets UTF-8 console encoding at startup.
- Use a terminal that supports Unicode well (Windows Terminal is recommended).
- Set terminal font to a Unicode-capable font such as `Cascadia Mono`, `Cascadia Code`, or `Segoe UI Emoji` fallback.
- Restart the terminal session and run the app again.
