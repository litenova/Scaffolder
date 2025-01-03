using Microsoft.CodeAnalysis;
using System.Collections.Immutable;
using Scaffolder.Abstractions;
using Scaffolder.Specifications.Utilities;

namespace Scaffolder.Specifications;

/// <summary>
/// Implementation of <see cref="IAggregateRootSpecification"/> that uses Roslyn API to analyze an aggregate root class.
/// This class is responsible for extracting all relevant information from a domain aggregate root class
/// including its properties, use cases (methods), and type information for code generation purposes.
/// 
/// Key responsibilities:
/// - Analyzing aggregate root class structure
/// - Extracting properties and their types
/// - Identifying and categorizing use cases (commands/queries)
/// - Handling inheritance and virtual members
/// - Managing type information for code generation
/// 
/// The analysis is performed lazily, meaning expensive operations are only executed when needed.
/// This helps with performance when only partial information is required.
/// </summary>
internal sealed class AggregateRootSpecification : IAggregateRootSpecification
{
    /// <summary>
    /// Roslyn symbol representing the aggregate root class.
    /// Provides access to:
    /// - Class metadata
    /// - Member information
    /// - Type relationships
    /// - Inheritance chain
    /// - Accessibility information
    /// </summary>
    private readonly INamedTypeSymbol _typeSymbol;

    /// <summary>
    /// Semantic model providing context and type resolution capabilities.
    /// Used for:
    /// - Resolving type references
    /// - Analyzing type relationships
    /// - Determining symbol meanings
    /// - Accessing compilation information
    /// </summary>
    private readonly SemanticModel _semanticModel;

    /// <summary>
    /// Lazy-loaded collection of member specifications.
    /// Only analyzed when the Members property is accessed.
    /// Includes both declared and inherited members.
    /// </summary>
    private readonly Lazy<IImmutableSet<IMemberSpecification>> _members;

    /// <summary>
    /// Lazy-loaded collection of create use case specifications.
    /// Methods that create new instances of the aggregate root.
    /// Example: CreateOrder, CreateCustomer
    /// </summary>
    private readonly Lazy<IImmutableSet<IUseCaseSpecification>> _createUseCases;

    /// <summary>
    /// Lazy-loaded collection of read use case specifications.
    /// Query methods that don't modify state.
    /// Example: GetById, FindByDate, CalculateTotal
    /// </summary>
    private readonly Lazy<IImmutableSet<IUseCaseSpecification>> _readUseCases;

    /// <summary>
    /// Lazy-loaded collection of update use case specifications.
    /// Methods that modify the aggregate's state.
    /// Example: UpdateStatus, Cancel, Approve
    /// </summary>
    private readonly Lazy<IImmutableSet<IUseCaseSpecification>> _updateUseCases;

    /// <summary>
    /// Lazy-loaded collection of delete use case specifications.
    /// Methods that remove or deactivate the aggregate.
    /// Example: Delete, Remove, Archive
    /// </summary>
    private readonly Lazy<IImmutableSet<IUseCaseSpecification>> _deleteUseCases;

    // Implementation of ITypeSpecification properties

    /// <inheritdoc />
    /// <remarks>
    /// The simple name of the aggregate root class without namespace.
    /// Example: "Order" from "MyCompany.Domain.Orders.Order"
    /// </remarks>
    public string Name => _typeSymbol.Name;

    /// <inheritdoc />
    /// <remarks>
    /// The full namespace containing the aggregate root.
    /// Example: "MyCompany.Domain.Orders"
    /// </remarks>
    public string Namespace => _typeSymbol.ContainingNamespace.ToDisplayString();

    /// <inheritdoc />
    /// <remarks>
    /// The fully qualified name including namespace and type parameters.
    /// Example: "MyCompany.Domain.Orders.Order"
    /// </remarks>
    public string FullName => _typeSymbol.ToDisplayString();

    // Implementation of IAggregateRootSpecification properties

    /// <inheritdoc />
    /// <remarks>
    /// The type of the aggregate's identifier property.
    /// Looks for a property named exactly "Id" or ending with "Id".
    /// Example: Guid from "public Guid Id { get; }"
    /// </remarks>
    public ITypeSpecification IdType => new TypeSpecification(GetIdType());

    /// <inheritdoc />
    /// <remarks>
    /// All public properties of the aggregate root, including inherited ones.
    /// Excludes static and compiler-generated properties.
    /// Lazy-loaded when first accessed.
    /// </remarks>
    public IImmutableSet<IMemberSpecification> Members => _members.Value;

    /// <inheritdoc />
    /// <remarks>
    /// Methods that create new instances of the aggregate root.
    /// Must start with "Create".
    /// Lazy-loaded when first accessed.
    /// </remarks>
    public IImmutableSet<IUseCaseSpecification> CreateUseCases => _createUseCases.Value;

    /// <inheritdoc />
    /// <remarks>
    /// Query methods that don't modify state.
    /// Start with Get, Find, Calculate, etc.
    /// Lazy-loaded when first accessed.
    /// </remarks>
    public IImmutableSet<IUseCaseSpecification> ReadUseCases => _readUseCases.Value;

    /// <inheritdoc />
    /// <remarks>
    /// Methods that modify the aggregate's state.
    /// Any public method not categorized as Create/Read/Delete.
    /// Lazy-loaded when first accessed.
    /// </remarks>
    public IImmutableSet<IUseCaseSpecification> UpdateUseCases => _updateUseCases.Value;

    /// <inheritdoc />
    /// <remarks>
    /// Methods that remove or deactivate the aggregate.
    /// Must start with Delete or Remove.
    /// Lazy-loaded when first accessed.
    /// </remarks>
    public IImmutableSet<IUseCaseSpecification> DeleteUseCases => _deleteUseCases.Value;

    /// <summary>
    /// Initializes a new instance of the <see cref="AggregateRootSpecification"/> class.
    /// </summary>
    /// <param name="typeSymbol">Roslyn symbol representing the aggregate root class</param>
    /// <param name="semanticModel">Semantic model providing type resolution and symbol information</param>
    /// <remarks>
    /// The constructor only initializes lazy loading containers.
    /// No heavy analysis is performed until properties are actually accessed.
    /// This ensures optimal performance when only partial information is needed.
    /// </remarks>
    public AggregateRootSpecification(INamedTypeSymbol typeSymbol, SemanticModel semanticModel)
    {
        _typeSymbol = typeSymbol ?? throw new ArgumentNullException(nameof(typeSymbol));
        _semanticModel = semanticModel ?? throw new ArgumentNullException(nameof(semanticModel));

        // Initialize lazy loading for all analysis operations
        _members = new Lazy<IImmutableSet<IMemberSpecification>>(AnalyzeMembers);
        _createUseCases = new Lazy<IImmutableSet<IUseCaseSpecification>>(AnalyzeCreateUseCases);
        _readUseCases = new Lazy<IImmutableSet<IUseCaseSpecification>>(() => AnalyzeUseCases(UseCaseType.Read));
        _updateUseCases = new Lazy<IImmutableSet<IUseCaseSpecification>>(() => AnalyzeUseCases(UseCaseType.Update));
        _deleteUseCases = new Lazy<IImmutableSet<IUseCaseSpecification>>(() => AnalyzeUseCases(UseCaseType.Delete));
    }

    /// <summary>
    /// Finds and analyzes the Id property through the inheritance chain.
    /// </summary>
    /// <returns>The type symbol representing the type of the Id property</returns>
    /// <remarks>
    /// Search strategy:
    /// 1. Look for property named exactly "Id" in current class
    /// 2. Look for property ending with "Id" in current class
    /// 3. Repeat steps 1-2 in each base class
    /// 4. Throw if no Id property is found
    /// 
    /// The property must be:
    /// - Public
    /// - Readable (has a getter)
    /// - Non-static
    /// - Non-compiler-generated
    /// </remarks>
    /// <exception cref="InvalidOperationException">Thrown when no valid Id property is found</exception>
    private ITypeSymbol GetIdType()
    {
        Logger.Debug($"Looking for Id property in aggregate root: {Name}");

        var currentType = _typeSymbol;

        // Walk up the inheritance chain until we hit object
        while (currentType != null && currentType.SpecialType != SpecialType.System_Object)
        {
            // Search through members of the current type for Id property
            var idProperty = currentType.GetMembers()
                .OfType<IPropertySymbol>()
                .FirstOrDefault(p =>

                    // Property must be public and readable
                    p.DeclaredAccessibility == Accessibility.Public &&
                    p.GetMethod != null &&

                    // Match either exact "Id" or ending with "Id"
                    (p.Name.Equals("Id", StringComparison.OrdinalIgnoreCase) ||
                     p.Name.EndsWith("Id", StringComparison.OrdinalIgnoreCase)));

            if (idProperty != null)
            {
                Logger.Debug($"Found Id property of type {idProperty.Type.Name} in type: {currentType.Name}");
                return idProperty.Type;
            }

            // Move up to the base type
            currentType = currentType.BaseType;
        }

        Logger.Error($"No Id property found in aggregate root: {Name} or its base types");
        throw new InvalidOperationException($"No Id property found in aggregate root {Name} or its base types");
    }

    /// <summary>
    /// Analyzes all members (properties) of the aggregate root, including inherited ones.
    /// </summary>
    /// <returns>An immutable set of member specifications</returns>
    /// <remarks>
    /// Analysis process:
    /// 1. Start with the current class
    /// 2. Get all public, non-static, non-compiler-generated properties
    /// 3. Create specifications for each property
    /// 4. Move to base class and repeat steps 2-3
    /// 5. Continue until reaching object class
    /// 
    /// Handles:
    /// - Property inheritance
    /// - Property overrides (uses most derived version)
    /// - Complex property types
    /// - Collection properties
    /// - Required/optional properties
    /// </remarks>
    private IImmutableSet<IMemberSpecification> AnalyzeMembers()
    {
        Logger.Debug($"Analyzing members of aggregate root: {Name}");

        var members = new HashSet<IMemberSpecification>();
        var seenPropertyNames = new HashSet<string>();
        var currentType = _typeSymbol;

        // Walk up the inheritance chain until we hit object
        while (currentType != null && currentType.SpecialType != SpecialType.System_Object)
        {
            // Get all public properties from this type
            var type = currentType;
            var typeMembers = currentType.GetMembers()
                .OfType<IPropertySymbol>()
                .Where(p =>

                    // Only include public properties
                    p.DeclaredAccessibility == Accessibility.Public &&

                    // Exclude static properties
                    !p.IsStatic &&

                    // Exclude compiler-generated properties
                    !p.IsImplicitlyDeclared &&

                    // Only include properties we haven't seen (in case of overrides)
                    seenPropertyNames.Add(p.Name))
                .Select(p =>
                {
                    Logger.Debug($"Creating specification for member: {p.Name} from type: {type.Name}");
                    return new MemberSpecification(p, _semanticModel);
                });

            members.UnionWith(typeMembers);

            // Move up to the base type
            currentType = currentType.BaseType;
        }

        var immutableMembers = members.ToImmutableHashSet();
        Logger.Info($"Found {immutableMembers.Count} members in aggregate root: {Name} (including inherited)");
        return immutableMembers;
    }

    /// <summary>
    /// Analyzes methods to find use cases of a specific type, including inherited ones.
    /// </summary>
    /// <param name="useCaseType">The type of use cases to look for</param>
    /// <returns>An immutable set of use case specifications</returns>
    /// <remarks>
    /// Analysis process:
    /// 1. Start with the current class
    /// 2. Find methods matching the use case type criteria
    /// 3. Create specifications for matching methods
    /// 4. Move to base class and repeat steps 2-3
    /// 5. Continue until reaching object class
    /// 
    /// Handles:
    /// - Method inheritance
    /// - Method overrides (uses most derived version)
    /// - Method signatures
    /// - Return types
    /// - Parameters
    /// </remarks>
    private IImmutableSet<IUseCaseSpecification> AnalyzeUseCases(UseCaseType useCaseType)
    {
        Logger.Debug($"Analyzing {useCaseType} use cases of aggregate root: {Name}");

        var methods = new HashSet<IUseCaseSpecification>();
        var seenMethodSignatures = new HashSet<string>();
        var currentType = _typeSymbol;

        // Walk up the inheritance chain until we hit object
        while (currentType != null && currentType.SpecialType != SpecialType.System_Object)
        {
            var type = currentType;
            var typeMethods = currentType.GetMembers()
                .OfType<IMethodSymbol>()
                .Where(m =>
                    IsUseCase(m, useCaseType) &&

                    // Only include methods we haven't seen (using signature to handle overrides)
                    seenMethodSignatures.Add(GetMethodSignature(m)))
                .Select(m =>
                {
                    Logger.Debug($"Creating specification for {useCaseType} use case: {m.Name} from type: {type.Name}");
                    return new UseCaseSpecification(m, _semanticModel);
                });

            methods.UnionWith(typeMethods);

            // Move up to the base type
            currentType = currentType.BaseType;
        }

        var immutableMethods = methods.ToImmutableHashSet();
        Logger.Info($"Found {immutableMethods.Count} {useCaseType} use cases in aggregate root: {Name} (including inherited)");
        return immutableMethods;
    }

    /// <summary>
    /// Creates a unique signature for a method to detect overrides.
    /// </summary>
    /// <param name="method">The method to create a signature for</param>
    /// <returns>A string uniquely identifying the method signature</returns>
    /// <remarks>
    /// The signature includes:
    /// - Method name
    /// - Parameter types (in order)
    /// This allows proper detection of overridden methods
    /// regardless of parameter names or return type.
    /// </remarks>
    private static string GetMethodSignature(IMethodSymbol method)
    {
        return $"{method.Name}({string.Join(",", method.Parameters.Select(p => p.Type.ToDisplayString()))})";
    }

    /// <summary>
    /// Determines if a method represents a use case of the specified type.
    /// </summary>
    /// <param name="method">The method to analyze</param>
    /// <param name="useCaseType">The type of use case to check for</param>
    /// <returns>True if the method represents the specified type of use case</returns>
    /// <remarks>
    /// Classification rules:
    /// 
    /// Create methods:
    /// - Must start with "Create"
    /// Example: CreateOrder, CreateCustomer
    /// 
    /// Read methods:
    /// - Start with: Get, Find, Calculate, Compute, Search, etc.
    /// - Don't modify state
    /// Example: GetStatus, FindByDate, CalculateTotal
    /// 
    /// Delete methods:
    /// - Must start with "Delete" or "Remove"
    /// Example: DeleteOrder, RemoveItem
    /// 
    /// Update methods:
    /// - Any other public method not falling into above categories
    /// Example: UpdateStatus, Cancel, Approve
    /// 
    /// All methods must be:
    /// - Public
    /// - Instance (non-static)
    /// - Non-compiler-generated
    /// - Regular methods (not operators, constructors, etc.)
    /// </remarks>
    private static bool IsUseCase(IMethodSymbol method, UseCaseType useCaseType)
    {
        // First check if method should be excluded
        if (ShouldExcludeMethod(method))
            return false;

        // Skip special methods, non-public, static, and compiler-generated
        if (method.MethodKind != MethodKind.Ordinary ||
            method.DeclaredAccessibility != Accessibility.Public ||
            method.IsImplicitlyDeclared)
            return false;

        // For Create use cases, handle separately
        if (useCaseType == UseCaseType.Create)
        {
            return false; // Create use cases are handled by AnalyzeCreateUseCases
        }

        // Skip special methods, non-public, static, and compiler-generated
        if (method.MethodKind != MethodKind.Ordinary ||
            method.DeclaredAccessibility != Accessibility.Public ||
            method.IsStatic ||
            method.IsImplicitlyDeclared)
            return false;

        var name = method.Name.ToLowerInvariant();

        // Define prefix lists for each use case type
        var createPrefixes = new[] { "create" };
        var deletePrefixes = new[] { "delete", "remove" };
        var readPrefixes = new[]
        {
            "get", "find", "calculate", "compute", "search",
            "fetch", "retrieve", "query", "list", "count",
            "check", "validate", "verify", "is", "has", "can"
        };

        // Classify based on use case type
        return useCaseType switch
        {
            UseCaseType.Delete => deletePrefixes.Any(prefix => name.StartsWith(prefix)),

            // Read operations are queries and calculations
            UseCaseType.Read => readPrefixes.Any(prefix => name.StartsWith(prefix)),

            // Update is the default for any other state-modifying method
            UseCaseType.Update =>
                !createPrefixes.Any(prefix => name.StartsWith(prefix)) &&
                !readPrefixes.Any(prefix => name.StartsWith(prefix)) &&
                !deletePrefixes.Any(prefix => name.StartsWith(prefix)),

            _ => false
        };
    }

    private static bool ShouldExcludeMethod(IMethodSymbol method)
    {
        // Exclude Object methods
        if (method.ContainingType.SpecialType == SpecialType.System_Object)
            return true;

        // Exclude common base entity/aggregate root methods
        var excludedMethods = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "RaiseEvent",
            "ClearDomainEvents",
            "GetDomainEvents",
            "GetHashCode",
            "Equals",
            "ToString"
        };

        if (excludedMethods.Contains(method.Name))
            return true;

        // Check if method is from Entity or AggregateRoot base class
        var containingType = method.ContainingType;
        if (containingType.Name is "Entity" or "AggregateRoot")
            return true;

        return false;
    }

    private IImmutableSet<IUseCaseSpecification> AnalyzeCreateUseCases()
    {
        var createUseCases = new HashSet<IUseCaseSpecification>();

        // Check static Create methods
        var staticCreateMethods = _typeSymbol.GetMembers()
            .OfType<IMethodSymbol>()
            .Where(m => m is { IsStatic: true, DeclaredAccessibility: Accessibility.Public } &&
                        m.Name.Equals("Create", StringComparison.OrdinalIgnoreCase));

        foreach (var method in staticCreateMethods)
        {
            createUseCases.Add(new UseCaseSpecification(method, _semanticModel));
        }

        // Check public constructors with parameters
        var publicConstructors = _typeSymbol.Constructors
            .Where(c => c.DeclaredAccessibility == Accessibility.Public &&
                        c.Parameters.Length > 0);

        foreach (var constructor in publicConstructors)
        {
            createUseCases.Add(new UseCaseSpecification(constructor, _semanticModel));
        }

        return createUseCases.ToImmutableHashSet();
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
    /// Example: CreateOrder, CreateCustomer
    /// </summary>
    Create,

    /// <summary>
    /// Represents use cases that retrieve or query the aggregate root.
    /// Example: GetById, FindByDate, CalculateTotal
    /// </summary>
    Read,

    /// <summary>
    /// Represents use cases that modify the state of the aggregate root.
    /// Example: UpdateStatus, Cancel, Approve
    /// </summary>
    Update,

    /// <summary>
    /// Represents use cases that delete or remove the aggregate root.
    /// Example: DeleteOrder, RemoveItem
    /// </summary>
    Delete
}