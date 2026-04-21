# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Common Commands

```bash
# Build / restore
dotnet build                                              # whole solution
dotnet build src/entrypoints/CodeDesignPlus.Net.Microservice.FileStorage.Rest

# Run REST entrypoint (only entrypoint currently scaffolded)
dotnet run --project src/entrypoints/CodeDesignPlus.Net.Microservice.FileStorage.Rest

# Tests
dotnet test                                                                          # all tests
dotnet test tests/unit/CodeDesignPlus.Net.Microservice.FileStorage.Rest.Test         # one project
dotnet test --filter "FullyQualifiedName~FileStorageControllerTest.Upload"           # single test / pattern

# Ops tooling (bash or ps1 variants in same folder)
./tools/vault/config-vault.sh              # seed secrets used by appsettings.*.json
./tools/update-packages/update-packages.sh # refresh all NuGet refs across the solution
./tools/upgrade-dotnet/upgrade-dotnet.sh   # bump TFM across all csproj
./tools/sonarqube/sonarqube.sh             # local SonarQube scan

# Docker (network=backend expected; see README)
docker build -t ms-filestorage-rest . -f src/entrypoints/CodeDesignPlus.Net.Microservice.FileStorage.Rest/Dockerfile
```

External dependencies (MongoDB, Redis, RabbitMQ, Vault) are provisioned via the sibling `CodeDesignPlus.Environment.Dev` repo's `resources/docker-compose.yml`. Nothing works locally until Vault is seeded — `Program.cs` calls `builder.Configuration.AddVault()` before service registration.

## Architecture

Clean Architecture + DDD + CQRS, scaffolded from the CodeDesignPlus archetype (see `archetype.json`). Solution folders follow the layering:

- `src/domain/...Domain` — `FileStorageAggregate` (inherits `AggregateRoot`), value objects (`File`, `FileDetail`, `Metadata`), domain events, repository *interface*. No framework leakage beyond the CodeDesignPlus SDK. Invariants enforced via `DomainGuard.*`.
- `src/domain/...Application` — MediatR handlers, FluentValidation validators, Mapster mappings, DTOs. Commands/queries grouped per use case (e.g. `FileStorage/Commands/CreateFileStorage/`). Pre-condition checks use `ApplicationGuard.*`. Publishes events through `IPubSub` after persistence. Multi-tenancy comes from `IUserContext.Tenant` injected into handlers — always thread it through repository calls.
- `src/domain/...Infrastructure` — Mongo repository implementation (`FileStorageRepository : RepositoryBase`). DI wired in `Startup.cs`.
- `src/entrypoints/...Rest` — ASP.NET Core 9 (`WebApplication.CreateSlimBuilder`). Only REST is implemented today; gRPC/Worker folders referenced in README are not scaffolded in this repo.

### Request flow

HTTP → `FileStorageController` → `IMediator.Send` → handler → `IFileStorageRepository` (Mongo) + `IFileStorage` (cloud SDK) → `IPubSub.PublishAsync(aggregate.GetAndClearEvents())`. Handlers never return domain types to controllers — they return DTOs decorated with `[DtoGenerator]` (source-generated via `CodeDesignPlus.Net.Generator`).

### File upload/download is a proxy, not presigned URLs

The frontend **always** talks to this microservice; the cloud provider behind `IFileStorage` (AzureBlob / AzureFile / Local, chosen via `FileStorage` section in `appsettings.json`) is intentionally opaque. Consumer microservices should persist only `FileStorageId` (Guid), not a cloud URL.

- Upload streams via `MultipartReader` in `FileStorageController.Upload`. Form fields `Id`, `Target`, `Renowned` **must precede** the file part — the handler reads them off the stream in order, then pipes `section.Body` directly into `CreateFileStorageCommand.Stream` without buffering.
- `[DisableFormValueModelBinding]` (local attribute at the bottom of `FileStorageController.cs`) strips the form value providers so ASP.NET doesn't buffer the request body. Kestrel's `MaxRequestBodySize` is set to `null` in `Program.cs`; `FormOptions.MultipartBodyLengthLimit` is `long.MaxValue`. Don't re-enable form binding on this action.
- `CreateFileStorageCommand`'s validator gates `Stream.Length > 0` only `When(x.Stream.CanSeek)` — `MultipartSection.Body` is non-seekable and would throw otherwise.
- `Download` authorizes by loading the aggregate and asserting the requested `file`+`target` pair exists on it. Never call `IFileStorage.DownloadAsync` straight from a path passed in by the caller.

### Domain rules that are easy to trip over

- `FileDetail` requires `size > 0`. Any fixture that builds a `File.Storage.Abstractions.Models.File` with default `Size = 0` will throw `FileSizeIsInvalid` on mapping. Several pre-existing Domain/Application/Integration tests still have this bug (arrange passes `Size = 0`) — fix the fixtures, don't loosen the guard.
- `Metadata` constructor sets `Provider` from the ctor arg (was a real bug before); keep it set. `MapsterConfig` depends on it to round-trip SDK models to domain value objects.
- `FileStorageAggregate.AddFile` rejects duplicates keyed on `(FileDetail.FullName, Provider)` — so re-uploading the same file to a different provider is allowed, but the same provider is not.

### Configuration surface

`appsettings.json` is the source of truth for local dev; Vault overrides it in non-local envs. Notable sections:

- `FileStorage` — pick the backend by toggling `Enable` under `Local` / `AzureFile` / `AzureBlob`. `UriDownload` is the base URL embedded in `Metadata.UriDownload` so consumers can build links.
- `Core.PathBase = /ms-filestorage` — ingress strips this, `app.UsePath()` re-applies it.
- `Security` — OAuth2/JWT validation; `MapControllers().RequireAuthorization()` is global, so every endpoint needs a valid token unless explicitly opted out.

## Testing notes

- Tests split into `tests/unit/*` (per layer) and `tests/integration/*` (REST). k6 load script at `tests/load/load-rest.js`.
- REST unit tests build real multipart bodies with a `MemoryStream` helper (`BuildMultipartBody`) — follow the same pattern rather than mocking `MultipartReader`.
- For handlers that send `IRequest` (non-generic, returns `Task` not `Task<T>`), mock with `.Returns(Task.CompletedTask)`, not `.ReturnsAsync(Unit.Value)` — the latter won't compile against the MediatR v12 signature.

## CI / Release

`.github/workflows/ci.yml` delegates to the shared `codedesignplus/workflows/build-microservice.yaml` (pins microservice name to `ms-filestorage`). Helm charts live in `charts/ms-filestorage-rest` and `charts/ms-filestorage-worker` with per-env `Staging.yaml` / `Production.yaml` overlays. The chart depends on a `ms-base` sub-chart whose version is pinned in `Chart.yaml` — recent commits have been version bumps there.
