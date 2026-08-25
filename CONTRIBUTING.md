# Contributing

Open an issue before a large change. Never include real API keys, personal file names, local paths, logs, or downloaded files.

Before submitting a pull request, run:

```powershell
dotnet build SmartDownloads.slnx -c Release
dotnet run --project SmartDownloads.SmokeTests -c Release --no-build
dotnet list SmartDownloads.slnx package --vulnerable --include-transitive
```

Pull requests must preserve these boundaries: no downloaded file execution, no content upload, no overwrite, no deletion, and no movement of existing files without explicit consent.
