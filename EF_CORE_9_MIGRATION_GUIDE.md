# EF Core 9 Migration Guide for Pgvector Scaffolding

## File-by-File Migration Path

### File 1: `PgvectorAnnotationCodeGenerator.cs`

**CURRENT (EF Core 8) - BROKEN:**

```csharp
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Scaffolding.Internal;  // OLD API

internal class PgvectorAnnotationCodeGenerator : IAnnotationCodeGenerator
{
    private readonly IAnnotationCodeGenerator _inner;

    public PgvectorAnnotationCodeGenerator(IAnnotationCodeGenerator inner)
    {
        _inner = inner;
    }

    // EF Core 8 signature - returns string
    public string GenerateFluentApi(IModel model, IAnnotation annotation)
        => _inner.GenerateFluentApi(model, annotation);

    public string GenerateFluentApi(IProperty property, IAnnotation annotation)
        => GeneratePropertyAnnotation(property, annotation)
            ?? _inner.GenerateFluentApi(property, annotation);

    // This no longer works!
    private string? GeneratePropertyAnnotation(IProperty property, IAnnotation annotation)
    {
        if (annotation.Name == "pgvector:StoreType")
        {
            return $@".HasColumnType(""{annotation.Value}"")";  // Wrong!
        }
        return null;
    }
}
```

**CORRECTED (EF Core 9):**

```csharp
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Scaffolding.Internal;

internal class PgvectorAnnotationCodeGenerator : IAnnotationCodeGenerator
{
    private readonly IAnnotationCodeGenerator _inner;

    public PgvectorAnnotationCodeGenerator(IAnnotationCodeGenerator inner)
    {
        _inner = inner;
    }

    // NEW EF Core 9 signature - returns list of MethodCallCodeFragment
    public IReadOnlyList<MethodCallCodeFragment> GenerateFluentApiCalls(
        IModel model,
        IDictionary<string, IAnnotation> annotations)
    {
        var results = new List<MethodCallCodeFragment>(
            _inner.GenerateFluentApiCalls(model, annotations));

        // Handle pgvector-specific model-level annotations here if needed

        return results;
    }

    public IReadOnlyList<MethodCallCodeFragment> GenerateFluentApiCalls(
        IProperty property,
        IDictionary<string, IAnnotation> annotations)
    {
        var results = new List<MethodCallCodeFragment>(
            _inner.GenerateFluentApiCalls(property, annotations));

        // Generate pgvector-specific fluent API calls
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

    public IReadOnlyList<MethodCallCodeFragment> GenerateFluentApiCalls(
        IIndex index,
        IDictionary<string, IAnnotation> annotations)
    {
        var results = new List<MethodCallCodeFragment>(
            _inner.GenerateFluentApiCalls(index, annotations));

        // Handle pgvector index methods
        if (annotations.TryGetValue("pgvector:IndexMethod", out var methodAnnotation)
            && methodAnnotation.Value is string indexMethod)
        {
            results.Add(new MethodCallCodeFragment(
                "HasMethod",
                indexMethod));
            annotations.Remove("pgvector:IndexMethod");
        }

        if (annotations.TryGetValue("pgvector:Operators", out var opAnnotation)
            && opAnnotation.Value is string operators)
        {
            results.Add(new MethodCallCodeFragment(
                "HasOperators",
                operators));
            annotations.Remove("pgvector:Operators");
        }

        return results;
    }

    // IMPLEMENT ALL OTHER REQUIRED INTERFACE METHODS
    // Most will simply delegate to _inner

    // Model annotations
    public void RemoveAnnotationsHandledByConventions(
        IModel model,
        IDictionary<string, IAnnotation> annotations)
        => _inner.RemoveAnnotationsHandledByConventions(model, annotations);

    // Entity type annotations
    public void RemoveAnnotationsHandledByConventions(
        IEntityType entityType,
        IDictionary<string, IAnnotation> annotations)
        => _inner.RemoveAnnotationsHandledByConventions(entityType, annotations);

    public IReadOnlyList<MethodCallCodeFragment> GenerateFluentApiCalls(
        IEntityType entityType,
        IDictionary<string, IAnnotation> annotations)
        => _inner.GenerateFluentApiCalls(entityType, annotations);

    // Complex type annotations
    public void RemoveAnnotationsHandledByConventions(
        IComplexType complexType,
        IDictionary<string, IAnnotation> annotations)
        => _inner.RemoveAnnotationsHandledByConventions(complexType, annotations);

    public IReadOnlyList<MethodCallCodeFragment> GenerateFluentApiCalls(
        IComplexType complexType,
        IDictionary<string, IAnnotation> annotations)
        => _inner.GenerateFluentApiCalls(complexType, annotations);

    // Key annotations
    public void RemoveAnnotationsHandledByConventions(
        IKey key,
        IDictionary<string, IAnnotation> annotations)
        => _inner.RemoveAnnotationsHandledByConventions(key, annotations);

    public IReadOnlyList<MethodCallCodeFragment> GenerateFluentApiCalls(
        IKey key,
        IDictionary<string, IAnnotation> annotations)
        => _inner.GenerateFluentApiCalls(key, annotations);

    // Foreign key annotations
    public void RemoveAnnotationsHandledByConventions(
        IForeignKey foreignKey,
        IDictionary<string, IAnnotation> annotations)
        => _inner.RemoveAnnotationsHandledByConventions(foreignKey, annotations);

    public IReadOnlyList<MethodCallCodeFragment> GenerateFluentApiCalls(
        IForeignKey foreignKey,
        IDictionary<string, IAnnotation> annotations)
        => _inner.GenerateFluentApiCalls(foreignKey, annotations);

    // Navigation annotations
    public void RemoveAnnotationsHandledByConventions(
        INavigation navigation,
        IDictionary<string, IAnnotation> annotations)
        => _inner.RemoveAnnotationsHandledByConventions(navigation, annotations);

    public IReadOnlyList<MethodCallCodeFragment> GenerateFluentApiCalls(
        INavigation navigation,
        IDictionary<string, IAnnotation> annotations)
        => _inner.GenerateFluentApiCalls(navigation, annotations);

    // Skip navigation annotations
    public void RemoveAnnotationsHandledByConventions(
        ISkipNavigation skipNavigation,
        IDictionary<string, IAnnotation> annotations)
        => _inner.RemoveAnnotationsHandledByConventions(skipNavigation, annotations);

    public IReadOnlyList<MethodCallCodeFragment> GenerateFluentApiCalls(
        ISkipNavigation skipNavigation,
        IDictionary<string, IAnnotation> annotations)
        => _inner.GenerateFluentApiCalls(skipNavigation, annotations);

    // Index annotations - already customized above
    public void RemoveAnnotationsHandledByConventions(
        IIndex index,
        IDictionary<string, IAnnotation> annotations)
        => _inner.RemoveAnnotationsHandledByConventions(index, annotations);

    // Property annotations - already customized above
    public void RemoveAnnotationsHandledByConventions(
        IProperty property,
        IDictionary<string, IAnnotation> annotations)
        => _inner.RemoveAnnotationsHandledByConventions(property, annotations);

    // Additional overloads for EFCore 8+ metadata types
    public void RemoveAnnotationsHandledByConventions(
        IComplexProperty complexProperty,
        IDictionary<string, IAnnotation> annotations)
        => _inner.RemoveAnnotationsHandledByConventions(complexProperty, annotations);

    public IReadOnlyList<MethodCallCodeFragment> GenerateFluentApiCalls(
        IComplexProperty complexProperty,
        IDictionary<string, IAnnotation> annotations)
        => _inner.GenerateFluentApiCalls(complexProperty, annotations);

    // Entity type mapping fragment
    public void RemoveAnnotationsHandledByConventions(
        IEntityTypeMappingFragment fragment,
        IDictionary<string, IAnnotation> annotations)
        => _inner.RemoveAnnotationsHandledByConventions(fragment, annotations);

    public IReadOnlyList<MethodCallCodeFragment> GenerateFluentApiCalls(
        IEntityTypeMappingFragment fragment,
        IDictionary<string, IAnnotation> annotations)
        => _inner.GenerateFluentApiCalls(fragment, annotations);

    // Triggers
    public void RemoveAnnotationsHandledByConventions(
        ITrigger trigger,
        IDictionary<string, IAnnotation> annotations)
        => _inner.RemoveAnnotationsHandledByConventions(trigger, annotations);

    public IReadOnlyList<MethodCallCodeFragment> GenerateFluentApiCalls(
        ITrigger trigger,
        IDictionary<string, IAnnotation> annotations)
        => _inner.GenerateFluentApiCalls(trigger, annotations);

    // Check constraints
    public void RemoveAnnotationsHandledByConventions(
        ICheckConstraint checkConstraint,
        IDictionary<string, IAnnotation> annotations)
        => _inner.RemoveAnnotationsHandledByConventions(checkConstraint, annotations);

    public IReadOnlyList<MethodCallCodeFragment> GenerateFluentApiCalls(
        ICheckConstraint checkConstraint,
        IDictionary<string, IAnnotation> annotations)
        => _inner.GenerateFluentApiCalls(checkConstraint, annotations);

    // Sequences
    public void RemoveAnnotationsHandledByConventions(
        ISequence sequence,
        IDictionary<string, IAnnotation> annotations)
        => _inner.RemoveAnnotationsHandledByConventions(sequence, annotations);

    public IReadOnlyList<MethodCallCodeFragment> GenerateFluentApiCalls(
        ISequence sequence,
        IDictionary<string, IAnnotation> annotations)
        => _inner.GenerateFluentApiCalls(sequence, annotations);

    // Relational property overrides
    public void RemoveAnnotationsHandledByConventions(
        IRelationalPropertyOverrides overrides,
        IDictionary<string, IAnnotation> annotations)
        => _inner.RemoveAnnotationsHandledByConventions(overrides, annotations);

    public IReadOnlyList<MethodCallCodeFragment> GenerateFluentApiCalls(
        IRelationalPropertyOverrides overrides,
        IDictionary<string, IAnnotation> annotations)
        => _inner.GenerateFluentApiCalls(overrides, annotations);

    // Generic annotations handler
    public void RemoveAnnotationsHandledByConventions(
        IAnnotatable annotatable,
        IDictionary<string, IAnnotation> annotations)
        => _inner.RemoveAnnotationsHandledByConventions(annotatable, annotations);

    public IReadOnlyList<MethodCallCodeFragment> GenerateFluentApiCalls(
        IAnnotatable annotatable,
        IDictionary<string, IAnnotation> annotations)
        => _inner.GenerateFluentApiCalls(annotatable, annotations);

    // Data annotation attributes
    public IReadOnlyList<AttributeCodeFragment> GenerateDataAnnotationAttributes(
        IEntityType entityType,
        IDictionary<string, IAnnotation> annotations)
        => _inner.GenerateDataAnnotationAttributes(entityType, annotations);

    public IReadOnlyList<AttributeCodeFragment> GenerateDataAnnotationAttributes(
        IProperty property,
        IDictionary<string, IAnnotation> annotations)
        => _inner.GenerateDataAnnotationAttributes(property, annotations);

    public IReadOnlyList<AttributeCodeFragment> GenerateDataAnnotationAttributes(
        IAnnotatable annotatable,
        IDictionary<string, IAnnotation> annotations)
        => _inner.GenerateDataAnnotationAttributes(annotatable, annotations);

    // Filter ignored annotations
    public IEnumerable<IAnnotation> FilterIgnoredAnnotations(
        IEnumerable<IAnnotation> annotations)
        => _inner.FilterIgnoredAnnotations(annotations);
}
```

---

### File 2: `PgvectorDbContextGeneratorDecorator.cs`

**STATUS: DELETE THIS FILE ENTIRELY**

In EF Core 9, you cannot decorate the DbContext generator. The entire generation logic is driven through:

1. T4 templates (internal)
2. `IAnnotationCodeGenerator` for determining what code to generate

Any UseVector() configuration should be emitted through `GenerateFluentApiCalls()` on the model, not by decorating the context generator.

---

### File 3: `PgvectorDesignTimeServices.cs`

**CURRENT (EF Core 8):**

```csharp
services.Decorate<ICSharpDbContextGenerator>(
    inner => new PgvectorDbContextGeneratorDecorator(inner));
```

**CORRECTED (EF Core 9):**

```csharp
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore.Scaffolding;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;

public class PgvectorDesignTimeServices : IDesignTimeServices
{
    public void ConfigureDesignTimeServices(IServiceCollection services)
    {
        // Type mapping plugin - register as before
        services.AddSingleton<IRelationalTypeMappingSourcePlugin,
            PgvectorTypeMappingSourcePlugin>();

        // Decorate annotation code generator with EF Core 9 API
        services.Decorate<IAnnotationCodeGenerator>(
            inner => new PgvectorAnnotationCodeGenerator(inner));

        // REMOVE: This no longer works in EF Core 9
        // services.Decorate<ICSharpDbContextGenerator>(
        //    inner => new PgvectorDbContextGeneratorDecorator(inner));
    }
}
```

---

### File 4: `PgvectorTypeMappings.cs` and `PgvectorTypeMappingSourcePlugin.cs`

**STATUS: NO CHANGES NEEDED**

These files correctly implement `IRelationalTypeMappingSourcePlugin` which is still the correct API in EF Core 9.

---

## Testing the Migration

After making these changes:

1. **Build the project:**

   ```powershell
   dotnet build
   ```

2. **Run scaffolding:**

   ```powershell
   dotnet ef dbcontext scaffold --help
   ```

3. **Test with sample database:**

   ```powershell
   cd sample
   .\run-test.ps1
   ```

4. **Verify generated code includes:**
   - `.HasColumnType("vector(N)")` for vector properties
   - `.HasMethod("hnsw")` or `.HasMethod("ivfflat")` for indexes
   - `.HasOperators("...")` for operator classes
   - `.UseVector()` called in OnConfiguring (if implemented)

---

## Key Changes Summary

| Item                 | EF Core 8                                   | EF Core 9                                              | Migration                                 |
| -------------------- | ------------------------------------------- | ------------------------------------------------------ | ----------------------------------------- |
| Return type          | `string`                                    | `IReadOnlyList<MethodCallCodeFragment>`                | ✅ Change all return types                |
| Method name          | `GenerateFluentApi()`                       | `GenerateFluentApiCalls()`                             | ✅ Rename methods                         |
| Parameter            | Single annotation                           | Dictionary of annotations                              | ✅ Change parameters                      |
| Annotation value     | Return in string `.PropertyName(...)`       | Return `MethodCallCodeFragment("PropertyName", value)` | ✅ Create objects instead of strings      |
| DbContext decoration | Via `ICSharpDbContextGenerator.WriteCode()` | Via `IAnnotationCodeGenerator` model-level calls       | ✅ Move logic to IAnnotationCodeGenerator |
