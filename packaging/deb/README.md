# Debian Packaging

This folder contains a simple Debian package build script for Time Tracker.

## Prerequisites

Run on a Linux machine with:

- .NET 8 SDK
- `dpkg-deb` (usually included with Debian/Ubuntu)

## Build package

From repository root:

```bash
bash packaging/deb/build-deb.sh
```

Optional parameters:

```bash
bash packaging/deb/build-deb.sh Release linux-x64
bash packaging/deb/build-deb.sh Release linux-arm64
```

## Output

- Published app files: `artifacts/publish/<rid>/`
- Debian package: `artifacts/deb/time-tracker_<version>_<arch>.deb`

## Install package

```bash
sudo apt install ./artifacts/deb/time-tracker_<version>_<arch>.deb
```

## Launch

```bash
time-tracker
```
