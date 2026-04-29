# ── ArchLucid API ───────────────────────────────────────────────────
# Multi-stage build: SDK (restore + publish) → Alpine runtime.
# Build context MUST be the repository root so project references resolve.
#
#   docker build -f ArchLucid.Api/Dockerfile -t archlucid-api .
# Publishes API, Worker, and Jobs CLI into /app (shared Host.Core); image contains ArchLucid.Api.dll + ArchLucid.Worker.dll + ArchLucid.Jobs.Cli.dll.
# Default ENTRYPOINT runs the API; Container Apps worker revision sets command ["dotnet","ArchLucid.Worker.dll"]; scheduled jobs use ["dotnet","ArchLucid.Jobs.Cli.dll","--job",...].
# ────────────────────────────────────────────────────────────────────

# ── Stage 1: restore (layer-cached independently of source changes) ─
# SDK image tags use the SDK band (e.g. 10.0.201-alpine3.23). The aspnet *runtime* image must use an ASP.NET Core
# runtime tag — never reuse the SDK patch (10.0.201) on mcr.microsoft.com/dotnet/aspnet.
# Use 10.0-alpine3.23 (not 10.0-alpine): same Alpine band as the SDK image; the short 10.0-alpine manifest has hit
# MCR 403 on HEAD in some buildx/CI registries, and pinning OS aligns libc/musl expectations with publish -r linux-musl-x64.
# Pin SDK patch + Alpine; bump SDK tag when you change global.json / CI SDK.
FROM mcr.microsoft.com/dotnet/sdk:10.0.201-alpine3.23 AS restore

WORKDIR /src

COPY global.json Directory.Build.props Directory.Build.targets Directory.Packages.props ArchLucid.sln ./

# Copy every .csproj so NuGet restore is cached until a project file changes.
COPY ArchLucid.Api/*.csproj                ArchLucid.Api/
COPY ArchLucid.Host.Core/*.csproj          ArchLucid.Host.Core/
COPY ArchLucid.Host.Composition/*.csproj   ArchLucid.Host.Composition/
COPY ArchLucid.AgentRuntime/*.csproj       ArchLucid.AgentRuntime/
COPY ArchLucid.AgentSimulator/*.csproj     ArchLucid.AgentSimulator/
COPY ArchLucid.Application/*.csproj        ArchLucid.Application/
COPY ArchLucid.Application.Tests/*.csproj    ArchLucid.Application.Tests/
COPY ArchLucid.ArtifactSynthesis/*.csproj  ArchLucid.ArtifactSynthesis/
COPY ArchLucid.ArtifactSynthesis.Tests/*.csproj ArchLucid.ArtifactSynthesis.Tests/
COPY ArchLucid.Contracts/*.csproj          ArchLucid.Contracts/
COPY ArchLucid.Contracts.Abstractions/*.csproj ArchLucid.Contracts.Abstractions/
COPY ArchLucid.ContextIngestion/*.csproj   ArchLucid.ContextIngestion/
COPY ArchLucid.Core/*.csproj               ArchLucid.Core/
COPY ArchLucid.Decisioning/*.csproj        ArchLucid.Decisioning/
COPY ArchLucid.Integrations.AzureDevOps/*.csproj ArchLucid.Integrations.AzureDevOps/
COPY ArchLucid.KnowledgeGraph/*.csproj     ArchLucid.KnowledgeGraph/
COPY ArchLucid.Persistence/*.csproj        ArchLucid.Persistence/
COPY ArchLucid.Persistence.Coordination/*.csproj ArchLucid.Persistence.Coordination/
COPY ArchLucid.Persistence.Runtime/*.csproj ArchLucid.Persistence.Runtime/
COPY ArchLucid.Persistence.Advisory/*.csproj ArchLucid.Persistence.Advisory/
COPY ArchLucid.Persistence.Alerts/*.csproj ArchLucid.Persistence.Alerts/
COPY ArchLucid.Persistence.Integration/*.csproj ArchLucid.Persistence.Integration/
COPY ArchLucid.Provenance/*.csproj         ArchLucid.Provenance/
COPY ArchLucid.Retrieval/*.csproj          ArchLucid.Retrieval/

# Test/CLI projects are referenced by the solution but not needed at runtime.
# Provide stubs so `dotnet restore` on the .sln succeeds.
COPY ArchLucid.Cli/*.csproj                ArchLucid.Cli/
COPY ArchLucid.Backfill.Cli/*.csproj       ArchLucid.Backfill.Cli/
COPY ArchLucid.Api.Tests/*.csproj          ArchLucid.Api.Tests/
COPY ArchLucid.AgentRuntime.Tests/*.csproj  ArchLucid.AgentRuntime.Tests/
COPY ArchLucid.Cli.Tests/*.csproj          ArchLucid.Cli.Tests/
COPY ArchLucid.ContextIngestion.Tests/*.csproj ArchLucid.ContextIngestion.Tests/
COPY ArchLucid.Contracts.Tests/*.csproj    ArchLucid.Contracts.Tests/
COPY ArchLucid.Decisioning.Tests/*.csproj  ArchLucid.Decisioning.Tests/
COPY ArchLucid.KnowledgeGraph.Tests/*.csproj ArchLucid.KnowledgeGraph.Tests/
COPY ArchLucid.Persistence.Tests/*.csproj  ArchLucid.Persistence.Tests/
COPY ArchLucid.Retrieval.Tests/*.csproj    ArchLucid.Retrieval.Tests/
COPY ArchLucid.Core.Tests/*.csproj         ArchLucid.Core.Tests/
COPY ArchLucid.Provenance.Tests/*.csproj   ArchLucid.Provenance.Tests/
COPY ArchLucid.Api.Client.Tests/*.csproj   ArchLucid.Api.Client.Tests/
COPY ArchLucid.Api.Client/*.csproj         ArchLucid.Api.Client/
COPY ArchLucid.Benchmarks/*.csproj         ArchLucid.Benchmarks/
COPY ArchLucid.TestSupport/*.csproj        ArchLucid.TestSupport/
COPY ArchLucid.Worker/*.csproj             ArchLucid.Worker/
COPY ArchLucid.Jobs.Cli/*.csproj           ArchLucid.Jobs.Cli/

# RID must match publish (-r linux-musl-x64) or assets lack the musl target (NETSDK1047).
# PublishReadyToRun=true on restore pulls R2R/crossgen packages into the graph; publish no longer uses --no-restore so Alpine
# can reconcile any remaining packages (dotnet/sdk#22706-class issues when R2R + --no-restore interact badly).
RUN dotnet restore ArchLucid.Api/ArchLucid.Api.csproj -r linux-musl-x64 /p:PublishReadyToRun=true \
    && dotnet restore ArchLucid.Worker/ArchLucid.Worker.csproj -r linux-musl-x64 /p:PublishReadyToRun=true \
    && dotnet restore ArchLucid.Jobs.Cli/ArchLucid.Jobs.Cli.csproj -r linux-musl-x64 /p:PublishReadyToRun=true

# ── Stage 2: publish ────────────────────────────────────────────────
FROM restore AS publish

COPY . .
# Pilot scorecard persistence lives under ArchLucid.Persistence/Pilots. If this subtree is missing from the context
# (stale cache, wrong build context, or incomplete checkout), ArchLucid.Persistence still compiles but Host.Composition
# fails with CS0234 ("Pilots does not exist in ArchLucid.Persistence"). Re-COPY explicitly so the layer invalidates when
# these files change, and fail fast with a clear error if they never arrived.
COPY ArchLucid.Persistence/Pilots/ ArchLucid.Persistence/Pilots/
RUN test -f ArchLucid.Persistence/Pilots/IPilotBaselineRepository.cs \
    && test -f ArchLucid.Persistence/Pilots/DapperPilotBaselineRepository.cs \
    || (echo "FATAL: ArchLucid.Persistence/Pilots sources missing from Docker context. Use repo root as context (docker build -f ArchLucid.Api/Dockerfile .) and ensure Pilots/*.cs are committed." >&2; exit 1)
# Re-restore after full sources land: restore stage only had .csproj stubs; full tree must match assets before publish.
# Publish omits --no-restore so ReadyToRun/crossgen can reconcile packages on Alpine (dotnet/sdk#22706-style failures with --no-restore+R2R).
RUN dotnet restore ArchLucid.Api/ArchLucid.Api.csproj -r linux-musl-x64 /p:PublishReadyToRun=true \
    && dotnet restore ArchLucid.Worker/ArchLucid.Worker.csproj -r linux-musl-x64 /p:PublishReadyToRun=true \
    && dotnet restore ArchLucid.Jobs.Cli/ArchLucid.Jobs.Cli.csproj -r linux-musl-x64 /p:PublishReadyToRun=true \
    && dotnet publish ArchLucid.Api/ArchLucid.Api.csproj \
        -c Release \
        -r linux-musl-x64 \
        --self-contained false \
        -o /app \
        /p:PublishReadyToRun=true \
    && dotnet publish ArchLucid.Worker/ArchLucid.Worker.csproj \
        -c Release \
        -r linux-musl-x64 \
        --self-contained false \
        -o /app \
        /p:PublishReadyToRun=true \
    && dotnet publish ArchLucid.Jobs.Cli/ArchLucid.Jobs.Cli.csproj \
        -c Release \
        -r linux-musl-x64 \
        --self-contained false \
        -o /app \
        /p:PublishReadyToRun=true

# ── Stage 3: runtime ───────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine3.23 AS runtime

# ICU for globalisation; wget for HEALTHCHECK (already present in Alpine aspnet image).
# apk upgrade + explicit OpenSSL floor (CVE-2026-28390) so cached layers cannot keep 3.5.5-r0.
RUN apk update --no-cache \
    && apk upgrade --no-cache \
    && apk add --no-cache --upgrade \
         "openssl>=3.5.6-r0" \
         "libssl3>=3.5.6-r0" \
         "libcrypto3>=3.5.6-r0" \
    && apk add --no-cache icu-libs

ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false

WORKDIR /app

RUN addgroup -S archlucid && adduser -S archlucid -G archlucid
USER archlucid

COPY --from=publish /app .

EXPOSE 8080

HEALTHCHECK --interval=15s --timeout=3s --start-period=10s --retries=3 \
    CMD wget --no-verbose --tries=1 --spider http://localhost:8080/health/live || exit 1

ENTRYPOINT ["dotnet", "ArchLucid.Api.dll"]
