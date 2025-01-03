using Scaffolder.Abstractions;
using Scaffolder.Specifications.Utilities;

namespace Scaffolder.Specifications;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MSBuild;
using System.Collections.Immutable;

/// <summary>
/// Implementation of <see cref="ISolutionSpecification"/> that uses Roslyn API to analyze a solution.
/// This class provides access to solution-level information and its projects for code generation purposes.
/// </summary>
public sealed class SolutionSpecification : ISolutionSpecification
{
    // Roslyn's Solution object containing the semantic model of the solution
    private readonly Solution _solution;

    // Lazy loading of projects to avoid unnecessary analysis until needed
    private readonly Lazy<IImmutableSet<IProjectSpecification>> _projects;

    /// <inheritdoc />
    public string Name => Path.GetFileNameWithoutExtension(_solution.FilePath) ?? throw new InvalidOperationException("Solution file path is null");

    /// <inheritdoc />
    public string FullPath => _solution.FilePath ?? throw new InvalidOperationException("Solution file path is null");

    /// <inheritdoc />
    public IImmutableSet<IProjectSpecification> Projects => _projects.Value;

    /// <summary>
    /// Initializes a new instance of the <see cref="SolutionSpecification"/> class.
    /// Private constructor to enforce usage of factory method.
    /// </summary>
    private SolutionSpecification(Solution solution)
    {
        _solution = solution;
        _projects = new Lazy<IImmutableSet<IProjectSpecification>>(AnalyzeProjects);
    }

    /// <summary>
    /// Creates a new instance of SolutionSpecification by loading and analyzing a solution file.
    /// Uses Roslyn's MSBuild workspace to load the solution and its projects.
    /// </summary>
    /// <param name="solutionPath">Full path to the .sln file</param>
    /// <param name="cancellationToken">Cancellation token for async operations</param>
    /// <returns>A Result containing either the solution specification or an error message</returns>
    public static async Task<Result<ISolutionSpecification>> CreateAsync(
        string solutionPath,
        CancellationToken cancellationToken = default)
    {
        Logger.Info($"Loading solution: {solutionPath}");

        // Ensure solution file exists
        if (!File.Exists(solutionPath))
        {
            Logger.Error($"Solution file not found: {solutionPath}");
            return Result.Failure<ISolutionSpecification>("Solution file not found");
        }

        // Load solution using Roslyn
        var solutionResult = await LoadSolutionWithRoslyn(solutionPath, cancellationToken);

        if (solutionResult.IsFailure)
        {
            return Result.Failure<ISolutionSpecification>(solutionResult.Error);
        }

        return Result.Success<ISolutionSpecification>(new SolutionSpecification(solutionResult.Value));
    }

    /// <summary>
    /// Loads a solution using Roslyn's MSBuild workspace.
    /// Handles the complexity of MSBuild initialization and solution loading.
    /// </summary>
    private static async Task<Result<Solution>> LoadSolutionWithRoslyn(
        string solutionPath,
        CancellationToken cancellationToken)
    {
        try
        {
            // Register MSBuild instance to ensure Roslyn can find the build tools
            MSBuildInitializer.Initialize();

            // Create MSBuild workspace to load the solution
            var workspace = MSBuildWorkspace.Create();

            // Subscribe to workspace failures to log them
            workspace.WorkspaceFailed += (_, args) => { Logger.Warning($"Workspace warning: {args.Diagnostic.Message}"); };

            // Actually load the solution
            var solution = await workspace.OpenSolutionAsync(solutionPath, cancellationToken: cancellationToken);

            return Result.Success(solution);
        }
        catch (Exception ex)
        {
            Logger.Error($"Failed to load solution: {ex.Message}");
            return Result.Failure<Solution>($"Failed to load solution: {ex.Message}");
        }
    }

    /// <summary>
    /// Analyzes all projects in the solution and creates their specifications.
    /// Called lazily when Projects property is first accessed.
    /// </summary>
    private IImmutableSet<IProjectSpecification> AnalyzeProjects()
    {
        Logger.Info($"Analyzing projects in solution: {Name}");

        var projectSpecs = _solution.Projects
            .Select(project =>
            {
                Logger.Debug($"Analyzing project: {project.Name}");
                return new ProjectSpecification(project);
            })
            .Cast<IProjectSpecification>()
            .ToImmutableHashSet();

        Logger.Info($"Found {projectSpecs.Count} projects in solution");

        return projectSpecs;
    }
}