using Microsoft.CodeAnalysis;
using System.Collections.Immutable;
using Scaffolder.Abstractions;

namespace Scaffolder.Specifications;

/// <summary>
/// Implementation of <see cref="IAggregateRootSpecification"/> that uses Roslyn API to analyze an aggregate root class.
/// This class is responsible for extracting all relevant information from a domain aggregate root class
/// including its properties, use cases (methods), and type information for code generation purposes.
/// </summary>
internal sealed class AggregateRootSpecification : IAggregateRootSpecification
{
    // Roslyn symbols providing access to the type system and semantic information
    // INamedTypeSymbol represents the aggregate root class itself
    private readonly INamedTypeSymbol _typeSymbol;

    // SemanticModel provides context and type resolution capabilities
    // Used for analyzing type relationships and resolving symbols
    private readonly SemanticModel _semanticModel;

    // Lazy loading of expensive operations to improve performance
    // These are only computed when first accessed
    private readonly Lazy<IImmutableSet<IMemberSpecification>> _members;
    private readonly Lazy<IImmutableSet<IUseCaseSpecification>> _createUseCases;
    private readonly Lazy<IImmutableSet<IUseCaseSpecification>> _readUseCases;
    private readonly Lazy<IImmutableSet<IUseCaseSpecification>> _updateUseCases;
    private readonly Lazy<IImmutableSet<IUseCaseSpecification>> _deleteUseCases;

    // Implementation of ITypeSpecification
    // These properties provide basic type information about the aggregate root

    /// <inheritdoc />
    public string Name => _typeSymbol.Name;

    /// <inheritdoc />
    public string Namespace => _typeSymbol.ContainingNamespace.ToDisplayString();

    /// <inheritdoc />
    public string FullName => _typeSymbol.ToDisplayString();

    // Implementation of IAggregateRootSpecification

    /// <inheritdoc />
    public ITypeSpecification IdType => new TypeSpecification(GetIdType());

    /// <inheritdoc />
    public IImmutableSet<IMemberSpecification> Members => _members.Value;

    /// <inheritdoc />
    public IImmutableSet<IUseCaseSpecification> CreateUseCases => _createUseCases.Value;

    /// <inheritdoc />
    public IImmutableSet<IUseCaseSpecification> ReadUseCases => _readUseCases.Value;

    /// <inheritdoc />
    public IImmutableSet<IUseCaseSpecification> UpdateUseCases => _updateUseCases.Value;

    /// <inheritdoc />
    public IImmutableSet<IUseCaseSpecification> DeleteUseCases => _deleteUseCases.Value;

    /// <summary>
    /// Initializes a new instance of the <see cref="AggregateRootSpecification"/> class.
    /// </summary>
    /// <param name="typeSymbol">Roslyn symbol representing the aggregate root class</param>
    /// <param name="semanticModel">Semantic model providing type resolution and symbol information</param>
    /// <remarks>
    /// The constructor initializes lazy loading for all expensive operations.
    /// Actual analysis is deferred until the respective properties are accessed.
    /// </remarks>
    public AggregateRootSpecification(INamedTypeSymbol typeSymbol, SemanticModel semanticModel)
    {
        _typeSymbol = typeSymbol ?? throw new ArgumentNullException(nameof(typeSymbol));
        _semanticModel = semanticModel ?? throw new ArgumentNullException(nameof(semanticModel));

        // Initialize lazy loading for all analysis operations
        // This ensures we only perform expensive operations when needed
        _members = new Lazy<IImmutableSet<IMemberSpecification>>(AnalyzeMembers);
        _createUseCases = new Lazy<IImmutableSet<IUseCaseSpecification>>(() => AnalyzeUseCases(UseCaseType.Create));
        _readUseCases = new Lazy<IImmutableSet<IUseCaseSpecification>>(() => AnalyzeUseCases(UseCaseType.Read));
        _updateUseCases = new Lazy<IImmutableSet<IUseCaseSpecification>>(() => AnalyzeUseCases(UseCaseType.Update));
        _deleteUseCases = new Lazy<IImmutableSet<IUseCaseSpecification>>(() => AnalyzeUseCases(UseCaseType.Delete));
    }

    /// <summary>
    /// Finds and analyzes the Id property of the aggregate root.
    /// </summary>
    /// <returns>The type symbol representing the type of the Id property</returns>
    /// <remarks>
    /// This method looks for a property that either:
    /// - Is exactly named "Id"
    /// - Ends with "Id" (e.g., "CustomerId", "OrderId")
    /// The property must be public and readable.
    /// </remarks>
    /// <exception cref="InvalidOperationException">Thrown when no valid Id property is found</exception>
    private ITypeSymbol GetIdType()
    {
        Logger.Debug($"Looking for Id property in aggregate root: {Name}");

        // Search through all members of the type for Id property
        var idProperty = _typeSymbol.GetMembers()
            .OfType<IPropertySymbol>()
            .FirstOrDefault(p =>

                // Property must be public and readable
                p.DeclaredAccessibility == Accessibility.Public &&
                p.GetMethod != null &&

                // Match either exact "Id" or ending with "Id"
                (p.Name.Equals("Id", StringComparison.OrdinalIgnoreCase) ||
                 p.Name.EndsWith("Id", StringComparison.OrdinalIgnoreCase)));

        if (idProperty is null)
        {
            Logger.Error($"No Id property found in aggregate root: {Name}");
            throw new InvalidOperationException($"No Id property found in aggregate root {Name}");
        }

        Logger.Debug($"Found Id property of type {idProperty.Type.Name} in aggregate root: {Name}");
        return idProperty.Type;
    }

    /// <summary>
    /// Analyzes the aggregate root class to find and create specifications for all its members (properties).
    /// </summary>
    /// <returns>An immutable set of member specifications</returns>
    /// <remarks>
    /// This method analyzes all public properties of the aggregate root.
    /// Each property is converted into a MemberSpecification for code generation purposes.
    /// </remarks>
    private IImmutableSet<IMemberSpecification> AnalyzeMembers()
    {
        Logger.Debug($"Analyzing members of aggregate root: {Name}");

        // Get all public properties
        var members = _typeSymbol.GetMembers()
            .OfType<IPropertySymbol>()
            .Where(p =>

                // Only include public properties
                p.DeclaredAccessibility == Accessibility.Public &&

                // Exclude static properties
                !p.IsStatic &&

                // Exclude compiler-generated properties
                !p.IsImplicitlyDeclared)
            .Select(p =>
            {
                Logger.Debug($"Creating specification for member: {p.Name}");
                return new MemberSpecification(p, _semanticModel);
            })
            .Cast<IMemberSpecification>()
            .ToImmutableHashSet();

        Logger.Info($"Found {members.Count} members in aggregate root: {Name}");
        return members;
    }

    /// <summary>
    /// Analyzes the aggregate root class to find use cases of a specific type.
    /// </summary>
    /// <param name="useCaseType">The type of use cases to look for</param>
    /// <returns>An immutable set of use case specifications</returns>
    /// <remarks>
    /// This method identifies methods that represent specific types of use cases based on:
    /// - Method name conventions
    /// - Method signature
    /// - Method accessibility
    /// </remarks>
    private IImmutableSet<IUseCaseSpecification> AnalyzeUseCases(UseCaseType useCaseType)
    {
        Logger.Debug($"Analyzing {useCaseType} use cases of aggregate root: {Name}");

        var methods = _typeSymbol.GetMembers()
            .OfType<IMethodSymbol>()
            .Where(m => IsUseCase(m, useCaseType))
            .Select(m =>
            {
                Logger.Debug($"Creating specification for {useCaseType} use case: {m.Name}");
                return new UseCaseSpecification(m, _semanticModel);
            })
            .Cast<IUseCaseSpecification>()
            .ToImmutableHashSet();

        Logger.Info($"Found {methods.Count} {useCaseType} use cases in aggregate root: {Name}");
        return methods;
    }

    /// <summary>
    /// Determines if a method represents a use case of the specified type.
    /// </summary>
    /// <param name="method">The method symbol to analyze</param>
    /// <param name="useCaseType">The type of use case to check for</param>
    /// <returns>True if the method represents the specified type of use case</returns>
    /// <remarks>
    /// Use cases are identified based on method name conventions and behavior:
    /// 
    /// Create methods:
    /// - Must explicitly start with "Create"
    /// - Example: CreateOrder, CreateCustomer
    /// 
    /// Read methods (query/calculate operations):
    /// - Start with: "Get", "Find", "Calculate", "Compute", "Search"
    /// - Don't modify state
    /// - Example: GetStatus, FindByDate, CalculateTotal
    /// 
    /// Delete methods:
    /// - Must explicitly start with "Delete" or "Remove"
    /// - Example: DeleteOrder, RemoveItem
    /// 
    /// Update methods (any state modification):
    /// - Any public method that doesn't fall into above categories
    /// - Examples: UpdateStatus, ChangeAddress, Cancel, Approve, Reject, 
    ///            Submit, Process, Handle, Revoke, Add, Set, Modify
    /// 
    /// Additional criteria for all:
    /// - Must be a regular method (not constructor, operator, etc.)
    /// - Must be public
    /// - Must not be static
    /// - Must not be compiler-generated
    /// </remarks>
    private static bool IsUseCase(IMethodSymbol method, UseCaseType useCaseType)
    {
        // Skip special methods, non-public, static, and compiler-generated
        if (method.MethodKind != MethodKind.Ordinary ||
            method.DeclaredAccessibility != Accessibility.Public ||
            method.IsStatic ||
            method.IsImplicitlyDeclared)
            return false;

        var name = method.Name.ToLowerInvariant();

        // Explicit prefixes for Create and Delete
        var createPrefixes = new[] { "create" };
        var deletePrefixes = new[] { "delete", "remove" };

        // Read operation prefixes (queries and calculations)
        var readPrefixes = new[]
        {
            "get", "find", "calculate", "compute", "search",
            "fetch", "retrieve", "query", "list", "count",
            "check", "validate", "verify", "is", "has", "can"
        };

        return useCaseType switch
        {
            // Create and Delete must match exact prefixes
            UseCaseType.Create => createPrefixes.Any(prefix => name.StartsWith(prefix)),
            UseCaseType.Delete => deletePrefixes.Any(prefix => name.StartsWith(prefix)),

            // Read operations are queries and calculations
            UseCaseType.Read => readPrefixes.Any(prefix => name.StartsWith(prefix)),

            // Update is the default for any other state-modifying method
            UseCaseType.Update =>

                // If it's not Create/Read/Delete, it's probably an Update
                !createPrefixes.Any(prefix => name.StartsWith(prefix)) &&
                !readPrefixes.Any(prefix => name.StartsWith(prefix)) &&
                !deletePrefixes.Any(prefix => name.StartsWith(prefix)),

            _ => false
        };
    }
}

/// <summary>
/// Represents the type of a use case for classification purposes.
/// Used internally to categorize methods into different types of use cases.
/// </summary>
internal enum UseCaseType
{
    /// <summary>
    /// Represents use cases that create new instances of the aggregate root.
    /// </summary>
    Create,

    /// <summary>
    /// Represents use cases that retrieve or query the aggregate root.
    /// </summary>
    Read,

    /// <summary>
    /// Represents use cases that modify the state of the aggregate root.
    /// </summary>
    Update,

    /// <summary>
    /// Represents use cases that delete or remove the aggregate root.
    /// </summary>
    Delete
}

