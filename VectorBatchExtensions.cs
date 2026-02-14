using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Pgvector;

namespace Pgvector.EntityFrameworkCore.Scaffolding.Extensions;

/// <summary>
/// Extension methods for efficient batch operations on vector columns.
/// Optimized for common embedding workflows where you need to insert/update many vectors at once.
/// </summary>
public static class VectorBatchExtensions
{
    private static readonly Regex ValidIdentifier = new(@"^[a-zA-Z_][a-zA-Z0-9_]*$", RegexOptions.Compiled);

    private static void ValidateIdentifier(string value, string paramName)
    {
        if (string.IsNullOrEmpty(value) || !ValidIdentifier.IsMatch(value))
            throw new ArgumentException($"'{value}' is not a valid identifier. Use only letters, digits, and underscores.", paramName);
    }

    /// <summary>
    /// Performs a batch upsert of embeddings using PostgreSQL's ON CONFLICT DO UPDATE.
    /// This is optimized for the common pattern of computing embeddings and storing them.
    /// </summary>
    /// <param name="context">The DbContext instance.</param>
    /// <param name="tableName">The target table name.</param>
    /// <param name="keyColumnName">The primary key or unique column name to match on. Must be a single column; composite keys are not supported.</param>
    /// <param name="vectorColumnName">The vector column to update.</param>
    /// <param name="entries">The key-vector pairs to upsert.</param>
    /// <param name="schema">Optional schema name.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The number of rows affected.</returns>
    /// <example>
    /// <code>
    /// var embeddings = new List&lt;(object Key, Vector Embedding)&gt;
    /// {
    ///     (1, new Vector(new float[] { 0.1f, 0.2f, 0.3f })),
    ///     (2, new Vector(new float[] { 0.4f, 0.5f, 0.6f })),
    /// };
    /// await db.BatchUpsertEmbeddingsAsync("products", "id", "embedding", embeddings);
    /// </code>
    /// </example>
    public static async Task<int> BatchUpsertEmbeddingsAsync(
        this DbContext context,
        string tableName,
        string keyColumnName,
        string vectorColumnName,
        IEnumerable<(object Key, Vector Embedding)> entries,
        string schema = "public",
        CancellationToken cancellationToken = default)
    {
        ValidateIdentifier(tableName, nameof(tableName));
        ValidateIdentifier(keyColumnName, nameof(keyColumnName));
        ValidateIdentifier(vectorColumnName, nameof(vectorColumnName));
        ValidateIdentifier(schema, nameof(schema));

        var entriesList = entries.ToList();
        if (entriesList.Count == 0)
            return 0;

        var qualifiedTable = schema == "public" ? tableName : $"{schema}.{tableName}";
        var totalAffected = 0;

        // Process in batches of 500 to avoid parameter limits
        const int batchSize = 500;

        for (int i = 0; i < entriesList.Count; i += batchSize)
        {
            var batch = entriesList.Skip(i).Take(batchSize).ToList();
            var valueClauses = new List<string>();
            var parameters = new List<NpgsqlParameter>();

            for (int j = 0; j < batch.Count; j++)
            {
                var keyParam = $"@key_{j}";
                var vecParam = $"@vec_{j}";
                valueClauses.Add($"({keyParam}, {vecParam})");
                parameters.Add(new NpgsqlParameter(keyParam, batch[j].Key));
                parameters.Add(new NpgsqlParameter(vecParam, batch[j].Embedding));
            }

            var sql = $@"INSERT INTO {qualifiedTable} ({keyColumnName}, {vectorColumnName}) 
                         VALUES {string.Join(", ", valueClauses)} 
                         ON CONFLICT ({keyColumnName}) 
                         DO UPDATE SET {vectorColumnName} = EXCLUDED.{vectorColumnName}";

            totalAffected += await context.Database.ExecuteSqlRawAsync(
                sql,
                parameters,
                cancellationToken);
        }

        return totalAffected;
    }
}
