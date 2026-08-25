# Security Policy

## Supported version

Security fixes are provided for the latest release.

## Reporting a vulnerability

Use [GitHub private vulnerability reporting](https://github.com/Marcus71-q/SmartDownloads/security/advisories/new). Do not open a public issue for API-key exposure, authentication bypass, remote code execution, or privacy leaks. Never include real secrets or personal files.

## Security boundaries

- Downloaded files are not executed, extracted, deleted, overwritten, or uploaded.
- ZIP contents and executable metadata are not opened for classification.
- Existing files require explicit opt-in; directories are never moved automatically.
- Provider credentials exist only in the hosting secret manager.
- Device tokens are encrypted locally with Windows DPAPI.
- Shared daily counters use Redis-compatible storage when `REDIS_URL` is configured.
- Production fails closed if configured quota storage is unavailable.

Rotate a provider key immediately if it appears in Git history, logs, screenshots, builds, or issue content. Enable provider-side quotas and billing alerts. Public binaries should be Authenticode-signed before a stable production launch.
