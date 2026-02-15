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

Or run scaffolding manually:

```powershell
# Start database
docker compose up -d

# Run scaffolding with pgvector support
cd sample
.\run-scaffolding.ps1

# Or manually:
cd sample\SampleApp
dotnet ef dbcontext scaffold "Host=localhost;Port=5433;Database=pgvector_test;Username=testuser;Password=testpass" Npgsql.EntityFrameworkCore.PostgreSQL -o Models/Scaffolded --force --no-onconfiguring
```

## What the Scaffolding Does

With the Pgvector.EntityFrameworkCore.Scaffolding package installed, `dotnet ef dbcontext scaffold` will:

- Map `vector(N)` columns to `Pgvector.Vector` (not `byte[]`)
- Preserve vector dimensions: `.HasMaxLength(N)`
- Add index annotations for pgvector indexes (hnsw, ivfflat)
- Inject `UseVector()` into the DbContext OnConfiguring method

Generated code will include:

```csharp
// Model
public partial class Document
{
    public int Id { get; set; }
    public Pgvector.Vector? Embedding { get; set; }  // Correctly mapped
}

// DbContext
protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
{
    optionsBuilder.UseNpgsql(connectionString, o => o.UseVector());  // Auto-injected
}

// Fluent API
modelBuilder.Entity<Document>(entity =>
{
    entity.Property(e => e.Embedding).HasMaxLength(3);  // Dimension preserved
    entity.HasIndex(e => e.Embedding).HasMethod("hnsw").HasOperators("vector_cosine_ops");  // Index metadata
});
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
