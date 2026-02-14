# Pgvector Scaffolding Sample

Tests the Pgvector.EntityFrameworkCore.Scaffolding package against a PostgreSQL database running in Docker. See the [main README](../README.md#docker--testing) for the full Docker & testing section.

## Prerequisites

- [Docker Desktop](https://www.docker.com/products/docker-desktop/) (or Docker Engine + Docker Compose)
- .NET 8 SDK
- EF Core tools: `dotnet tool install -g dotnet-ef`

## Quick Start

From the project root:

```powershell
# Start PostgreSQL with pgvector
docker compose up -d

# Run full test (scaffold + sample app)
.\sample\run-test.ps1
```

Or run manually:

```powershell
# 1. Start database
docker compose up -d

# 2. Scaffold (pass connection string; run-test.ps1 reads from appsettings.json)
cd sample\SampleApp
dotnet ef dbcontext scaffold "Host=localhost;Port=5433;Database=pgvector_test;Username=testuser;Password=testpass" Npgsql.EntityFrameworkCore.PostgreSQL -o Models/Scaffolded --force --no-onconfiguring

# 3. Run the sample app (uses appsettings.json)
dotnet run
```

## Connection String

The sample uses the project's Entity Framework configuration. Edit `sample/SampleApp/appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5433;Database=pgvector_test;Username=testuser;Password=testpass"
  }
}
```

For production, use `appsettings.Development.json` (gitignored) or user secrets for overrides.

## What Gets Tested

1. **Scaffolding** — `dotnet ef dbcontext scaffold` produces `Vector?` for vector columns (not `byte[]`)
2. **Runtime** — Sample app queries products, runs similarity search with `L2Distance`

## Test as NuGet Package

To verify the package works when consumed from NuGet (instead of project reference):

```powershell
.\sample\run-test-nuget.ps1
```

This packs the project, switches the sample to use the local `.nupkg`, runs the full test, then restores the project reference.

## Stop Docker

```powershell
docker compose down
```
