using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.Extensions.Logging;
using Scaffolder.Utilities;

// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace Scaffolder.Internal.Descriptors;

/// <summary>
/// Represents a descriptor for an aggregate root, containing information about its properties, use cases, and create use case.
/// </summary>
public sealed class AggregateRootDescriptor
{
    /// <summary>
    /// Gets the name of the aggregate root.
    /// </summary>
    public RichString Name { get; }

    /// <summary>
    /// Gets the namespace of the aggregate root.
    /// </summary>
    public string Namespace { get; }

    /// <summary>
    /// Gets the type of the ID property for the aggregate root.
    /// </summary>
    public string IdType { get; }

    /// <summary>
    /// Gets the collection of use cases associated with the aggregate root.
    /// </summary>
    public ImmutableArray<UseCaseDescriptor> UseCases { get; }

    /// <summary>
    /// Gets the create use case descriptor, if it exists.
    /// </summary>
    public CreateUseCaseDescriptor? CreateUseCase { get; }

    /// <summary>
    /// Gets the collection of properties of the aggregate root.
    /// </summary>
    public ImmutableArray<MemberDescriptor> Properties { get; }

    /// <summary>
    /// Gets the directory containing the aggregate root file.
    /// </summary>
    public DirectoryInfo Directory { get; }

    /// <summary>
    /// Gets the file information for the aggregate root.
    /// </summary>
    public FileInfo File { get; }

    private AggregateRootDescriptor(
        string name,
        string @namespace,
        string idType,
        FileInfo file,
        DirectoryInfo directory,
        ImmutableArray<MemberDescriptor> properties,
        ImmutableArray<UseCaseDescriptor> useCases,
        CreateUseCaseDescriptor? createUseCase)
    {
        Name = name;
        Namespace = @namespace;
        IdType = idType;
        Properties = properties;
        UseCases = useCases;
        Directory = directory;
        File = file;
        CreateUseCase = createUseCase;
    }

    /// <summary>
    /// Creates an AggregateRootDescriptor from a ClassDeclarationSyntax.
    /// </summary>
    /// <param name="classDeclaration">The class declaration syntax.</param>
    /// <param name="semanticModel">The semantic model.</param>
    /// <param name="file">The file information.</param>
    /// <param name="directory">The directory information.</param>
    /// <param name="logger">The logger.</param>
    /// <returns>An AggregateRootDescriptor instance.</returns>
    public static AggregateRootDescriptor FromSyntax(
        ClassDeclarationSyntax classDeclaration,
        SemanticModel semanticModel,
        FileInfo file,
        DirectoryInfo directory,
        ILogger logger)
    {
        var name = classDeclaration.Identifier.Text;
        logger.LogInformation("Processing aggregate root: {AggregateRootName}", name);

        var typeSymbol = semanticModel.GetDeclaredSymbol(classDeclaration) as INamedTypeSymbol;
        if (typeSymbol == null)
        {
            throw new InvalidOperationException($"Unable to get type symbol for class: {name}");
        }

        var properties = ExtractProperties(typeSymbol, logger);
        var useCases = ExtractUseCases(typeSymbol, logger);
        var createUseCase = ExtractCreateUseCase(typeSymbol, logger);

        var namespaceName = classDeclaration.Ancestors().OfType<NamespaceDeclarationSyntax>().FirstOrDefault()?.Name.ToString()
                            ?? classDeclaration.Ancestors().OfType<FileScopedNamespaceDeclarationSyntax>().FirstOrDefault()?.Name.ToString()
                            ?? "DefaultNamespace";

        var idType = DetermineIdType(properties, logger);

        logger.LogInformation("Aggregate root {AggregateRootName} processed. Namespace: {Namespace}, IdType: {IdType}, Properties: {PropertyCount}, Use Cases: {UseCaseCount}",
            name, namespaceName, idType, properties.Length, useCases.Length);

        return new AggregateRootDescriptor(name, namespaceName, idType, file, directory, properties, useCases, createUseCase);
    }

    private static string DetermineIdType(ImmutableArray<MemberDescriptor> properties, ILogger logger)
    {
        var idProperty = properties.FirstOrDefault(descriptor => descriptor.Name.Original.Equals("Id", StringComparison.OrdinalIgnoreCase));

        if (idProperty == null)
        {
            logger.LogWarning("No 'Id' property found. Using 'Guid' as default IdType.");
            return "Guid";
        }

        return idProperty.Type;
    }

    private static ImmutableArray<MemberDescriptor> ExtractProperties(INamedTypeSymbol? typeSymbol, ILogger logger)
    {
        var properties = new List<MemberDescriptor>();
        var seenProperties = new HashSet<string>();

        while (typeSymbol != null && typeSymbol.SpecialType != SpecialType.System_Object)
        {
            foreach (var member in typeSymbol.GetMembers().OfType<IPropertySymbol>())
            {
                if (!seenProperties.Add(member.Name) || member.Name == "Events" || member.Name == "Identifier" ||
                    member.GetMethod?.DeclaredAccessibility != Accessibility.Public)
                    continue;

                properties.Add(MemberDescriptor.CreateFromProperty(member, logger));
            }

            typeSymbol = typeSymbol.BaseType;
        }

        return [.. properties];
    }

    private static ImmutableArray<UseCaseDescriptor> ExtractUseCases(INamedTypeSymbol typeSymbol, ILogger logger)
    {
        return
        [
            .. typeSymbol.GetMembers()
                .OfType<IMethodSymbol>()
                .Where(IsUserDefinedPublicMethod)
                .Select(method => UseCaseDescriptor.Create(method, logger))
        ];
    }

    private static bool IsUserDefinedPublicMethod(IMethodSymbol methodSymbol)
    {
        return methodSymbol.DeclaredAccessibility == Accessibility.Public &&
               methodSymbol.ContainingType.SpecialType != SpecialType.System_Object &&
               methodSymbol is { IsImplicitlyDeclared: false, AssociatedSymbol: not IPropertySymbol } &&
               methodSymbol.MethodKind != MethodKind.Constructor;
    }

    private static CreateUseCaseDescriptor? ExtractCreateUseCase(INamedTypeSymbol typeSymbol, ILogger logger)
    {
        // Check for properties with required and public init
        var initProperties = typeSymbol.GetMembers()
            .OfType<IPropertySymbol>()
            .Where(p => p is { IsRequired: true, SetMethod: { DeclaredAccessibility: Accessibility.Public, IsInitOnly: true } })
            .ToList();

        if (initProperties.Count != 0)
        {
            logger.LogDebug("Found {Count} required properties with public init for create use case in {ClassName}", initProperties.Count, typeSymbol.Name);
            var parameters = MemberDescriptor.CreateManyFromProperties(initProperties, logger);
            return CreateUseCaseDescriptor.Create("Create", typeSymbol.Name, parameters, CreateUseCaseDescriptor.CreateMechanism.InitProperties, logger);
        }

        // Check for static Create method
        var staticCreateMethod = typeSymbol.GetMembers("Create")
            .OfType<IMethodSymbol>()
            .FirstOrDefault(m => m is { IsStatic: true, DeclaredAccessibility: Accessibility.Public });
        if (staticCreateMethod != null)
        {
            logger.LogDebug("Found static Create method for create use case in {ClassName}", typeSymbol.Name);
            return CreateUseCaseDescriptor.Create("Create", typeSymbol.Name, MemberDescriptor.CreateManyFromParameters(staticCreateMethod.Parameters, logger),
                CreateUseCaseDescriptor.CreateMechanism.StaticCreateMethod, logger);
        }

        // Check for public constructor with parameters
        var publicConstructorWithParams = typeSymbol.Constructors
            .FirstOrDefault(c => c is { DeclaredAccessibility: Accessibility.Public, Parameters.Length: > 0 });

        if (publicConstructorWithParams != null)
        {
            logger.LogDebug("Found public constructor with parameters for create use case in {ClassName}", typeSymbol.Name);
            return CreateUseCaseDescriptor.Create("Create", typeSymbol.Name, MemberDescriptor.CreateManyFromParameters(publicConstructorWithParams.Parameters, logger),
                CreateUseCaseDescriptor.CreateMechanism.Constructor, logger);
        }

        // Check for empty public constructor or implicit default constructor
        var emptyPublicConstructor = typeSymbol.Constructors
            .FirstOrDefault(c => c is { DeclaredAccessibility: Accessibility.Public, Parameters.IsEmpty: true });
        if (emptyPublicConstructor != null || typeSymbol.Constructors.Length == 0)
        {
            logger.LogDebug("Using empty or implicit constructor for create use case in {ClassName}", typeSymbol.Name);
            return CreateUseCaseDescriptor.Create("Create", typeSymbol.Name, [], CreateUseCaseDescriptor.CreateMechanism.EmptyConstructor, logger);
        }

        logger.LogWarning("No suitable create method found for {ClassName}", typeSymbol.Name);
        return null;
    }
}