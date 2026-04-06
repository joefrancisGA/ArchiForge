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
COPY ArchiForge.Host.Composition/*.csproj   ArchiForge.Host.Composition/
COPY ArchiForge.AgentRuntime/*.csproj       ArchiForge.AgentRuntime/
COPY ArchiForge.AgentSimulator/*.csproj     ArchiForge.AgentSimulator/
COPY ArchiForge.Application/*.csproj        ArchiForge.Application/
COPY ArchLucid.Application.Tests/*.csproj    ArchLucid.Application.Tests/
COPY ArchiForge.ArtifactSynthesis/*.csproj  ArchiForge.ArtifactSynthesis/
COPY ArchLucid.ArtifactSynthesis.Tests/*.csproj ArchLucid.ArtifactSynthesis.Tests/
COPY ArchiForge.Contracts/*.csproj          ArchiForge.Contracts/
COPY ArchiForge.ContextIngestion/*.csproj   ArchiForge.ContextIngestion/
COPY ArchiForge.Coordinator/*.csproj        ArchiForge.Coordinator/
COPY ArchiForge.Core/*.csproj               ArchiForge.Core/
COPY ArchiForge.Decisioning/*.csproj        ArchiForge.Decisioning/
COPY ArchiForge.KnowledgeGraph/*.csproj     ArchiForge.KnowledgeGraph/
COPY ArchiForge.Persistence/*.csproj        ArchiForge.Persistence/
COPY ArchiForge.Persistence.Runtime/*.csproj ArchiForge.Persistence.Runtime/
COPY ArchiForge.Persistence.Advisory/*.csproj ArchiForge.Persistence.Advisory/
COPY ArchiForge.Persistence.Alerts/*.csproj ArchiForge.Persistence.Alerts/
COPY ArchiForge.Persistence.Integration/*.csproj ArchiForge.Persistence.Integration/
COPY ArchiForge.Provenance/*.csproj         ArchiForge.Provenance/
COPY ArchiForge.Retrieval/*.csproj          ArchiForge.Retrieval/

# Test/CLI projects are referenced by the solution but not needed at runtime.
# Provide stubs so `dotnet restore` on the .sln succeeds.
COPY ArchiForge.Cli/*.csproj                ArchiForge.Cli/
COPY ArchiForge.Backfill.Cli/*.csproj       ArchiForge.Backfill.Cli/
COPY ArchLucid.Api.Tests/*.csproj          ArchLucid.Api.Tests/
COPY ArchLucid.AgentRuntime.Tests/*.csproj  ArchLucid.AgentRuntime.Tests/
COPY ArchLucid.Cli.Tests/*.csproj          ArchLucid.Cli.Tests/
COPY ArchLucid.ContextIngestion.Tests/*.csproj ArchLucid.ContextIngestion.Tests/
COPY ArchLucid.Contracts.Tests/*.csproj    ArchLucid.Contracts.Tests/
COPY ArchLucid.Coordinator.Tests/*.csproj  ArchLucid.Coordinator.Tests/
COPY ArchLucid.Decisioning.Tests/*.csproj  ArchLucid.Decisioning.Tests/
COPY ArchLucid.KnowledgeGraph.Tests/*.csproj ArchLucid.KnowledgeGraph.Tests/
COPY ArchLucid.Persistence.Tests/*.csproj  ArchLucid.Persistence.Tests/
COPY ArchLucid.Retrieval.Tests/*.csproj    ArchLucid.Retrieval.Tests/
COPY ArchLucid.Core.Tests/*.csproj         ArchLucid.Core.Tests/
COPY ArchLucid.Provenance.Tests/*.csproj   ArchLucid.Provenance.Tests/
COPY ArchLucid.Api.Client.Tests/*.csproj   ArchLucid.Api.Client.Tests/
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
        /p:PublishReadyToRun=true \
    && dotnet publish ArchiForge.Worker/ArchiForge.Worker.csproj \
        -c Release \
        -o /app \
        --no-restore \
        /p:PublishReadyToRun=true

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
