# Security Policy

## Supported version

Security fixes are provided for the latest release.

## Reporting a vulnerability

Do not open a public issue for API-key exposure, authentication bypass, remote code execution, or privacy leaks. Contact the repository owner privately and include reproduction steps without real secrets or personal files.

## Secret handling

- Never commit `.env.local` or a real API key.
- The desktop client must never contain provider credentials.
- Production secrets must be injected by the hosting platform's secret manager.
- Rotate a key immediately if it appears in Git history, logs, screenshots, builds, or issue content.
- Enable Gemini billing alerts and provider-side usage controls.

## Security boundaries

The client does not execute downloads, extract archives, delete files, or overwrite existing paths. ZIP archives are opened read-only only to collect bounded entry names. Device tokens are encrypted with Windows DPAPI for the current user.
