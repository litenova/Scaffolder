using System.Collections;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.MSBuild;
using Microsoft.Extensions.Logging;

namespace Scaffolder.Descriptors;

public sealed class SolutionDescriptor : IReadOnlyList<ProjectDescriptor>
{
    private readonly ILogger _logger;

    /// <summary>
    /// The solution file information.
    /// </summary>
    public FileInfo File { get; }

    /// <summary>
    /// The directory containing the solution file.
    /// </summary>
    public DirectoryInfo Directory { get; }

    private readonly ImmutableArray<ProjectDescriptor> _projects;

    public int Count => _projects.Length;

    public ProjectDescriptor this[int index] => _projects[index];

    private SolutionDescriptor(FileInfo file, DirectoryInfo directoryInfo, ImmutableArray<ProjectDescriptor> projects, ILogger logger)
    {
        File = file;
        Directory = directoryInfo;
        _projects = projects;
        _logger = logger;
    }

    public IEnumerator<ProjectDescriptor> GetEnumerator() => _projects.AsEnumerable().GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public static async Task<SolutionDescriptor> LoadAsync(string solutionPath, ILogger logger)
    {
        logger.LogInformation("Loading solution: {SolutionPath}", solutionPath);

        using var workspace = MSBuildWorkspace.Create();
        var msBuildSolution = await workspace.OpenSolutionAsync(solutionPath);

        if (msBuildSolution.FilePath is null)
        {
            logger.LogError("Unable to determine solution file path");
            throw new InvalidOperationException("Unable to determine solution file path.");
        }

        var solutionFileInfo = new FileInfo(solutionPath);
        var solutionDirectory = solutionFileInfo.Directory ?? throw new InvalidOperationException("Unable to determine solution directory.");

        logger.LogInformation("Solution loaded successfully. Loading projects...");
        var projects = await Task.WhenAll(msBuildSolution.Projects.Select(p => ProjectDescriptor.LoadAsync(p, logger)));
        logger.LogInformation("All projects loaded. Total projects: {ProjectCount}", projects.Length);

        return new SolutionDescriptor(solutionFileInfo, solutionDirectory, [..projects], logger);
    }
}