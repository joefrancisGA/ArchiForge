#!/usr/bin/env bash
# Reusable: create SQL catalog, start ArchLucid.Api in background, wait for /health/ready.
# Usage: ./scripts/ci/start_api_for_k6.sh <database_name> <log_file> <pid_file>
# Env: SA_PASSWORD (default: LocalTesting123!), API_PORT (default: 5128)
set -euo pipefail

DB_NAME="${1:?usage: start_api_for_k6.sh <database_name> <log_file> <pid_file>}"
LOG_FILE="${2:?usage: start_api_for_k6.sh <database_name> <log_file> <pid_file>}"
PID_FILE="${3:?usage: start_api_for_k6.sh <database_name> <log_file> <pid_file>}"

SA_PASSWORD="${SA_PASSWORD:-LocalTesting123!}"
API_PORT="${API_PORT:-5128}"
API_URL="http://127.0.0.1:${API_PORT}"

echo "Creating SQL catalog ${DB_NAME}..."
docker run --rm --network host --entrypoint /opt/mssql-tools18/bin/sqlcmd \
  mcr.microsoft.com/mssql/server:2022-latest \
  -S "127.0.0.1,1433" -U sa -P "${SA_PASSWORD}" -C \
  -Q "IF NOT EXISTS (SELECT 1 FROM sys.databases WHERE name = N'${DB_NAME}') CREATE DATABASE [${DB_NAME}];"

echo "Starting ArchLucid.Api (background, port ${API_PORT})..."
export ASPNETCORE_ENVIRONMENT=Development
export ASPNETCORE_URLS="${API_URL}"
export ConnectionStrings__ArchLucid="Server=127.0.0.1,1433;User Id=sa;Password=${SA_PASSWORD};TrustServerCertificate=True;Initial Catalog=${DB_NAME}"
export ArchLucid__StorageProvider=Sql
export ArchLucidAuth__Mode=DevelopmentBypass
export Authentication__ApiKey__DevelopmentBypassAll=true
export AgentExecution__Mode=Simulator
# appsettings.Advanced.json enables SqlServer:RowLevelSecurity:ApplySessionContext; DbUp + ISchemaBootstrapper use SqlRowLevelSecurityBypassAmbient.Enter().
export ARCHLUCID_ALLOW_RLS_BYPASS=true
export ArchLucid__Persistence__AllowRlsBypass=true
export RateLimiting__FixedWindow__PermitLimit=200000
export RateLimiting__FixedWindow__WindowMinutes=1

nohup dotnet run --no-build -c Release --project ArchLucid.Api/ArchLucid.Api.csproj > "${LOG_FILE}" 2>&1 &
echo $! > "${PID_FILE}"

echo "Waiting for ${API_URL}/health/ready..."
for i in $(seq 1 90); do
  if curl -fsS "${API_URL}/health/ready"; then
    echo ""
    echo "API ready."
    exit 0
  fi

  if [ "$i" -eq 90 ]; then
    echo "::error::API did not reach /health/ready in time"
    tail -n 200 "${LOG_FILE}" || true
    exit 1
  fi

  sleep 2
done
