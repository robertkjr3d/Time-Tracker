# Time Tracker

Time Tracker is a desktop activity time-tracking app built with .NET 8 and Avalonia.

## Features

- Job/category management
- Activity start/stop tracking
- Activity/session editing
- CSV export (all jobs or selected job)
- SQLite local data storage

## Tech Stack

- .NET 8
- Avalonia UI
- SQLite (`Microsoft.Data.Sqlite`)

## Development

### Prerequisites

- .NET 8 SDK
- Visual Studio 2026+ (Windows) or any editor with .NET support

### Build

```bash
dotnet build "Time Tracker/Time Tracker.csproj"
```

### Run

```bash
dotnet run --project "Time Tracker/Time Tracker.csproj"
```

## Publish Targets

Project runtime identifiers are configured for:

- `win-x64`
- `linux-x64`
- `linux-arm64`
- `osx-x64`
- `osx-arm64`

### Windows publish

```bash
dotnet publish "Time Tracker/Time Tracker.csproj" -c Release -r win-x64
```

### Linux publish

```bash
dotnet publish "Time Tracker/Time Tracker.csproj" -c Release -r linux-x64
```

### macOS publish

```bash
dotnet publish "Time Tracker/Time Tracker.csproj" -c Release -r osx-arm64
```

## Linux `.deb` Packaging

A Debian package script is included:

- `packaging/deb/build-deb.sh`
- `packaging/deb/README.md`

Build on Linux:

```bash
bash packaging/deb/build-deb.sh Release linux-x64
```

Output package:

- `artifacts/deb/time-tracker_<version>_<arch>.deb`

## macOS Security Disclaimer

macOS artifacts are built for testing and distribution preparation. Unsigned/unnotarized macOS binaries may be blocked by Gatekeeper and other security controls.

For smooth end-user installation on macOS, use:

- Apple Developer signing
- Apple notarization

Until signed/notarized, users may need manual security override steps to run the app.

## CI/CD

GitHub Actions workflow (added in this repo) builds artifacts on every push to `master` for Windows, Linux, and macOS.

## License

See `LICENSE.txt`.
