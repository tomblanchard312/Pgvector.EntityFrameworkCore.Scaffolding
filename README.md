# Pgvector.EntityFrameworkCore.Scaffolding

[![NuGet](https://img.shields.io/nuget/v/Pgvector.EntityFrameworkCore.Scaffolding.svg)](https://www.nuget.org/packages/Pgvector.EntityFrameworkCore.Scaffolding/)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

**Fixes pgvector scaffolding in EF Core.** When you run `Scaffold-DbContext` or `dotnet ef dbcontext scaffold` against a PostgreSQL database with pgvector columns, EF Core maps them to `byte[]` instead of `Pgvector.Vector`. This package fixes that — and adds similarity search helpers, vector index management, and batch embedding operations.

## The Problem

Without this package, scaffolding a table with a `vector(1536)` column produces:

```csharp
// What EF Core generates by default — broken
public partial class Product
{
    public int Id { get; set; }
    public string Name { get; set; }
    public byte[]? Embedding { get; set; }  // Wrong! Should be Vector
}
```

## The Fix

Install this package, and scaffolding correctly produces:

```csharp
// What this package generates — correct
using Pgvector;

public partial class Product
{
    public int Id { get; set; }
    public string Name { get; set; }
    public Vector? Embedding { get; set; }  // Correctly mapped!
}
```

## Installation

```bash
dotnet add package Pgvector.EntityFrameworkCore.Scaffolding
```

That's it. The package implements `IDesignTimeServices` which EF Core's tooling auto-discovers. No additional configuration is needed.

## Scaffolding

Run scaffolding as normal — vector columns are now handled correctly. Use your project's connection string (from `appsettings.json`, user secrets, or wherever your Entity Framework is configured):

```bash
# ASP.NET Core: use Name= to read from project config
dotnet ef dbcontext scaffold "Name=ConnectionStrings:DefaultConnection" Npgsql.EntityFrameworkCore.PostgreSQL -o Models --no-onconfiguring

# Or pass connection string directly (e.g. from appsettings.json)
dotnet ef dbcontext scaffold "Host=localhost;Database=mydb;Username=myuser;Password=mypassword" Npgsql.EntityFrameworkCore.PostgreSQL -o Models --no-onconfiguring
```

### Supported pgvector Types

| PostgreSQL Type | .NET Type | Example |
|---|---|---|
| `vector(N)` | `Pgvector.Vector` | `vector(1536)` |
| `halfvec(N)` | `Pgvector.HalfVector` | `halfvec(768)` |
| `sparsevec(N)` | `Pgvector.SparseVector` | `sparsevec(1536)` |

## Similarity Search

The package includes extension methods for type-safe similarity search:

### Find Nearest Neighbors

```csharp
using Pgvector.EntityFrameworkCore.Scaffolding.Extensions;

// Find 5 most similar products using cosine distance
var similar = await db.Products
    .FindNearest(
        p => p.Embedding,
        queryVector,
        k: 5,
        VectorDistanceFunction.Cosine)
    .ToListAsync();
```

### Hybrid Search (Vector + Filters)

```csharp
// Combine vector similarity with traditional WHERE filters
var results = await db.Products
    .FindNearestWhere(
        p => p.Embedding,
        queryVector,
        p => p.Category == "Electronics" && p.Price < 500,
        k: 10,
        VectorDistanceFunction.Cosine)
    .ToListAsync();
```

### Get Distances

```csharp
// Get results with their distance scores
var results = await db.Products
    .FindNearestWithDistance(
        p => p.Embedding,
        queryVector,
        k: 10,
        VectorDistanceFunction.L2)
    .ToListAsync();

foreach (var result in results)
{
    Console.WriteLine($"{result.Entity.Name}: distance = {result.Distance:F4}");
}
```

## Vector Index Management

### Fluent API (in OnModelCreating)

```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    // Register the pgvector extension
    modelBuilder.HasPgvectorExtension();

    // Configure HNSW index (recommended for most use cases)
    modelBuilder.Entity<Product>()
        .HasHnswIndex(
            p => p.Embedding,
            distanceOps: "vector_cosine_ops",
            m: 16,
            efConstruction: 64);

    // Or IVFFlat index (faster builds, less memory)
    modelBuilder.Entity<Document>()
        .HasIvfFlatIndex(
            d => d.Embedding,
            distanceOps: "vector_l2_ops",
            lists: 100);
}
```

### Runtime Index Creation

```csharp
// Ensure pgvector extension exists
await db.EnsurePgvectorExtensionAsync();

// Create HNSW index
await db.CreateHnswIndexAsync(
    tableName: "products",
    columnName: "embedding",
    distanceOps: "vector_cosine_ops");

// Create IVFFlat with auto-recommended list count
var rowCount = await db.Products.LongCountAsync();
var lists = VectorIndexExtensions.RecommendIvfFlatLists(rowCount);
await db.CreateIvfFlatIndexAsync("products", "embedding", lists: lists);
```

## Batch Embedding Operations

Optimized for common AI/ML workflows where you compute embeddings and store them in bulk:

```csharp
var embeddings = products.Select(p => (
    Key: (object)p.Id,
    Embedding: new Vector(embeddingService.Generate(p.Description))
));

// Batch upsert — inserts new rows, updates existing embeddings
await db.BatchUpsertEmbeddingsAsync(
    tableName: "products",
    keyColumnName: "id",
    vectorColumnName: "embedding",
    entries: embeddings);
```

## Complete Example

```csharp
using Microsoft.EntityFrameworkCore;
using Pgvector;
using Pgvector.EntityFrameworkCore.Scaffolding.Extensions;

// Scaffolded entity — vector column is correctly typed
public partial class Product
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string? Category { get; set; }
    public decimal Price { get; set; }
    public Vector? Embedding { get; set; }
}

// Usage
var queryEmbedding = new Vector(await embeddingService.GenerateAsync("wireless headphones"));

var recommendations = await db.Products
    .FindNearestWhere(
        p => p.Embedding,
        queryEmbedding,
        p => p.Category == "Electronics" && p.Price < 200,
        k: 5,
        VectorDistanceFunction.Cosine)
    .ToListAsync();
```

## Requirements

- .NET 8.0+
- PostgreSQL with pgvector extension installed
- Npgsql.EntityFrameworkCore.PostgreSQL 9.x
- Pgvector.EntityFrameworkCore 0.3.x

## Docker & Testing

A sample project with Docker is included to verify the package works end-to-end.

### Prerequisites

- [Docker Desktop](https://www.docker.com/products/docker-desktop/) (or Docker Engine + Docker Compose)
- .NET 8 SDK
- EF Core tools: `dotnet tool install -g dotnet-ef`

### Quick Test

From the project root:

```powershell
# Start PostgreSQL with pgvector (port 5433)
docker compose up -d

# Run full test: scaffold + sample app
.\sample\run-test.ps1
```

### Test as NuGet Package

To verify the package works when consumed from NuGet (instead of project reference):

```powershell
.\sample\run-test-nuget.ps1
```

This packs the project, switches the sample to use the local `.nupkg`, runs the full test, then restores the project reference.

### Manual Steps

```powershell
# 1. Start database
docker compose up -d

# 2. Edit sample/SampleApp/appsettings.json with your connection string (default: localhost:5433)
# 3. Scaffold
cd sample\SampleApp
dotnet ef dbcontext scaffold "Host=localhost;Port=5433;Database=pgvector_test;Username=testuser;Password=testpass" Npgsql.EntityFrameworkCore.PostgreSQL -o Models/Scaffolded --force --no-onconfiguring

# 4. Run the sample app
dotnet run
```

### Connection String

The sample uses `sample/SampleApp/appsettings.json`. Default for Docker:

| Setting | Value |
|---------|-------|
| Host | localhost |
| Port | 5433 (mapped from container's 5432) |
| Database | pgvector_test |
| User | testuser |
| Password | testpass |

### What Gets Tested

1. **Scaffolding** — `dotnet ef dbcontext scaffold` produces `Vector?` for vector columns (not `byte[]`)
2. **Runtime** — Sample app queries products and runs similarity search with `L2Distance`

### Stop Docker

```powershell
docker compose down
```

## Troubleshooting

**NU1605 (package downgrade):** Ensure the main project uses Pgvector 0.3.2+ and Npgsql.EntityFrameworkCore.PostgreSQL 9.0.1+ to match Pgvector.EntityFrameworkCore 0.3.0.

**NU1101 (Unable to find package):** The SampleApp uses a project reference by default. If you see this after running `run-test-nuget.ps1`, the script should restore the project reference. Run `dotnet restore` from the project root, or revert `sample/SampleApp/SampleApp.csproj` to use `<ProjectReference Include="..\..\Pgvector.EntityFrameworkCore.Scaffolding.csproj" />` instead of `PackageReference`.

**Stale restore:** Run `dotnet clean` and `dotnet restore` from the project root.

## How It Works

This package implements `IDesignTimeServices` — the same extension point that EF Core uses for NodaTime and NetTopologySuite support. When EF Core's scaffolding tools run, they discover this package's `PgvectorDesignTimeServices` class via assembly scanning. This class registers a custom `IRelationalTypeMappingSourcePlugin` that teaches the scaffolder how to map PostgreSQL's `vector(N)`, `halfvec(N)`, and `sparsevec(N)` types to their corresponding Pgvector .NET types.

No code changes are required in your project — just adding the NuGet package reference is enough.

## License

MIT
