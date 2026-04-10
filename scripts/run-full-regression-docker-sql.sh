#!/usr/bin/env bash
# From repo root: bash scripts/run-full-regression-docker-sql.sh
set -euo pipefail
ROOT="$(cd "$(dirname "$0")/.." && pwd)"
cd "$ROOT"

echo "Starting SQL Server (docker compose sqlserver)..."
docker compose up -d sqlserver

export ARCHLUCID_SQL_TEST="Server=127.0.0.1,1433;User Id=sa;Password=ArchLucid_Dev_Pass123!;TrustServerCertificate=True;Initial Catalog=ArchLucidPersistenceTests"
echo "ARCHLUCID_SQL_TEST set."

echo "Waiting for SQL healthcheck (~35s)..."
sleep 35

echo "Running dotnet test (full solution, Release)..."
dotnet test ArchLucid.sln -c Release --collect:"XPlat Code Coverage"
