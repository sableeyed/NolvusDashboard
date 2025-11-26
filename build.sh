#!/bin/bash
set -e

PROJECT_ROOT=$(pwd)
OUT_DIR="$PROJECT_ROOT/bin"

echo "Cleaning previous build..."
rm -rf "$OUT_DIR"
mkdir -p "$OUT_DIR/Dashboard"
mkdir -p "$OUT_DIR/Updater"
mkdir -p "$OUT_DIR/Browser"

###########################################
# 1. Publish Browser Process (CEF extracted)
###########################################
echo "Publishing Browser..."
dotnet publish "$PROJECT_ROOT/Nolvus.Browser/Nolvus.Browser.csproj" \
    -c Release \
    -r linux-x64 \
    --self-contained true \
    -p:PublishSingleFile=false \
    -o "$OUT_DIR/Browser"

###########################################
# 2. Publish Dashboard as fully self-contained single file
#    (csproj already has PublishSingleFile/SelfContained set)
###########################################
echo "Publishing Nolvus.Dashboard..."
dotnet publish "$PROJECT_ROOT/Nolvus.Dashboard/Nolvus.Dashboard.csproj" \
    -c Release \
    -r linux-x64 \
    -o "$OUT_DIR/Dashboard"

###########################################
# 3. Publish Updater as fully self-contained single file
###########################################
echo "Publishing Nolvus.Updater..."
dotnet publish "$PROJECT_ROOT/Nolvus.Updater/Nolvus.Updater.csproj" \
    -c Release \
    -r linux-x64 \
    -o "$OUT_DIR/Updater"

echo "Renaming Updater..."
if [ -f "$OUT_DIR/Updater/Nolvus.Updater" ]; then
    mv "$OUT_DIR/Updater/Nolvus.Updater" "$OUT_DIR/Updater/NolvusUpdater"
fi

echo "Build complete!"
echo "Dashboard: $OUT_DIR/Dashboard"
echo "Updater:   $OUT_DIR/Updater"
echo "Browser:   $OUT_DIR/Browser"
