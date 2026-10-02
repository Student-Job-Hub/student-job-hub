#!/usr/bin/env bash
set -euo pipefail

if [[ -z "${API_BASE_URL:-}" ]]; then
  echo "API_BASE_URL must be set to the HTTPS Render API URL."
  exit 1
fi

if ! command -v dotnet >/dev/null 2>&1; then
  install_dir="${HOME}/.dotnet"
  mkdir -p "$install_dir"
  curl --fail --silent --show-error --location \
    https://dot.net/v1/dotnet-install.sh \
    --output /tmp/dotnet-install.sh
  bash /tmp/dotnet-install.sh --channel 10.0 --install-dir "$install_dir"
  export DOTNET_ROOT="$install_dir"
  export PATH="$install_dir:$PATH"
fi

dotnet publish src/StudentJobHub.Client/StudentJobHub.Client.csproj \
  --configuration Release \
  --output artifacts/vercel-client \
  --nologo

node scripts/set-client-api-url.mjs \
  artifacts/vercel-client/wwwroot/appsettings.json \
  "$API_BASE_URL"