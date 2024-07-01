using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.Extensions.Logging;
using Scaffolder.Utilities;

namespace Scaffolder.Internal.Descriptors;

/// <summary>
/// Represents an aggregate root descriptor that provides access to properties and use cases.
/// </summary>
public sealed class AggregateRootDescriptor
{
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
    /// Extracts properties from the class declaration.
    /// </summary>
    private static ImmutableArray<PropertyDescriptor> ExtractProperties(ClassDeclarationSyntax classDeclaration, SemanticModel semanticModel, ILogger logger)
    {
        var properties = new List<PropertyDescriptor>();

        foreach (var property in classDeclaration.Members.OfType<PropertyDeclarationSyntax>())
        {
            var propertySymbol = semanticModel.GetDeclaredSymbol(property) as IPropertySymbol;
            if (propertySymbol == null)
            {
                logger.LogWarning("Unable to get symbol for property: {PropertyName}", property.Identifier.Text);
                continue;
            }

            var isComplex = IsComplexType(propertySymbol.Type);
            var nestedProperties = isComplex ? ExtractNestedProperties(propertySymbol.Type, logger) : ImmutableArray<PropertyDescriptor>.Empty;

            var propertyDescriptor = new PropertyDescriptor(
                property.Identifier.Text,
                property.Type.ToString(),
                property.Modifiers.Any(m => m.Text == "required"),
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
    /// Extracts use cases from the class declaration.
    /// </summary>
    private static ImmutableArray<UseCaseDescriptor> ExtractUseCases(ClassDeclarationSyntax classDeclaration, SemanticModel semanticModel, ILogger logger)
    {
        var useCases = classDeclaration.Members
            .OfType<MethodDeclarationSyntax>()
            .Where(m => m.Modifiers.Any(mod => mod.Text == "public"))
            .Select(m =>
            {
                var parameters = ExtractParameters(m.ParameterList.Parameters, semanticModel, logger);

                var useCase = new UseCaseDescriptor(
                    m.Identifier.Text,
                    m.ReturnType.ToString(),
                    parameters
                );

                logger.LogDebug("Extracted use case: {UseCaseName}, ReturnType: {ReturnType}, ParametersCount: {ParametersCount}",
                    useCase.Name, useCase.ReturnType, useCase.Parameters.Length);

                return useCase;
            })
            .ToArray();

        return [..useCases];
    }

    /// <summary>
    /// Extracts parameters from a parameter list.
    /// </summary>
    private static ImmutableArray<ParameterDescriptor> ExtractParameters(SeparatedSyntaxList<ParameterSyntax> parameters, SemanticModel semanticModel, ILogger logger)
    {
        var extractedParameters = new List<ParameterDescriptor>();

        foreach (var parameter in parameters)
        {
            if (semanticModel.GetDeclaredSymbol(parameter) is not IParameterSymbol parameterSymbol)
            {
                logger.LogWarning("Unable to get symbol for parameter: {ParameterName}", parameter.Identifier.Text);
                continue;
            }

            var isComplex = IsComplexType(parameterSymbol.Type);
            var nestedParameters = isComplex ? ExtractNestedParameters(parameterSymbol.Type, logger) : ImmutableArray<ParameterDescriptor>.Empty;

            var parameterDescriptor = new ParameterDescriptor(
                parameter.Identifier.Text,
                parameter.Type?.ToString() ?? "object",
                parameterSymbol.IsOptional,
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