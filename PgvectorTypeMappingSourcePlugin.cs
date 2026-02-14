using Microsoft.EntityFrameworkCore.Storage;
using Pgvector;

namespace Pgvector.EntityFrameworkCore.Scaffolding.TypeMapping;

/// <summary>
/// A type mapping source plugin that maps PostgreSQL vector(N) columns to <see cref="Vector"/>.
/// This plugin is registered during design-time services to enable proper scaffolding of pgvector columns.
/// </summary>
public class PgvectorTypeMappingSourcePlugin : IRelationalTypeMappingSourcePlugin
{
    /// <summary>
    /// Finds a type mapping for the given store type name.
    /// Handles "vector", "vector(N)", "halfvec", "halfvec(N)", "sparsevec", and "sparsevec(N)".
    /// </summary>
    public RelationalTypeMapping? FindMapping(in RelationalTypeMappingInfo mappingInfo)
    {
        var storeTypeName = mappingInfo.StoreTypeName;

        if (string.IsNullOrEmpty(storeTypeName))
            return null;

        var normalizedType = storeTypeName.ToLowerInvariant().Trim();

        // Match vector or vector(N)
        if (normalizedType == "vector" || normalizedType.StartsWith("vector("))
        {
            int? dimensions = ParseDimensions(normalizedType, "vector");
            return new PgvectorTypeMapping(storeTypeName, typeof(Vector), dimensions);
        }

        // Match halfvec or halfvec(N)
        if (normalizedType == "halfvec" || normalizedType.StartsWith("halfvec("))
        {
            int? dimensions = ParseDimensions(normalizedType, "halfvec");
            return new PgvectorHalfVecTypeMapping(storeTypeName, typeof(HalfVector), dimensions);
        }

        // Match sparsevec or sparsevec(N)
        if (normalizedType == "sparsevec" || normalizedType.StartsWith("sparsevec("))
        {
            int? dimensions = ParseDimensions(normalizedType, "sparsevec");
            return new PgvectorSparseVecTypeMapping(storeTypeName, typeof(SparseVector), dimensions);
        }

        return null;
    }

    private static int? ParseDimensions(string storeType, string prefix)
    {
        if (!storeType.StartsWith(prefix + "("))
            return null;

        var inner = storeType[(prefix.Length + 1)..^1];
        return int.TryParse(inner, out var dims) ? dims : null;
    }
}
