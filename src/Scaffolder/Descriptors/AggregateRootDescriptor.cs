using System.Collections;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.Extensions.Logging;
using Scaffolder.Utilities;

namespace Scaffolder.Descriptors;

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
    /// <param name="name">The name of the aggregate root.</param>
    /// <param name="namespace">The namespace of the aggregate root.</param>
    /// <param name="file">The file information for the aggregate root.</param>
    /// <param name="directory">The directory containing the aggregate root file.</param>
    /// <param name="properties">An immutable array of properties.</param>
    /// <param name="useCases">An immutable array of use cases.</param>
    /// <param name="logger">The logger instance.</param>
    private AggregateRootDescriptor(string name, string @namespace, FileInfo file, DirectoryInfo directory, ImmutableArray<PropertyDescriptor> properties, ImmutableArray<UseCaseDescriptor> useCases, ILogger logger)
    {
        Name = name;
        Namespace = @namespace;
        Properties = properties;
        UseCases = useCases;
        _logger = logger;
        Directory = directory;
        File = file;
    }

    /// <summary>
    /// Creates an AggregateRootDescriptor from a ClassDeclarationSyntax.
    /// </summary>
    /// <param name="classDeclaration">The ClassDeclarationSyntax to process.</param>
    /// <param name="file">The file information for the aggregate root.</param>
    /// <param name="directory">The directory containing the aggregate root file.</param>
    /// <param name="logger">The logger instance.</param>
    /// <returns>A new instance of AggregateRootDescriptor.</returns>
    public static AggregateRootDescriptor FromSyntax(ClassDeclarationSyntax classDeclaration, FileInfo file, DirectoryInfo directory, ILogger logger)
    {
        var name = classDeclaration.Identifier.Text;

        logger.LogInformation("Processing aggregate root: {AggregateRootName}", name);

        var properties = ExtractProperties(classDeclaration);
        var useCases = ExtractUseCases(classDeclaration);

        // Extract namespace from the syntax tree
        var namespaceName = classDeclaration.Ancestors().OfType<NamespaceDeclarationSyntax>().FirstOrDefault()?.Name.ToString()
                            ?? classDeclaration.Ancestors().OfType<FileScopedNamespaceDeclarationSyntax>().FirstOrDefault()?.Name.ToString()
                            ?? "DefaultNamespace";

        logger.LogInformation("Aggregate root {AggregateRootName} processed. Properties: {PropertyCount}, Use Cases: {UseCaseCount}", name, properties.Length, useCases.Length);

        return new AggregateRootDescriptor(name, namespaceName, file, directory, properties, useCases, logger);
    }

    /// <summary>
    /// Extracts properties from the class declaration.
    /// </summary>
    /// <param name="classDeclaration">The ClassDeclarationSyntax to extract properties from.</param>
    /// <returns>An immutable array of Property objects.</returns>
    private static ImmutableArray<PropertyDescriptor> ExtractProperties(ClassDeclarationSyntax classDeclaration) =>
    [
        ..classDeclaration.Members
            .OfType<PropertyDeclarationSyntax>()
            .Select(p => new PropertyDescriptor(
                p.Identifier.Text,
                p.Type.ToString(),
                p.Modifiers.Any(m => m.Text == "required")
            ))
    ];

    /// <summary>
    /// Extracts use cases from the class declaration.
    /// </summary>
    /// <param name="classDeclaration">The ClassDeclarationSyntax to extract use cases from.</param>
    /// <returns>An immutable array of UseCase objects.</returns>
    private static ImmutableArray<UseCaseDescriptor> ExtractUseCases(ClassDeclarationSyntax classDeclaration) =>
    [
        ..classDeclaration.Members
            .OfType<MethodDeclarationSyntax>()
            .Where(m => m.Modifiers.Any(mod => mod.Text == "public"))
            .Select(m => new UseCaseDescriptor(
                m.Identifier.Text,
                m.ReturnType?.ToString() ?? "void",
                [
                    ..m.ParameterList.Parameters.Select(p =>
                        new ParameterDescriptor(p.Identifier.Text, p.Type?.ToString() ?? "object"))
                ]
            ))
    ];
}