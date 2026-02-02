#!/usr/bin/env bash
set -euo pipefail

PROJECT_ROOT=$(pwd)
OUT_DIR="$PROJECT_ROOT/bin"

DASH_CSPROJ="$PROJECT_ROOT/Nolvus.Dashboard/Nolvus.Dashboard.csproj"
UPDATER_CSPROJ="$PROJECT_ROOT/Nolvus.Updater/Nolvus.Updater.csproj"

VERSION=$(grep -oPm1 "(?<=<Version>)[^<]+" "$DASH_CSPROJ" || true)

if [ -z "${VERSION:-}" ]; then
    echo "ERROR: Could not find <Version> in $DASH_CSPROJ"
    exit 1
fi

DESKTOP_RELEASE="$HOME/Desktop/Binaries-$VERSION.tar.gz"

echo "Building Nolvus Dashboard v$VERSION"

echo "Cleaning previous build..."
rm -rf "$OUT_DIR"
mkdir -p "$OUT_DIR/Dashboard" "$OUT_DIR/Updater" "$OUT_DIR/Nolvus"

echo "Publishing Nolvus.Dashboard..."
dotnet publish "$DASH_CSPROJ" \
    -c Release \
    -r linux-x64 \
    -o "$OUT_DIR/Dashboard"

echo "Publishing Nolvus.Updater..."
dotnet publish "$UPDATER_CSPROJ" \
    -c Release \
    -r linux-x64 \
    -o "$OUT_DIR/Updater"

cp -a "$OUT_DIR/Dashboard/." "$OUT_DIR/Nolvus/"
cp -a "$OUT_DIR/Updater/." "$OUT_DIR/Nolvus/"

echo "Copying icon asset..."
cp "$PROJECT_ROOT/Nolvus.Dashboard/Assets/nolvus-ico.jpg" "$OUT_DIR/Nolvus/" || true

DASH_APPHOST="$OUT_DIR/Nolvus/NolvusDashboard"
UPDATER_APPHOST="$OUT_DIR/Nolvus/NolvusUpdater"

if [ -f "$DASH_APPHOST" ]; then
    chmod +x "$DASH_APPHOST"
else
    echo "WARNING: Dashboard apphost not found at $DASH_APPHOST"
fi

if [ -f "$UPDATER_APPHOST" ]; then
    chmod +x "$UPDATER_APPHOST"
else
    echo "WARNING: Updater apphost not found at $UPDATER_APPHOST"
fi

find "$OUT_DIR/Nolvus" -type f \( -name "*.sh" -o -perm -u+x \) -exec chmod +x {} \; 2>/dev/null || true

echo "Compressing to: $DESKTOP_RELEASE"
cd "$OUT_DIR"
tar -czf "$DESKTOP_RELEASE" Nolvus

echo "Cleaning up build directory..."
cd "$PROJECT_ROOT"
rm -rf "$OUT_DIR"

echo "Build complete!"
echo "Created: $DESKTOP_RELEASE"
