#!/bin/bash
set -e

PROJECT_ROOT=$(pwd)
OUT_DIR="$PROJECT_ROOT/bin"

# --- Extract version from the csproj ---
CSPROJ="$PROJECT_ROOT/Nolvus.Dashboard/Nolvus.Dashboard.csproj"
VERSION=$(grep -oPm1 "(?<=<Version>)[^<]+" "$CSPROJ")

if [ -z "$VERSION" ]; then
    echo "ERROR: Could not find <Version> in $CSPROJ"
    exit 1
fi

DESKTOP_RELEASE="$HOME/Desktop/Binaries-$VERSION.tar.gz"

echo "Building Nolvus Dashboard v$VERSION"

echo "Cleaning previous build..."
rm -rf "$OUT_DIR"
mkdir -p "$OUT_DIR/Dashboard"

echo "Publishing Nolvus.Dashboard..."
dotnet publish "$CSPROJ" \
    -c Release \
    -r linux-x64 \
    -o "$OUT_DIR/Dashboard"

echo "Copying icon asset..."
cp "$PROJECT_ROOT/Nolvus.Dashboard/Assets/nolvus-ico.jpg" "$OUT_DIR/Dashboard/"

echo "Renaming Dashboard -> Nolvus..."
cd "$OUT_DIR"
cp -r Dashboard Nolvus

echo "Compressing to: $DESKTOP_RELEASE"
tar -czf "$DESKTOP_RELEASE" Nolvus

echo "Cleaning up build directory..."
cd "$PROJECT_ROOT"
rm -rf "$OUT_DIR"

echo "Build complete!"
echo "Created: $DESKTOP_RELEASE"
