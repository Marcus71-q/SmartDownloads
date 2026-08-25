# Privacy

Smart Downloads performs file movement locally and asks for consent before enabling server-assisted classification.

## Metadata sent

- File name
- Extension
- Byte size
- Whether the item is a file or directory
- A pseudonymous installation identifier used for authentication and abuse protection

Directories are not automatically moved. ZIP entry names, executable version fields, file bytes, document contents, images, videos, audio, browser data, passwords, Windows account details, and user API keys are not sent.

The client sends requests to the configured Smart Downloads HTTPS server. The reference deployment uses Render for hosting and Google's Gemini API for classification. Those infrastructure providers can retain operational network and service logs under their own policies.

The reference server does not intentionally persist classification metadata or model responses. It stores only bounded daily usage counters in Redis-compatible quota storage. In-memory classification cache entries expire after at most seven days and are lost on restart.

The desktop app stores settings, an encrypted device token, and a size-limited operational log under `%LOCALAPPDATA%\SmartDownloads`. The log does not contain downloaded file names. Remove that directory after exiting the app to erase local app data.
