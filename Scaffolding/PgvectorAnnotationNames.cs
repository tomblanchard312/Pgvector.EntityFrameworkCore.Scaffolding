namespace Pgvector.EntityFrameworkCore.Scaffolding.Scaffolding;

/// <summary>
/// Constants for pgvector-specific annotations used during scaffolding.
/// These annotations are attached to the database model metadata and later
/// converted to fluent API calls by the annotation code generator.
/// </summary>
internal static class PgvectorAnnotationNames
{
    /// <summary>
    /// Annotation key for storing the exact store type of a pgvector column.
    /// Used to generate .HasColumnType("vector(1536)") in the fluent API.
    /// </summary>
    public const string StoreType = "Pgvector:StoreType";

    /// <summary>
    /// Annotation key for the index method (e.g., "hnsw", "ivfflat").
    /// Used to generate .HasMethod("hnsw") in the fluent API.
    /// </summary>
    public const string IndexMethod = "Pgvector:IndexMethod";

    /// <summary>
    /// Annotation key for the index operator class (e.g., "vector_cosine_ops").
    /// Used to generate .HasOperators("vector_cosine_ops") in the fluent API.
    /// </summary>
    public const string IndexOperators = "Pgvector:IndexOperators";

    /// <summary>
    /// Annotation key indicating that the model contains pgvector types.
    /// Used to determine if UseVector() should be injected into the DbContext.
    /// </summary>
    public const string HasVectorTypes = "Pgvector:HasVectorTypes";
}