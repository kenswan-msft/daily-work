#!/usr/bin/env bash
set -euo pipefail

# DailyWork CLI — Install / Update Script
# Packs the CLI as a global .NET tool and writes ~/.dailywork/config.json

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"
CLI_PROJECT="$REPO_ROOT/src/DailyWork.Cli/DailyWork.Cli.csproj"
APPHOST_PATH="$REPO_ROOT/src/DailyWork.AppHost"
NUPKG_DIR="$REPO_ROOT/src/DailyWork.Cli/nupkg"
CONFIG_DIR="$HOME/.dailywork"
CONFIG_FILE="$CONFIG_DIR/config.json"
PACKAGE_ID="DailyWork.Cli"
TOOL_NAME="daily"

echo "╔══════════════════════════════════════════╗"
echo "║   DailyWork CLI — Install / Update       ║"
echo "╚══════════════════════════════════════════╝"
echo ""

# Verify prerequisites
if ! command -v dotnet &> /dev/null; then
    echo "Error: dotnet SDK is not installed or not in PATH."
    exit 1
fi

if [ ! -f "$CLI_PROJECT" ]; then
    echo "Error: CLI project not found at $CLI_PROJECT"
    exit 1
fi

# Clean previous packages
echo "→ Cleaning previous packages..."
rm -rf "$NUPKG_DIR"

# Pack the CLI project with a unique timestamp suffix so updates are always detected
TIMESTAMP=$(date -u +%Y%m%d%H%M)
echo "→ Packing DailyWork.Cli (version suffix: $TIMESTAMP)..."
dotnet pack "$CLI_PROJECT" -c Release --nologo -v quiet -p:VersionSuffix="$TIMESTAMP"

# Install or update the global tool
echo "→ Installing global tool '$TOOL_NAME'..."
if dotnet tool list -g | grep -q "$PACKAGE_ID"; then
    dotnet tool update --global --prerelease --add-source "$NUPKG_DIR" "$PACKAGE_ID" --no-cache
else
    dotnet tool install --global --prerelease --add-source "$NUPKG_DIR" "$PACKAGE_ID" --no-cache
fi

# Write configuration file
echo "→ Writing configuration to $CONFIG_FILE..."
mkdir -p "$CONFIG_DIR"
cat > "$CONFIG_FILE" <<EOF
{
  "AppHostProjectPath": "$APPHOST_PATH",
  "DailyWorkApiOptions": {
    "BaseAddress": "https://localhost:7048",
    "ChatEndpoint": "/api/chat"
  }
}
EOF

echo ""
echo "✓ DailyWork CLI installed successfully!"
echo ""
echo "  Tool command:   $TOOL_NAME"
echo "  AppHost path:   $APPHOST_PATH"
echo "  Config file:    $CONFIG_FILE"
echo ""
echo "Run '$TOOL_NAME' from any directory to start."
