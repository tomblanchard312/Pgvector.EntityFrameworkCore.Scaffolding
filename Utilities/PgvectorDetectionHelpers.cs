using Microsoft.EntityFrameworkCore.Metadata;
using Pgvector.EntityFrameworkCore.Scaffolding.Scaffolding;
using Pgvector;
using Pgvector;

namespace Pgvector.EntityFrameworkCore.Scaffolding.Utilities;

/// <summary>
/// Utility methods for detecting pgvector-related elements in EF Core models.
/// </summary>
internal static class PgvectorDetectionHelpers
{
    /// <summary>
    /// Determines if the given model contains any pgvector vector types.
    /// Checks all entity types and their properties for pgvector CLR types.
    /// </summary>
    public static bool ModelHasVectorTypes(IModel model)
    {
        foreach (var entityType in model.GetEntityTypes())
        {
            foreach (var property in entityType.GetProperties())
            {
                if (PropertyIsVectorType(property))
                {
                    return true;
                }
            }
        }
        return false;
    }

    /// <summary>
    /// Determines if the given property is a pgvector vector type.
    /// Checks the CLR type for Vector, HalfVector, or SparseVector.
    /// </summary>
    public static bool PropertyIsVectorType(IProperty property)
    {
        var clrType = property.ClrType;
        return clrType == typeof(Vector) || clrType == typeof(HalfVector) || clrType == typeof(SparseVector);
    }

    /// <summary>
    /// Determines if the given index is a pgvector index.
    /// Checks if the index has pgvector-specific annotations.
    /// </summary>
    public static bool IndexIsPgvectorIndex(IIndex index)
    {
        return index.FindAnnotation(PgvectorAnnotationNames.IndexMethod) != null ||
               index.FindAnnotation(PgvectorAnnotationNames.IndexOperators) != null;
    }
}