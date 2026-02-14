using System.Data;
using Microsoft.EntityFrameworkCore.Storage;
using Pgvector;

namespace Pgvector.EntityFrameworkCore.Scaffolding.TypeMapping;

/// <summary>
/// EF Core type mapping for pgvector's vector type → <see cref="Vector"/>.
/// </summary>
public class PgvectorTypeMapping : RelationalTypeMapping
{
    /// <summary>
    /// The number of dimensions for this vector column, if known.
    /// </summary>
    public int? Dimensions { get; }

    public PgvectorTypeMapping(string storeType, Type clrType, int? dimensions)
        : base(new RelationalTypeMappingParameters(
            new CoreTypeMappingParameters(clrType),
            storeType,
            StoreTypePostfix.None,
            System.Data.DbType.Object))
    {
        Dimensions = dimensions;
    }

    protected PgvectorTypeMapping(RelationalTypeMappingParameters parameters, int? dimensions)
        : base(parameters)
    {
        Dimensions = dimensions;
    }

    protected override RelationalTypeMapping Clone(RelationalTypeMappingParameters parameters)
        => new PgvectorTypeMapping(parameters, Dimensions);
}

/// <summary>
/// EF Core type mapping for pgvector's halfvec type → <see cref="HalfVector"/>.
/// </summary>
public class PgvectorHalfVecTypeMapping : RelationalTypeMapping
{
    public int? Dimensions { get; }

    public PgvectorHalfVecTypeMapping(string storeType, Type clrType, int? dimensions)
        : base(new RelationalTypeMappingParameters(
            new CoreTypeMappingParameters(clrType),
            storeType,
            StoreTypePostfix.None,
            System.Data.DbType.Object))
    {
        Dimensions = dimensions;
    }

    protected PgvectorHalfVecTypeMapping(RelationalTypeMappingParameters parameters, int? dimensions)
        : base(parameters)
    {
        Dimensions = dimensions;
    }

    protected override RelationalTypeMapping Clone(RelationalTypeMappingParameters parameters)
        => new PgvectorHalfVecTypeMapping(parameters, Dimensions);
}

/// <summary>
/// EF Core type mapping for pgvector's sparsevec type → <see cref="SparseVector"/>.
/// </summary>
public class PgvectorSparseVecTypeMapping : RelationalTypeMapping
{
    public int? Dimensions { get; }

    public PgvectorSparseVecTypeMapping(string storeType, Type clrType, int? dimensions)
        : base(new RelationalTypeMappingParameters(
            new CoreTypeMappingParameters(clrType),
            storeType,
            StoreTypePostfix.None,
            System.Data.DbType.Object))
    {
        Dimensions = dimensions;
    }

    protected PgvectorSparseVecTypeMapping(RelationalTypeMappingParameters parameters, int? dimensions)
        : base(parameters)
    {
        Dimensions = dimensions;
    }

    protected override RelationalTypeMapping Clone(RelationalTypeMappingParameters parameters)
        => new PgvectorSparseVecTypeMapping(parameters, Dimensions);
}
