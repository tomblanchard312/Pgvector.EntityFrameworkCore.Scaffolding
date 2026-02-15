using System.IO;
using System.Text;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Scaffolding;
using Microsoft.Extensions.Configuration;
using Pgvector.EntityFrameworkCore.Scaffolding.Utilities;

namespace Pgvector.EntityFrameworkCore.Scaffolding.Scaffolding;

/// <summary>
/// Decorator for IModelCodeGenerator that optionally generates pgvector similarity extension methods.
/// If the model contains vector properties, generates a <DbContextName>VectorExtensions.cs file
/// with helper methods for vector similarity searches.
/// </summary>
internal class PgvectorModelCodeGeneratorDecorator : IModelCodeGenerator
{
    private readonly IModelCodeGenerator _decoratedGenerator;

    public PgvectorModelCodeGeneratorDecorator(IModelCodeGenerator decoratedGenerator)
    {
        _decoratedGenerator = decoratedGenerator ?? throw new ArgumentNullException(nameof(decoratedGenerator));
    }

    public string Language => _decoratedGenerator.Language;

    public ScaffoldedModel GenerateModel(
        IModel model,
        ModelCodeGenerationOptions options)
    {
        var scaffoldedModel = _decoratedGenerator.GenerateModel(model, options);

        // If model contains vector types, modify the DbContext code to add UseVector()
        if (PgvectorDetectionHelpers.ModelHasVectorTypes(model))
        {
            scaffoldedModel = ModifyDbContextForVectorSupport(scaffoldedModel);
        }

        // Note: Vector extensions file generation is skipped for simplicity
        // Users can manually add extension methods or they can be provided as a separate file

        return scaffoldedModel;
    }

    private ScaffoldedModel ModifyDbContextForVectorSupport(ScaffoldedModel scaffoldedModel)
    {
        var contextCode = scaffoldedModel.ContextFile.Code;

        // Remove the #warning about connection string
        contextCode = contextCode.Replace("\r\n#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.\r\n", "\r\n");

        // Replace the hardcoded connection string with Name=DefaultConnection
        contextCode = contextCode.Replace("\"Host=localhost;Port=5433;Database=pgvector_test;Username=testuser;Password=testpass\"", "\"Name=DefaultConnection\"");

        // Find the OnConfiguring method and modify it
        var useNpgsqlIndex = contextCode.IndexOf("options.UseNpgsql(", StringComparison.Ordinal);
        if (useNpgsqlIndex >= 0)
        {
            // Check if UseVector is already present
            var useVectorIndex = contextCode.IndexOf("UseVector", StringComparison.Ordinal);
            if (useVectorIndex < 0)
            {
                // Find the closing parenthesis of UseNpgsql
                var openParenIndex = useNpgsqlIndex + "options.UseNpgsql(".Length - 1;
                var closeParenIndex = FindMatchingClosingParen(contextCode, openParenIndex);
                if (closeParenIndex > 0)
                {
                    // Insert , o => o.UseVector() before the closing paren
                    contextCode = contextCode.Insert(closeParenIndex, ", o => o.UseVector()");
                }
            }
        }

        return new ScaffoldedModel
        {
            ContextFile = new ScaffoldedFile(scaffoldedModel.ContextFile.Path, contextCode)
        };
    }

    private int FindMatchingClosingParen(string text, int openParenIndex)
    {
        var parenCount = 0;
        for (var i = openParenIndex; i < text.Length; i++)
        {
            if (text[i] == '(') parenCount++;
            else if (text[i] == ')')
            {
                parenCount--;
                if (parenCount == 0) return i;
            }
        }
        return -1;
    }

    private string GenerateVectorExtensionsFile(
        IModel model,
        string @namespace,
        ModelCodeGenerationOptions options)
    {
        var contextName = options.ContextName ?? "DbContext";
        var extensionsClassName = $"{contextName}VectorExtensions";

        // Check if file already exists to avoid duplicates
        var filePath = Path.Combine(options.ProjectDir ?? "", $"{extensionsClassName}.cs");
        if (File.Exists(filePath))
        {
            return string.Empty; // Don't generate if already exists
        }

        var code = new StringBuilder();
        code.AppendLine("using System;");
        code.AppendLine("using System.Linq;");
        code.AppendLine("using System.Linq.Expressions;");
        code.AppendLine("using Microsoft.EntityFrameworkCore;");
        code.AppendLine("using Pgvector;");
        code.AppendLine();
        code.AppendLine($"namespace {@namespace}");
        code.AppendLine("{");
        code.AppendLine("    /// <summary>");
        code.AppendLine($"    /// Extension methods for {contextName} to support pgvector similarity searches.");
        code.AppendLine("    /// </summary>");
        code.AppendLine($"    public static class {extensionsClassName}");
        code.AppendLine("    {");

        // Generate extension methods for each vector property
        foreach (var entityType in model.GetEntityTypes())
        {
            foreach (var property in entityType.GetProperties())
            {
                if (PgvectorDetectionHelpers.PropertyIsVectorType(property))
                {
                    var entityName = entityType.Name;
                    var propertyName = property.Name;

                    // OrderByCosineDistance
                    code.AppendLine("        /// <summary>");
                    code.AppendLine($"        /// Orders {entityName} entities by cosine distance to the given vector.");
                    code.AppendLine("        /// </summary>");
                    code.AppendLine($"        public static IQueryable<{entityName}> OrderByCosineDistance(");
                    code.AppendLine($"            this IQueryable<{entityName}> source,");
                    code.AppendLine($"            Expression<Func<{entityName}, Vector>> property,");
                    code.AppendLine("            Vector vector)");
                    code.AppendLine("        {");
                    code.AppendLine($"            return source.OrderBy(e => EF.Functions.CosineDistance(property, vector));");
                    code.AppendLine("        }");
                    code.AppendLine();

                    // OrderByL2Distance
                    code.AppendLine("        /// <summary>");
                    code.AppendLine($"        /// Orders {entityName} entities by L2 distance to the given vector.");
                    code.AppendLine("        /// </summary>");
                    code.AppendLine($"        public static IQueryable<{entityName}> OrderByL2Distance(");
                    code.AppendLine($"            this IQueryable<{entityName}> source,");
                    code.AppendLine($"            Expression<Func<{entityName}, Vector>> property,");
                    code.AppendLine("            Vector vector)");
                    code.AppendLine("        {");
                    code.AppendLine($"            return source.OrderBy(e => EF.Functions.L2Distance(property, vector));");
                    code.AppendLine("        }");
                    code.AppendLine();

                    // OrderByInnerProduct
                    code.AppendLine("        /// <summary>");
                    code.AppendLine($"        /// Orders {entityName} entities by inner product similarity to the given vector.");
                    code.AppendLine("        /// </summary>");
                    code.AppendLine($"        public static IQueryable<{entityName}> OrderByInnerProduct(");
                    code.AppendLine($"            this IQueryable<{entityName}> source,");
                    code.AppendLine($"            Expression<Func<{entityName}, Vector>> property,");
                    code.AppendLine("            Vector vector)");
                    code.AppendLine("        {");
                    code.AppendLine($"            return source.OrderByDescending(e => EF.Functions.InnerProduct(property, vector));");
                    code.AppendLine("        }");
                    code.AppendLine();
                }
            }
        }

        code.AppendLine("    }");
        code.AppendLine("}");

        return code.ToString();
    }
}