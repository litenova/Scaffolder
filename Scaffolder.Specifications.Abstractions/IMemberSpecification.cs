using System.Collections.Immutable;

namespace Scaffolder.Abstractions;

/// <summary>
/// Represents a member specification that can be used for properties, parameters, and model members
/// across different parts of the system (aggregates, entities, value objects, use cases, etc.).
/// 
/// This specification handles:
/// - Basic member information (name, type, requirements)
/// - Complex type structures (nested members, collections)
/// - Type hierarchies (inheritance, abstract/sealed types)
/// - Documentation and metadata
/// 
/// Example hierarchies:
/// 1. PersonalizationField (abstract)
///    ├── EmailPersonalizationField (sealed)
///    │   └── Properties: RequireVerification
///    ├── DatePersonalizationField (sealed)
///    │   └── Properties: MinDate, MaxDate, MinimumAge
///    └── TextPersonalizationField (sealed)
///        └── Properties: MinLength, MaxLength, Pattern
/// 
/// 2. PersonalizationTemplateState (abstract)
///    ├── PersonalizationTemplateDraftState (sealed)
///    ├── PersonalizationTemplateActiveState (sealed)
///    └── PersonalizationTemplateArchivedState (sealed)
/// </summary>
public interface IMemberSpecification
{
    /// <summary>
    /// Gets the name of the member exactly as it appears in the source code.
    /// </summary>
    /// <remarks>
    /// Used for:
    /// - Property names in generated code
    /// - Parameter names in methods
    /// - JSON property names in serialization
    /// 
    /// Examples:
    /// - "Id" for identifier properties
    /// - "Label" for display text
    /// - "RequireVerification" for specific features
    /// </remarks>
    string Name { get; }

    /// <summary>
    /// Gets the type specification of the member.
    /// </summary>
    /// <remarks>
    /// Represents:
    /// - Primitive types (int, string, etc.)
    /// - Complex types (custom classes/records)
    /// - System types (DateTime, Guid, etc.)
    /// - Enums (PersonalizationFieldPrivacyClassification)
    /// 
    /// Used for:
    /// - Type declarations in generated code
    /// - Validation rule generation
    /// - Serialization handling
    /// </remarks>
    ITypeSpecification Type { get; }

    /// <summary>
    /// Gets a value indicating whether this member must have a value.
    /// </summary>
    /// <remarks>
    /// Determined by:
    /// - 'required' keyword in C# (required string Label)
    /// - Nullable annotation (string? vs string)
    /// - Parameter default values
    /// 
    /// Affects:
    /// - Validation rule generation
    /// - API model requirements
    /// - Constructor parameters
    /// </remarks>
    bool IsRequired { get; }

    /// <summary>
    /// Gets a value indicating whether this member represents a collection type.
    /// </summary>
    /// <remarks>
    /// Applies to:
    /// - Arrays (string[])
    /// - Generic collections (IEnumerable{T}, List{T})
    /// - Custom collections
    /// 
    /// Example:
    /// IReadOnlyList{string} Options in ChoicePersonalizationField
    /// </remarks>
    bool IsCollection { get; }

    /// <summary>
    /// Gets a value indicating whether this member represents a complex type.
    /// </summary>
    /// <remarks>
    /// True for:
    /// - Custom domain types
    /// - Value objects
    /// - Nested records/classes
    /// 
    /// False for:
    /// - Primitive types
    /// - System types
    /// - Enums
    /// 
    /// Used to determine if NestedMembers should be analyzed
    /// </remarks>
    bool IsComplex { get; }

    /// <summary>
    /// Gets the element type for collection members.
    /// </summary>
    /// <remarks>
    /// Only relevant when IsCollection is true.
    /// 
    /// Examples:
    /// - string for IEnumerable{string}
    /// - PersonalizationField for IReadOnlyCollection{PersonalizationField}
    /// 
    /// Null when the member is not a collection
    /// </remarks>
    ITypeSpecification? ElementType { get; }

    /// <summary>
    /// Gets the nested members for complex types.
    /// </summary>
    /// <remarks>
    /// Only relevant when IsComplex is true.
    /// 
    /// Contains:
    /// - Public properties of the type
    /// - Inherited properties from base types
    /// - Properties from generic type arguments
    /// 
    /// Null when the type is not complex
    /// </remarks>
    IImmutableSet<IMemberSpecification>? NestedMembers { get; }

    /// <summary>
    /// Gets the XML documentation comment for this member.
    /// </summary>
    /// <remarks>
    /// Used for generating:
    /// - C# XML documentation comments
    /// - API documentation (Swagger/OpenAPI)
    /// - IntelliSense tooltips
    /// 
    /// Returns empty string if no documentation exists
    /// </remarks>
    string XmlDocumentation { get; }

    /// <summary>
    /// Gets a value indicating whether this type is abstract.
    /// </summary>
    /// <remarks>
    /// True for:
    /// - Abstract classes (abstract class PersonalizationField)
    /// - Abstract records (abstract record PersonalizationTemplateState)
    /// 
    /// Affects:
    /// - Code generation strategy
    /// - Type discrimination in serialization
    /// - Factory method generation
    /// </remarks>
    bool IsAbstract { get; }

    /// <summary>
    /// Gets a value indicating whether this type is sealed.
    /// </summary>
    /// <remarks>
    /// True for:
    /// - Sealed classes (sealed class EmailPersonalizationField)
    /// - Sealed records (sealed record PersonalizationTemplateDraftState)
    /// 
    /// Affects:
    /// - Inheritance prevention in generated code
    /// - Optimization opportunities
    /// </remarks>
    bool IsSealed { get; }

    /// <summary>
    /// Gets the derived types of this member, if any.
    /// </summary>
    /// <remarks>
    /// Represents the complete inheritance hierarchy with unlimited levels.
    /// Each derived type is a complete IMemberSpecification with its own:
    /// - Properties (NestedMembers)
    /// - Type information
    /// - Documentation
    /// - Further derived types
    /// 
    /// Example hierarchy:
    /// PersonalizationField
    /// ├── Properties: Id, Label, Description, Required, Order, PrivacyClassification
    /// └── Derived Types:
    ///     ├── EmailPersonalizationField
    ///     │   └── Properties: RequireVerification
    ///     ├── DatePersonalizationField
    ///     │   └── Properties: MinDate, MaxDate, MinimumAge
    ///     ├── TextPersonalizationField
    ///     │   └── Properties: MinLength, MaxLength, Pattern
    ///     └── ChoicePersonalizationField
    ///         └── Properties: Options, AllowMultiple, MinSelections, MaxSelections
    /// 
    /// Used for:
    /// - Generating type hierarchies
    /// - Polymorphic serialization
    /// - Type discrimination
    /// - Factory methods
    /// - Validation rules
    /// </remarks>
    IImmutableSet<IMemberSpecification> DerivedTypes { get; }
}