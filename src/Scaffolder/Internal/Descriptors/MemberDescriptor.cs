using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.Extensions.Logging;
using Scaffolder.Utilities;

namespace Scaffolder.Internal.Descriptors;

/// <summary>
/// Represents a descriptor for a member, which can be either a parameter or a property.
/// </summary>
public sealed class MemberDescriptor
{
    /// <summary>
    /// Gets the name of the member.
    /// </summary>
    public RichString Name { get; }

    /// <summary>
    /// Gets the type of the member.
    /// </summary>
    public string Type { get; }

    /// <summary>
    /// Gets a value indicating whether the member is required.
    /// </summary>
    public bool IsRequired { get; }

    /// <summary>
    /// Gets a value indicating whether the member is of a complex type.
    /// </summary>
    public bool IsComplex { get; }

    /// <summary>
    /// Gets the nested members if this member is of a complex type.
    /// </summary>
    public ImmutableArray<MemberDescriptor> NestedMembers { get; }

    /// <summary>
    /// Gets the kind of the member (Parameter or Property).
    /// </summary>
    public MemberKind Kind { get; }

    /// <summary>
    /// Gets a value indicating whether the member is optional.
    /// This is true for parameters that are nullable and have a default value.
    /// </summary>
    public bool IsOptional => !IsRequired && Kind == MemberKind.Parameter;

    /// <summary>
    /// Defines the possible kinds of members.
    /// </summary>
    public enum MemberKind
    {
        /// <summary>
        /// Represents a parameter.
        /// </summary>
        Parameter,

        /// <summary>
        /// Represents a property.
        /// </summary>
        Property
    }

    private MemberDescriptor(
        string name,
        string type,
        bool isRequired,
        bool isComplex,
        ImmutableArray<MemberDescriptor> nestedMembers,
        MemberKind kind)
    {
        Name = name;
        Type = type;
        IsRequired = isRequired;
        IsComplex = isComplex;
        NestedMembers = nestedMembers;
        Kind = kind;
    }

    /// <summary>
    /// Creates a MemberDescriptor from a parameter symbol.
    /// </summary>
    /// <param name="parameterSymbol">The parameter symbol to create the descriptor from.</param>
    /// <param name="logger">The logger to use for logging information about the created descriptor.</param>
    /// <returns>A new MemberDescriptor representing the parameter.</returns>
    public static MemberDescriptor CreateFromParameter(IParameterSymbol parameterSymbol, ILogger logger)
    {
        var isComplex = IsComplexType(parameterSymbol.Type);
        var nestedMembers = isComplex ? ExtractNestedMembers(parameterSymbol.Type, logger) : [];
        var isRequired = parameterSymbol is not { NullableAnnotation: NullableAnnotation.Annotated, HasExplicitDefaultValue: true };

        var descriptor = new MemberDescriptor(
            parameterSymbol.Name,
            parameterSymbol.Type.ToString() ?? throw new InvalidOperationException($"Unable to determine type for parameter {parameterSymbol.Name}"),
            isRequired,
            isComplex,
            nestedMembers,
            MemberKind.Parameter
        );

        logger.LogDebug("Created parameter member: {Name}, Type: {Type}, IsRequired: {IsRequired}, IsComplex: {IsComplex}, NestedMembersCount: {NestedMembersCount}",
            descriptor.Name, descriptor.Type, descriptor.IsRequired, descriptor.IsComplex, descriptor.NestedMembers.Length);

        return descriptor;
    }

    /// <summary>
    /// Creates a MemberDescriptor from a property symbol.
    /// </summary>
    /// <param name="propertySymbol">The property symbol to create the descriptor from.</param>
    /// <param name="logger">The logger to use for logging information about the created descriptor.</param>
    /// <returns>A new MemberDescriptor representing the property.</returns>
    public static MemberDescriptor CreateFromProperty(IPropertySymbol propertySymbol, ILogger logger)
    {
        var isComplex = IsComplexType(propertySymbol.Type);
        var nestedMembers = isComplex ? ExtractNestedMembers(propertySymbol.Type, logger) : [];

        var descriptor = new MemberDescriptor(
            propertySymbol.Name,
            propertySymbol.Type.ToString() ?? throw new InvalidOperationException($"Unable to determine type for property {propertySymbol.Name}"),
            propertySymbol.IsRequired,
            isComplex,
            nestedMembers,
            MemberKind.Property
        );

        logger.LogDebug("Created property member: {Name}, Type: {Type}, IsRequired: {IsRequired}, IsComplex: {IsComplex}, NestedMembersCount: {NestedMembersCount}",
            descriptor.Name, descriptor.Type, descriptor.IsRequired, descriptor.IsComplex, descriptor.NestedMembers.Length);

        return descriptor;
    }

    /// <summary>
    /// Creates multiple MemberDescriptors from a collection of parameter symbols.
    /// </summary>
    /// <param name="parameters">The collection of parameter symbols to create descriptors from.</param>
    /// <param name="logger">The logger to use for logging information about the created descriptors.</param>
    /// <returns>An ImmutableArray of MemberDescriptors representing the parameters.</returns>
    public static ImmutableArray<MemberDescriptor> CreateManyFromParameters(ImmutableArray<IParameterSymbol> parameters, ILogger logger)
    {
        return [.. parameters.Select(param => CreateFromParameter(param, logger))];
    }

    /// <summary>
    /// Creates multiple MemberDescriptors from a collection of property symbols.
    /// </summary>
    /// <param name="properties">The collection of property symbols to create descriptors from.</param>
    /// <param name="logger">The logger to use for logging information about the created descriptors.</param>
    /// <returns>An ImmutableArray of MemberDescriptors representing the properties.</returns>
    public static ImmutableArray<MemberDescriptor> CreateManyFromProperties(IEnumerable<IPropertySymbol> properties, ILogger logger)
    {
        return [.. properties.Select(prop => CreateFromProperty(prop, logger))];
    }

    private static bool IsComplexType(ITypeSymbol typeSymbol)
    {
        if (typeSymbol.IsValueType || typeSymbol.SpecialType != SpecialType.None)
            return false;

        if (typeSymbol is INamedTypeSymbol { TypeArguments.Length: > 0 } namedType)
        {
            return namedType.TypeArguments.Any(IsComplexType);
        }

        return true;
    }

    private static ImmutableArray<MemberDescriptor> ExtractNestedMembers(ITypeSymbol typeSymbol, ILogger logger)
    {
        return
        [
            .. typeSymbol.GetMembers()
                .OfType<IPropertySymbol>()
                .Select(member => CreateFromProperty(member, logger))
        ];
    }
}