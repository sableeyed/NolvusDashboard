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

echo "Copying icon asset..."
cp "$PROJECT_ROOT/Nolvus.Dashboard/Assets/nolvus-ico.jpg" "$OUT_DIR/Dashboard/"

echo "Renaming Dashboard -> Nolvus..."
cd "$OUT_DIR"
cp -r Dashboard Nolvus

echo "Compressing..."
tar -czf "$DESKTOP_RELEASE" Nolvus

echo "Cleaning up build directory..."
cd "$PROJECT_ROOT"
rm -rf "$OUT_DIR"

echo "Build complete!"
echo "Created: $DESKTOP_RELEASE"
