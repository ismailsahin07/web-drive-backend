# Web Drive — Backend

The backend for a cloud storage web app (in the spirit of Google Drive / OneDrive), built with **.NET 10** on **Azure**.

This repository contains the backend only. The frontend (Azure Static Web Apps) lives in a separate repository and talks to this API over HTTPS.

---

## Status

The project is being built bottom-up: pure domain code first, Azure integrations last.

| # | Step | Status |
|---|------|--------|
| 0 | Repo foundation: solution, build settings, central package management, SDK pinning | Done |
| 1 | SharedKernel: `Result`/`Error`, `Entity`, `AggregateRoot`, strongly typed IDs, domain events | Done |
| 2 | SharedKernel: messaging contracts (commands, queries, integration events) | Done |
| 3 | SharedKernel: abstractions (`ICurrentUser`) | Done |
| 4 | SharedKernel unit tests (xUnit v3 + Shouldly) | Done |
| 5 | Files module: domain model (`ItemName`, `StorageDrive`, `DriveItem`) | Done |
| 6 | Files module: application use cases (folders, uploads/downloads, rename/move/trash/restore, listings, drive provisioning) | Done |
| 7 | Infrastructure: Cosmos DB + Blob Storage, tested against local emulators | Next |
| 8 | Azure Functions host: HTTP endpoints, authentication middleware | Planned |
| 9 | Sharing and Users modules, background jobs, Bicep infrastructure, CI/CD | Planned |

---

## Architecture

### Style: modular monolith

One deployable (an Azure Functions app) built from independent **modules**. Each module owns its domain, use cases and data, and other modules can only reach it through its public `*.Contracts` project. This keeps the simplicity of a single deployment while preserving clear boundaries; a module can be split into its own service later if it ever needs to be.

| Module | Responsibility |
|--------|----------------|
| **Files** | Drives, folders, files, uploads/downloads, trash, storage quota |
| **Sharing** | Sharing items with users, public share links, permission checks |
| **Users** | User profiles, first-sign-in provisioning, user lookup |

### Azure services

| Concern | Service |
|---------|---------|
| Compute | **Azure Functions** (.NET 10 isolated worker, Flex Consumption) — HTTP API, event triggers, timers, Durable Functions |
| File contents | **Azure Blob Storage** |
| Metadata | **Azure Cosmos DB** (NoSQL API) |
| Identity | **Microsoft Entra External ID** (JWT bearer tokens) |
| Events | **Azure Event Grid** (blob created, malware scan results) |
| Malware scanning | **Microsoft Defender for Storage** |
| Secrets & identity | **Managed Identity** + **Azure Key Vault** |
| Monitoring | **Application Insights** |
| Later | **API Management** (rate limiting), **Azure AI Search** (full-text search) |

### Request flow

```
Browser (SWA) ──JWT──► Azure Functions (HTTP) ──► Module handlers ──► Cosmos DB (metadata)
      │                                                    │
      │                                                    └──► issues short-lived SAS URLs
      │
      └──────── uploads / downloads file bytes directly ────────► Blob Storage
```

File bytes never pass through the API (the *valet key* pattern). The API only authorizes the request and hands out a short-lived SAS URL.

---

## Solution structure

```
web-drive-backend/
├─ web-drive-backend.slnx
├─ global.json                     # pins the .NET 10 SDK line and the test runner
├─ Directory.Build.props           # shared build settings for every project
├─ Directory.Packages.props        # central NuGet package versions
├─ .editorconfig
├─ src/
│  ├─ BuildingBlocks/
│  │  ├─ Drive.SharedKernel/                    # Result/Error, Entity, AggregateRoot, IDs (no dependencies)
│  │  └─ Drive.BuildingBlocks.Infrastructure/   # planned: Cosmos/Blob setup, in-process event bus
│  ├─ Modules/
│  │  ├─ Files/    (Drive.Modules.Files,    Drive.Modules.Files.Contracts)   # domain + application done
│  │  ├─ Sharing/  (Drive.Modules.Sharing,  Drive.Modules.Sharing.Contracts) # planned
│  │  └─ Users/    (Drive.Modules.Users,    Drive.Modules.Users.Contracts)   # planned
│  └─ Host/
│     └─ Drive.Functions/                       # planned: the only deployable
└─ tests/
   ├─ Drive.SharedKernel.UnitTests/             # Result/Error, UserId, Entity, AggregateRoot
   ├─ Drive.Modules.Files.UnitTests/            # domain, handlers (in-memory fakes), DI registration
   ├─ Drive.Modules.*.UnitTests/                # planned for Sharing and Users
   ├─ Drive.IntegrationTests/                   # planned: Cosmos emulator + Azurite via Testcontainers
   └─ Drive.ArchitectureTests/                  # planned: enforces the dependency rules below
```

Inside each module, code is organised as `Domain/`, `Application/` (one folder per use case: command/query, handler, validator) and `Infrastructure/`. Implementation types are `internal`; only `*.Contracts` and the module's `AddXxxModule()` registration method are public.

### Dependency rules

```
Drive.Functions      → module implementations, BuildingBlocks
Module (impl)        → its own .Contracts, other modules' .Contracts only, BuildingBlocks
Module .Contracts    → SharedKernel only
SharedKernel         → nothing
```

These rules are enforced by architecture tests, not just convention.

---

## Key design decisions

### Uploads (valet key pattern)

1. `POST /uploads` — the API checks permissions, the file name and the quota, **reserves** the quota, creates the item as `PendingUpload` and returns a write-only SAS URL.
2. The browser uploads directly to Blob Storage in blocks (resumable for large files).
3. `POST /uploads/{id}/complete` — the API verifies the blob, commits the quota and marks the item `Available`.
4. A blob-created Event Grid trigger completes the upload as a fallback; a timer cleans up uploads abandoned for over 24 hours.

### Data model (Cosmos DB)

| Container | Partition key | Contents |
|-----------|---------------|----------|
| `items` | `/driveId` | One `Drive` document (quota) plus every file and folder in that drive |
| `shares` | `/granteeId` | Share grants ("shared with me" is a single-partition query) |
| `shareLinks` | `/id` | Public link tokens (stored as hashes) |
| `users` | `/id` | User profiles |

- Items store only `parentId`, so moving a folder of any size is a single-document update.
- Blobs are named `{driveId}/{itemId}/{versionId}` — renames and moves never touch Blob Storage.
- A unique key on `nameKey` (`{parentId}/{uppercased, Unicode-normalized name}`) guarantees case-insensitive unique names per folder.
- The drive's quota lives in the same partition as its items, so creating a file and updating used storage happen in one transactional batch.
- Every write uses the document's ETag for optimistic concurrency.

Example item document:

```json
{
  "id": "0199c1e2-...",
  "driveId": "0199a0b4-...",
  "parentId": "0199a0b4-...",
  "kind": "File",
  "name": "report.pdf",
  "nameKey": "0199a0b4-.../REPORT.PDF",
  "status": "Available",
  "size": 1048576,
  "contentType": "application/pdf",
  "blobName": "0199a0b4-.../0199c1e2-.../v1",
  "createdAt": "2026-10-11T09:30:00+00:00",
  "createdBy": "3f2a..."
}
```

### Code conventions

- **Expected failures are values, not exceptions.** Handlers return `Result` / `Result<T>` with a typed `Error` (`Validation`, `NotFound`, `Conflict`, `Forbidden`, `Failure`) that the host maps to HTTP status codes and ProblemDetails. Exceptions are reserved for bugs and infrastructure failures.
- **Strongly typed IDs** (`UserId`, `ItemId`, `DriveId`, ...) instead of raw `Guid`s, so mixing up IDs is a compile error.
- **Entities compare by identity**; aggregates record domain events and carry a concurrency token.
- **No MediatR** — small in-house handler interfaces instead.
- **Time comes from `TimeProvider`** so tests can control it.

---

## Planned API (v1)

| Method | Route | Description |
|--------|-------|-------------|
| `GET` | `/me` | Current user (provisions the profile and drive on first sign-in) |
| `GET` | `/items/{id}/children` | List a folder's contents (paged) |
| `GET` | `/items/{id}/breadcrumbs` | Path from the drive root to the item |
| `POST` | `/folders` | Create a folder |
| `PATCH` | `/items/{id}` | Rename an item |
| `POST` | `/items/{id}/move` | Move an item to another folder |
| `DELETE` | `/items/{id}` | Move an item to the trash |
| `POST` | `/items/{id}/restore` | Restore an item from the trash |
| `POST` | `/uploads` | Start an upload; returns a write-only SAS URL |
| `POST` | `/uploads/{id}/complete` | Confirm an upload |
| `GET` | `/items/{id}/download-url` | Get a read-only SAS URL |
| `POST` | `/items/{id}/shares` | Share an item with a user |
| `POST` | `/items/{id}/links` | Create a public share link |
| `GET` | `/s/{token}` | Resolve a public share link (anonymous) |

---

## Getting started

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- Visual Studio 2026, or VS Code with the C# Dev Kit
- Later steps: Azure Functions Core Tools, Docker (for Azurite and the Cosmos DB emulator)

### Build

```bash
dotnet build
```

The build treats warnings as errors and runs the recommended .NET analyzers, so a clean build means zero warnings.

### Test

```bash
dotnet test
```

Or use **Test Explorer** in Visual Studio (*Test → Test Explorer → Run All*).

Tests use **xUnit v3** on the **Microsoft Testing Platform**: each test project is a small executable, so it needs only the `xunit.v3` package (no `Microsoft.NET.Test.Sdk` or `xunit.runner.visualstudio`). The `"test"` section in `global.json` tells `dotnet test` on the .NET 10 SDK to use it.

---

## Contributing conventions

- **Build settings** go in `Directory.Build.props`, never in individual `.csproj` files.
- **Package versions** go in `Directory.Packages.props`; projects reference packages without a version.
- **Commit messages** follow [Conventional Commits](https://www.conventionalcommits.org/): `feat(files): add CreateFolder use case`, `fix(sharing): ...`, `chore: ...`.
- **New projects** are created with `dotnet new ... -o <path>` (or *Add → New Project* in Visual Studio) so each project gets its own folder.

### Testing conventions

- **One test project per production project**, named `<Project>.UnitTests`, under `tests/`. The production project grants it access to `internal` types with `<InternalsVisibleTo>`.
- **Test names** follow `Method_Scenario_ExpectedResult`, e.g. `Failure_WithNullError_Throws`.
- **Arrange / Act / Assert**, separated by blank lines; one behaviour per test.
- **Assertions** use [Shouldly](https://docs.shouldly.org/) (`result.Value.ShouldBe(42)`).
- **No real clocks or randomness** in assertions: use fixed dates (and `FakeTimeProvider` once handlers take a `TimeProvider`).
- **Test doubles** (small concrete subclasses and fakes) live in a `TestDoubles/` folder and are `internal`.
- A few analyzer rules that do not fit test code (CA1707, CA1515, CA1861) are relaxed for `tests/**` only, in `.editorconfig`.
