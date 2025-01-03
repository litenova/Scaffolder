using Microsoft.CodeAnalysis;
using System.Collections.Immutable;
using Scaffolder.Abstractions;

namespace Scaffolder.Specifications;

/// <summary>
/// Implementation of <see cref="IMemberSpecification"/> that uses Roslyn API to analyze type members.
/// This class provides detailed type information for code generation purposes, handling:
/// 
/// Member sources:
/// - Properties of aggregate roots, entities, and value objects
/// - Parameters of domain methods (use cases)
/// - Return types of domain methods (use case results)
/// - Properties of command/query models
/// - Properties of API request/response models
/// 
/// Type analysis capabilities:
/// - Simple types (primitives, enums, system types)
/// - Complex types (domain types, value objects, entities)
/// - Collection types (arrays, generic collections)
/// - Nested types and their members
/// - Async types (Task{T})
/// - Type requirements (required/optional)
/// 
/// Additional features:
/// - XML documentation extraction
/// - Lazy loading of nested members
/// - Proper handling of nullability
/// - Support for generic type resolution
/// </summary>
internal sealed class MemberSpecification : IMemberSpecification
{
    /// <summary>
    /// The original Roslyn symbol (property, parameter, or method) from which this specification was created.
    /// Used primarily for extracting XML documentation and maintaining reference to the original code element.
    /// </summary>
    private readonly ISymbol? _symbol;

    /// <summary>
    /// The semantic model providing type information and symbol resolution capabilities.
    /// Essential for analyzing complex types, resolving type relationships, and handling generics.
    /// </summary>
    private readonly SemanticModel? _semanticModel;

    /// <summary>
    /// Lazy loading container for nested member analysis.
    /// Only performs the expensive operation of analyzing nested members when actually needed.
    /// This is particularly important for complex types with deep hierarchies.
    /// </summary>
    private readonly Lazy<IImmutableSet<IMemberSpecification>?> _nestedMembers;

    /// <inheritdoc/>
    /// <remarks>
    /// The name of the member exactly as it appears in the source code.
    /// Special cases:
    /// - For properties: The property name (e.g., "FirstName", "OrderDate")
    /// - For parameters: The parameter name (e.g., "customerId", "orderItems")
    /// - For return types: Always "Result" by convention
    /// Used in:
    /// - Generated property names
    /// - Parameter names in generated methods
    /// - JSON property names in API models
    /// </remarks>
    public string Name { get; }

    /// <inheritdoc/>
    /// <remarks>
    /// The type specification representing this member's type.
    /// Handles:
    /// - For simple types: The type itself (e.g., "string", "int", "DateTime")
    /// - For complex types: The full type including namespace
    /// - For collections: The collection type (e.g., "List{T}", "IEnumerable{T}")
    /// - For async types: The unwrapped type (e.g., "T" from "Task{T}")
    /// </remarks>
    public ITypeSpecification Type { get; }

    /// <inheritdoc/>
    /// <remarks>
    /// Indicates whether this member must have a value.
    /// Determined by:
    /// - For properties: Required keyword, nullable annotation
    /// - For parameters: Nullable annotation, default value presence
    /// - For return types: Always true (void methods don't create specifications)
    /// Used for:
    /// - Generating validation rules
    /// - Creating API model requirements
    /// - Determining nullable annotations in generated code
    /// </remarks>
    public bool IsRequired { get; }

    /// <inheritdoc/>
    /// <remarks>
    /// Indicates whether this member represents a collection type.
    /// True for:
    /// - Arrays (e.g., string[], int[])
    /// - IEnumerable{T} and derived interfaces
    /// - List{T}, Collection{T}, etc.
    /// - Custom collections implementing IEnumerable{T}
    /// Used for:
    /// - Generating appropriate type declarations
    /// - Creating collection-specific mapping logic
    /// - Handling collection validation
    /// </remarks>
    public bool IsCollection { get; }

    /// <inheritdoc/>
    /// <remarks>
    /// Indicates whether this member represents a complex type.
    /// True for:
    /// - Custom domain types (entities, value objects)
    /// - DTOs and view models
    /// - Any non-system type with properties
    /// False for:
    /// - Primitive types (int, string, etc.)
    /// - System namespace types (DateTime, Guid, etc.)
    /// - Enums
    /// Used for:
    /// - Determining when to generate nested types
    /// - Creating mapping logic
    /// - Handling complex type validation
    /// </remarks>
    public bool IsComplex { get; }

    /// <inheritdoc/>
    /// <remarks>
    /// For collection types, represents the type of elements in the collection.
    /// Examples:
    /// - For List{Customer}: ElementType is Customer
    /// - For OrderItem[]: ElementType is OrderItem
    /// - For IEnumerable{int}: ElementType is int
    /// Null for non-collection types.
    /// </remarks>
    public ITypeSpecification? ElementType { get; }

    /// <inheritdoc/>
    /// <remarks>
    /// Lazily loaded set of nested member specifications.
    /// Only populated for complex types when accessed.
    /// Used for generating nested models and mapping logic.
    /// </remarks>
    public IImmutableSet<IMemberSpecification>? NestedMembers => _nestedMembers.Value;

    /// <inheritdoc/>
    /// <remarks>
    /// XML documentation extracted from the original symbol.
    /// Used for generating documentation in:
    /// - Generated class files
    /// - API documentation
    /// - IntelliSense documentation
    /// Returns empty string if no documentation is available.
    /// </remarks>
    public string XmlDocumentation => _symbol?.GetDocumentationCommentXml() ?? string.Empty;

    /// <summary>
    /// Initializes a new instance from a property symbol.
    /// Used when analyzing properties of aggregate roots, entities, or value objects.
    /// </summary>
    /// <param name="propertySymbol">The Roslyn property symbol to analyze</param>
    /// <param name="semanticModel">The semantic model for type resolution</param>
    public MemberSpecification(IPropertySymbol propertySymbol, SemanticModel semanticModel)
        : this(
            propertySymbol.Name,
            propertySymbol.Type,
            propertySymbol.IsRequired, // Use the required modifier if present
            semanticModel)
    {
        _symbol = propertySymbol;
    }

    /// <summary>
    /// Initializes a new instance from a parameter symbol.
    /// Used when analyzing method parameters in use cases.
    /// </summary>
    /// <param name="parameterSymbol">The Roslyn parameter symbol to analyze</param>
    /// <param name="semanticModel">The semantic model for type resolution</param>
    public MemberSpecification(IParameterSymbol parameterSymbol, SemanticModel semanticModel)
        : this(
            parameterSymbol.Name,
            parameterSymbol.Type,

            // A parameter is required if:
            // 1. It's not marked as nullable AND
            // 2. It doesn't have a default value
            parameterSymbol.NullableAnnotation != NullableAnnotation.Annotated &&
            !parameterSymbol.HasExplicitDefaultValue,
            semanticModel)
    {
        _symbol = parameterSymbol;
    }

    /// <summary>
    /// Initializes a new instance from a method return type.
    /// Used when analyzing use case return types for generating result types.
    /// </summary>
    /// <param name="methodSymbol">The Roslyn method symbol whose return type to analyze</param>
    /// <param name="semanticModel">The semantic model for type resolution</param>
    /// <remarks>
    /// Special considerations:
    /// - Name is always "Result" by convention
    /// - IsRequired is true (void methods should be handled before creating specification)
    /// - Handles unwrapping of Task{T} for async methods
    /// </remarks>
    public MemberSpecification(IMethodSymbol methodSymbol, SemanticModel semanticModel)
        : this(
            "Result", // Convention: return types are named "Result"
            methodSymbol.ReturnType,
            true, // Return types are always required (void methods don't create specs)
            semanticModel)
    {
        _symbol = methodSymbol;
    }

    /// <summary>
    /// Core initialization logic shared by all constructors.
    /// Performs the actual type analysis and member specification setup.
    /// </summary>
    /// <param name="name">The name of the member</param>
    /// <param name="type">The type symbol representing the member's type</param>
    /// <param name="isRequired">Whether the member is required</param>
    /// <param name="semanticModel">The semantic model for type resolution</param>
    private MemberSpecification(string name, ITypeSymbol type, bool isRequired, SemanticModel semanticModel)
    {
        // Validate parameters
        ArgumentNullException.ThrowIfNull(type);
        ArgumentNullException.ThrowIfNull(semanticModel);
        ArgumentException.ThrowIfNullOrEmpty(name);

        _semanticModel = semanticModel;
        Name = name;
        IsRequired = isRequired;

        // Handle async return types by unwrapping Task<T>
        if (IsTaskType(type, out var taskResultType))
        {
            // Use the type argument of Task<T> as the actual type
            Logger.Debug($"Unwrapping Task<T> type: {type} -> {taskResultType}");
            type = taskResultType!; // Non-null because IsTaskType ensures it
        }

        // Perform core type analysis
        (IsCollection, IsComplex, var elementType) = AnalyzeType(type);

        // Create type specifications
        Type = new TypeSpecification(type);
        ElementType = elementType != null ? new TypeSpecification(elementType) : null;

        // Initialize lazy loading of nested members
        _nestedMembers = new Lazy<IImmutableSet<IMemberSpecification>?>(AnalyzeNestedMembers);

        Logger.Debug($"Created member specification: {this}");
    }

    /// <summary>
    /// Determines if a type is Task{T} and extracts its result type.
    /// Handles special case of async method return types.
    /// </summary>
    /// <param name="type">The type to check</param>
    /// <param name="resultType">The type argument of Task{T}, if applicable</param>
    /// <returns>True if the type is Task{T}, false otherwise</returns>
    private static bool IsTaskType(ITypeSymbol type, out ITypeSymbol? resultType)
    {
        resultType = null;

        // Check if type is a generic Task<T>
        if (type is INamedTypeSymbol namedType &&
            namedType.Name == "Task" &&
            namedType.TypeArguments.Length == 1)
        {
            resultType = namedType.TypeArguments[0];
            return true;
        }

        return false;
    }

    /// <summary>
    /// Analyzes a type to determine its characteristics and element type if it's a collection.
    /// Core type analysis logic that determines how the type will be handled in code generation.
    /// </summary>
    /// <param name="type">The type symbol to analyze</param>
    /// <returns>Tuple containing: (IsCollection, IsComplex, ElementType)</returns>
    private (bool IsCollection, bool IsComplex, ITypeSymbol? ElementType) AnalyzeType(ITypeSymbol type)
    {
        // Handle array types (e.g., string[], int[])
        if (type is IArrayTypeSymbol arrayType)
        {
            return (true, true, arrayType.ElementType);
        }

        if (type is INamedTypeSymbol namedType)
        {
            // Check if it's a collection type (e.g., List<T>, IEnumerable<T>)
            if (IsCollectionType(namedType))
            {
                var elementType = namedType.TypeArguments.FirstOrDefault();
                return (true, true, elementType);
            }

            // Check if it's a complex type (class that's not a system type)
            if (namedType.TypeKind == TypeKind.Class &&
                namedType.SpecialType == SpecialType.None)
            {
                return (false, true, null);
            }
        }

        // Simple type (primitive, enum, system type)
        return (false, false, null);
    }

    /// <summary>
    /// Determines if a type is a collection by checking if it implements IEnumerable{T}
    /// or is derived from common collection interfaces.
    /// </summary>
    /// <param name="type">The type to check</param>
    /// <returns>True if the type is a collection, false otherwise</returns>
    private static bool IsCollectionType(INamedTypeSymbol type)
    {
        // Check interfaces that indicate a collection type
        var collectionInterfaces = new[] { "IEnumerable", "ICollection", "IList" };

        // Check if type implements any collection interface
        var implementsCollectionInterface = type.AllInterfaces
            .Any(i => collectionInterfaces.Contains(i.Name));

        // Check if type itself is a collection interface
        var isCollectionType = collectionInterfaces.Contains(type.Name);

        return implementsCollectionInterface || isCollectionType;
    }

    /// <summary>
    /// Analyzes nested members of a complex type.
    /// Only called when NestedMembers property is accessed.
    /// </summary>
    /// <returns>Set of member specifications for nested public properties</returns>
    private IImmutableSet<IMemberSpecification>? AnalyzeNestedMembers()
    {
        // Only analyze nested members for complex types
        if (!IsComplex || _semanticModel == null)
        {
            return null;
        }

        Logger.Debug($"Analyzing nested members for type: {Type.Name}");

        // Get the type symbol to analyze
        var typeSymbol = Type.Name switch
        {
            // For collections, analyze the element type
            _ when IsCollection && ElementType != null =>
                _semanticModel.Compilation.GetTypeByMetadataName(ElementType.FullName),

            // For regular types, analyze the type itself
            _ => _semanticModel.Compilation.GetTypeByMetadataName(Type.FullName)
        };

        if (typeSymbol == null)
        {
            Logger.Warning($"Could not resolve type symbol for {Type.FullName}");
            return ImmutableHashSet<IMemberSpecification>.Empty;
        }

        // Create specifications for all public properties
        var nestedMembers = typeSymbol.GetMembers()
            .OfType<IPropertySymbol>()
            .Where(p => p.DeclaredAccessibility == Accessibility.Public)
            .Select(p => new MemberSpecification(p, _semanticModel))
            .Cast<IMemberSpecification>()
            .ToImmutableHashSet();

        Logger.Debug($"Found {nestedMembers.Count} nested members in {Type.Name}");
        return nestedMembers;
    }

    /// <summary>
    /// Creates a string representation of the member specification.
    /// Useful for debugging and logging purposes.
    /// </summary>
    /// <returns>A string showing the member's name, type, and requirements</returns>
    public override string ToString()
    {
        var typeDescription = IsCollection
            ? $"IEnumerable<{ElementType?.Name ?? "unknown"}>"
            : Type.Name;

        return $"{Name}: {typeDescription}{(IsRequired ? " (required)" : string.Empty)}";
    }
}