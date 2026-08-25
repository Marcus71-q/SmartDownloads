# Smart Downloads Classification Server

Server-side Gemini classification API for Smart Downloads. The provider key never reaches desktop clients.

## Local development

1. Copy `.env.example` to `.env.local`.
2. Set `GEMINI_API_KEY` in `.env.local`.
3. Run `dotnet run --project SmartDownloads.Server`.

Endpoints:

- `GET /health`
- `POST /v1/devices/register`
- `POST /v1/classify` with a device bearer token

## Production

Build the root-level Dockerfile and inject `GEMINI_API_KEY` through a secret manager. Use HTTPS, a trusted reverse proxy, provider billing alerts, centralized redacted logs, and persistent shared rate-limit/cache storage when running multiple instances.

`.env.local` is ignored by Git and Docker. It must never be copied into a deployment image.
