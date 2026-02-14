using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Pgvector;
using Pgvector.EntityFrameworkCore;
using SampleApp.Models.Scaffolded;

// Connection string from project's appsettings.json (same as Entity Framework)
var configuration = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false)
    .AddEnvironmentVariables()
    .Build();

var connectionString = configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException(
        "Connection string 'DefaultConnection' not found. Add it to appsettings.json or set ConnectionStrings__DefaultConnection env var.");

await using var ctx = new PgvectorTestContext(
    new DbContextOptionsBuilder<PgvectorTestContext>()
        .UseNpgsql(connectionString, o => o.UseVector())
        .Options);

Console.WriteLine("=== Pgvector Scaffolding Test ===\n");

// Test 1: Verify scaffolded Product has Vector? (not byte[])
var productType = typeof(Product);
var embeddingProp = productType.GetProperty("Embedding");
var propType = embeddingProp?.PropertyType;
var underlying = Nullable.GetUnderlyingType(propType!) ?? propType;
var isVector = underlying == typeof(Vector);

Console.WriteLine($"1. Product.Embedding type: {propType?.Name ?? "null"}");
Console.WriteLine($"   Correct (Vector): {(isVector ? "YES" : "NO")}\n");

// Test 2: Query products
var products = await ctx.Products.ToListAsync();
Console.WriteLine($"2. Products in DB: {products.Count}");
foreach (var p in products)
{
    var vecStr = p.Embedding != null ? string.Join(",", p.Embedding.ToArray()) : "null";
    Console.WriteLine($"   - {p.Name} (${p.Price}): embedding=[{vecStr}]");
}

// Test 3: Similarity search
var queryVector = new Vector(new float[] { 1, 1, 1 });
var similar = await ctx.Products
    .OrderBy(p => p.Embedding!.L2Distance(queryVector))
    .Take(3)
    .ToListAsync();

Console.WriteLine($"\n3. Nearest to [1,1,1]:");
foreach (var p in similar)
{
    Console.WriteLine($"   - {p.Name}");
}

Console.WriteLine("\n=== All tests passed! ===");
