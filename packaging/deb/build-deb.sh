#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
PROJECT_PATH="$ROOT_DIR/Time Tracker/Time Tracker.csproj"
CONFIGURATION="${1:-Release}"
RUNTIME_IDENTIFIER="${2:-linux-x64}"
APP_ID="time-tracker"
APP_NAME="Time Tracker"

if [[ ! -f "$PROJECT_PATH" ]]; then
  echo "Project file not found: $PROJECT_PATH" >&2
  exit 1
fi

case "$RUNTIME_IDENTIFIER" in
  linux-x64)
	DEB_ARCH="amd64"
	;;
  linux-arm64)
	DEB_ARCH="arm64"
	;;
  *)
	echo "Unsupported RID '$RUNTIME_IDENTIFIER'. Use linux-x64 or linux-arm64." >&2
	exit 1
	;;
esac

VERSION="$(grep -oPm1 '(?<=<Version>)[^<]+' "$PROJECT_PATH")"
if [[ -z "$VERSION" ]]; then
  echo "Could not read <Version> from $PROJECT_PATH" >&2
  exit 1
fi

PUBLISH_DIR="$ROOT_DIR/artifacts/publish/$RUNTIME_IDENTIFIER"
PACKAGE_WORK_DIR="$ROOT_DIR/artifacts/deb/work/${APP_ID}_${VERSION}_${DEB_ARCH}"
PACKAGE_OUTPUT_DIR="$ROOT_DIR/artifacts/deb"
INSTALL_DIR="$PACKAGE_WORK_DIR/opt/$APP_ID"
DEBIAN_DIR="$PACKAGE_WORK_DIR/DEBIAN"
BIN_DIR="$PACKAGE_WORK_DIR/usr/bin"
APPLICATIONS_DIR="$PACKAGE_WORK_DIR/usr/share/applications"

rm -rf "$PUBLISH_DIR" "$PACKAGE_WORK_DIR"
mkdir -p "$PUBLISH_DIR" "$INSTALL_DIR" "$DEBIAN_DIR" "$BIN_DIR" "$APPLICATIONS_DIR" "$PACKAGE_OUTPUT_DIR"

echo "Publishing $APP_NAME for $RUNTIME_IDENTIFIER..."
dotnet publish "$PROJECT_PATH" -c "$CONFIGURATION" -r "$RUNTIME_IDENTIFIER" --self-contained true -o "$PUBLISH_DIR"

echo "Staging package files..."
cp -a "$PUBLISH_DIR/." "$INSTALL_DIR/"
chmod +x "$INSTALL_DIR/$APP_NAME"

cat > "$BIN_DIR/$APP_ID" <<'EOF'
#!/bin/sh
exec "/opt/time-tracker/Time Tracker" "$@"
EOF
chmod 755 "$BIN_DIR/$APP_ID"

cat > "$APPLICATIONS_DIR/$APP_ID.desktop" <<'EOF'
[Desktop Entry]
Type=Application
Version=1.0
Name=Time Tracker
Comment=Track activity time
Exec=/usr/bin/time-tracker
Terminal=false
Icon=applications-office
Categories=Office;Utility;
EOF

INSTALLED_SIZE="$(du -s "$INSTALL_DIR" | cut -f1)"

cat > "$DEBIAN_DIR/control" <<EOF
Package: $APP_ID
Version: $VERSION
Section: utils
Priority: optional
Architecture: $DEB_ARCH
Maintainer: Robert Koernke
Depends: libsqlite3-0
Installed-Size: $INSTALLED_SIZE
Description: Time Tracker desktop application
 Track activities and session durations.
EOF

echo "Building .deb package..."
OUTPUT_DEB="$PACKAGE_OUTPUT_DIR/${APP_ID}_${VERSION}_${DEB_ARCH}.deb"
dpkg-deb --build --root-owner-group "$PACKAGE_WORK_DIR" "$OUTPUT_DEB"

echo "Created package: $OUTPUT_DEB"
