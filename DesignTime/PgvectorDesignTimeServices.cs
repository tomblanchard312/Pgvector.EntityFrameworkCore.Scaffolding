using System.Data.Common;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore.Scaffolding;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Pgvector.EntityFrameworkCore.Scaffolding.Scaffolding;
using Pgvector.EntityFrameworkCore.Scaffolding.TypeMapping;

namespace Pgvector.EntityFrameworkCore.Scaffolding.DesignTime;

/// <summary>
/// Design-time services that register pgvector scaffolding extensions for EF Core 9.
/// 
/// When this package is referenced in a project, EF Core's scaffolding tools will
/// automatically discover this class (via <see cref="IDesignTimeServices"/>) and 
/// register all pgvector scaffolding services. This enables:
/// - Type mapping: vector(N) columns map to Pgvector.Vector (not byte[])
/// - Column type preservation: The store type details are retained in the property metadata
/// - Index scaffolding: Index method/operators can be set via model configuration  
/// - DbContext injection: UseVector() is added to OnConfiguring when vector types are present
/// - Optional similarity extensions: Additional helper methods for vector searches
/// 
/// Usage: Simply add the NuGet package to your project. No additional code is needed.
/// The scaffolding tools will automatically pick up the design-time services.
/// </summary>
public class PgvectorDesignTimeServices : IDesignTimeServices
{
    public void ConfigureDesignTimeServices(IServiceCollection services)
    {
        // Register the pgvector type mapping plugin so that Scaffold-DbContext
        // recognizes vector columns and maps them to Pgvector.Vector instead of byte[]
        // The store type details (e.g., "vector(1536)") are preserved in the property metadata
        services.AddSingleton<IRelationalTypeMappingSourcePlugin, PgvectorTypeMappingSourcePlugin>();

        // Replace the database model factory with our enriched version
        var factoryDescriptor = services.FirstOrDefault(d => d.ServiceType == typeof(IDatabaseModelFactory));
        if (factoryDescriptor != null)
        {
            services.Remove(factoryDescriptor);
            services.AddSingleton<IDatabaseModelFactory>(sp =>
            {
                var originalFactory = (IDatabaseModelFactory)factoryDescriptor.ImplementationFactory!(sp);
                return new PgvectorDatabaseModelFactoryDecorator(sp.GetRequiredService<IRelationalTypeMappingSource>(), originalFactory);
            });
        }
        else
        {
            services.AddSingleton<IDatabaseModelFactory, PgvectorDatabaseModelFactoryDecorator>();
        }

        // Decorate the model code generator to optionally generate vector extensions
        services.AddSingleton<IModelCodeGenerator>(sp =>
        {
            // Get the existing generator (registered by EF Core)
            var inner = (IModelCodeGenerator)sp.GetService(typeof(IModelCodeGenerator));
            return new PgvectorModelCodeGeneratorDecorator(inner);
        });
    }
}
