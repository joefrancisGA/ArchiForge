# ArchiForge.Api.Client

Versioned .NET package containing an **NSwag-generated** `HttpClient`-based client for **ArchiForge API v1**.

## Source contract

Generation uses the committed OpenAPI document:

`ArchiForge.Api.Tests/Contracts/openapi-v1.contract.snapshot.json`

That file is kept in sync with `GET /openapi/v1.json` by `OpenApiContractSnapshotTests` in the main repository.

## Usage

```csharp
using System.Net.Http;
using ArchiForge.Api.Client.Generated;

HttpClient http = new HttpClient { BaseAddress = new Uri("https://your-archiforge-api/") };
ArchiForgeApiClient client = new ArchiForgeApiClient(http);
// Call client.*Async methods; pass api-version query where required.
```

Configure authentication on `HttpClient` (for example default `Authorization` headers) to match your deployment’s `ArchiForgeAuth` mode.

## Package versioning

The NuGet **package version** (`ArchiForgeApiClientPackageVersion` in `Directory.Build.props`) is the shipping line for the SDK. Bump it when you publish a new client drop; it is intentionally **independent** of the API’s `info.version` field inside the OpenAPI file.

## Regenerating locally

`dotnet build` on `ArchiForge.Api.Client` runs NSwag before compile. After API contract changes, update the snapshot (`ARCHIFORGE_UPDATE_OPENAPI_SNAPSHOT=1` per test comments), then rebuild this project.
