using Microsoft.EntityFrameworkCore.Storage;
using Pgvector;
using Pgvector.EntityFrameworkCore.Scaffolding.Scaffolding;

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

        var parsed = PgvectorStoreTypeParser.TryParse(storeTypeName);
        if (parsed == null)
            return null;

        return parsed.TypeBase switch
        {
            "vector" => new PgvectorTypeMapping(storeTypeName, typeof(Vector), parsed.Dimensions),
            "halfvec" => new PgvectorHalfVecTypeMapping(storeTypeName, typeof(HalfVector), parsed.Dimensions),
            "sparsevec" => new PgvectorSparseVecTypeMapping(storeTypeName, typeof(SparseVector), parsed.Dimensions),
            _ => null
        };
    }
}
