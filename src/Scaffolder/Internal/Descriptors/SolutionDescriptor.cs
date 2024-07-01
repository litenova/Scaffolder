using System.Collections.Immutable;
using Microsoft.CodeAnalysis.MSBuild;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Scaffolder.Internal.Descriptors;

public sealed class SolutionDescriptor
{
    /// <summary>
    /// The solution file information.
    /// </summary>
    public FileInfo File { get; }

    /// <summary>
    /// The directory containing the solution file.
    /// </summary>
    public DirectoryInfo Directory { get; }

    /// <summary>
    /// The projects in the solution.
    /// </summary>
    public ImmutableArray<ProjectDescriptor> Projects { get; }

    // ReSharper disable once NotAccessedField.Local
    private readonly ILogger _logger;

    private SolutionDescriptor(FileInfo file, DirectoryInfo directoryInfo, ImmutableArray<ProjectDescriptor> projects, ILogger logger)
    {
        File = file;
        Directory = directoryInfo;
        Projects = projects;
        _logger = logger;
    }

    public static async Task<SolutionDescriptor> LoadAsync(string solutionPath, ILogger? logger = null, CancellationToken cancellationToken = default)
    {
        logger ??= NullLogger.Instance;

        logger.LogInformation("Processing solution path: {SolutionPath}", solutionPath);

        var fullSolutionPath = FullSolutionPath(solutionPath, logger);

        logger.LogInformation("Loading solution: {FullSolutionPath}", fullSolutionPath);

        using var workspace = MSBuildWorkspace.Create();
        var msBuildSolution = await workspace.OpenSolutionAsync(fullSolutionPath, cancellationToken: cancellationToken);

        if (msBuildSolution == null)
        {
            logger.LogError("Failed to load solution: {FullSolutionPath}", fullSolutionPath);
            throw new InvalidOperationException($"Failed to load solution: {fullSolutionPath}");
        }

        if (msBuildSolution.FilePath is null)
        {
            logger.LogError("Unable to determine solution file path");
            throw new InvalidOperationException("Unable to determine solution file path.");
        }

        var solutionFileInfo = new FileInfo(fullSolutionPath);
        var solutionDirectory = solutionFileInfo.Directory ?? throw new InvalidOperationException("Unable to determine solution directory.");

        logger.LogInformation("Loading projects...");
        var projects = await Task.WhenAll(msBuildSolution.Projects.Select(async p => await ProjectDescriptor.LoadAsync(p, logger, cancellationToken)));
        logger.LogInformation("All projects loaded. Total projects: {ProjectCount}", projects.Length);

        return new SolutionDescriptor(solutionFileInfo, solutionDirectory, [..projects], logger);
    }

    private static string FullSolutionPath(string solutionPath, ILogger logger)
    {
        string fullSolutionPath;

        if (Path.HasExtension(solutionPath) && Path.GetExtension(solutionPath).Equals(".sln", StringComparison.OrdinalIgnoreCase))
        {
            // Full file name provided
            if (!System.IO.File.Exists(solutionPath))
            {
                logger.LogError("Specified .sln file does not exist: {SolutionPath}", solutionPath);
                throw new FileNotFoundException("Specified .sln file does not exist.", solutionPath);
            }

            fullSolutionPath = solutionPath;
        }
        else
        {
            // Search for .sln files in the given path
            var solutionFiles = System.IO.Directory.GetFiles(solutionPath, "*.sln");

            if (solutionFiles.Length == 0)
            {
                logger.LogError("No .sln files found in the specified path: {SolutionPath}", solutionPath);
                throw new FileNotFoundException("No .sln files found in the specified path.", solutionPath);
            }

            if (solutionFiles.Length > 1)
            {
                logger.LogError("Multiple .sln files found in the specified path: {SolutionPath}", solutionPath);
                throw new InvalidOperationException($"Multiple .sln files found. Please specify which one to use. Found files: {string.Join(", ", solutionFiles)}");
            }

            fullSolutionPath = solutionFiles[0];
        }

        return fullSolutionPath;
    }
}