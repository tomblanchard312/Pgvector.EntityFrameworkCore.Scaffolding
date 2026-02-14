using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Pgvector;

namespace Pgvector.EntityFrameworkCore.Scaffolding.Extensions;

/// <summary>
/// Extension methods for <see cref="ModelBuilder"/> to configure pgvector-specific 
/// model attributes during OnModelCreating. These methods generate the correct 
/// HasMethod/HasOperators/HasStorageParameter calls that pgvector requires.
/// </summary>
public static class VectorModelBuilderExtensions
{
    /// <summary>
    /// Configures an HNSW index on a vector property using the fluent API.
    /// </summary>
    /// <typeparam name="TEntity">The entity type.</typeparam>
    /// <param name="entityBuilder">The entity type builder.</param>
    /// <param name="vectorProperty">Expression selecting the vector property.</param>
    /// <param name="distanceOps">Distance operator class (default: "vector_l2_ops").</param>
    /// <param name="m">Max connections per layer (default: 16).</param>
    /// <param name="efConstruction">Dynamic candidate list size during build (default: 64).</param>
    /// <returns>The entity type builder for chaining.</returns>
    /// <example>
    /// <code>
    /// protected override void OnModelCreating(ModelBuilder modelBuilder)
    /// {
    ///     modelBuilder.Entity&lt;Product&gt;()
    ///         .HasHnswIndex(p => p.Embedding, "vector_cosine_ops");
    /// }
    /// </code>
    /// </example>
    public static EntityTypeBuilder<TEntity> HasHnswIndex<TEntity>(
        this EntityTypeBuilder<TEntity> entityBuilder,
        Expression<Func<TEntity, Vector?>> vectorProperty,
        string distanceOps = "vector_l2_ops",
        int m = 16,
        int efConstruction = 64)
        where TEntity : class
    {
        var propertyName = GetPropertyName(vectorProperty);
        entityBuilder
            .HasIndex(propertyName)
            .HasMethod("hnsw")
            .HasOperators(distanceOps)
            .HasStorageParameter("m", m)
            .HasStorageParameter("ef_construction", efConstruction);

        return entityBuilder;
    }

    /// <summary>
    /// Configures an IVFFlat index on a vector property using the fluent API.
    /// </summary>
    /// <typeparam name="TEntity">The entity type.</typeparam>
    /// <param name="entityBuilder">The entity type builder.</param>
    /// <param name="vectorProperty">Expression selecting the vector property.</param>
    /// <param name="distanceOps">Distance operator class (default: "vector_l2_ops").</param>
    /// <param name="lists">Number of inverted lists (default: 100).</param>
    /// <returns>The entity type builder for chaining.</returns>
    public static EntityTypeBuilder<TEntity> HasIvfFlatIndex<TEntity>(
        this EntityTypeBuilder<TEntity> entityBuilder,
        Expression<Func<TEntity, Vector?>> vectorProperty,
        string distanceOps = "vector_l2_ops",
        int lists = 100)
        where TEntity : class
    {
        var propertyName = GetPropertyName(vectorProperty);
        entityBuilder
            .HasIndex(propertyName)
            .HasMethod("ivfflat")
            .HasOperators(distanceOps)
            .HasStorageParameter("lists", lists);

        return entityBuilder;
    }

    private static string GetPropertyName<TEntity>(Expression<Func<TEntity, Vector?>> vectorProperty)
        where TEntity : class
    {
        if (vectorProperty.Body is MemberExpression member)
            return member.Member.Name;
        throw new ArgumentException("Vector property must be a simple property access expression.", nameof(vectorProperty));
    }

    /// <summary>
    /// Registers the pgvector extension with the model. Call this in OnModelCreating
    /// to ensure the extension is created during migrations.
    /// </summary>
    public static ModelBuilder HasPgvectorExtension(this ModelBuilder modelBuilder)
    {
        modelBuilder.HasPostgresExtension("vector");
        return modelBuilder;
    }
}
