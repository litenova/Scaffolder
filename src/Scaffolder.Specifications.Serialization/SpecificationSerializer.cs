using System.Text.Json;
using System.Text.Json.Serialization;
using Scaffolder.Abstractions;
using Scaffolder.Specifications.Serialization.Abstractions;

namespace Scaffolder.Specifications.Serialization;

/// <summary>
/// Default implementation of <see cref="ISpecificationSerializer"/> using System.Text.Json.
/// Serializes solution specifications into a specifications folder alongside the solution file.
/// </summary>
public sealed class SpecificationSerializer : ISpecificationSerializer
{
    // Constants for file naming and paths
    private const string SolutionFileName = "solution.json";
    private const string SpecificationsFolder = "specifications";

    // JSON serializer options - configured once and reused for performance
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters =
        {
            new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)
        }
    };

    public async Task<SerializationResult> SerializeAsync(ISolutionSpecification solution, SpecificationsSerializationOptions? options = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(solution);

        // If no options provided, create default options using solution path
        options ??= CreateDefaultOptions(solution);

        // Configure JSON formatting based on options
        JsonOptions.WriteIndented = options.Indent;

        try
        {
            // Ensure output directory exists
            Directory.CreateDirectory(options.OutputDirectory);

            // Check for existing files if overwrite is disabled
            if (!options.Overwrite)
            {
                EnsureNoExistingFiles(solution, options.OutputDirectory, cancellationToken);
            }

            // Serialize solution and aggregate roots
            var solutionFile = await SerializeSolutionFile(
                solution,
                options.OutputDirectory,
                cancellationToken);

            var aggregateRootFiles = await SerializeAggregateRoots(
                solution,
                options.OutputDirectory,
                cancellationToken);

            return new SerializationResult(
                options.OutputDirectory,
                solutionFile,
                aggregateRootFiles);
        }
        catch (Exception ex) when (ex is not IOException && ex is not UnauthorizedAccessException)
        {
            // Wrap unexpected exceptions while preserving IO-related ones
            throw new InvalidOperationException("Failed to serialize specifications.", ex);
        }
    }

    /// <summary>
    /// Creates default serialization options based on the solution path.
    /// </summary>
    /// <param name="solution">The solution specification containing the path information.</param>
    /// <returns>Default serialization options.</returns>
    private static SpecificationsSerializationOptions CreateDefaultOptions(ISolutionSpecification solution)
    {
        // Get solution directory from the solution path
        var solutionDirectory = Path.GetDirectoryName(solution.FullPath)
                                ?? throw new InvalidOperationException("Unable to determine solution directory.");

        // Create specifications directory path alongside the solution
        var defaultDirectory = Path.Combine(solutionDirectory, SpecificationsFolder);

        return new SpecificationsSerializationOptions
        {
            OutputDirectory = defaultDirectory,
            Indent = true,
            Overwrite = true
        };
    }

    /// <summary>
    /// Checks for existing specification files when overwrite is disabled.
    /// </summary>
    /// <exception cref="IOException">Thrown when any specification file already exists.</exception>
    private static void EnsureNoExistingFiles(
        ISolutionSpecification solution,
        string outputDirectory,
        CancellationToken cancellationToken)
    {
        // Check solution.json
        var solutionPath = Path.Combine(outputDirectory, SolutionFileName);
        if (File.Exists(solutionPath))
        {
            throw new IOException($"File already exists: {solutionPath}");
        }

        // Check aggregate root files
        foreach (var project in solution.Projects)
        {
            foreach (var aggregateRoot in project.AggregateRoots)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var fileName = GetAggregateRootFileName(aggregateRoot);
                var filePath = Path.Combine(outputDirectory, fileName);

                if (File.Exists(filePath))
                {
                    throw new IOException($"File already exists: {filePath}");
                }
            }
        }
    }

    /// <summary>
    /// Serializes the solution metadata to solution.json.
    /// </summary>
    /// <returns>Information about the serialized solution file.</returns>
    private static async Task<SerializedFile> SerializeSolutionFile(
        ISolutionSpecification solution,
        string outputDirectory,
        CancellationToken cancellationToken)
    {
        // Create solution DTO with references to aggregate root files
        var solutionDto = new
        {
            solution.Name,
            solution.FullPath,
            Projects = solution.Projects.Select(p => new
            {
                p.Name,
                p.Namespace,
                p.FullPath,
                p.RootNamespace,
                p.AssemblyName,
                p.Layer,

                // Store only the file names of aggregate roots
                AggregateRoots = p.AggregateRoots
                    .Select(GetAggregateRootFileName)
                    .ToList()
            }).ToList()
        };

        var filePath = Path.Combine(outputDirectory, SolutionFileName);

        // Use FileStream for better performance with large files
        await using var fileStream = new FileStream(
            filePath,
            FileMode.Create,
            FileAccess.Write,
            FileShare.None,
            bufferSize: 4096, // Optimal buffer size for most file systems
            useAsync: true);  // Enable async IO

        await JsonSerializer.SerializeAsync(
            fileStream,
            solutionDto,
            JsonOptions,
            cancellationToken);

        var fileInfo = new FileInfo(filePath);
        return new SerializedFile(
            fileInfo.FullName,
            fileInfo.Name,
            fileInfo.Length);
    }

    /// <summary>
    /// Serializes each aggregate root to its own JSON file.
    /// </summary>
    /// <returns>List of information about each serialized aggregate root file.</returns>
    private static async Task<List<SerializedFile>> SerializeAggregateRoots(
        ISolutionSpecification solution,
        string outputDirectory,
        CancellationToken cancellationToken)
    {
        var results = new List<SerializedFile>();

        foreach (var project in solution.Projects)
        {
            foreach (var aggregateRoot in project.AggregateRoots)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var fileName = GetAggregateRootFileName(aggregateRoot);
                var filePath = Path.Combine(outputDirectory, fileName);

                await using var fileStream = new FileStream(
                    filePath,
                    FileMode.Create,
                    FileAccess.Write,
                    FileShare.None,
                    bufferSize: 4096,
                    useAsync: true);

                await JsonSerializer.SerializeAsync(
                    fileStream,
                    aggregateRoot,
                    JsonOptions,
                    cancellationToken);

                var fileInfo = new FileInfo(filePath);
                results.Add(new SerializedFile(
                    fileInfo.FullName,
                    fileInfo.Name,
                    fileInfo.Length));
            }
        }

        return results;
    }

    /// <summary>
    /// Generates a consistent file name for an aggregate root specification.
    /// </summary>
    /// <param name="aggregateRoot">The aggregate root specification.</param>
    /// <returns>The file name (e.g., "order.json", "customer.json").</returns>
    private static string GetAggregateRootFileName(IAggregateRootSpecification aggregateRoot)
    {
        // Convert aggregate root name to lowercase for consistent file naming
        // Example: "Order" -> "order.json"
        return $"{aggregateRoot.Name.ToLowerInvariant()}.json";
    }
}