using Scaffolder.Abstractions;
using Microsoft.CodeAnalysis;
using System.Collections.Immutable;
using Scaffolder.Specifications.Utilities;

namespace Scaffolder.Specifications;

/// <summary>
/// Implementation of <see cref="IUseCaseSpecification"/> that uses Roslyn API to analyze a domain method.
/// This class extracts and provides information about:
/// - Method parameters and their types
/// - Return type (if method returns a value)
/// - XML documentation for code generation
/// - Method name and signature
/// Used for generating commands, queries, API endpoints, and related infrastructure.
/// </summary>
internal sealed class UseCaseSpecification : IUseCaseSpecification
{
    // Roslyn symbols providing access to method information and type resolution
    private readonly IMethodSymbol _methodSymbol;
    private readonly SemanticModel _semanticModel;

    // Lazy loading of parameters to avoid unnecessary analysis
    // Only computed when Parameters property is accessed
    private readonly Lazy<IImmutableSet<IMemberSpecification>> _parameters;

    /// <inheritdoc />
    /// <remarks>
    /// The name of the use case method exactly as defined in the source code.
    /// This name is used for:
    /// - Generating command/query class names
    /// - Creating API endpoint routes
    /// - Naming handler classes
    /// Example: "UpdateShippingAddress" -> "UpdateShippingAddressCommand"
    /// </remarks>
    public string Name => _methodSymbol.Name;

    /// <inheritdoc />
    /// <remarks>
    /// XML documentation is extracted directly from the source code.
    /// This documentation is used for:
    /// - Command/Query class documentation
    /// - API endpoint documentation
    /// - Swagger/OpenAPI descriptions
    /// - Handler class documentation
    /// </remarks>
    public string XmlDocumentation => _methodSymbol.GetDocumentationCommentXml() ?? string.Empty;

    /// <inheritdoc />
    /// <remarks>
    /// Parameters are analyzed to extract:
    /// - Parameter names and types
    /// - Whether they are required or optional
    /// - Complex type information
    /// - Collection type information
    /// This information is used to generate:
    /// - Command/Query properties
    /// - API request models
    /// - Method parameters in handlers
    /// </remarks>
    public IImmutableSet<IMemberSpecification> Parameters => _parameters.Value;

    /// <inheritdoc />
    /// <remarks>
    /// Return type is analyzed to determine:
    /// - If the method returns a value (non-void)
    /// - The type of the return value (including unwrapping Task<T>)
    /// - Whether it's a complex type
    /// - Collection type information
    /// 
    /// Special handling for:
    /// - Void methods (returns null)
    /// - Async methods (unwraps Task<T>)
    /// - Collection return types
    /// - Complex return types with nested members
    /// 
    /// This information is used to generate:
    /// - Command/Query result types
    /// - API response models
    /// - Return types in handlers
    /// - Mapping logic
    /// </remarks>
    public IMemberSpecification? ReturnType
    {
        get
        {
            // Don't create specification for void methods
            if (_methodSymbol.ReturnsVoid)
            {
                return null;
            }

            // Use the new constructor specifically for method return types
            return new MemberSpecification(_methodSymbol, _semanticModel);
        }
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="UseCaseSpecification"/> class.
    /// </summary>
    /// <param name="methodSymbol">Roslyn symbol representing the use case method</param>
    /// <param name="semanticModel">Semantic model providing type resolution and symbol information</param>
    /// <remarks>
    /// The constructor initializes lazy loading for parameters analysis.
    /// The semantic model is used for resolving types and type relationships.
    /// </remarks>
    public UseCaseSpecification(IMethodSymbol methodSymbol, SemanticModel semanticModel)
    {
        _methodSymbol = methodSymbol ?? throw new ArgumentNullException(nameof(methodSymbol));
        _semanticModel = semanticModel ?? throw new ArgumentNullException(nameof(semanticModel));

        _parameters = new Lazy<IImmutableSet<IMemberSpecification>>(AnalyzeParameters);
    }

    /// <summary>
    /// Analyzes the method parameters to create member specifications.
    /// </summary>
    /// <returns>An immutable set of parameter specifications</returns>
    /// <remarks>
    /// For each parameter, this method:
    /// - Creates a parameter-specific member specification
    /// - Handles nullability and optional parameters
    /// - Processes complex types and their nested members
    /// - Manages collection types and their element types
    /// - Preserves parameter order for correct code generation
    /// 
    /// Special handling for:
    /// - Optional parameters with default values
    /// - Nullable reference types
    /// - Complex parameter types
    /// - Collection parameters
    /// 
    /// The resulting specifications are used to generate:
    /// - Command/Query properties
    /// - API request model properties
    /// - Method parameters in generated code
    /// - Validation rules
    /// - Mapping logic
    /// </remarks>
    private IImmutableSet<IMemberSpecification> AnalyzeParameters()
    {
        Logger.Debug($"Analyzing parameters for use case: {Name}");

        var parameters = _methodSymbol.Parameters
            .Select(param =>
            {
                Logger.Debug($"Creating specification for parameter: {param.Name}");

                // Use the parameter-specific constructor
                return new MemberSpecification(param, _semanticModel);
            })
            .Cast<IMemberSpecification>()
            .ToImmutableHashSet();

        Logger.Debug($"Found {parameters.Count} parameters in use case: {Name}");
        return parameters;
    }

    /// <summary>
    /// Creates a detailed string representation of the use case for debugging purposes.
    /// </summary>
    /// <returns>A string showing the complete method signature including return type</returns>
    public override string ToString()
    {
        var returnTypeStr = ReturnType?.Type.Name ?? "void";
        var parameters = string.Join(", ",
            _methodSymbol.Parameters.Select(p =>
                $"{p.Type.Name} {p.Name}{(p.HasExplicitDefaultValue ? " = default" : "")}"
            ));

        return $"{returnTypeStr} {Name}({parameters})";
    }
}