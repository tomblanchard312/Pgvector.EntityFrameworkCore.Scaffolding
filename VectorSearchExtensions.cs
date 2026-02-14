using System.Linq.Expressions;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Pgvector;
using Pgvector.EntityFrameworkCore;

namespace Pgvector.EntityFrameworkCore.Scaffolding.Extensions;

/// <summary>
/// Defines the distance functions supported by pgvector.
/// </summary>
public enum VectorDistanceFunction
{
    /// <summary>L2 (Euclidean) distance: embedding &lt;-&gt; query</summary>
    L2,

    /// <summary>Cosine distance: embedding &lt;=&gt; query</summary>
    Cosine,

    /// <summary>Inner product (negative): embedding &lt;#&gt; query</summary>
    InnerProduct,

    /// <summary>L1 (Manhattan) distance: embedding &lt;+&gt; query</summary>
    L1
}

/// <summary>
/// Extension methods for querying entities with pgvector columns.
/// These provide a high-level, type-safe API for similarity search
/// that eliminates the need for hand-written distance operator SQL.
/// </summary>
public static class VectorSearchExtensions
{
    /// <summary>
    /// Orders the query by vector similarity (nearest neighbors) and returns the top K results.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="query">The source queryable.</param>
    /// <param name="vectorSelector">Expression selecting the vector property on the entity.</param>
    /// <param name="queryVector">The query vector to compare against.</param>
    /// <param name="k">The number of nearest neighbors to return.</param>
    /// <param name="distanceFunction">The distance function to use (default: L2).</param>
    /// <returns>An IQueryable ordered by similarity, limited to K results.</returns>
    /// <example>
    /// <code>
    /// var similar = await db.Products
    ///     .FindNearest(p => p.Embedding, queryVector, k: 5, VectorDistanceFunction.Cosine)
    ///     .ToListAsync();
    /// </code>
    /// </example>
    public static IQueryable<T> FindNearest<T>(
        this IQueryable<T> query,
        Expression<Func<T, Vector?>> vectorSelector,
        Vector queryVector,
        int k,
        VectorDistanceFunction distanceFunction = VectorDistanceFunction.L2)
        where T : class
    {
        var distanceSelector = BuildDistanceExpression(vectorSelector, queryVector, distanceFunction);
        return query.OrderBy(distanceSelector).Take(k);
    }

    /// <summary>
    /// Orders the query by vector similarity with an additional filter predicate.
    /// Useful for "hybrid search" — combining vector similarity with traditional WHERE filters.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="query">The source queryable.</param>
    /// <param name="vectorSelector">Expression selecting the vector property on the entity.</param>
    /// <param name="queryVector">The query vector to compare against.</param>
    /// <param name="filter">A filter predicate applied before the similarity search.</param>
    /// <param name="k">The number of nearest neighbors to return.</param>
    /// <param name="distanceFunction">The distance function to use (default: L2).</param>
    /// <returns>An IQueryable filtered, ordered by similarity, and limited to K results.</returns>
    /// <example>
    /// <code>
    /// var similar = await db.Products
    ///     .FindNearestWhere(
    ///         p => p.Embedding,
    ///         queryVector,
    ///         p => p.Category == "Electronics",
    ///         k: 10,
    ///         VectorDistanceFunction.Cosine)
    ///     .ToListAsync();
    /// </code>
    /// </example>
    public static IQueryable<T> FindNearestWhere<T>(
        this IQueryable<T> query,
        Expression<Func<T, Vector?>> vectorSelector,
        Vector queryVector,
        Expression<Func<T, bool>> filter,
        int k,
        VectorDistanceFunction distanceFunction = VectorDistanceFunction.L2)
        where T : class
    {
        return query
            .Where(filter)
            .FindNearest(vectorSelector, queryVector, k, distanceFunction);
    }

    /// <summary>
    /// Projects the query to include the distance value alongside each entity.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="query">The source queryable.</param>
    /// <param name="vectorSelector">Expression selecting the vector property on the entity.</param>
    /// <param name="queryVector">The query vector to compare against.</param>
    /// <param name="k">The number of nearest neighbors to return.</param>
    /// <param name="distanceFunction">The distance function to use (default: L2).</param>
    /// <returns>An IQueryable of (Entity, Distance) tuples ordered by similarity.</returns>
    public static IQueryable<VectorSearchResult<T>> FindNearestWithDistance<T>(
        this IQueryable<T> query,
        Expression<Func<T, Vector?>> vectorSelector,
        Vector queryVector,
        int k,
        VectorDistanceFunction distanceFunction = VectorDistanceFunction.L2)
        where T : class
    {
        var (distanceSelector, param) = BuildDistanceExpressionInternal(vectorSelector, queryVector, distanceFunction);
        var entityParam = param;
        var resultInit = Expression.MemberInit(
            Expression.New(typeof(VectorSearchResult<T>)),
            Expression.Bind(typeof(VectorSearchResult<T>).GetProperty("Entity")!, entityParam),
            Expression.Bind(typeof(VectorSearchResult<T>).GetProperty("Distance")!, distanceSelector.Body));
        var selectLambda = Expression.Lambda<Func<T, VectorSearchResult<T>>>(resultInit, entityParam);
        return query.Select(selectLambda).OrderBy(r => r.Distance).Take(k);
    }

    private static Expression<Func<T, double>> BuildDistanceExpression<T>(
        Expression<Func<T, Vector?>> vectorSelector,
        Vector queryVector,
        VectorDistanceFunction distanceFunction)
    {
        var (lambda, _) = BuildDistanceExpressionInternal(vectorSelector, queryVector, distanceFunction);
        return lambda;
    }

    private static (Expression<Func<T, double>>, ParameterExpression) BuildDistanceExpressionInternal<T>(
        Expression<Func<T, Vector?>> vectorSelector,
        Vector queryVector,
        VectorDistanceFunction distanceFunction)
    {
        var methodName = distanceFunction switch
        {
            VectorDistanceFunction.L2 => nameof(VectorDbFunctionsExtensions.L2Distance),
            VectorDistanceFunction.Cosine => nameof(VectorDbFunctionsExtensions.CosineDistance),
            VectorDistanceFunction.InnerProduct => nameof(VectorDbFunctionsExtensions.MaxInnerProduct),
            VectorDistanceFunction.L1 => nameof(VectorDbFunctionsExtensions.L1Distance),
            _ => throw new ArgumentOutOfRangeException(nameof(distanceFunction))
        };

        var method = typeof(VectorDbFunctionsExtensions).GetMethod(methodName, BindingFlags.Public | BindingFlags.Static)
            ?? throw new InvalidOperationException($"Distance method {methodName} not found.");

        var param = vectorSelector.Parameters[0];
        var vectorAccess = vectorSelector.Body;
        var call = Expression.Call(
            method,
            Expression.Convert(vectorAccess, typeof(object)),
            Expression.Constant(queryVector, typeof(object)));

        var lambda = Expression.Lambda<Func<T, double>>(call, param);
        return (lambda, param);
    }

}

/// <summary>
/// Represents a search result that includes both the entity and its distance from the query vector.
/// </summary>
/// <typeparam name="T">The entity type.</typeparam>
public class VectorSearchResult<T> where T : class
{
    /// <summary>The matched entity.</summary>
    public T Entity { get; set; } = default!;

    /// <summary>The distance from the query vector (lower = more similar for L2/Cosine).</summary>
    public double Distance { get; set; }
}
