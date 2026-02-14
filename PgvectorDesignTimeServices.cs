using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Pgvector.EntityFrameworkCore.Scaffolding.TypeMapping;

namespace Pgvector.EntityFrameworkCore.Scaffolding.DesignTime;

/// <summary>
/// Design-time services that register pgvector type mappings for EF Core scaffolding.
/// 
/// When this package is referenced in a project, EF Core's scaffolding tools will
/// automatically discover this class (via <see cref="IDesignTimeServices"/>) and 
/// register the pgvector type mapping plugin. This enables proper scaffolding of 
/// vector(N), halfvec(N), and sparsevec(N) columns.
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
        services.AddSingleton<IRelationalTypeMappingSourcePlugin, PgvectorTypeMappingSourcePlugin>();
    }
}
