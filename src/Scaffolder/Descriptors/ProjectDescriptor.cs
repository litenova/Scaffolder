using System.Collections;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.Extensions.Logging;
using Scaffolder.Utilities;

namespace Scaffolder.Descriptors;

/// <summary>
/// Represents a project descriptor that provides access to aggregate root descriptors.
/// </summary>
/// <remarks>
/// This class implements IReadOnlyList<AggregateRootDescriptor> to allow easy enumeration of aggregate roots.
/// </remarks>
public sealed class ProjectDescriptor : IReadOnlyList<AggregateRootDescriptor>
{
    private readonly ILogger _logger;
    private readonly ImmutableArray<AggregateRootDescriptor> _aggregateRoots;

    /// <summary>
    /// Gets the name of the project.
    /// </summary>
    public RichString Name { get; }

    /// <summary>
    /// Gets the file information for the project file (i.e. .csproj).
    /// </summary>
    public FileInfo File { get; }

    /// <summary>
    /// Gets the directory containing the project file.
    /// </summary>
    public DirectoryInfo Directory { get; init; }

    /// <summary>
    /// Gets the namespace of the project.
    /// </summary>
    public string Namespace { get; }

    /// <summary>
    /// Gets the number of aggregate roots in the project.
    /// </summary>
    public int Count => _aggregateRoots.Length;

    /// <summary>
    /// Gets the aggregate root descriptor at the specified index.
    /// </summary>
    /// <param name="index">The zero-based index of the aggregate root descriptor to get.</param>
    /// <returns>The aggregate root descriptor at the specified index.</returns>
    public AggregateRootDescriptor this[int index] => _aggregateRoots[index];

    /// <summary>
    /// Initializes a new instance of the <see cref="ProjectDescriptor"/> class.
    /// </summary>
    /// <param name="name">The name of the project.</param>
    /// <param name="file">The project file information.</param>
    /// <param name="directory">The directory containing the project file.</param>
    /// <param name="namespace">The namespace of the project.</param>
    /// <param name="aggregateRoots">An immutable array of aggregate root descriptors.</param>
    /// <param name="logger">The logger instance.</param>
    private ProjectDescriptor(string name, FileInfo file, DirectoryInfo directory, string @namespace, ImmutableArray<AggregateRootDescriptor> aggregateRoots, ILogger logger)
    {
        Name = name;
        File = file;
        Directory = directory;
        Namespace = @namespace;
        _aggregateRoots = aggregateRoots;
        _logger = logger;
    }

    /// <summary>
    /// Returns an enumerator that iterates through the aggregate root descriptors.
    /// </summary>
    /// <returns>An enumerator for the aggregate root descriptors.</returns>
    public IEnumerator<AggregateRootDescriptor> GetEnumerator() => _aggregateRoots.AsEnumerable().GetEnumerator();

    /// <summary>
    /// Returns an enumerator that iterates through the aggregate root descriptors.
    /// </summary>
    /// <returns>An enumerator for the aggregate root descriptors.</returns>
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    /// <summary>
    /// Asynchronously loads a project descriptor from the specified Microsoft.CodeAnalysis.Project.
    /// </summary>
    /// <param name="project">The Microsoft.CodeAnalysis.Project to load from.</param>
    /// <param name="logger">The logger instance.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the loaded <see cref="ProjectDescriptor"/>.</returns>
    /// <exception cref="InvalidOperationException">Thrown when unable to get compilation, determine project file path or directory.</exception>
    public static async Task<ProjectDescriptor> LoadAsync(Microsoft.CodeAnalysis.Project project, ILogger logger)
    {
        logger.LogInformation("Loading project: {ProjectName}", project.Name);

        var compilation = await project.GetCompilationAsync();
        if (compilation is null)
        {
            logger.LogError("Failed to get compilation for project: {ProjectName}", project.Name);
            throw new InvalidOperationException($"Failed to get compilation for project: {project.Name}");
        }

        var projectFileInfo = new FileInfo(project.FilePath ?? throw new InvalidOperationException("Project file path is null."));
        var projectDirectory = projectFileInfo.Directory ?? throw new InvalidOperationException("Unable to determine project directory.");

        logger.LogInformation("Searching for aggregate roots in project: {ProjectName}", project.Name);
        var aggregateRoots = await Task.WhenAll(
            compilation.SyntaxTrees.Select(async tree =>
            {
                var root = await tree.GetRootAsync();
                var sourceFileInfo = new FileInfo(tree.FilePath);
                var sourceDirectory = sourceFileInfo.Directory ?? projectDirectory;
                return root.DescendantNodes()
                    .OfType<ClassDeclarationSyntax>()
                    .Where(IsAggregateRoot)
                    .Select(cd => AggregateRootDescriptor.FromSyntax(cd, sourceFileInfo, sourceDirectory, logger));
            }));

        var flattenedAggregateRoots = aggregateRoots.SelectMany(x => x).ToImmutableArray();

        logger.LogInformation("Found {AggregateRootCount} aggregate roots in project: {ProjectName}", flattenedAggregateRoots.Length, project.Name);

        // Determine the project namespace (you might want to adjust this logic based on your project structure)
        string projectNamespace = project.DefaultNamespace ?? project.Name;

        return new ProjectDescriptor(project.Name, projectFileInfo, projectDirectory, projectNamespace, flattenedAggregateRoots, logger);
    }

    /// <summary>
    /// Determines if a class declaration represents an aggregate root.
    /// </summary>
    /// <param name="classDeclaration">The class declaration to check.</param>
    /// <returns>True if the class is an aggregate root, false otherwise.</returns>
    private static bool IsAggregateRoot(ClassDeclarationSyntax classDeclaration) =>
        classDeclaration.BaseList?.Types.Any(t => t.ToString() == "AggregateRoot") ?? false;
}