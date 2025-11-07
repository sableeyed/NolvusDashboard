#!/bin/bash
set -e

# Root of the project
PROJECT_ROOT=$(pwd)
BIN_DIR="$PROJECT_ROOT/bin"

echo "Cleaning previous build..."
rm -rf "$BIN_DIR"
mkdir -p "$BIN_DIR"

echo "Building class library projects as DLLs..."

# List of library projects (DLLs)
LIB_PROJECTS=(
    "Nolvus.Browser"
    "Nolvus.Components"
    "Nolvus.Core"
    #"Nolvus.Api.Installer"
    "Nolvus.Downgrader"
    "Nolvus.GrassCache"
    "Nolvus.Instance"
    "Nolvus.Launcher"
    "Nolvus.NexusApi"
    "Nolvus.Package"
    "Nolvus.Services"
    "Nolvus.StockGame"
    # Add other library projects here if any
)

for proj in "${LIB_PROJECTS[@]}"; do
    echo "Building $proj..."
    dotnet build "$PROJECT_ROOT/$proj/$proj.csproj" \
        -c Release \
        -r linux-x64 \
        -o "$BIN_DIR"
done

echo "Building Nolvus.Api.Installer..."
dotnet build "$PROJECT_ROOT/Nolvus.Api.Library.Installer/Nolvus.Api.Installer.csproj" \
    -c Release \
    -r linux-x64 \
    -o "$BIN_DIR"

echo "Publishing executables..."

# List of executable projects
EXE_PROJECTS=(
    "Nolvus.Dashboard"
    "Nolvus.Updater"
)

for proj in "${EXE_PROJECTS[@]}"; do
    echo "Publishing $proj..."
    dotnet publish "$PROJECT_ROOT/$proj/$proj.csproj" \
        -c Release \
        -r linux-x64 \
        --self-contained true \
        /p:PublishSingleFile=true \
        -o "$BIN_DIR"
done

mv $BIN_DIR/Nolvus.Dashboard $BIN_DIR/NolvusDashboard
mv $BIN_DIR/Nolvus.Updater $BIN_DIR/NolvusUpdater

echo "Build complete! All DLLs and executables are in $BIN_DIR"
