using System.Text.RegularExpressions;

namespace Pgvector.EntityFrameworkCore.Scaffolding.Scaffolding;

/// <summary>
/// Utility for parsing pgvector store types and extracting dimension information.
/// Supports vector(N), halfvec(N), sparsevec, and their dimensionless forms.
/// </summary>
internal class PgvectorStoreTypeParser
{
    private static readonly Regex VectorTypeRegex = new(
        @"^(vector|halfvec|sparsevec)(?:\((\d+)\))?$",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public string StoreTypeName { get; }
    public string TypeBase { get; }
    public int? Dimensions { get; }

    private PgvectorStoreTypeParser(string storeTypeName, string typeBase, int? dimensions)
    {
        StoreTypeName = storeTypeName;
        TypeBase = typeBase;
        Dimensions = dimensions;
    }

    /// <summary>
    /// Attempts to parse a store type string.
    /// Returns null if the store type is not a recognized pgvector type.
    /// </summary>
    public static PgvectorStoreTypeParser? TryParse(string storeType)
    {
        if (string.IsNullOrWhiteSpace(storeType))
            return null;

        var match = VectorTypeRegex.Match(storeType.Trim());
        if (!match.Success)
            return null;

        var typeBase = match.Groups[1].Value.ToLowerInvariant();
        var dimensionsStr = match.Groups[2].Value;
        int? dimensions = string.IsNullOrEmpty(dimensionsStr) ? (int?)null : int.Parse(dimensionsStr);

        return new PgvectorStoreTypeParser(storeType, typeBase, dimensions);
    }

    /// <summary>
    /// Determines if a store type is a pgvector vector type.
    /// </summary>
    public static bool IsPgvectorType(string storeType)
    {
        return TryParse(storeType) != null;
    }

    /// <summary>
    /// Gets the preferred C# type name for this store type.
    /// </summary>
    public string GetClrTypeName()
    {
        return TypeBase switch
        {
            "vector" => "Vector",
            "halfvec" => "HalfVector",
            "sparsevec" => "SparseVector",
            _ => "Vector"
        };
    }
}
