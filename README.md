# Smart Downloads

Smart Downloads is a privacy-conscious Windows organizer that classifies completed downloads with a server-side Gemini model and safely falls back to local rules when the service is unavailable.

## What users get

- One Windows application; users never provide or receive an API key.
- First-run language selection: Turkish, English, Russian, or Hindi.
- Localized category folder names.
- Automatic startup and background tray operation.
- Protection for incomplete downloads and duplicate file names.
- No file execution, deletion, or overwrite.
- AI fallback: network or server failure never prevents local organization.

## Privacy model

Files are never uploaded. The client sends only bounded metadata: file name, extension, size, optional Windows publisher/product fields, and up to 100 names from a ZIP archive. The server rejects oversized requests and does not store Gemini responses.

## Architecture

```text
Windows client -> HTTPS classification server -> Gemini API
       |                    |
 local fallback       server-only secret
```

The Gemini key exists only in the deployed server's secret manager. It is excluded from Git, Docker images, desktop binaries, logs, and device responses.

## Build

Requirements: Windows and .NET 8 SDK.

```powershell
dotnet build SmartDownloads.slnx -c Release
dotnet run --project SmartDownloads.SmokeTests -c Release
```

Create a portable Windows release:

```powershell
.\scripts\build-release.ps1 -ServerUrl "https://your-api-domain.example"
```

## Server

The ASP.NET Core server is in `SmartDownloads.Server`. Copy `.env.example` to `.env.local` for local development and set `GEMINI_API_KEY`. Never commit `.env.local`.

For production, deploy the Docker image behind HTTPS and inject `GEMINI_API_KEY` through the hosting provider's secret manager. Set the final HTTPS URL when building the Windows release.

## Important limitation

No public API can be literally abuse-proof. The server uses device tokens, IP rate limiting, request validation, bounded concurrency, caching, and high abuse thresholds while keeping normal use effectively unrestricted. Operators should also configure billing alerts and provider-level spending controls.

See [SECURITY.md](SECURITY.md) and [PRIVACY.md](PRIVACY.md).

## License

MIT
