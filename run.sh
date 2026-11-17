#!/usr/bin/env bash
set -euo pipefail
cd "$(dirname "$0")"
dotnet run --project LegitContent --urls http://localhost:5095
