# Smart Downloads

Smart Downloads is a privacy-conscious Windows organizer that classifies newly completed downloads with a server-side Gemini model and safely falls back to local extension rules.

[Download the latest Windows release](https://github.com/Marcus71-q/SmartDownloads/releases/latest/download/SmartDownloads-win-x64.zip)

## Safety defaults

- Only newly downloaded files are processed by default.
- Existing files require an explicit opt-in.
- Directories are never moved automatically.
- Files are never executed, deleted, overwritten, extracted, or uploaded.
- ZIP contents and executable metadata are not opened or inspected.
- Duplicate names receive a numbered suffix.
- The tray menu can undo the most recent move.
- Network failure falls back to local rules.

## First run

1. Download and extract `SmartDownloads-win-x64.zip`.
2. Run `SmartDownloads.exe` on 64-bit Windows 10 or Windows 11.
3. Choose Turkish, English, Russian, or Hindi.
4. Review and accept the metadata notice.
5. Leave **organize existing files** disabled unless you explicitly want a one-time cleanup.

The app uses the real Windows Known Downloads folder, including redirected locations. It can start with Windows and continue in the notification area. Disable startup from the tray menu before removing the extracted folder.

> The current community build is not code-signed. Windows SmartScreen might show an unknown-publisher warning. Verify the SHA-256 file published with each release.

## Metadata and architecture

The client sends only the bounded file name, extension, byte size, a directory flag, and a pseudonymous installation identifier. File bytes and document contents never leave the PC.

```text
Windows client -> HTTPS classification server -> Gemini API
       |                    |
 local fallback       server-only secret
```

The Gemini key exists only in the hosting provider's secret manager. It is excluded from Git, Docker images, desktop binaries, logs, and API responses. See [PRIVACY.md](PRIVACY.md) and [SECURITY.md](SECURITY.md).

## Build and test

Requirements: Windows and the .NET 8 SDK.

```powershell
dotnet build SmartDownloads.slnx -c Release
dotnet run --project SmartDownloads.SmokeTests -c Release --no-build
dotnet list SmartDownloads.slnx package --vulnerable --include-transitive
```

Create a portable Windows package:

```powershell
.\scripts\build-release.ps1 -ServerUrl "https://smartdownloads-api.onrender.com"
```

Tag pushes trigger a GitHub Actions build that creates the ZIP and a SHA-256 checksum. Public production releases should additionally be Authenticode-signed with a protected code-signing certificate.

## Server operations

The ASP.NET Core server is in `SmartDownloads.Server`. Production requires HTTPS, `GEMINI_API_KEY`, a 32+ character `DEVICE_TOKEN_SECRET`, and `REDIS_URL`. Render Blueprint provisions a private Key Value service for shared quotas.

No public API is literally abuse-proof. Keep Gemini provider quotas and billing alerts enabled. The default server limits are 500 AI calls per device and 1,000 total AI calls per UTC day.

## License

MIT
