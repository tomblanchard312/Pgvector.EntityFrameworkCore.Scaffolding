using System.Data.Common;
using System.Reflection;
using Microsoft.EntityFrameworkCore.Scaffolding;
using Microsoft.EntityFrameworkCore.Scaffolding.Metadata;
using Microsoft.EntityFrameworkCore.Storage;
using Npgsql;
using Pgvector.EntityFrameworkCore.Scaffolding.Scaffolding;
using Pgvector.EntityFrameworkCore.Scaffolding.Utilities;

namespace Pgvector.EntityFrameworkCore.Scaffolding.Scaffolding;

/// <summary>
/// Decorator for IDatabaseModelFactory that enriches the database model with pgvector-specific metadata.
/// Adds store type annotations to properties and index method/operator annotations to indexes.
/// Also marks the model as containing vector types if any are found.
/// </summary>
internal class PgvectorDatabaseModelFactoryDecorator : IDatabaseModelFactory
{
    private readonly IRelationalTypeMappingSource _typeMappingSource;
    private readonly IDatabaseModelFactory _originalFactory;
    private readonly PgvectorIndexMetadataEnricher _indexEnricher;

    public PgvectorDatabaseModelFactoryDecorator(IRelationalTypeMappingSource typeMappingSource, IDatabaseModelFactory originalFactory)
    {
        _typeMappingSource = typeMappingSource;
        _originalFactory = originalFactory ?? throw new ArgumentNullException(nameof(originalFactory));
        _indexEnricher = new PgvectorIndexMetadataEnricher();
    }

    public DatabaseModel Create(string connectionString, DatabaseModelFactoryOptions options)
    {
        // For string connection, we need to create a connection
        // But since we need DbConnection for the enricher, we'll create it
        using var connection = new NpgsqlConnection(connectionString);
        connection.Open();
        return Create(connection, options);
    }

    public DatabaseModel Create(DbConnection connection, DatabaseModelFactoryOptions options)
    {
        // Call the original Npgsql implementation to get the model
        var model = _originalFactory.Create(connection, options);

        // Enrich the model with pgvector metadata
        EnrichModelWithPgvectorMetadata(model, connection);

        return model;
    }

    private void EnrichModelWithPgvectorMetadata(DatabaseModel model, DbConnection connection)
    {
        // Enrich properties with store type annotations
        EnrichPropertiesWithStoreTypes(model, connection);

        // Enrich indexes with pgvector metadata
        _indexEnricher.EnrichIndexes(model, connection);

        // Mark model as having vector types if any found
        if (ModelHasVectorTypes(model))
        {
            model.SetAnnotation(PgvectorAnnotationNames.HasVectorTypes, true);
        }
    }

    private void EnrichPropertiesWithStoreTypes(DatabaseModel model, DbConnection connection)
    {
        foreach (var table in model.Tables)
        {
            foreach (var column in table.Columns)
            {
                var storeType = GetStoreTypeFromDatabase(connection, table.Name, column.Name);
                if (!string.IsNullOrEmpty(storeType))
                {
                    // Set the ColumnType so that scaffolding generates HasColumnType
                    var columnTypeProperty = column.GetType().GetProperty("ColumnType");
                    if (columnTypeProperty != null)
                    {
                        columnTypeProperty.SetValue(column, storeType);
                    }
                }
            }
        }
    }

    private string? GetStoreTypeFromDatabase(DbConnection connection, string tableName, string columnName)
    {
        // Query to get the formatted type from PostgreSQL
        const string query = @"
            SELECT format_type(atttypid, atttypmod) as formatted_type
            FROM pg_attribute
            JOIN pg_class ON pg_attribute.attrelid = pg_class.oid
            JOIN pg_namespace ON pg_class.relnamespace = pg_namespace.oid
            WHERE pg_namespace.nspname = 'public'
            AND pg_class.relname = @tableName
            AND pg_attribute.attname = @columnName
            AND pg_attribute.attnum > 0
            AND NOT pg_attribute.attisdropped";

        using var command = connection.CreateCommand();
        command.CommandText = query;
        var tableParam = command.CreateParameter();
        tableParam.ParameterName = "@tableName";
        tableParam.Value = tableName;
        command.Parameters.Add(tableParam);
        var columnParam = command.CreateParameter();
        columnParam.ParameterName = "@columnName";
        columnParam.Value = columnName;
        command.Parameters.Add(columnParam);

        var result = command.ExecuteScalar();
        return result?.ToString();
    }

    private bool ModelHasVectorTypes(DatabaseModel model)
    {
        return model.Tables.Any(table =>
            table.Columns.Any(column =>
            {
                var columnTypeProperty = column.GetType().GetProperty("ColumnType");
                var columnType = columnTypeProperty?.GetValue(column) as string;
                return !string.IsNullOrEmpty(columnType) && columnType.StartsWith("vector");
            }));
    }
}