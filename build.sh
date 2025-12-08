#!/bin/bash
set -e

PROJECT_ROOT=$(pwd)
OUT_DIR="$PROJECT_ROOT/bin"
DESKTOP_RELEASE="$HOME/Desktop/Release.tar.gz"

echo "Cleaning previous build..."
rm -rf "$OUT_DIR"
mkdir -p "$OUT_DIR/Dashboard"

echo "Publishing Nolvus.Dashboard..."
dotnet publish "$PROJECT_ROOT/Nolvus.Dashboard/Nolvus.Dashboard.csproj" \
    -c Release \
    -r linux-x64 \
    -o "$OUT_DIR/Dashboard"

echo "Compressing..."
cd "$OUT_DIR"
tar -czf "$DESKTOP_RELEASE" Dashboard

echo "Cleaning up build directory..."
cd "$PROJECT_ROOT"
rm -rf "$OUT_DIR"

echo "Build complete!"
echo "Created: $DESKTOP_RELEASE"
