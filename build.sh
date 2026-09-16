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
    -p:DebugType=none \
    -p:DebugSymbols=false \
    -o "$OUT_DIR/Dashboard"

echo "Publishing Nolvus.Updater..."
dotnet publish "$UPDATER_CSPROJ" \
    -c Release \
    -r linux-x64 \
    -p:DebugType=none \
    -p:DebugSymbols=false \
    -o "$OUT_DIR/Updater"

cp -a "$OUT_DIR/Dashboard/." "$OUT_DIR/Nolvus/"
cp -a "$OUT_DIR/Updater/." "$OUT_DIR/Nolvus/"

# DebugType only governs what our own projects emit. Dependencies such as CefGlue ship prebuilt
# .pdb files in their packages, so sweep those out of the payload as well.
echo "Removing debug symbols..."
find "$OUT_DIR/Nolvus" -name "*.pdb" -type f -delete

# The CefGlue package ships its browser subprocess as a self-contained .NET 8 application, so the
# payload carries a second copy of the runtime (~70MB) that the machine already has - the dashboard
# itself is framework dependent and resolves Microsoft.NETCore.App from the system. Point the
# subprocess at the shared runtime too and drop the bundled copy.
#
# Three things have to change together. The runtimeconfig has to ask for a framework instead of
# declaring an included one. The deps manifest describes a self-contained layout and has to go, or
# the host keeps looking for a runtime beside the assembly. And the prebuilt apphost is itself a
# self-contained native host that only ever looks for coreclr next to itself, so it is replaced by a
# launcher that runs the assembly through the installed dotnet. CEF executes whatever sits at that
# path and appends its own arguments, so keeping the file name is all that is needed to stay wired
# up - no dashboard code has to know about any of this.
echo "Removing the bundled .NET runtime from the CEF subprocess..."

SUBPROC_DIR="$OUT_DIR/Nolvus/CefGlueBrowserProcess"
SUBPROC_HOST="$SUBPROC_DIR/Xilium.CefGlue.BrowserProcess"

if [ -f "$SUBPROC_HOST" ]; then
    cat > "$SUBPROC_DIR/Xilium.CefGlue.BrowserProcess.runtimeconfig.json" <<'JSON'
{
  "runtimeOptions": {
    "tfm": "net8.0",
    "rollForward": "Major",
    "framework": {
      "name": "Microsoft.NETCore.App",
      "version": "8.0.0"
    },
    "configProperties": {
      "System.Reflection.Metadata.MetadataUpdater.IsSupported": false,
      "System.Runtime.Serialization.EnableUnsafeBinaryFormatterSerialization": false
    }
  }
}
JSON

    rm -f "$SUBPROC_DIR/Xilium.CefGlue.BrowserProcess.deps.json"

    (
        cd "$SUBPROC_DIR"
        rm -f System.dll System.*.dll Microsoft.*.dll netstandard.dll mscorlib.dll WindowsBase.dll \
              libcoreclr.so libcoreclrtraceptprovider.so libclrjit.so libclrgc.so \
              libhostfxr.so libhostpolicy.so libmscordaccore.so libmscordbi.so \
              libSystem.*.so createdump
    )

    cat > "$SUBPROC_HOST" <<'LAUNCHER'
#!/usr/bin/env bash
# Runs the CEF browser subprocess on the system .NET runtime instead of a bundled copy. CEF execs
# this path directly and appends its own arguments, so they are forwarded verbatim.
set -eu

HERE="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

DOTNET="$(command -v dotnet || true)"

if [ -z "$DOTNET" ]; then
    for CANDIDATE in "${DOTNET_ROOT:-}/dotnet" /usr/share/dotnet/dotnet /usr/lib/dotnet/dotnet /opt/dotnet/dotnet; do
        if [ -x "$CANDIDATE" ]; then
            DOTNET="$CANDIDATE"
            break
        fi
    done
fi

if [ -z "$DOTNET" ]; then
    echo "Nolvus Dashboard : the .NET runtime is required but 'dotnet' could not be found." >&2
    exit 1
fi

exec "$DOTNET" "$HERE/Xilium.CefGlue.BrowserProcess.dll" "$@"
LAUNCHER

    chmod +x "$SUBPROC_HOST"
else
    echo "WARNING: CEF subprocess not found at $SUBPROC_HOST"
fi

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
