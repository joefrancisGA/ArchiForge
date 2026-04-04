# ── ArchiForge API ──────────────────────────────────────────────────
# Multi-stage build: SDK (restore + publish) → Alpine runtime.
# Build context MUST be the repository root so project references resolve.
#
#   docker build -f ArchiForge.Api/Dockerfile -t archiforge-api .
# Publishes API and Worker into /app (shared Host.Core); image contains ArchiForge.Api.dll + ArchiForge.Worker.dll.
# Default ENTRYPOINT runs the API; Container Apps worker revision sets command ["dotnet","ArchiForge.Worker.dll"].
# ────────────────────────────────────────────────────────────────────

# ── Stage 1: restore (layer-cached independently of source changes) ─
FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS restore

WORKDIR /src

COPY global.json Directory.Build.props Directory.Packages.props ArchiForge.sln ./

# Copy every .csproj so NuGet restore is cached until a project file changes.
COPY ArchiForge.Api/*.csproj                ArchiForge.Api/
COPY ArchiForge.Host.Core/*.csproj          ArchiForge.Host.Core/
COPY ArchiForge.AgentRuntime/*.csproj       ArchiForge.AgentRuntime/
COPY ArchiForge.AgentSimulator/*.csproj     ArchiForge.AgentSimulator/
COPY ArchiForge.Application/*.csproj        ArchiForge.Application/
COPY ArchiForge.Application.Tests/*.csproj    ArchiForge.Application.Tests/
COPY ArchiForge.ArtifactSynthesis/*.csproj  ArchiForge.ArtifactSynthesis/
COPY ArchiForge.ArtifactSynthesis.Tests/*.csproj ArchiForge.ArtifactSynthesis.Tests/
COPY ArchiForge.Contracts/*.csproj          ArchiForge.Contracts/
COPY ArchiForge.ContextIngestion/*.csproj   ArchiForge.ContextIngestion/
COPY ArchiForge.Coordinator/*.csproj        ArchiForge.Coordinator/
COPY ArchiForge.Core/*.csproj               ArchiForge.Core/
COPY ArchiForge.DecisionEngine/*.csproj     ArchiForge.DecisionEngine/
COPY ArchiForge.Decisioning/*.csproj        ArchiForge.Decisioning/
COPY ArchiForge.KnowledgeGraph/*.csproj     ArchiForge.KnowledgeGraph/
COPY ArchiForge.Persistence/*.csproj        ArchiForge.Persistence/
COPY ArchiForge.Provenance/*.csproj         ArchiForge.Provenance/
COPY ArchiForge.Retrieval/*.csproj          ArchiForge.Retrieval/

# Test/CLI projects are referenced by the solution but not needed at runtime.
# Provide stubs so `dotnet restore` on the .sln succeeds.
COPY ArchiForge.Cli/*.csproj                ArchiForge.Cli/
COPY ArchiForge.Backfill.Cli/*.csproj       ArchiForge.Backfill.Cli/
COPY ArchiForge.Api.Tests/*.csproj          ArchiForge.Api.Tests/
COPY ArchiForge.AgentRuntime.Tests/*.csproj  ArchiForge.AgentRuntime.Tests/
COPY ArchiForge.Cli.Tests/*.csproj          ArchiForge.Cli.Tests/
COPY ArchiForge.ContextIngestion.Tests/*.csproj ArchiForge.ContextIngestion.Tests/
COPY ArchiForge.Contracts.Tests/*.csproj    ArchiForge.Contracts.Tests/
COPY ArchiForge.Coordinator.Tests/*.csproj  ArchiForge.Coordinator.Tests/
COPY ArchiForge.DecisionEngine.Tests/*.csproj ArchiForge.DecisionEngine.Tests/
COPY ArchiForge.Decisioning.Tests/*.csproj  ArchiForge.Decisioning.Tests/
COPY ArchiForge.KnowledgeGraph.Tests/*.csproj ArchiForge.KnowledgeGraph.Tests/
COPY ArchiForge.Persistence.Tests/*.csproj  ArchiForge.Persistence.Tests/
COPY ArchiForge.Retrieval.Tests/*.csproj    ArchiForge.Retrieval.Tests/
COPY ArchiForge.TestSupport/*.csproj        ArchiForge.TestSupport/
COPY ArchiForge.Worker/*.csproj             ArchiForge.Worker/

RUN dotnet restore ArchiForge.Api/ArchiForge.Api.csproj \
    && dotnet restore ArchiForge.Worker/ArchiForge.Worker.csproj

# ── Stage 2: publish ────────────────────────────────────────────────
FROM restore AS publish

COPY . .
RUN dotnet publish ArchiForge.Api/ArchiForge.Api.csproj \
        -c Release \
        -o /app \
        --no-restore \
    && dotnet publish ArchiForge.Worker/ArchiForge.Worker.csproj \
        -c Release \
        -o /app \
        --no-restore

# ── Stage 3: runtime ───────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS runtime

# ICU for globalisation; wget for HEALTHCHECK (already present in Alpine aspnet image)
RUN apk add --no-cache icu-libs

ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false

WORKDIR /app

RUN addgroup -S archiforge && adduser -S archiforge -G archiforge
USER archiforge

COPY --from=publish /app .

EXPOSE 8080

HEALTHCHECK --interval=15s --timeout=3s --start-period=10s --retries=3 \
    CMD wget --no-verbose --tries=1 --spider http://localhost:8080/health/live || exit 1

ENTRYPOINT ["dotnet", "ArchiForge.Api.dll"]
