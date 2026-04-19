# Stringify Desktop

Windows desktop uploader for Strinova replay files, rebuilt as an `Avalonia + SukiUI` app on `.NET 10`.

The app watches the local replay folder, uploads new `.replay` files through the Strinova API, and uses Clerk OAuth/OIDC for secure browser sign-in.

## Stack

- `Avalonia 11.3.12` for the desktop shell
- `SukiUI 6.0.3` for theming and window styling
- `.NET 10` / C#
- Clerk OAuth/OIDC with Authorization Code + PKCE
- Windows tray integration via `NotifyIcon`
- `FileSystemWatcher` with file-stability checks before upload
- JSON settings and upload log persistence in `%LOCALAPPDATA%\StringifyDesktop`
- DPAPI-protected session storage for OAuth tokens

## Architecture

| Area | Path | Responsibility |
| --- | --- | --- |
| App shell | [StringifyDesktop/App.axaml](StringifyDesktop/App.axaml) | Avalonia application theme/bootstrap |
| Window/UI | [StringifyDesktop/MainWindow.axaml](StringifyDesktop/MainWindow.axaml) | SukiUI shell hosting the dashboard |
| View state | [StringifyDesktop/ViewModels/MainViewModel.cs](StringifyDesktop/ViewModels/MainViewModel.cs) | Auth, watcher, sync, retry, activity, and history state |
| Auth | [StringifyDesktop/Services/AuthService.cs](StringifyDesktop/Services/AuthService.cs) | PKCE, callback handling, token refresh, and user info |
| Uploads | [StringifyDesktop/Services/BackendApiClient.cs](StringifyDesktop/Services/BackendApiClient.cs) | `POST /api/upload` request for signed URLs |
| Uploads | [StringifyDesktop/Services/UploadService.cs](StringifyDesktop/Services/UploadService.cs) | File upload pipeline and 403 handling |
| Uploads | [StringifyDesktop/Services/ReplayFileValidator.cs](StringifyDesktop/Services/ReplayFileValidator.cs) | Lightweight replay-header validation before upload |
| Replay watching | [StringifyDesktop/Services/ReplayWatcherService.cs](StringifyDesktop/Services/ReplayWatcherService.cs) | Folder watch + file-stability checks |
| Persistence | [StringifyDesktop/Services/SettingsStore.cs](StringifyDesktop/Services/SettingsStore.cs) | `settings.json` persistence |
| Persistence | [StringifyDesktop/Services/UploadLogStore.cs](StringifyDesktop/Services/UploadLogStore.cs) | `upload-log.json` persistence |
| Windows integration | [StringifyDesktop/Services/TrayService.cs](StringifyDesktop/Services/TrayService.cs) | Tray icon, reopen, quit, and balloon tip |
| Windows integration | [StringifyDesktop/Services/ProtocolRegistrationService.cs](StringifyDesktop/Services/ProtocolRegistrationService.cs) | Registers `stringify-gg://` under the current user |
| Single instance | [StringifyDesktop/Services/SingleInstanceService.cs](StringifyDesktop/Services/SingleInstanceService.cs) | Named-pipe argument forwarding for OAuth callback re-entry |

## OAuth callback

The desktop app uses this redirect URI:

- `stringify-gg://auth/callback`

On startup, the app registers the `stringify-gg` protocol under the current user so the browser callback can reopen or forward back into the running app.

## Configuration

Defaults live in [StringifyDesktop/appsettings.json](StringifyDesktop/appsettings.json). You can also override them with process environment variables:

| Env var | Purpose |
| --- | --- |
| `CLERK_OAUTH_ISSUER` | OAuth issuer base URL, defaults to `https://clerk.strinova.gg` |
| `CLERK_OAUTH_CLIENT_ID` | Public OAuth client ID, defaults to `9YfNu3Z7Vm9PvZ6G` |
| `CLERK_OAUTH_SCOPES` | OAuth scopes, defaults to `openid profile email` |
| `CLERK_OAUTH_CALLBACK_URI` | Callback URI, defaults to `stringify-gg://auth/callback` |
| `BACKEND_URL` | Base URL of the Strinova API, defaults to `https://strinova.gg` |
| `DEFAULT_REPLAY_FOLDER` | Default replay folder, defaults to `%LOCALAPPDATA%\Strinova\Saved\Demos` |

## Development

```powershell
dotnet restore StringifyDesktop.slnx
dotnet build StringifyDesktop.slnx
dotnet test StringifyDesktop.Tests/StringifyDesktop.Tests.csproj
dotnet run --project StringifyDesktop/StringifyDesktop.csproj
```

## Current behavior

- Opens the system browser for Clerk sign-in and completes PKCE through `stringify-gg://auth/callback`
- Keeps only one app instance running and forwards callback launches into it through a named pipe
- Watches the replay folder and waits for files to stabilize before upload
- Validates replay headers locally before requesting an upload URL
- Treats HTTP `403` from either the signed-URL request or the upload itself as "already uploaded"
- Supports manual sync, file picking, retry failed, clear failed, and log export
- Persists settings and upload history locally
- Stores OAuth sessions encrypted with Windows DPAPI

## Verification

Verified in this repo with:

```powershell
dotnet list StringifyDesktop/StringifyDesktop.csproj package --vulnerable --include-transitive
dotnet build StringifyDesktop/StringifyDesktop.csproj -c Debug --no-restore
dotnet test StringifyDesktop.Tests/StringifyDesktop.Tests.csproj -c Debug --no-restore
```

## Security note

The app explicitly pins `Tmds.DBus.Protocol` to `0.21.3` to override the vulnerable `0.21.2` version that currently arrives transitively through the Avalonia desktop dependency graph. This clears the active NuGet advisory while preserving the existing Windows behavior.
