#!/bin/bash
set -e

PROJECT_ROOT=$(pwd)
OUT_DIR="$PROJECT_ROOT/bin"

echo "Cleaning previous build..."
rm -rf "$OUT_DIR"
mkdir -p "$OUT_DIR/Dashboard"

echo "Publishing Nolvus.Dashboard..."
dotnet publish "$PROJECT_ROOT/Nolvus.Dashboard/Nolvus.Dashboard.csproj" \
    -c Release \
    -r linux-x64 \
    -o "$OUT_DIR/Dashboard"

echo "Build complete!"
echo "Dashboard: $OUT_DIR/Dashboard"