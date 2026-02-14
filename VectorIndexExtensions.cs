using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Pgvector.EntityFrameworkCore.Scaffolding.Extensions;

/// <summary>
/// Extension methods for <see cref="DbContext"/> to manage pgvector indexes.
/// </summary>
public static class VectorIndexExtensions
{
    private static readonly Regex ValidIdentifier = new(@"^[a-zA-Z_][a-zA-Z0-9_]*$", RegexOptions.Compiled);

    private static void ValidateIdentifier(string value, string paramName)
    {
        if (string.IsNullOrEmpty(value) || !ValidIdentifier.IsMatch(value))
            throw new ArgumentException($"'{value}' is not a valid identifier. Use only letters, digits, and underscores.", paramName);
    }

    /// <summary>
    /// Creates an HNSW index on a vector column for fast approximate nearest neighbor search.
    /// HNSW is recommended for most use cases — it provides excellent recall and query speed.
    /// </summary>
    /// <param name="context">The DbContext instance.</param>
    /// <param name="tableName">The table containing the vector column.</param>
    /// <param name="columnName">The name of the vector column.</param>
    /// <param name="distanceOps">
    /// The distance operator class. Common values:
    /// "vector_l2_ops" (default), "vector_cosine_ops", "vector_ip_ops"
    /// </param>
    /// <param name="m">Max connections per layer (default: 16). Higher = better recall, more memory.</param>
    /// <param name="efConstruction">Size of dynamic candidate list during build (default: 64). Higher = better recall, slower build.</param>
    /// <param name="schema">Optional schema name (default: "public").</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public static async Task CreateHnswIndexAsync(
        this DbContext context,
        string tableName,
        string columnName,
        string distanceOps = "vector_l2_ops",
        int m = 16,
        int efConstruction = 64,
        string schema = "public",
        CancellationToken cancellationToken = default)
    {
        ValidateIdentifier(tableName, nameof(tableName));
        ValidateIdentifier(columnName, nameof(columnName));
        ValidateIdentifier(distanceOps, nameof(distanceOps));
        ValidateIdentifier(schema, nameof(schema));

        var indexName = $"ix_{tableName}_{columnName}_hnsw";
        var qualifiedTable = schema == "public" ? tableName : $"{schema}.{tableName}";

        var sql = $@"CREATE INDEX IF NOT EXISTS {indexName} 
                     ON {qualifiedTable} 
                     USING hnsw ({columnName} {distanceOps}) 
                     WITH (m = {m}, ef_construction = {efConstruction})";

        await context.Database.ExecuteSqlRawAsync(sql, cancellationToken);
    }

    /// <summary>
    /// Creates an IVFFlat index on a vector column. 
    /// IVFFlat builds faster than HNSW and uses less memory, but has lower query performance.
    /// Recommended when you need faster index builds or have memory constraints.
    /// </summary>
    /// <param name="context">The DbContext instance.</param>
    /// <param name="tableName">The table containing the vector column.</param>
    /// <param name="columnName">The name of the vector column.</param>
    /// <param name="distanceOps">The distance operator class.</param>
    /// <param name="lists">
    /// Number of inverted lists. Rule of thumb: rows / 1000 for up to 1M rows, 
    /// sqrt(rows) for larger tables.
    /// </param>
    /// <param name="schema">Optional schema name.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public static async Task CreateIvfFlatIndexAsync(
        this DbContext context,
        string tableName,
        string columnName,
        string distanceOps = "vector_l2_ops",
        int lists = 100,
        string schema = "public",
        CancellationToken cancellationToken = default)
    {
        ValidateIdentifier(tableName, nameof(tableName));
        ValidateIdentifier(columnName, nameof(columnName));
        ValidateIdentifier(distanceOps, nameof(distanceOps));
        ValidateIdentifier(schema, nameof(schema));

        var indexName = $"ix_{tableName}_{columnName}_ivfflat";
        var qualifiedTable = schema == "public" ? tableName : $"{schema}.{tableName}";

        var sql = $@"CREATE INDEX IF NOT EXISTS {indexName} 
                     ON {qualifiedTable} 
                     USING ivfflat ({columnName} {distanceOps}) 
                     WITH (lists = {lists})";

        await context.Database.ExecuteSqlRawAsync(sql, cancellationToken);
    }

    /// <summary>
    /// Enables the pgvector extension in the database. 
    /// Safe to call multiple times (uses IF NOT EXISTS).
    /// </summary>
    public static async Task EnsurePgvectorExtensionAsync(
        this DbContext context,
        CancellationToken cancellationToken = default)
    {
        await context.Database.ExecuteSqlRawAsync(
            "CREATE EXTENSION IF NOT EXISTS vector",
            cancellationToken);
    }

    /// <summary>
    /// Returns a recommended number of IVFFlat lists based on the row count.
    /// </summary>
    /// <param name="rowCount">The approximate number of rows in the table.</param>
    /// <returns>The recommended number of lists for the IVFFlat index.</returns>
    public static int RecommendIvfFlatLists(long rowCount)
    {
        if (rowCount <= 0)
            return 1;

        if (rowCount <= 1_000_000)
            return Math.Max(1, (int)(rowCount / 1000));

        return (int)Math.Sqrt(rowCount);
    }
}
