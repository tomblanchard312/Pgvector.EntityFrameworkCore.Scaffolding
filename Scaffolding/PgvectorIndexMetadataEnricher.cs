using System.Data.Common;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Scaffolding.Metadata;
using Pgvector.EntityFrameworkCore.Scaffolding.Scaffolding;

namespace Pgvector.EntityFrameworkCore.Scaffolding.Scaffolding;

/// <summary>
/// Enriches index metadata with pgvector-specific information by querying the database.
/// Adds annotations for index methods (hnsw, ivfflat) and operator classes.
/// </summary>
internal class PgvectorIndexMetadataEnricher
{
    public PgvectorIndexMetadataEnricher()
    {
    }

    /// <summary>
    /// Enriches the database model with pgvector index information.
    /// Queries pg_catalog to find indexes using pgvector methods and operators.
    /// </summary>
    public void EnrichIndexes(DatabaseModel databaseModel, DbConnection connection)
    {
        if (databaseModel == null) throw new ArgumentNullException(nameof(databaseModel));

        var pgvectorIndexes = GetPgvectorIndexes(connection);

        foreach (var indexInfo in pgvectorIndexes)
        {
            var table = databaseModel.Tables.FirstOrDefault(t =>
                t.Schema == indexInfo.Schema && t.Name == indexInfo.TableName);

            if (table == null) continue;

            var index = table.Indexes.FirstOrDefault(i => i.Name == indexInfo.IndexName);
            if (index == null) continue;

            // Add annotations for the index method and operators
            if (!string.IsNullOrEmpty(indexInfo.Method))
            {
                index.SetAnnotation(PgvectorAnnotationNames.IndexMethod, indexInfo.Method);
            }

            if (!string.IsNullOrEmpty(indexInfo.OperatorClass))
            {
                index.SetAnnotation(PgvectorAnnotationNames.IndexOperators, indexInfo.OperatorClass);
            }
        }
    }

    private List<PgvectorIndexInfo> GetPgvectorIndexes(DbConnection connection)
    {
        var indexes = new List<PgvectorIndexInfo>();

        using var command = connection.CreateCommand();
        command.CommandText = @"
            SELECT
                n.nspname AS schema_name,
                t.relname AS table_name,
                i.relname AS index_name,
                am.amname AS method_name,
                opc.opcname AS operator_class
            FROM pg_index idx
            JOIN pg_class i ON idx.indexrelid = i.oid
            JOIN pg_class t ON idx.indrelid = t.oid
            JOIN pg_namespace n ON t.relnamespace = n.oid
            JOIN pg_am am ON i.relam = am.oid
            JOIN pg_opclass opc ON idx.indclass[0] = opc.oid
            WHERE am.amname IN ('hnsw', 'ivfflat')
               AND opc.opcname LIKE 'vector_%_ops'";

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            indexes.Add(new PgvectorIndexInfo
            {
                Schema = reader.GetString(0),
                TableName = reader.GetString(1),
                IndexName = reader.GetString(2),
                Method = reader.GetString(3),
                OperatorClass = reader.GetString(4)
            });
        }

        return indexes;
    }

    private class PgvectorIndexInfo
    {
        public string Schema { get; set; } = string.Empty;
        public string TableName { get; set; } = string.Empty;
        public string IndexName { get; set; } = string.Empty;
        public string Method { get; set; } = string.Empty;
        public string OperatorClass { get; set; } = string.Empty;
    }
}