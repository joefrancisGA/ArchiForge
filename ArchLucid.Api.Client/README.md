# ArchLucid.Api.Client

Versioned .NET package containing an **NSwag-generated** `HttpClient`-based client for **ArchLucid API v1**.

## Source contract

Generation uses the committed OpenAPI document:

`ArchLucid.Api.Tests/Contracts/openapi-v1.contract.snapshot.json`

That file is kept in sync with `GET /openapi/v1.json` by `OpenApiContractSnapshotTests` in the main repository.

## Usage

```csharp
using System.Net.Http;
using ArchLucid.Api.Client.Generated;

HttpClient http = new HttpClient { BaseAddress = new Uri("https://your-archlucid-api/") };
ArchLucidApiClient client = new ArchLucidApiClient(http);
// Call client.*Async methods; pass api-version query where required.
```

Configure authentication on `HttpClient` (for example default `Authorization` headers) to match your deployment’s `ArchLucidAuth` mode.

## Package versioning

The NuGet **package version** (`ArchLucidApiClientPackageVersion` in `Directory.Build.props`) is the shipping line for the SDK. Bump it when you publish a new client drop; it is intentionally **independent** of the API’s `info.version` field inside the OpenAPI file.

## Regenerating locally

`dotnet build` on `ArchLucid.Api.Client` runs NSwag before compile. After API contract changes, update the snapshot (`ARCHLUCID_UPDATE_OPENAPI_SNAPSHOT=1` per test comments), then rebuild this project.
