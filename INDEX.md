# Pgvector.EntityFrameworkCore.Scaffolding — Project Index

A reference index of the project structure, components, and their relationships.

## Overview

This package fixes pgvector scaffolding in EF Core. When running `Scaffold-DbContext` or `dotnet ef dbcontext scaffold` against PostgreSQL with pgvector columns, it maps `vector(N)`, `halfvec(N)`, and `sparsevec(N)` to the correct Pgvector .NET types instead of `byte[]`.

**Entry point:** `PgvectorDesignTimeServices` implements `IDesignTimeServices` and is auto-discovered by EF Core tooling.

---

## Project Structure

```
Pgvector.EntityFrameworkCore.Scaffolding/
├── DesignTime/
│   └── PgvectorDesignTimeServices.cs    # IDesignTimeServices — registers type mapping plugin
├── TypeMapping/
│   ├── PgvectorTypeMappingSourcePlugin.cs   # Maps store types → CLR types
│   └── PgvectorTypeMappings.cs              # RelationalTypeMapping implementations
├── Extensions/
│   ├── VectorSearchExtensions.cs        # FindNearest, FindNearestWhere, FindNearestWithDistance
│   ├── VectorModelBuilderExtensions.cs  # HasHnswIndex, HasIvfFlatIndex, HasPgvectorExtension
│   ├── VectorIndexExtensions.cs          # CreateHnswIndexAsync, CreateIvfFlatIndexAsync, EnsurePgvectorExtensionAsync
│   └── VectorBatchExtensions.cs         # BatchUpsertEmbeddingsAsync
├── Pgvector.EntityFrameworkCore.Scaffolding.csproj
├── README.md
└── INDEX.md (this file)
```

---

## Component Reference

### 1. Design-Time Services

| File | Purpose |
|------|---------|
| `PgvectorDesignTimeServices.cs` | Registers `PgvectorTypeMappingSourcePlugin` as a singleton. EF Core discovers this via assembly scanning when scaffolding runs. |

### 2. Type Mapping (Scaffolding Core)

| File | Purpose |
|------|---------|
| `PgvectorTypeMappingSourcePlugin.cs` | `IRelationalTypeMappingSourcePlugin` that intercepts store type lookups. Matches `vector`, `vector(N)`, `halfvec`, `halfvec(N)`, `sparsevec`, `sparsevec(N)` and returns the appropriate mapping. |
| `PgvectorTypeMappings.cs` | Defines `PgvectorTypeMapping`, `PgvectorHalfVecTypeMapping`, `PgvectorSparseVecTypeMapping` — maps PostgreSQL types to `Vector`, `HalfVector`, `SparseVector`. |

**Type mapping flow:** Scaffolder reads schema → `IRelationalTypeMappingSource` asks plugins → `PgvectorTypeMappingSourcePlugin` returns mapping for vector types → generated code uses `Vector?` instead of `byte[]`.

### 3. Extensions (Runtime)

| File | Class | Key Methods |
|------|-------|-------------|
| `VectorSearchExtensions.cs` | `VectorSearchExtensions` | `FindNearest`, `FindNearestWhere`, `FindNearestWithDistance` — similarity search using L2, Cosine, InnerProduct, L1 |
| `VectorSearchExtensions.cs` | `VectorSearchResult<T>` | Holds `Entity` + `Distance` for `FindNearestWithDistance` |
| `VectorSearchExtensions.cs` | `VectorDistanceFunction` | Enum: L2, Cosine, InnerProduct, L1 |
| `VectorModelBuilderExtensions.cs` | `VectorModelBuilderExtensions` | `HasHnswIndex`, `HasIvfFlatIndex`, `HasPgvectorExtension` — fluent API for OnModelCreating |
| `VectorIndexExtensions.cs` | `VectorIndexExtensions` | `CreateHnswIndexAsync`, `CreateIvfFlatIndexAsync`, `EnsurePgvectorExtensionAsync`, `RecommendIvfFlatLists` |
| `VectorBatchExtensions.cs` | `VectorBatchExtensions` | `BatchUpsertEmbeddingsAsync` — bulk upsert of embeddings via ON CONFLICT |

---

## Dependencies

| Package | Version | Purpose |
|---------|---------|---------|
| Npgsql.EntityFrameworkCore.PostgreSQL | 9.0.1 | PostgreSQL EF Core provider |
| Pgvector | 0.3.2 | Vector, HalfVector, SparseVector types |
| Pgvector.EntityFrameworkCore | 0.3.0 | EF Core integration, distance methods |
| Microsoft.EntityFrameworkCore.Design | 9.0.0 | Design-time scaffolding support |

---

## Namespaces

| Namespace | Contents |
|-----------|----------|
| `Pgvector.EntityFrameworkCore.Scaffolding.DesignTime` | `PgvectorDesignTimeServices` |
| `Pgvector.EntityFrameworkCore.Scaffolding.TypeMapping` | Plugin + type mappings |
| `Pgvector.EntityFrameworkCore.Scaffolding.Extensions` | Search, model builder, index, batch extensions |

---

## Key Implementation Details

- **Scaffolding:** Uses `IDesignTimeServices` + `IRelationalTypeMappingSourcePlugin` — same pattern as NodaTime/NetTopologySuite.
- **Similarity search:** Uses `VectorDbFunctionsExtensions` from Pgvector.EntityFrameworkCore (L2Distance, CosineDistance, etc.) — translated to SQL by the provider.
- **Index configuration:** `HasIndex(propertyName)` with string overload for EF Core 9 compatibility; HNSW/IVFFlat configured via `HasMethod`, `HasOperators`, `HasStorageParameter`.
