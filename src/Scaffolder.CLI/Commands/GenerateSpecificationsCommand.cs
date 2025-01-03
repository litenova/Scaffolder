using System.Text.Json;
using CliFx;
using CliFx.Attributes;
using CliFx.Infrastructure;
using Scaffolder.Specifications;
using Scaffolder.Specifications.Serialization;
using Scaffolder.Specifications.Serialization.Abstractions;

namespace Scaffolder.Cli.Commands;

/// <summary>
/// Command to generate specifications.json file from solution analysis.
/// This file serves as the source of truth for subsequent code generation commands.
/// 
/// The specifications file contains:
/// - Solution structure and metadata
/// - Project information and their layers
/// - Aggregate root definitions
/// - Use cases (commands and queries)
/// - Domain types and their members
/// - Relationships between different components
/// </summary>
[Command("generate specifications", Description = "Generates specifications.json from solution analysis")]
public sealed class GenerateSpecificationsCommand : ICommand
{
    /// <summary>
    /// Path to the solution file (.sln) or directory containing it.
    /// If a directory is provided, it will search for a single .sln file.
    /// </summary>
    /// <remarks>
    /// This is a required parameter and must be provided as the first argument.
    /// Examples:
    /// - "C:\Projects\MySolution\MySolution.sln"
    /// - "C:\Projects\MySolution"
    /// </remarks>
    [CommandParameter(0, Description = "Path to the solution file or directory")]
    public required string SolutionPath { get; init; }

    /// <summary>
    /// Executes the command to generate specifications.json.
    /// </summary>
    /// <param name="console">Console for output and error messages</param>
    /// <returns>A task representing the asynchronous operation</returns>
    public async ValueTask ExecuteAsync(IConsole console)
    {
        try
        {
            // Analyze the solution using Roslyn
            var solutionResult = await SolutionSpecification.CreateAsync(SolutionPath);
            if (solutionResult.IsFailure)
            {
                await console.Error.WriteLineAsync($"Failed to analyze solution: {solutionResult.Error}");
                return;
            }

            // Create serializer and options
            var serializer = new SpecificationSerializer();

            // Serialize specifications
            var result = await serializer.SerializeAsync(solutionResult.Value);

            // Output success message with details
            await console.Output.WriteLineAsync();
            await console.Output.WriteLineAsync($"Specifications generated successfully:");
            await console.Output.WriteLineAsync($"  Directory: {result.OutputDirectory}");
            await console.Output.WriteLineAsync($"  Files generated:");
            await console.Output.WriteLineAsync($"    {result.SolutionFile.Name} ({FormatFileSize(result.SolutionFile.Size)})");

            foreach (var file in result.AggregateRootFiles.OrderBy(f => f.Name))
            {
                await console.Output.WriteLineAsync($"    {file.Name} ({FormatFileSize(file.Size)})");
            }

            await console.Output.WriteLineAsync();
            await console.Output.WriteLineAsync($"Total size: {FormatFileSize(result.TotalSize)}");
            await console.Output.WriteLineAsync($"Total files: {result.AggregateRootFiles.Count + 1}");
        }
        catch (IOException ex)
        {
            await console.Error.WriteLineAsync($"File system error: {ex.Message}");
            await console.Error.WriteLineAsync("Use --force to overwrite existing files.");
            throw;
        }
        catch (Exception ex)
        {
            await console.Error.WriteLineAsync($"Error generating specifications: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Formats a file size in bytes to a human-readable string.
    /// </summary>
    private static string FormatFileSize(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB" };
        double len = bytes;
        int order = 0;

        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len /= 1024;
        }

        return $"{len:0.##} {sizes[order]}";
    }
}