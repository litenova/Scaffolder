using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.Extensions.Logging;
using Scaffolder.Utilities;
// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace Scaffolder.Internal.Descriptors;

/// <summary>
/// Represents an aggregate root descriptor that provides access to properties and use cases.
/// </summary>
public sealed class AggregateRootDescriptor
{
    // ReSharper disable once NotAccessedField.Local
    private readonly ILogger _logger;

    /// <summary>
    /// The name of the aggregate root.
    /// </summary>
    public RichString Name { get; }

    /// <summary>
    /// The namespace of the aggregate root.
    /// </summary>
    public string Namespace { get; }

    /// <summary>
    /// The type of the ID property for the aggregate root.
    /// </summary>
    public string IdType { get; }

    /// <summary>
    /// The use cases associated with the aggregate root.
    /// </summary>
    public ImmutableArray<UseCaseDescriptor> UseCases { get; }

    /// <summary>
    /// The properties of the aggregate root.
    /// </summary>
    public ImmutableArray<PropertyDescriptor> Properties { get; }

    /// <summary>
    /// The directory containing the aggregate root file.
    /// </summary>
    public DirectoryInfo Directory { get; }

    /// <summary>
    /// The file information for the aggregate root.
    /// </summary>
    public FileInfo File { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="AggregateRootDescriptor"/> class.
    /// </summary>
    private AggregateRootDescriptor(
        string name,
        string @namespace,
        string idType,
        FileInfo file,
        DirectoryInfo directory,
        ImmutableArray<PropertyDescriptor> properties,
        ImmutableArray<UseCaseDescriptor> useCases,
        ILogger logger)
    {
        Name = name;
        Namespace = @namespace;
        IdType = idType;
        Properties = properties;
        UseCases = useCases;
        _logger = logger;
        Directory = directory;
        File = file;
    }

    /// <summary>
    /// Creates an AggregateRootDescriptor from a ClassDeclarationSyntax.
    /// </summary>
    public static AggregateRootDescriptor FromSyntax(ClassDeclarationSyntax classDeclaration, SemanticModel semanticModel, FileInfo file, DirectoryInfo directory, ILogger logger)
    {
        var name = classDeclaration.Identifier.Text;

        logger.LogInformation("Processing aggregate root: {AggregateRootName}", name);

        var properties = ExtractProperties(classDeclaration, semanticModel, logger);
        var useCases = ExtractUseCases(classDeclaration, semanticModel, logger);

        // Extract namespace from the syntax tree
        var namespaceName = classDeclaration.Ancestors().OfType<NamespaceDeclarationSyntax>().FirstOrDefault()?.Name.ToString()
                            ?? classDeclaration.Ancestors().OfType<FileScopedNamespaceDeclarationSyntax>().FirstOrDefault()?.Name.ToString()
                            ?? "DefaultNamespace";

        // Determine the ID type
        var idType = DetermineIdType(properties, logger);

        logger.LogInformation("Aggregate root {AggregateRootName} processed. Namespace: {Namespace}, IdType: {IdType}, Properties: {PropertyCount}, Use Cases: {UseCaseCount}",
            name, namespaceName, idType, properties.Length, useCases.Length);

        return new AggregateRootDescriptor(name, namespaceName, idType, file, directory, properties, useCases, logger);
    }

    /// <summary>
    /// Determines the ID type based on the properties.
    /// </summary>
    private static string DetermineIdType(ImmutableArray<PropertyDescriptor> properties, ILogger logger)
    {
        var idProperty = properties.FirstOrDefault(descriptor => descriptor.Name.Original.Equals("Id", StringComparison.OrdinalIgnoreCase));

        if (idProperty == null)
        {
            logger.LogWarning("No 'Id' property found. Using 'Guid' as default IdType.");
            return "Guid";
        }

        return idProperty.Type;
    }

    /// <summary>
    /// Extracts properties from the class declaration, including inherited properties with public getters.
    /// </summary>
    private static ImmutableArray<PropertyDescriptor> ExtractProperties(ClassDeclarationSyntax classDeclaration, SemanticModel semanticModel, ILogger logger)
    {
        var properties = new List<PropertyDescriptor>();
        var typeSymbol = semanticModel.GetDeclaredSymbol(classDeclaration) as ITypeSymbol;

        if (typeSymbol == null)
        {
            logger.LogWarning("Unable to get type symbol for class: {ClassName}", classDeclaration.Identifier.Text);
            return ImmutableArray<PropertyDescriptor>.Empty;
        }

        foreach (var member in typeSymbol.GetMembers().OfType<IPropertySymbol>())
        {
            // Include only properties with public getters
            if (member.GetMethod?.DeclaredAccessibility != Accessibility.Public)
                continue;

            var isComplex = IsComplexType(member.Type);
            var nestedProperties = isComplex ? ExtractNestedProperties(member.Type, logger) : ImmutableArray<PropertyDescriptor>.Empty;

            var propertyDescriptor = new PropertyDescriptor(
                member.Name,
                member.Type.ToString() ?? throw new InvalidOperationException(),
                member.IsRequired,
                isComplex,
                nestedProperties
            );

            properties.Add(propertyDescriptor);

            logger.LogDebug("Extracted property: {PropertyName}, Type: {PropertyType}, IsRequired: {IsRequired}, IsComplex: {IsComplex}, NestedPropertiesCount: {NestedPropertiesCount}",
                propertyDescriptor.Name, propertyDescriptor.Type, propertyDescriptor.IsRequired, propertyDescriptor.IsComplex, propertyDescriptor.NestedProperties.Length);
        }

        return [..properties];
    }

    /// <summary>
    /// Determines if a type is a complex user-defined type.
    /// </summary>
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

    /// <summary>
    /// Extracts nested properties from a complex type.
    /// </summary>
    private static ImmutableArray<PropertyDescriptor> ExtractNestedProperties(ITypeSymbol typeSymbol, ILogger logger)
    {
        var nestedProperties = new List<PropertyDescriptor>();

        foreach (var member in typeSymbol.GetMembers().OfType<IPropertySymbol>())
        {
            var isComplex = IsComplexType(member.Type);
            var subNestedProperties = isComplex ? ExtractNestedProperties(member.Type, logger) : ImmutableArray<PropertyDescriptor>.Empty;

            var propertyDescriptor = new PropertyDescriptor(
                member.Name,
                member.Type.ToString() ?? throw new InvalidOperationException($"Unable to determine type for property {member.Name}"),
                member.IsRequired,
                isComplex,
                subNestedProperties
            );

            nestedProperties.Add(propertyDescriptor);

            logger.LogDebug("Extracted nested property: {PropertyName}, Type: {PropertyType}, IsRequired: {IsRequired}, IsComplex: {IsComplex}, SubNestedPropertiesCount: {SubNestedPropertiesCount}",
                propertyDescriptor.Name, propertyDescriptor.Type, propertyDescriptor.IsRequired, propertyDescriptor.IsComplex, propertyDescriptor.NestedProperties.Length);
        }

        return [..nestedProperties];
    }

    /// <summary>
    /// Extracts use cases from the class declaration, including inherited public methods.
    /// </summary>
    private static ImmutableArray<UseCaseDescriptor> ExtractUseCases(ClassDeclarationSyntax classDeclaration, SemanticModel semanticModel, ILogger logger)
    {
        if (semanticModel.GetDeclaredSymbol(classDeclaration) is not INamedTypeSymbol typeSymbol)
        {
            logger.LogWarning("Unable to get type symbol for class: {ClassName}", classDeclaration.Identifier.Text);
            return ImmutableArray<UseCaseDescriptor>.Empty;
        }

        var useCases = new List<UseCaseDescriptor>();

        foreach (var member in typeSymbol.GetMembers().OfType<IMethodSymbol>())
        {
            if (!IsUserDefinedPublicMethod(member))
                continue;

            var parameters = ExtractParameters(member.Parameters, logger);

            var useCase = new UseCaseDescriptor(
                member.Name,
                member.ReturnType.ToString() ?? throw new InvalidOperationException(),
                parameters
            );

            useCases.Add(useCase);

            logger.LogDebug("Extracted use case: {UseCaseName}, ReturnType: {ReturnType}, ParametersCount: {ParametersCount}",
                useCase.Name, useCase.ReturnType, useCase.Parameters.Length);
        }

        return [..useCases];
    }

    /// <summary>
    /// Determines if a method is a user-defined public method.
    /// </summary>
    private static bool IsUserDefinedPublicMethod(IMethodSymbol methodSymbol)
    {
        // Must be public
        if (methodSymbol.DeclaredAccessibility != Accessibility.Public)
            return false;

        // Exclude methods from Object class
        if (methodSymbol.ContainingType.SpecialType == SpecialType.System_Object)
            return false;

        // Exclude compiler-generated methods
        if (methodSymbol.IsImplicitlyDeclared)
            return false;

        // Exclude property accessors
        if (methodSymbol.AssociatedSymbol is IPropertySymbol)
            return false;

        // Exclude constructors
        if (methodSymbol.MethodKind == MethodKind.Constructor)
            return false;

        return true;
    }

    /// <summary>
    /// Extracts parameters from a parameter list.
    /// </summary>
    private static ImmutableArray<ParameterDescriptor> ExtractParameters(ImmutableArray<IParameterSymbol> parameters, ILogger logger)
    {
        var extractedParameters = new List<ParameterDescriptor>();

        foreach (var parameter in parameters)
        {
            var isComplex = IsComplexType(parameter.Type);
            var nestedParameters = isComplex ? ExtractNestedParameters(parameter.Type, logger) : ImmutableArray<ParameterDescriptor>.Empty;

            var parameterDescriptor = new ParameterDescriptor(
                parameter.Name,
                parameter.Type.ToString() ?? throw new InvalidOperationException(),
                parameter.IsOptional,
                isComplex,
                nestedParameters
            );

            extractedParameters.Add(parameterDescriptor);

            logger.LogDebug("Extracted parameter: {ParameterName}, Type: {ParameterType}, IsRequired: {IsRequired}, IsComplex: {IsComplex}, NestedParametersCount: {NestedParametersCount}",
                parameterDescriptor.Name, parameterDescriptor.Type, !parameterDescriptor.IsRequired, parameterDescriptor.IsComplex, parameterDescriptor.NestedParameters.Length);
        }

        return [..extractedParameters];
    }

    /// <summary>
    /// Extracts nested parameters from a complex type.
    /// </summary>
    private static ImmutableArray<ParameterDescriptor> ExtractNestedParameters(ITypeSymbol typeSymbol, ILogger logger)
    {
        var nestedParameters = new List<ParameterDescriptor>();

        foreach (var member in typeSymbol.GetMembers().OfType<IPropertySymbol>())
        {
            var isComplex = IsComplexType(member.Type);
            var subNestedParameters = isComplex ? ExtractNestedParameters(member.Type, logger) : ImmutableArray<ParameterDescriptor>.Empty;

            var parameterDescriptor = new ParameterDescriptor(
                member.Name,
                member.Type.ToString() ?? throw new InvalidOperationException($"Unable to determine type for parameter {member.Name}"),
                !member.IsRequired,
                isComplex,
                subNestedParameters
            );

            nestedParameters.Add(parameterDescriptor);

            logger.LogDebug(
                "Extracted nested parameter: {ParameterName}, Type: {ParameterType}, IsRequired: {IsRequired}, IsComplex: {IsComplex}, SubNestedParametersCount: {SubNestedParametersCount}",
                parameterDescriptor.Name, parameterDescriptor.Type, !parameterDescriptor.IsRequired, parameterDescriptor.IsComplex, parameterDescriptor.NestedParameters.Length);
        }

        return [..nestedParameters];
    }
}