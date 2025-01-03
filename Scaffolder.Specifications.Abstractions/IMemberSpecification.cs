using System.Collections.Immutable;

namespace Scaffolder.Abstractions;

/// <summary>
/// Represents a member specification that can be used for properties, parameters, and model members
/// across different parts of the system (aggregates, entities, value objects, use cases, etc.).
/// This specification contains all necessary information for code generation.
/// </summary>
public interface IMemberSpecification
{
    /// <summary>
    /// Gets the name of the member.
    /// Examples: "FirstName", "OrderDate", "CustomerId"
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets the type specification of the member.
    /// This represents the actual type of the member, whether it's a primitive type,
    /// complex type, or collection's container type.
    /// </summary>
    ITypeSpecification Type { get; }

    /// <summary>
    /// Gets a value indicating whether this member is required.
    /// Required members will be marked with 'required' keyword in C# 
    /// and will generate appropriate validation rules.
    /// </summary>
    bool IsRequired { get; }

    /// <summary>
    /// Gets a value indicating whether this member represents a collection.
    /// Collections will be generated as IEnumerable, ICollection, or similar types
    /// and will require special handling in mapping and validation.
    /// </summary>
    bool IsCollection { get; }

    /// <summary>
    /// Gets a value indicating whether this member represents a complex type (not a primitive).
    /// Complex types require generation of additional models/types and special mapping handling.
    /// Examples of complex types: Address, Money, CustomerId (value object)
    /// </summary>
    bool IsComplex { get; }

    /// <summary>
    /// Gets the element type for collection members. Only relevant when IsCollection is true.
    /// For example, in IEnumerable{Customer}, the element type would be Customer.
    /// </summary>
    ITypeSpecification? ElementType { get; }

    /// <summary>
    /// Gets the nested members for complex types. Only relevant when IsComplex is true.
    /// These represent the properties of the complex type that need to be generated.
    /// </summary>
    IImmutableSet<IMemberSpecification>? NestedMembers { get; }

    /// <summary>
    /// Gets the XML documentation comment for this member.
    /// This will be used to generate XML documentation in the generated code,
    /// providing IntelliSense information and API documentation.
    /// </summary>
    string XmlDocumentation { get; }
}