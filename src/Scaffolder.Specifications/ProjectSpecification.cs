using Microsoft.CodeAnalysis;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Scaffolder.Abstractions;
using Scaffolder.Specifications.Utilities;

namespace Scaffolder.Specifications;

/// <summary>
/// Implementation of <see cref="IProjectSpecification"/> that uses Roslyn API to analyze a project.
/// Provides access to project-level information and its aggregate roots for code generation purposes.
/// </summary>
internal sealed class ProjectSpecification : IProjectSpecification
{
    // Roslyn's Project object containing compilation and metadata information
    private readonly Project _project;

    // Lazy loading of aggregate roots to avoid unnecessary compilation until needed
    private readonly Lazy<IImmutableSet<IAggregateRootSpecification>> _aggregateRoots;

    /// <inheritdoc />
    public string Name => _project.Name;

    /// <inheritdoc />

    // Fallback to project name if default namespace is not set
    public string Namespace => _project.DefaultNamespace ?? Name;

    /// <inheritdoc />

    // Full name includes both namespace and assembly name for complete type resolution
    public string FullName => $"{Namespace}, {AssemblyName}";

    /// <inheritdoc />
    public string FullPath => _project.FilePath ?? throw new InvalidOperationException("Project file path is null");

    /// <inheritdoc />

    // Root namespace is used as base for all types in the project
    public string RootNamespace => _project.DefaultNamespace ?? Name;

    /// <inheritdoc />

    // Assembly name is used for reference generation and type resolution
    public string AssemblyName => _project.AssemblyName;

    /// <inheritdoc />
    public IImmutableSet<IAggregateRootSpecification> AggregateRoots => _aggregateRoots.Value;

    /// <inheritdoc />
    public ProjectLayer Layer => DetermineProjectLayer();

    /// <summary>
    /// Initializes a new instance of the <see cref="ProjectSpecification"/> class.
    /// </summary>
    /// <param name="project">Roslyn Project object containing project metadata and compilation information</param>
    public ProjectSpecification(Project project)
    {
        _project = project ?? throw new ArgumentNullException(nameof(project));

        // Initialize lazy loading of aggregate roots
        _aggregateRoots = new Lazy<IImmutableSet<IAggregateRootSpecification>>(AnalyzeAggregateRoots);
    }

    /// <summary>
    /// Analyzes the project to find and create specifications for all aggregate roots.
    /// Only performed when AggregateRoots property is first accessed.
    /// </summary>
    private IImmutableSet<IAggregateRootSpecification> AnalyzeAggregateRoots()
    {
        Logger.Debug($"Analyzing aggregate roots in project: {Name}");

        // Performance optimization: only domain projects can contain aggregate roots
        if (Layer != ProjectLayer.Domain)
        {
            Logger.Debug($"Skipping aggregate root analysis for non-domain project: {Name}");
            return ImmutableHashSet<IAggregateRootSpecification>.Empty;
        }

        // Get the compilation which contains the semantic model
        // This is a heavy operation, hence the lazy loading
        var compilation = _project.GetCompilationAsync().Result;
        if (compilation is null)
        {
            Logger.Warning($"Failed to get compilation for project: {Name}");
            return ImmutableHashSet<IAggregateRootSpecification>.Empty;
        }

        var aggregateRoots = new HashSet<IAggregateRootSpecification>();

        // Analyze each syntax tree (source file) in the project
        foreach (var tree in compilation.SyntaxTrees)
        {
            // Get semantic model for this syntax tree
            // Semantic model provides type information and symbol resolution
            var semanticModel = compilation.GetSemanticModel(tree);
            var root = tree.GetRoot();

            // Find all class declarations that might be aggregate roots
            var aggregateRootClasses = root
                .DescendantNodes()
                .OfType<ClassDeclarationSyntax>()
                .Where(IsAggregateRoot);

            // Create specifications for each aggregate root found
            foreach (var aggregateRootClass in aggregateRootClasses)
            {
                // Get the symbol representing the class
                // Symbol provides access to detailed type information
                var symbol = semanticModel.GetDeclaredSymbol(aggregateRootClass);
                if (symbol is INamedTypeSymbol namedTypeSymbol)
                {
                    Logger.Debug($"Found aggregate root: {symbol.Name}");
                    aggregateRoots.Add(new AggregateRootSpecification(namedTypeSymbol, semanticModel));
                }
            }
        }

        Logger.Info($"Found {aggregateRoots.Count} aggregate roots in project: {Name}");
        return aggregateRoots.ToImmutableHashSet();
    }

    /// <summary>
    /// Determines if a class declaration represents an aggregate root.
    /// Checks if the class inherits from AggregateRoot or implements IAggregateRoot.
    /// </summary>
    private static bool IsAggregateRoot(ClassDeclarationSyntax classDeclaration)
    {
        // Check base types list for AggregateRoot
        // This could be enhanced to check for specific base types or interfaces
        return classDeclaration.BaseList?.Types
            .Any(t => t.Type.ToString().Contains("AggregateRoot")) ?? false;
    }

    /// <summary>
    /// Determines the architectural ProjectLayer of the project based on its name.
    /// This could be enhanced to also check project references or other characteristics.
    /// </summary>
    private ProjectLayer DetermineProjectLayer()
    {
        // Simple name-based ProjectLayer detection
        // Could be enhanced to check project references or other characteristics
        var name = Name.ToLowerInvariant();

        if (name.Contains(".domain"))
            return ProjectLayer.Domain;

        if (name.Contains(".application"))
            return ProjectLayer.Application;

        if (name.Contains(".webapi") || name.Contains(".api"))
            return ProjectLayer.WebApi;

        if (name.Contains(".infrastructure"))
            return ProjectLayer.Infrastructure;

        return ProjectLayer.Other;
    }
}