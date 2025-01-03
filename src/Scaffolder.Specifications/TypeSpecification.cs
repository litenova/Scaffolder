using Microsoft.CodeAnalysis;
using Scaffolder.Abstractions;

namespace Scaffolder.Specifications;

/// <summary>
/// Implementation of <see cref="ITypeSpecification"/> that represents a .NET type.
/// Can be created from either a Roslyn type symbol or explicit type information.
/// </summary>
internal sealed class TypeSpecification : ITypeSpecification
{
    /// <inheritdoc />
    public string Name { get; }

    /// <inheritdoc />
    public string Namespace { get; }

    /// <inheritdoc />
    public string FullName { get; }

    /// <summary>
    /// Creates a new instance of TypeSpecification from a Roslyn type symbol.
    /// </summary>
    public TypeSpecification(ITypeSymbol typeSymbol)
    {
        // Get the containing namespace, fallback to empty string if global namespace
        Namespace = typeSymbol.ContainingNamespace?.ToDisplayString() ?? string.Empty;
        
        // Get the simple name of the type without namespace
        Name = typeSymbol.Name;
        
        // For generic types, include type parameters in the full name
        // Example: Dictionary<string, int>
        FullName = typeSymbol.ToDisplayString(new SymbolDisplayFormat(
            typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
            genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters));
    }

    /// <summary>
    /// Creates a new instance of TypeSpecification from explicit type information.
    /// </summary>
    public TypeSpecification(string name, string @namespace, string fullName)
    {
        Name = name;
        Namespace = @namespace;
        FullName = fullName;
    }
}