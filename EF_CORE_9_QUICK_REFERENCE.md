# EF Core 9 Scaffolding - Quick Reference

## Correct Using Statements for EF Core 9

```csharp
// Design-time services
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.DependencyInjection;

// Annotation code generation and metadata
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

// Type mapping for custom types
using Microsoft.EntityFrameworkCore.Storage;

// Note: MethodCallCodeFragment, AttributeCodeFragment, and AnnotationCodeGenerator
// are in Microsoft.EntityFrameworkCore.Scaffolding.Internal (INTERNAL namespace)
// You can still reference them but they're not officially part of the public API
```

---

## Key Interface/Class Signatures for EF Core 9

### 1. IAnnotationCodeGenerator (PUBLIC API)

**Namespace:** `Microsoft.EntityFrameworkCore.Design`

**Key Methods (changed from EF Core 8):**

```csharp
public interface IAnnotationCodeGenerator
{
    // Remove annotations handled by convention
    void RemoveAnnotationsHandledByConventions(
        IModel model,
        IDictionary<string, IAnnotation> annotations);

    void RemoveAnnotationsHandledByConventions(
        IProperty property,
        IDictionary<string, IAnnotation> annotations);

    void RemoveAnnotationsHandledByConventions(
        IIndex index,
        IDictionary<string, IAnnotation> annotations);

    // ... (overloads for IEntityType, IComplexType, IForeignKey, IKey, INavigation, etc.)


    // Generate fluent API calls
    IReadOnlyList<MethodCallCodeFragment> GenerateFluentApiCalls(
        IModel model,
        IDictionary<string, IAnnotation> annotations);

    IReadOnlyList<MethodCallCodeFragment> GenerateFluentApiCalls(
        IProperty property,
        IDictionary<string, IAnnotation> annotations);

    IReadOnlyList<MethodCallCodeFragment> GenerateFluentApiCalls(
        IIndex index,
        IDictionary<string, IAnnotation> annotations);

    // ... (overloads for other metadata types)


    // Generate data annotation attributes
    IReadOnlyList<AttributeCodeFragment> GenerateDataAnnotationAttributes(
        IProperty property,
        IDictionary<string, IAnnotation> annotations);

    // ... (more overloads)


    // Filter and manage annotations
    IEnumerable<IAnnotation> FilterIgnoredAnnotations(
        IEnumerable<IAnnotation> annotations);
}
```

---

### 2. MethodCallCodeFragment (INTERNAL but needed)

**Namespace:** `Microsoft.EntityFrameworkCore.Scaffolding.Internal`

```csharp
public class MethodCallCodeFragment
{
    // Constructor
    public MethodCallCodeFragment(string method, params object?[] arguments);

    // Properties
    public string Method { get; }
    public IReadOnlyList<object?> Arguments { get; }
    public IEnumerable<string> GetRequiredUsings();
}
```

---

### 3. AttributeCodeFragment (INTERNAL but needed)

**Namespace:** `Microsoft.EntityFrameworkCore.Scaffolding.Internal`

```csharp
public class AttributeCodeFragment
{
    public AttributeCodeFragment(Type attributeType, params object?[] arguments);

    public Type AttributeType { get; }
    public IReadOnlyList<object?> Arguments { get; }
}
```

---

### 4. AnnotationCodeGenerator Base Class (DEFAULT IMPL)

**Namespace:** `Microsoft.EntityFrameworkCore.Scaffolding.Internal` (in 9.0), will be public in 10.0+

```csharp
public class AnnotationCodeGenerator : IAnnotationCodeGenerator
{
    protected AnnotationCodeGeneratorDependencies Dependencies { get; }

    public virtual void RemoveAnnotationsHandledByConventions(
        IModel model,
        IDictionary<string, IAnnotation> annotations);

    public virtual IReadOnlyList<MethodCallCodeFragment> GenerateFluentApiCalls(
        IModel model,
        IDictionary<string, IAnnotation> annotations);

    // ... more methods

    // Override these to add custom behavior:
    protected virtual bool IsHandledByConvention(IModel model, IAnnotation annotation)
        => false;

    protected virtual MethodCallCodeFragment? GenerateFluentApi(
        IModel model,
        IAnnotation annotation)
        => null;
}
```

---

### 5. IDesignTimeServices (Entry Point)

**Namespace:** `Microsoft.EntityFrameworkCore.Design`

```csharp
public interface IDesignTimeServices
{
    void ConfigureDesignTimeServices(IServiceCollection services);
}
```

---

### 6. IRelationalTypeMappingSourcePlugin (Type Mapping)

**Namespace:** `Microsoft.EntityFrameworkCore.Storage`

```csharp
public interface IRelationalTypeMappingSourcePlugin
{
    RelationalTypeMapping? FindMapping(in RelationalTypeMappingInfo mappingInfo);
}
```

---

## Metadata Interfaces (All in Microsoft.EntityFrameworkCore.Metadata)

```csharp
public interface IModel
{
    IEnumerable<IEntityType> GetEntityTypes();
    IEnumerable<IAnnotation> GetAnnotations();
    IAnnotation? FindAnnotation(string name);
}

public interface IEntityType
{
    string Name { get; }
    IEnumerable<IProperty> GetProperties();
    IEnumerable<IIndex> GetIndexes();
    IEnumerable<IKey> GetKeys();
    IEnumerable<IForeignKey> GetForeignKeys();
    IEnumerable<IAnnotation> GetAnnotations();
}

public interface IProperty
{
    string Name { get; }
    Type ClrType { get; }
    IEnumerable<IAnnotation> GetAnnotations();
}

public interface IIndex
{
    string Name { get; }
    IReadOnlyList<IProperty> Properties { get; }
    IEnumerable<IAnnotation> GetAnnotations();
}

public interface IAnnotation
{
    string Name { get; }
    object? Value { get; }
}
```

---

## Service Registration Pattern for EF Core 9

```csharp
public class PgvectorDesignTimeServices : IDesignTimeServices
{
    public void ConfigureDesignTimeServices(IServiceCollection services)
    {
        // Type mapping plugin - register as singleton
        services.AddSingleton<IRelationalTypeMappingSourcePlugin,
            PgvectorTypeMappingSourcePlugin>();

        // Annotation code generator decorator
        services.Decorate<IAnnotationCodeGenerator>(inner =>
            new PgvectorAnnotationCodeGenerator(inner));
    }
}
```

---

## Return Types Comparison: EF Core 8 vs 9

| Aspect           | EF Core 8                      | EF Core 9                                                   |
| ---------------- | ------------------------------ | ----------------------------------------------------------- |
| Method Name      | `GenerateFluentApi()`          | `GenerateFluentApiCalls()`                                  |
| Return Type      | `string`                       | `IReadOnlyList<MethodCallCodeFragment>`                     |
| Parameter        | `IAnnotation annotation`       | `IDictionary<string, IAnnotation> annotations`              |
| String Output    | `.HasColumnType("vector(10)")` | `new MethodCallCodeFragment("HasColumnType", "vector(10)")` |
| Handles Multiple | One at a time                  | Entire annotation dictionary                                |

---

## Solution: What NOT to Do

❌ **DO NOT implement:**

```csharp
public interface ICSharpDbContextGenerator
{
    string WriteCode(CodegenContext context, string dbContextName,
        string connectionString, bool useMapping);
}
```

This API is been removed from the public scaffolding extension point. Use `IAnnotationCodeGenerator` instead.

❌ **DO NOT use:**

- `CodegenContext` class - doesn't exist in EF Core 9
- Old `GenerateFluentApi()` returning strings
- Decorator pattern on `ICSharpDbContextGenerator`

---

## Solution: What TO Do

✅ **Implement:**

```csharp
public class PgvectorAnnotationCodeGenerator : IAnnotationCodeGenerator
{
    private readonly IAnnotationCodeGenerator _inner;

    public PgvectorAnnotationCodeGenerator(IAnnotationCodeGenerator inner)
    {
        _inner = inner;
    }

    public IReadOnlyList<MethodCallCodeFragment> GenerateFluentApiCalls(
        IProperty property,
        IDictionary<string, IAnnotation> annotations)
    {
        var results = new List<MethodCallCodeFragment>(
            _inner.GenerateFluentApiCalls(property, annotations));

        if (annotations.TryGetValue("pgvector:StoreType", out var annotation)
            && annotation.Value is string storeType)
        {
            results.Add(new MethodCallCodeFragment(
                "HasColumnType",
                storeType));
            annotations.Remove("pgvector:StoreType");
        }

        return results;
    }

    // Implement all other IAnnotationCodeGenerator methods...
    // Most will just delegate to _inner
}
```
