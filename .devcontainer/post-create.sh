#!/usr/bin/env bash
set -euo pipefail

echo "╔══════════════════════════════════════════╗"
echo "║   DailyWork — Dev Container Setup        ║"
echo "╚══════════════════════════════════════════╝"
echo ""

# Restore NuGet packages
echo "→ Restoring NuGet packages..."
dotnet restore DailyWork.slnx --verbosity quiet

# Trust the ASP.NET Core HTTPS development certificate
echo "→ Trusting HTTPS development certificate..."
dotnet dev-certs https --trust 2>/dev/null || true

# Verify Docker is available (required for Aspire orchestration)
echo "→ Verifying Docker availability..."
if docker info > /dev/null 2>&1; then
    echo "  ✓ Docker is available"
else
    echo "  ✗ Docker is not available — Aspire will not be able to start SQL Server or Playwright containers"
    exit 1
fi

echo ""
echo "✓ Dev container setup complete!"
echo ""
echo "  Run the full Aspire app:   dotnet run --project src/DailyWork.AppHost"
echo "  Build:                     dotnet build DailyWork.slnx"
echo "  Test:                      dotnet test DailyWork.slnx"
echo ""
echo "  Docker Model Runner is accessible at: http://model-runner.docker.internal/"
echo "  Override the default endpoint in src/DailyWork.Api/appsettings.json"
echo "  (ChatClientOptions:Endpoint) if needed."
echo ""
