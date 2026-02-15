# EF Core 9 Scaffolding API Research Findings

## Summary: Breaking Changes from EF Core 8 to EF Core 9

Your current code uses **EF Core 8 legacy scaffolding APIs** that have been significantly changed in EF Core 9. The decorating model has been completely redesigned.

---

## 1. Current API Issues in Your Code

### Problem 1: `ICSharpDbContextGenerator` Decorator Pattern is Obsolete

**Your current code:**

```csharp
internal class PgvectorDbContextGeneratorDecorator : ICSharpDbContextGenerator
{
    public string WriteCode(CodegenContext context, ...)
}
```

**Issue:**

- `ICSharpDbContextGenerator` still exists but is NOT meant to be decorated this way
- `CodegenContext` doesn't exist in EF Core 9
- The `WriteCode()` method signature is completely different
- This approach doesn't work with EF Core 9's T4 template-based generation

---

## 2. EF Core 9 Scaffolding Architecture

### New Architecture: T4 Templates

In EF Core 9, DbContext generation uses **T4 (Text Template Transformation Toolkit)** instead of direct code generation:

```
Scaffolding Process:
1. IModel metadata is created
2. T4 CSharpDbContextGenerator template is executed
3. Template calls GetFluentApiCalls() on metadata objects
4. IAnnotationCodeGenerator determines what code to generate
5. Final C# code is output
```

### Key Files in EF Core 9:

- **Location:** `Microsoft.EntityFrameworkCore.Design` NuGet package
- **Namespace:** `Microsoft.EntityFrameworkCore.Scaffolding.Internal` (internal APIs)
- **Template Files:**
  - `CSharpDbContextGenerator.tt` (T4 template source)
  - `CSharpDbContextGenerator.cs` (generated template output)
  - `CSharpEntityTypeGenerator.tt`
  - `CSharpModelGenerator.cs`

---

## 3. IAnnotationCodeGenerator - The CORRECT Extension Point

### What Changed: Complete Method Signature Redesign

**EF Core 8 Pattern (OLD):**

```csharp
public interface IAnnotationCodeGenerator
{
    string GenerateFluentApi(IModel model, IAnnotation annotation);
    string GenerateFluentApi(IProperty property, IAnnotation annotation);
    string GenerateFluentApi(IIndex index, IAnnotation annotation);
    // etc...
}
```

**EF Core 9 Pattern (NEW):**

```csharp
public interface IAnnotationCodeGenerator
{
    // Remove annotations that are handled by convention
    void RemoveAnnotationsHandledByConventions(IModel model,
        IDictionary<string, IAnnotation> annotations);
    void RemoveAnnotationsHandledByConventions(IProperty property,
        IDictionary<string, IAnnotation> annotations);
    // ... more overloads for other metadata types

    // Generate fluent API calls (now returns list, not string)
    IReadOnlyList<MethodCallCodeFragment> GenerateFluentApiCalls(IModel model,
        IDictionary<string, IAnnotation> annotations);
    IReadOnlyList<MethodCallCodeFragment> GenerateFluentApiCalls(IProperty property,
        IDictionary<string, IAnnotation> annotations);
    IReadOnlyList<MethodCallCodeFragment> GenerateFluentApiCalls(IIndex index,
        IDictionary<string, IAnnotation> annotations);
    // ... more overloads for other metadata types

    // Generate data annotation attributes
    IReadOnlyList<AttributeCodeFragment> GenerateDataAnnotationAttributes(
        IProperty property, IDictionary<string, IAnnotation> annotations);
    // ... more overloads
}
```

### Key Differences:

| Aspect                    | EF Core 8                                 | EF Core 9                                                                         |
| ------------------------- | ----------------------------------------- | --------------------------------------------------------------------------------- |
| **Return Type**           | `string`                                  | `IReadOnlyList<MethodCallCodeFragment>` or `IReadOnlyList<AttributeCodeFragment>` |
| **Annotations Parameter** | Single `IAnnotation` object               | `IDictionary<string, IAnnotation>` (entire annotation collection)                 |
| **Method Pattern**        | `GenerateFluentApi(metadata, annotation)` | `GenerateFluentApiCalls(metadata, annotations)`                                   |
| **New Methods**           | N/A                                       | `RemoveAnnotationsHandledByConventions()`                                         |
| **Base Class Available**  | No decorator pattern                      | `AnnotationCodeGenerator` base class with virtual methods                         |

---

## 4. Correct Namespaces for EF Core 9

### Primary Namespaces:

```csharp
// Design-time services registration
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.DependencyInjection;

// Annotation code generation
using Microsoft.EntityFrameworkCore.Design;  // IAnnotationCodeGenerator
using Microsoft.EntityFrameworkCore.Metadata;  // IAnnotation, IModel, IProperty, etc.
using Microsoft.EntityFrameworkCore.Scaffolding.Internal;  // AnnotationCodeGenerator, MethodCallCodeFragment

// Fluent API type mapping
using Microsoft.EntityFrameworkCore.Storage;  // IRelationalTypeMappingSource
using Microsoft.EntityFrameworkCore.Design;  // IRelationalTypeMappingSourcePlugin
```

### Important Type Namespaces:

```csharp
// From Microsoft.EntityFrameworkCore.Design
IAnnotationCodeGenerator - interface for annotation code generation
IDesignTimeServices - entry point for design-time services
AnnotationCodeGenerator - base class (NOT internal in 10.0+, but in 9.0 is internal)

// From Microsoft.EntityFrameworkCore.Metadata
IModel, IEntityType, IProperty, IIndex, IKey, IForeignKey, IAnnotation
IComplexType, IComplexProperty  // New in EF Core 8+

// From Microsoft.EntityFrameworkCore.Scaffolding.Internal
MethodCallCodeFragment - represents a fluent API method call
AttributeCodeFragment - represents a data annotation attribute
AnnotationCodeGeneratorDependencies - dependency container
```

---

## 5. How to Properly Decorate Scaffolding in EF Core 9

### Option A: Decorate IAnnotationCodeGenerator (PREFERRED)

```csharp
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Scaffolding.Internal;

public class PgvectorAnnotationCodeGenerator : IAnnotationCodeGenerator
{
    private readonly IAnnotationCodeGenerator _inner;

    public PgvectorAnnotationCodeGenerator(IAnnotationCodeGenerator inner)
    {
        _inner = inner;
    }

    // Implement ALL IAnnotationCodeGenerator methods
    // The scaffolding system calls these with BOTH the metadata object AND
    // the complete annotations dictionary

    public IReadOnlyList<MethodCallCodeFragment> GenerateFluentApiCalls(
        IModel model,
        IDictionary<string, IAnnotation> annotations)
    {
        var results = new List<MethodCallCodeFragment>();

        // Let inner generator handle standard annotations
        results.AddRange(_inner.GenerateFluentApiCalls(model, annotations));

        // Handle custom pgvector annotations
        if (annotations.TryGetValue("pgvector:IndexMethod", out var indexAnnotation))
        {
            results.Add(new MethodCallCodeFragment("UseVector"));
            annotations.Remove("pgvector:IndexMethod");
        }

        return results;
    }

    public IReadOnlyList<MethodCallCodeFragment> GenerateFluentApiCalls(
        IProperty property,
        IDictionary<string, IAnnotation> annotations)
    {
        var results = new List<MethodCallCodeFragment>();
        results.AddRange(_inner.GenerateFluentApiCalls(property, annotations));

        // Handle pgvector vector type annotations
        if (annotations.TryGetValue("pgvector:StoreType", out var storeTypeAnnotation)
            && storeTypeAnnotation.Value is string storeType)
        {
            results.Add(new MethodCallCodeFragment(
                "HasColumnType",
                storeType));
            annotations.Remove("pgvector:StoreType");
        }

        return results;
    }

    // ... Implement ALL other interface methods by delegating to _inner ...
}
```

### Option B: Inherit from AnnotationCodeGenerator (ALTERNATIVE)

```csharp
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Scaffolding.Internal;

public class PgvectorAnnotationCodeGenerator : AnnotationCodeGenerator
{
    public PgvectorAnnotationCodeGenerator(AnnotationCodeGeneratorDependencies dependencies)
        : base(dependencies)
    {
    }

    // Override virtual methods to add pgvector support
    protected virtual MethodCallCodeFragment? GenerateFluentApi(
        IProperty property,
        IAnnotation annotation)
    {
        if (annotation.Name == "pgvector:StoreType" &&
            annotation.Value is string storeType)
        {
            return new MethodCallCodeFragment("HasColumnType", storeType);
        }

        return base.GenerateFluentApi(property, annotation);
    }
}
```

---

## 6. DbContext Generation: NOT Decoratable Anymore

### The Old Way (EF Core 8 - NO LONGER WORKS):

```csharp
// This doesn't work in EF Core 9!
public class PgvectorDbContextGeneratorDecorator : ICSharpDbContextGenerator
{
    public string WriteCode(CodegenContext context, ...)
}
```

### The New Way (EF Core 9):

**DbContext generation is now handled entirely by T4 templates.** You cannot decorate it the old way.

**Instead, you should:**

1. Use `IAnnotationCodeGenerator.GenerateFluentApiCalls()` to emit `.UseVector()` as part of the OnConfiguring method
2. The T4 template will call `GenerateFluentApiCalls(model, annotations)` on the metadata
3. Return a `MethodCallCodeFragment` that represents `.UseVector()`

**Example:**

```csharp
public IReadOnlyList<MethodCallCodeFragment> GenerateFluentApiCalls(
    IModel model,
    IDictionary<string, IAnnotation> annotations)
{
    var results = new List<MethodCallCodeFragment>();
    results.AddRange(_inner.GenerateFluentApiCalls(model, annotations));

    // If model contains vector types, emit UseVector() in OnConfiguring
    if (ContainsVectorTypes(model))
    {
        // This will be picked up by the T4 template and placed in OnConfiguring
        results.Add(new MethodCallCodeFragment("UseVector"));
    }

    return results;
}
```

---

## 7. What CodegenContext Was (And What Replaced It)

### CodegenContext (EF Core 8):

- Old opaque context object passed to `WriteCode()`
- Contained the `IModel` and code generation options
- Not officially documented

### What EF Core 9 Uses Instead:

```csharp
// The T4 template receives these parameters directly:
public virtual string TransformText()
{
    // These are injected as template parameters
    var model = Model;                      // IModel
    var options = Options;                  // ModelCodeGenerationOptions
    var namespaceHint = NamespaceHint;     // string

    // The template then calls IAnnotationCodeGenerator methods:
    var annotationCodeGenerator = services.GetRequiredService<IAnnotationCodeGenerator>();
    var fluentApiCalls = key.GetFluentApiCalls(annotationCodeGenerator);
    // ...
}
```

**The key insight:** EF Core 9 uses **extension methods** on metadata that call the `IAnnotationCodeGenerator`:

```csharp
public static class ModelBuilderExtensions
{
    public static IReadOnlyList<MethodCallCodeFragment> GetFluentApiCalls(
        this IModel model,
        IAnnotationCodeGenerator annotationCodeGenerator)
    {
        return annotationCodeGenerator.GenerateFluentApiCalls(model,
            model.GetAnnotations().ToDictionary(a => a.Name));
    }

    public static IReadOnlyList<MethodCallCodeFragment> GetFluentApiCalls(
        this IProperty property,
        IAnnotationCodeGenerator annotationCodeGenerator)
    {
        return annotationCodeGenerator.GenerateFluentApiCalls(property,
            property.GetAnnotations().ToDictionary(a => a.Name));
    }

    // Similar for IIndex, IKey, etc.
}
```

---

## 8. Design-Time Services Registration (Correct Pattern for EF Core 9)

### Correct Implementation:

```csharp
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;

public class PgvectorDesignTimeServices : IDesignTimeServices
{
    public void ConfigureDesignTimeServices(IServiceCollection services)
    {
        // 1. Register type mapping plugin (this is still correct)
        services.AddSingleton<IRelationalTypeMappingSourcePlugin,
            PgvectorTypeMappingSourcePlugin>();

        // 2. Decorate IAnnotationCodeGenerator with new EF Core 9 API
        // Using the Decorate<T> extension method
        services.Decorate<IAnnotationCodeGenerator>(inner =>
            new PgvectorAnnotationCodeGenerator(inner));

        // 3. DO NOT try to decorate ICSharpDbContextGenerator
        // It's not intended for decoration in the new architecture
        // All DbContext customization goes through IAnnotationCodeGenerator
    }
}
```

### What `Decorate<T>` Actually Does:

The `Decorate<T>` extension method is a helper that:

1. Removes the existing service registration
2. Creates a decorator instance wrapping the original
3. Re-registers the decorator as the implementation

```csharp
// How it's typically implemented:
public static void Decorate<TService>(
    this IServiceCollection services,
    Func<TService, TService> decorator) where TService : class
{
    var wrappedDescriptor = services.FirstOrDefault(
        s => s.ServiceType == typeof(TService));

    if (wrappedDescriptor == null)
        throw new InvalidOperationException($"{typeof(TService).Name} is not registered");

    var objectFactory = ActivatorUtilities.CreateFactory(
        wrappedDescriptor.ImplementationType ?? typeof(TService),
        new[] { typeof(TService) });

    services.Insert(services.IndexOf(wrappedDescriptor),
        new ServiceDescriptor(typeof(TService),
            provider => decorator((TService)objectFactory(
                provider, new[] { provider.CreateInstance(wrappedDescriptor) })),
            wrappedDescriptor.Lifetime));

    services.Remove(wrappedDescriptor);
}
```

---

## 9. Summary: Required Changes for EF Core 9

| Component                      | EF Core 8 API                                                                       | EF Core 9 API                                                                                                            | Action                                                                                   |
| ------------------------------ | ----------------------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------ | ---------------------------------------------------------------------------------------- |
| **Annotation Code Generation** | `IAnnotationCodeGenerator` with `GenerateFluentApi(metadata, annotation) -> string` | `IAnnotationCodeGenerator` with `GenerateFluentApiCalls(metadata, annotations) -> IReadOnlyList<MethodCallCodeFragment>` | **REWRITE**: Change all method signatures, return `MethodCallCodeFragment` lists         |
| **DbContext Generation**       | `ICSharpDbContextGenerator` decorated with `WriteCode(CodegenContext)`              | T4 template that calls `IAnnotationCodeGenerator`                                                                        | **REMOVE**: Cannot decorate DbContext generation; use `IAnnotationCodeGenerator` instead |
| **Type Mapping**               | `IRelationalTypeMappingSourcePlugin`                                                | `IRelationalTypeMappingSourcePlugin` (unchanged)                                                                         | **NO CHANGE**: Keep as-is                                                                |
| **Service Registration**       | `services.Decorate<T>()` with manual composition                                    | `services.Decorate<T>()` helper                                                                                          | **NO CHANGE**: Still works the same way                                                  |

---

## 10. Concrete Example: Pgvector Extension

Here's what your scaffolding services should look like for EF Core 9:

```csharp
/// <summary>Annotation code generator for pgvector support</summary>
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

        // Check for pgvector store type annotation
        if (annotations.TryGetValue("pgvector:StoreType", out var annotation)
            && annotation.Value is string storeType)
        {
            results.Add(new MethodCallCodeFragment(
                nameof(RelationalPropertyBuilderExtensions.HasColumnType),
                storeType));
            annotations.Remove("pgvector:StoreType");
        }

        return results;
    }

    public IReadOnlyList<MethodCallCodeFragment> GenerateFluentApiCalls(
        IIndex index,
        IDictionary<string, IAnnotation> annotations)
    {
        var results = new List<MethodCallCodeFragment>(
            _inner.GenerateFluentApiCalls(index, annotations));

        // Check for pgvector index method annotation
        if (annotations.TryGetValue("pgvector:IndexMethod", out var methodAnnotation)
            && methodAnnotation.Value is string indexMethod)
        {
            results.Add(new MethodCallCodeFragment(
                "HasMethod",
                indexMethod));
            annotations.Remove("pgvector:IndexMethod");
        }

        return results;
    }

    // Implement all other required interface methods
    // Most will simply delegate to _inner
    public void RemoveAnnotationsHandledByConventions(
        IModel model,
        IDictionary<string, IAnnotation> annotations)
        => _inner.RemoveAnnotationsHandledByConventions(model, annotations);

    // ... more method implementations ...
}
```

---

## References

- **EF Core 9 Release Notes:** https://learn.microsoft.com/en-us/ef/core/what-is-new/ef-core-9.0/
- **IAnnotationCodeGenerator Interface:** Microsoft.EntityFrameworkCore.Design
- **T4 Template-Based Generation:** Used internally in EFCore.Design package
- **EF Core Source:** https://github.com/dotnet/efcore/tree/release/9.0/src/EFCore.Design
