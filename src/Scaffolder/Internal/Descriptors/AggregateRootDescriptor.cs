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

    public CreateUseCaseDescriptor? CreateUseCase { get; }

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
        CreateUseCaseDescriptor? createUseCase,
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
        CreateUseCase = createUseCase;
    }

    /// <summary>
    /// Creates an AggregateRootDescriptor from a ClassDeclarationSyntax.
    /// </summary>
    public static AggregateRootDescriptor FromSyntax(ClassDeclarationSyntax classDeclaration, SemanticModel semanticModel, FileInfo file, DirectoryInfo directory, ILogger logger)
    {
        var name = classDeclaration.Identifier.Text;

        logger.LogInformation("Processing aggregate root: {AggregateRootName}", name);

        var typeSymbol = semanticModel.GetDeclaredSymbol(classDeclaration) as INamedTypeSymbol;
        if (typeSymbol == null)
        {
            throw new InvalidOperationException($"Unable to get type symbol for class: {name}");
        }

        var properties = ExtractProperties(typeSymbol, logger);
        var useCases = ExtractUseCases(classDeclaration, semanticModel, logger);
        var createUseCase = ExtractCreateUseCase(classDeclaration, semanticModel, logger);

        // Extract namespace from the syntax tree
        var namespaceName = classDeclaration.Ancestors().OfType<NamespaceDeclarationSyntax>().FirstOrDefault()?.Name.ToString()
                            ?? classDeclaration.Ancestors().OfType<FileScopedNamespaceDeclarationSyntax>().FirstOrDefault()?.Name.ToString()
                            ?? "DefaultNamespace";

        // Determine the ID type
        var idType = DetermineIdType(properties, logger);

        logger.LogInformation("Aggregate root {AggregateRootName} processed. Namespace: {Namespace}, IdType: {IdType}, Properties: {PropertyCount}, Use Cases: {UseCaseCount}",
            name, namespaceName, idType, properties.Length, useCases.Length);

        return new AggregateRootDescriptor(name, namespaceName, idType, file, directory, properties, useCases, createUseCase, logger);
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
    private static ImmutableArray<PropertyDescriptor> ExtractProperties(INamedTypeSymbol? typeSymbol, ILogger logger)
    {
        var properties = new List<PropertyDescriptor>();
        var seenProperties = new HashSet<string>();

        while (typeSymbol != null && typeSymbol.SpecialType != SpecialType.System_Object)
        {
            foreach (var member in typeSymbol.GetMembers().OfType<IPropertySymbol>())
            {
                // Skip properties we've already seen (in derived classes)
                if (!seenProperties.Add(member.Name))
                    continue;

                // Skip 'Events' and 'Identifier' properties
                if (member.Name == "Events" || member.Name == "Identifier")
                    continue;

                // Include only properties with public getters
                if (member.GetMethod?.DeclaredAccessibility != Accessibility.Public)
                    continue;

                var isComplex = IsComplexType(member.Type);
                var nestedProperties = isComplex ? ExtractNestedProperties(member.Type, logger) : ImmutableArray<PropertyDescriptor>.Empty;

                var propertyDescriptor = new PropertyDescriptor(
                    member.Name,
                    member.Type.ToString() ?? throw new InvalidOperationException($"Unable to determine type for property {member.Name}"),
                    member.IsRequired,
                    isComplex,
                    nestedProperties
                );

                properties.Add(propertyDescriptor);

                logger.LogDebug("Extracted property: {PropertyName}, Type: {PropertyType}, IsRequired: {IsRequired}, IsComplex: {IsComplex}, NestedPropertiesCount: {NestedPropertiesCount}",
                    propertyDescriptor.Name, propertyDescriptor.Type, propertyDescriptor.IsRequired, propertyDescriptor.IsComplex, propertyDescriptor.NestedProperties.Length);
            }

            typeSymbol = typeSymbol.BaseType;
        }

        return [..properties];
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

    private static ImmutableArray<PropertyDescriptor> ExtractNestedProperties(ITypeSymbol typeSymbol, ILogger logger)
    {
        var nestedProperties = new List<PropertyDescriptor>();

        foreach (var member in typeSymbol.GetMembers().OfType<IPropertySymbol>())
        {
            if (member.GetMethod?.DeclaredAccessibility != Accessibility.Public)
                continue;

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

    private static CreateUseCaseDescriptor? ExtractCreateUseCase(ClassDeclarationSyntax classDeclaration, SemanticModel semanticModel, ILogger logger)
    {
        var typeSymbol = semanticModel.GetDeclaredSymbol(classDeclaration) as INamedTypeSymbol;
        if (typeSymbol == null) return null;

        // 1. Check for properties with required and public init
        var initProperties = typeSymbol.GetMembers().OfType<IPropertySymbol>()
            .Where(p => p.IsRequired && p.SetMethod?.DeclaredAccessibility == Accessibility.Public && p.SetMethod.IsInitOnly)
            .ToList();

        if (initProperties.Any())
        {
            logger.LogDebug("Found {Count} required properties with public init for create use case in {ClassName}", initProperties.Count, classDeclaration.Identifier.Text);
            var parameters = initProperties.Select(p =>
                    new ParameterDescriptor(p.Name, p.Type.ToString() ?? throw new InvalidOperationException(), false, IsComplexType(p.Type), ImmutableArray<ParameterDescriptor>.Empty))
                .ToImmutableArray();
            return new CreateUseCaseDescriptor("Create", typeSymbol.Name, parameters, CreateUseCaseDescriptor.CreateMechanism.InitProperties);
        }

        // 2. Check for static Create method
        var staticCreateMethod = typeSymbol.GetMembers("Create").OfType<IMethodSymbol>()
            .FirstOrDefault(m => m.IsStatic && m.DeclaredAccessibility == Accessibility.Public);
        if (staticCreateMethod != null)
        {
            logger.LogDebug("Found static Create method for create use case in {ClassName}", classDeclaration.Identifier.Text);
            return new CreateUseCaseDescriptor("Create", typeSymbol.Name, ExtractParameters(staticCreateMethod.Parameters, logger), CreateUseCaseDescriptor.CreateMechanism.StaticCreateMethod);
        }

        // 3. Check for public constructor with parameters
        var publicConstructorWithParams = typeSymbol.Constructors
            .FirstOrDefault(c => c.DeclaredAccessibility == Accessibility.Public && c.Parameters.Length > 0);
        if (publicConstructorWithParams != null)
        {
            logger.LogDebug("Found public constructor with parameters for create use case in {ClassName}", classDeclaration.Identifier.Text);
            return new CreateUseCaseDescriptor("Create", typeSymbol.Name, ExtractParameters(publicConstructorWithParams.Parameters, logger), CreateUseCaseDescriptor.CreateMechanism.Constructor);
        }

        // 4. Check for empty public constructor or implicit default constructor
        var emptyPublicConstructor = typeSymbol.Constructors
            .FirstOrDefault(c => c.DeclaredAccessibility == Accessibility.Public && c.Parameters.IsEmpty);
        if (emptyPublicConstructor != null || typeSymbol.Constructors.Length == 0)
        {
            logger.LogDebug("Using empty or implicit constructor for create use case in {ClassName}", classDeclaration.Identifier.Text);
            return new CreateUseCaseDescriptor("Create", typeSymbol.Name, ImmutableArray<ParameterDescriptor>.Empty, CreateUseCaseDescriptor.CreateMechanism.EmptyConstructor);
        }

        logger.LogWarning("No suitable create method found for {ClassName}", classDeclaration.Identifier.Text);
        return null;
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