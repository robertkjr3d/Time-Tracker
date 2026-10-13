# Windows installer packaging

This project includes a Windows-first Inno Setup script at `installer/TimeTracker.iss`.

## Prerequisites

- .NET 8 SDK
- Inno Setup 6

## 1. Publish the app

From the repository root, run:

`dotnet publish "Time Tracker/Time Tracker.csproj" -c Release`

This writes the self-contained Windows publish output to:

`Time Tracker/bin/Release/publish/`

## 2. Build the installer

Open `installer/TimeTracker.iss` in Inno Setup and compile it.

The installer will be created in:

`installer/Output/`

## Notes

- This installer is currently unsigned and intended for early testing/public beta use.
- It is configured as an admin installer and targets `Program Files`, so Windows will show a UAC prompt during install.
- Windows SmartScreen may still warn until the app is code-signed and builds reputation.
- Update `MyAppVersion`, `MyAppPublisher`, and related values in `installer/TimeTracker.iss` when preparing a real release.
